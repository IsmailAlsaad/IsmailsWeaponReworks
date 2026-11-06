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
    /// <summary>What a bee is doing. Patch 1 only has the swarm and the leaving; the attack states come with patch 2.</summary>
    public enum BeeState { Idle = 0, Leaving = 1 }

    /// <summary>
    /// One bee of the Bee Keeper's swarm (the base of the big and the small bee). It hovers around the owner's head on a wander pattern of its
    /// own, can be hurt by enemy bodies and hostile projectiles (the owner's client decides), and when its health runs out it leaves a poison
    /// cloud (BeeKeeperPoisonCloudProjectile). It deals no damage itself: it is a normal projectile only so tModLoader syncs it.
    /// MULTIPLAYER: the owner's client decides the state, the damage and the death. Synced through ai (netUpdate only on a change):
    /// ai[0] = state (BeeState), ai[1] = hurt counter (every machine notices a change and plays the hurt flash), ai[2] = the bee's seed
    /// (its wander pattern). Every machine runs the same wander from the seed and the owner's position, so the swarm is not sent every tick;
    /// the owner's position updates (netUpdate) pull the others back into line. A bee's health is the owner's alone.
    /// </summary>
    public abstract class BeeKeeperBeeProjectile : ModProjectile
    {
        protected abstract bool IsBig { get; }
        protected abstract int VanillaProjectile { get; }   // its texture until your own art exists
        protected abstract string ArtPath { get; }

        // The vanilla bee texture stands in until the art file exists.
        public override string Texture => "Terraria/Images/Projectile_" + VanillaProjectile;

        Asset<Texture2D> art;

        BeeState State => (BeeState)(int)Projectile.ai[0];
        float Seed => Projectile.ai[2];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // Per-machine state: counters and visuals, never synced.
        int ticks;
        float hp;            // owner only
        int hurtCooldown;    // owner only
        int seenHurt;        // the hurt counter this machine has already reacted to
        float flash;         // 1 right after a hit, fades to 0
        int leaveTicks;
        bool facingRight = true;

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
            Projectile.friendly = false; // it never damages by touching: stings are done by hand (patch 2)
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
                hp = IsBig ? BeeKeeperTuning.BigHp : BeeKeeperTuning.SmallHp;
                seenHurt = (int)Projectile.ai[1];
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
                if (State == BeeState.Idle)
                    CheckDamage(); // may kill the bee
                if (!Projectile.active)
                    return;
            }

            // Every machine (the owner too) reacts to a change of the hurt counter.
            int hurtNow = (int)Projectile.ai[1];
            if (hurtNow != seenHurt)
            {
                seenHurt = hurtNow;
                flash = 1f;
                BeeKeeperFx.BeeHurt(Projectile.Center);
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

            Steer(owner);
            if (Math.Abs(Projectile.velocity.X) > 0.3f)
                facingRight = Projectile.velocity.X > 0f;

            flash = Math.Max(0f, flash - 1f / Math.Max(1, BeeKeeperTuning.HurtFlashTicks));
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

        // ---- being hurt (owner's client only) ----

        void CheckDamage()
        {
            if (hurtCooldown > 0)
            {
                hurtCooldown--;
                return;
            }

            Rectangle box = Projectile.Hitbox;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.damage <= 0 || !npc.Hitbox.Intersects(box))
                    continue;
                Hurt(npc.Center);
                return;
            }

            if (!BeeKeeperTuning.HostileProjectilesHurtBees)
                return;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || !other.hostile || other.damage <= 0 || !other.Hitbox.Intersects(box))
                    continue;
                Hurt(other.Center);
                return;
            }
        }

        void Hurt(Vector2 from)
        {
            hp -= BeeKeeperTuning.EnemyTouchDamage;
            hurtCooldown = BeeKeeperTuning.HurtCooldownTicks;
            Projectile.velocity += (Projectile.Center - from).SafeNormalize(Vector2.UnitY) * BeeKeeperTuning.HurtKnock;
            Projectile.ai[1] += 1f;
            Projectile.netUpdate = true;
            if (hp <= 0f)
                Die();
        }

        // The poison cloud is its own projectile (synced), then the bee is gone.
        void Die()
        {
            int type = ModContent.ProjectileType<BeeKeeperPoisonCloudProjectile>();
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, type, 0, 0f, Projectile.owner, IsBig ? 1f : 0f);
            Projectile.Kill();
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
            float rotation = MathHelper.Clamp(Projectile.velocity.X * 0.04f, -0.4f, 0.4f);

            float pop = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((ticks + 1f) / Math.Max(1, BeeKeeperTuning.PopTicks), 0f, 1f));
            float leave = State == BeeState.Leaving ? 1f - MathHelper.Clamp(leaveTicks / (float)Math.Max(1, BeeKeeperTuning.LeaveTicks), 0f, 1f) : 1f;
            float scale = (IsBig ? BeeKeeperTuning.BigScale : BeeKeeperTuning.SmallScale) * pop * (0.4f + 0.6f * leave);

            Color color = Color.Lerp(lightColor, Color.White, 0.3f);
            color = Color.Lerp(color, new Color(255, 70, 50), 0.7f * flash) * leave;
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