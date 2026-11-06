using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// What a bee is doing. Idle = hovering over the owner, Leaving = the swarm is being dismissed,
    /// Travel = flying to an enemy it was sent at, Latched = sitting on that enemy and stinging it. No bee can be hurt in any state. The state is packed into the low 3 bits of ai[0], so there is room for 8.
    /// </summary>
    public enum BeeState { Idle = 0, Leaving = 1, Travel = 2, Latched = 3 }

    /// <summary>
    /// One bee of the Bee Keeper's swarm (the base of the big and the small bee). It hovers around the owner's head on a wander pattern of its
    /// own. Nothing can hurt or kill it (no hit points, no clouds). It deals no damage itself: it is a normal projectile only so tModLoader syncs it.
    /// MULTIPLAYER: the owner's client decides the state. Synced through ai (netUpdate only on a change):
    /// ai[0] = the state (BeeState, low 3 bits) + 8 x (the target NPC's index + 1) (one number carries both), ai[1] = unused (it was the hurt
    /// counter), ai[2] = the bee's seed (its wander pattern, and the spot it takes on an enemy).
    /// Every machine runs the same wander from the seed and the owner's position, so the swarm is not sent every tick; the owner's position
    /// updates (netUpdate) pull the others back into line.
    /// THE ATTACK: the owner's client sends it (Recruit) at an enemy; every machine then flies it there (Travel) and, once the owner says it
    /// has arrived, holds it at a spot of the enemy computed from the seed and the enemy's own position (Latched), so no position is sent
    /// while latched. The owner's client does the stings (NPC.SimpleStrikeNPC and NPC.AddBuff both sync themselves); every machine plays the
    /// same sting timing as a visual from its own clock. When the enemy dies or goes away, or MaxLatchTicks pass, the owner lets the bee go.
    /// </summary>
    public abstract class BeeKeeperBeeProjectile : ModProjectile
    {
        protected abstract bool IsBig { get; }
        protected abstract int VanillaProjectile { get; }   // its texture until your own art exists
        protected abstract string ArtPath { get; }

        // The vanilla bee texture stands in until the art file exists.
        public override string Texture => "Terraria/Images/Projectile_" + VanillaProjectile;

        Asset<Texture2D> art;

        BeeState State => (BeeState)((int)Projectile.ai[0] & 7);
        int TargetIndex => ((int)Projectile.ai[0] >> 3) - 1; // -1 = none
        float Seed => Projectile.ai[2];
        public bool IsIdle => State == BeeState.Idle;
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        int leaveTicks;
        bool facingRight = true;

        // The attack. targetType and stingDamage are the owner's alone; the rest runs on every machine (visual timing only).
        int targetType;      // owner only: the type of the target when the bee was sent (a reused NPC slot is not the same enemy)
        float stingDamage;   // owner only: the sword's damage when the bee was sent
        int travelTicks;
        int latchTicks;
        float jab;           // 1 right after a sting, fades to 0
        BeeState lastState;  // the state this machine saw on the previous tick (to notice a change)

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>(ArtPath, out art);
        }

        public override void Unload()
        {
            art = null;
        }

        public override void SetDefaults()
        {
            int size = IsBig ? BeeKeeperTuning.BigSize : BeeKeeperTuning.SmallSize;
            Projectile.width = size;
            Projectile.height = size;
            Projectile.aiStyle = -1;
            Projectile.friendly = false; // it never damages by touching: the owner's client stings by hand (SimpleStrikeNPC)
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 36000;  // ended by the AI, never by this countdown
            Projectile.netImportant = true;
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }

            if (ticks == 0)
            {
                lastState = State;
                BeeKeeperFx.BeeSpawn(Projectile.Center);
            }

            if (IsOwner)
            {
                if (owner.dead)
                {
                    Projectile.Kill();
                    return;
                }
                if (State == BeeState.Idle && owner.GetModPlayer<BeeKeeperPlayer>().Linger <= 0)
                {
                    Projectile.ai[0] = (float)BeeState.Leaving;
                    Projectile.netUpdate = true;
                }
            }

            // Every machine notices a change of the synced state (the owner's own change too, one tick later).
            BeeState now = State;
            if (now != lastState)
            {
                if (now == BeeState.Travel)
                {
                    travelTicks = 0;
                    BeeKeeperFx.Launch(Projectile.Center);
                }
                else if (now == BeeState.Latched)
                {
                    latchTicks = 0;
                    jab = 0f;
                }
                if (lastState == BeeState.Latched && now != BeeState.Latched)
                    BeeKeeperFx.FinalSting(Projectile.Center, IsBig);
                lastState = now;
            }

            if (State == BeeState.Leaving)
            {
                leaveTicks++;
                if (IsOwner && leaveTicks >= BeeKeeperTuning.LeaveTicks)
                {
                    Projectile.Kill();
                    return;
                }
            }

            if (State == BeeState.Travel)
                Travel();
            else if (State == BeeState.Latched)
                Latch();
            else
                Steer(owner);
            if (Math.Abs(Projectile.velocity.X) > 0.3f)
                facingRight = Projectile.velocity.X > 0f;

            Lighting.AddLight(Projectile.Center, 0.25f, 0.18f, 0.04f);
            ticks++;
        }

        static float Frac(float value)
        {
            return value - (float)Math.Floor(value);
        }

        // Where the bee wants to be: a slow wander (two sines with its own speeds and phases, from its seed) above the owner's head. Big
        // bees wander a little less and a little slower.
        Vector2 HoverTarget(Player owner)
        {
            float speedScale = IsBig ? 0.7f : 1f;
            float radiusX = IsBig ? BeeKeeperTuning.BigRadiusX : BeeKeeperTuning.SmallRadiusX;
            float t = ticks * BeeKeeperTuning.FlutterSpeed * speedScale;
            float x = (float)Math.Sin(t * (1f + 0.37f * Frac(Seed * 0.61f)) + Seed * 1.7f) * radiusX;
            float y = (float)Math.Sin(t * (1.4f + 0.5f * Frac(Seed * 0.83f)) + Seed * 2.9f) * BeeKeeperTuning.HoverRadiusY;
            return owner.Top + new Vector2(x, -BeeKeeperTuning.HoverHeight + y);
        }

        void Steer(Player owner)
        {
            Vector2 target = State == BeeState.Leaving ? Projectile.Center + new Vector2(0f, -60f) : HoverTarget(owner);

            if (State == BeeState.Idle && Vector2.DistanceSquared(Projectile.Center, target) > BeeKeeperTuning.SnapDistance * BeeKeeperTuning.SnapDistance)
            {
                Projectile.Center = target; // left far behind (a teleport, a mount): rejoin the swarm
                Projectile.velocity = Vector2.Zero;
                return;
            }

            Vector2 velocity = Projectile.velocity;
            velocity += (target - Projectile.Center) * BeeKeeperTuning.Spring;
            velocity *= BeeKeeperTuning.Damping;
            velocity += Main.rand.NextVector2Circular(BeeKeeperTuning.BuzzJitter, BeeKeeperTuning.BuzzJitter);
            float max = BeeKeeperTuning.MaxSpeed * (IsBig ? 0.8f : 1f);
            float speed = velocity.Length();
            if (speed > max)
                velocity *= max / speed;
            Projectile.velocity = velocity;
        }

        // ---- the attack ----

        void SetState(BeeState state, int target)
        {
            Projectile.ai[0] = (float)((int)state + ((target + 1) << 3));
            Projectile.netUpdate = true;
        }

        // Owner's client only (BeeKeeperPlayer picks the idle bees): fly to this enemy and latch on. 'swordDamage' is what the swing did.
        public void Recruit(NPC target, int swordDamage)
        {
            targetType = target.type;
            stingDamage = swordDamage;
            travelTicks = 0;
            Vector2 away = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(BeeKeeperTuning.RecruitKickSpread);
            Projectile.velocity += away * BeeKeeperTuning.RecruitKick;
            SetState(BeeState.Travel, target.whoAmI);
        }

        // Owner only: back to the swarm, thrown off the enemy with a little push.
        void Release()
        {
            Projectile.velocity = Main.rand.NextVector2Circular(5f, 5f);
            SetState(BeeState.Idle, -1);
        }

        // The enemy this bee was sent at, or null when it is gone (dead, despawned, or the slot now holds another enemy).
        NPC TargetNpc()
        {
            int index = TargetIndex;
            if (index < 0 || index >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[index];
            if (!npc.active || npc.life <= 0)
                return null;
            if (IsOwner && npc.type != targetType)
                return null;
            return npc;
        }

        // The bee's spot on its enemy: the enemy's centre plus a fixed offset from the bee's seed (scaled to the enemy's size, capped). Every
        // machine computes it from its own copy of the enemy, so nothing about the position is sent.
        Vector2 LatchPoint(NPC npc)
        {
            float spreadX = Math.Min(npc.width * BeeKeeperTuning.LatchSpread, BeeKeeperTuning.LatchMaxSpread);
            float spreadY = Math.Min(npc.height * BeeKeeperTuning.LatchSpread, BeeKeeperTuning.LatchMaxSpread);
            float x = (Frac(Seed * 0.7549f) * 2f - 1f) * spreadX;
            float y = (Frac(Seed * 0.5698f) * 2f - 1f) * spreadY;
            return npc.Center + new Vector2(x, y);
        }

        // Every machine flies the bee to its spot; the owner decides when it has arrived.
        void Travel()
        {
            NPC npc = TargetNpc();
            if (npc == null)
            {
                if (IsOwner)
                    Release(); // the target died or went away on the way: back to the swarm
                return;        // the other machines wait for the owner's state change
            }

            Vector2 delta = LatchPoint(npc) - Projectile.Center;
            float distance = delta.Length();
            Vector2 wanted = distance > 0.01f ? delta / distance * Math.Min(BeeKeeperTuning.TravelSpeed, 3f + distance * 0.25f) : Vector2.Zero;
            // A little sideways swoop while far away, so the bees curve in instead of flying a straight line.
            float weave = (float)Math.Sin(ticks * 0.35f + Seed * 1.9f) * BeeKeeperTuning.TravelWeave * Math.Min(1f, distance / 120f);
            Vector2 side = new Vector2(-wanted.Y, wanted.X).SafeNormalize(Vector2.Zero) * weave;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted + side, BeeKeeperTuning.TravelTurn);
            if (ticks % 2 == 0)
                BeeKeeperFx.TravelTrail(Projectile.Center, Projectile.velocity);

            travelTicks++;
            if (IsOwner && (distance <= BeeKeeperTuning.TravelArriveDistance || travelTicks >= BeeKeeperTuning.TravelTimeoutTicks))
                SetState(BeeState.Latched, TargetIndex);
        }

        // Every machine holds the bee at its spot and plays the sting timing; the owner's client does the real stings and decides the end.
        void Latch()
        {
            NPC npc = TargetNpc();
            if (npc == null)
            {
                Projectile.velocity = Vector2.Zero;
                if (IsOwner)
                    Release(); // the enemy died or went away: the swarm ends (the final sting is only shown, there is nothing left to hurt)
                return;
            }

            latchTicks++;
            jab = Math.Max(0f, jab - 1f / Math.Max(1, BeeKeeperTuning.StingJabTicks));
            int interval = Math.Max(1, BeeKeeperTuning.StingIntervalTicks);
            int phase = (int)(Frac(Seed * 0.713f) * interval); // every bee stings at its own moment of the interval
            if ((latchTicks + phase) % interval == 0)
            {
                jab = 1f;
                BeeKeeperFx.Sting(Projectile.Center, IsBig);
                if (IsOwner)
                    Sting(npc, false);
            }

            Vector2 point = LatchPoint(npc);
            float t = ticks * BeeKeeperTuning.LatchWobbleSpeed;
            Vector2 wobble = new Vector2((float)Math.Sin(t + Seed * 1.3f), (float)Math.Cos(t * 1.17f + Seed * 2.1f)) * BeeKeeperTuning.LatchWobble;
            Vector2 toCenter = (npc.Center - point).SafeNormalize(Vector2.UnitY);
            Projectile.Center = point + wobble + toCenter * (jab * BeeKeeperTuning.StingJab);
            Projectile.velocity = Vector2.Zero;
            facingRight = npc.Center.X >= Projectile.Center.X;

            if (IsOwner && latchTicks >= BeeKeeperTuning.MaxLatchTicks)
            {
                Sting(npc, true); // the final sting
                Release();
            }
        }

        // Owner's client only. The strike syncs itself (SimpleStrikeNPC strikes here and sends the hit to the server, which tells everyone
        // else), and so does AddBuff. Poisoned is only topped up when it runs low, so 15 bees do not send 30 buff packets a second.
        void Sting(NPC npc, bool final)
        {
            if (!npc.active || npc.life <= 0)
                return;
            int damage = Math.Max(1, (int)(stingDamage * BeeKeeperTuning.StingDamageFraction));
            int direction = npc.Center.X >= Main.player[Projectile.owner].Center.X ? 1 : -1;
            npc.SimpleStrikeNPC(damage, direction, false, 0f, DamageClass.Melee);
            if (!npc.active)
                return; // the sting killed it
            int buff = npc.FindBuffIndex(BuffID.Poisoned);
            if (final || buff < 0 || npc.buffTime[buff] < BeeKeeperTuning.StingPoisonTicks / 2)
                npc.AddBuff(BuffID.Poisoned, BeeKeeperTuning.StingPoisonTicks);
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            int frames = art != null ? Math.Max(1, BeeKeeperTuning.ArtFrames) : Math.Max(1, Main.projFrames[VanillaProjectile]);
            int frameHeight = texture.Height / frames;
            int frame = (ticks / Math.Max(1, BeeKeeperTuning.FlapTicks)) % frames;
            Rectangle source = new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = source.Size() * 0.5f;

            bool spriteFacesRight = art != null || BeeKeeperTuning.VanillaBeeFacesRight;
            SpriteEffects effects = facingRight == spriteFacesRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rotation = State == BeeState.Latched ? (float)Math.Sin(ticks * 0.45f + Seed) * 0.35f : MathHelper.Clamp(Projectile.velocity.X * 0.04f, -0.4f, 0.4f);

            float pop = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((ticks + 1f) / Math.Max(1, BeeKeeperTuning.PopTicks), 0f, 1f));
            float leave = State == BeeState.Leaving ? 1f - MathHelper.Clamp(leaveTicks / (float)Math.Max(1, BeeKeeperTuning.LeaveTicks), 0f, 1f) : 1f;
            float scale = (IsBig ? BeeKeeperTuning.BigScale : BeeKeeperTuning.SmallScale) * pop * (0.4f + 0.6f * leave);

            Color color = Color.Lerp(lightColor, Color.White, 0.3f);
            color *= leave;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, color, rotation, origin, scale, effects, 0);
            return false;
        }
    }

    /// <summary>A big bee: sturdier, a bigger poison cloud. Art: Content/Weapons/BeeKeeper/BeeKeeperBigBee.png (about 22x18, facing right).</summary>
    public class BeeKeeperBigBeeProjectile : BeeKeeperBeeProjectile
    {
        protected override bool IsBig => true;
        protected override int VanillaProjectile => ProjectileID.GiantBee;
        protected override string ArtPath => "IsmailsWeaponReworks/Content/Weapons/BeeKeeper/BeeKeeperBigBee";
    }

    /// <summary>A small bee: fragile, a smaller poison cloud. Art: Content/Weapons/BeeKeeper/BeeKeeperSmallBee.png (about 12x10, facing right).</summary>
    public class BeeKeeperSmallBeeProjectile : BeeKeeperBeeProjectile
    {
        protected override bool IsBig => false;
        protected override int VanillaProjectile => ProjectileID.Bee;
        protected override string ArtPath => "IsmailsWeaponReworks/Content/Weapons/BeeKeeper/BeeKeeperSmallBee";
    }
}