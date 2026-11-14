using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// One dark droplet of the Shadow Armor: spat up and out in a fan, it falls under gravity, lands on the first solid tile and lies there as a small
    /// dark puddle for DropLingerTicks. An enemy that touches it (flying or landed) is hurt (DropDamageMultiplier, at most once per DropHitCooldownTicks across all droplets) and set on Shadowflame. Water ends it.
    /// At most DropMax droplets live per player: when a throw would go over, the oldest are told to go.
    /// ai[0] = phase (0 falling, 1 landed, 2 gone = told by the owner to go), ai[1] = size (a multiplier, set by the owner at spawn).
    /// The body is the metaball layer of NightsEdgeBurstStyle (this projectile adds a blob every tick); it is drawn by the held blade, or after the
    /// tiles when none is held (MetaballLayers).
    /// MULTIPLAYER: the owner's client spawns the droplets (tModLoader syncs them with their velocity and size), decides the landing (it announces it with
    /// ai[0] and netUpdate), the cap and the Shadowflame (an overlap test on the owner's machine, NPC.AddBuff syncs). Every machine also runs the same fall
    /// and landing test on its own tile data (visual only, so a droplet never sinks through a floor while it waits for the owner's news) and removes its
    /// own copy when its linger time is over. No custom packets.
    /// </summary>
    public class NightsEdgeDropProjectile : ModProjectile
    {
        // Never drawn (the droplet is the metaball layer): any vanilla projectile texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Fireball;

        enum Phase { Falling = 0, Landed = 1, Gone = 2 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        float Size => Projectile.ai[1] > 0.1f ? Projectile.ai[1] : 1f;
        bool IsOwner => Projectile.owner == Main.myPlayer;

        bool landed;   // this machine has seen it land
        static readonly uint[] lastHit = new uint[256]; // owner's client: the tick each enemy was last hurt by ANY droplet (shared, so a field of puddles does not hit every tick)
        int ticks;
        int landedTicks;
        int goneTicks;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;   // it never damages by touching: the owner's client hurts and ignites by hand (Touch)
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // the landing is tested by hand, so the droplet rests exactly on the floor
            Projectile.ignoreWater = true;
            Projectile.timeLeft = NightsEdgeTuning.DropFlightMaxTicks + NightsEdgeTuning.DropLingerTicks;
        }

        // Owner's client: spits 'count' droplets up and out of the player (a fan; one droplet gets a random direction inside it).
        public static void Throw(Player owner, int count)
        {
            MakeRoom(owner.whoAmI, count);
            int type = ModContent.ProjectileType<NightsEdgeDropProjectile>();
            float half = MathHelper.ToRadians(NightsEdgeTuning.DropFanDeg);
            // The Night's Edge's base damage with the player's melee bonuses (the armor outlives the sword in the hand, so the held item is not used).
            int damage = (int)(owner.GetTotalDamage(DamageClass.Melee).ApplyTo(ContentSamples.ItemsByType[ItemID.NightsEdge].damage) * NightsEdgeTuning.DropDamageMultiplier);
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? MathHelper.Lerp(-half, half, i / (float)(count - 1)) + Main.rand.NextFloat(-0.12f, 0.12f) : Main.rand.NextFloat(-half, half);
                Vector2 velocity = new Vector2(0f, -1f).RotatedBy(angle) * Main.rand.NextFloat(NightsEdgeTuning.DropSpeedMin, NightsEdgeTuning.DropSpeedMax) + owner.velocity * 0.5f;
                Projectile.NewProjectile(owner.GetSource_Misc("NightsEdgeShadowArmor"), owner.Center, velocity, type, damage, 0f, owner.whoAmI, 0f,
                    Main.rand.NextFloat(NightsEdgeTuning.DropSizeMin, NightsEdgeTuning.DropSizeMax));
            }
        }

        // The droplets with the least time left are the oldest (a landed one counts down, a falling one has not started). They are told to go.
        static void MakeRoom(int owner, int incoming)
        {
            int type = ModContent.ProjectileType<NightsEdgeDropProjectile>();
            List<Projectile> alive = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == owner && projectile.type == type && projectile.ai[0] < 2f)
                    alive.Add(projectile);
            }

            int excess = alive.Count + incoming - NightsEdgeTuning.DropMax;
            if (excess <= 0)
                return;
            alive.Sort((a, b) => a.timeLeft.CompareTo(b.timeLeft));
            for (int i = 0; i < excess && i < alive.Count; i++)
            {
                alive[i].ai[0] = (float)Phase.Gone;
                alive[i].netUpdate = true;
            }
        }

        // ---- AI ----

        public override void AI()
        {
            if (CurrentPhase == Phase.Gone)
            {
                AiGone();
                return;
            }

            if (!landed && CurrentPhase == Phase.Landed)
                Land(false); // the owner says it landed and this machine has not seen it yet

            if (landed)
                AiLanded();
            else
                AiFalling();
            ticks++;

            if (CurrentPhase != Phase.Gone)
                Touch();
        }

        void AiFalling()
        {
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + NightsEdgeTuning.DropGravity, 16f);

            if (Collision.WetCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                HissOut(true);
                return;
            }

            // Small steps, so a fast droplet cannot pass through a floor: the last free step is where it lands.
            Vector2 move = Projectile.velocity;
            int steps = Math.Max(1, (int)Math.Ceiling(move.Length() / 4f));
            Vector2 safe = Projectile.position;
            for (int i = 1; i <= steps; i++)
            {
                Vector2 next = Projectile.position + move * (i / (float)steps);
                if (Collision.SolidCollision(next, Projectile.width, Projectile.height))
                {
                    Projectile.position = safe;
                    Land(true);
                    return;
                }
                safe = next;
            }

            if (!Main.dedServ)
                Feed(false);
        }

        // 'announce': this machine found the landing itself. Only the owner tells the others (ai[0] and netUpdate carry the phase and the position).
        void Land(bool announce)
        {
            landed = true;
            landedTicks = 0;
            Projectile.velocity = Vector2.Zero;
            if (announce && IsOwner)
            {
                Projectile.ai[0] = (float)Phase.Landed;
                Projectile.netUpdate = true;
            }
            if (!Main.dedServ)
                NightsEdgeFx.DropLand(Projectile.Center, Size);
        }

        void AiLanded()
        {
            Projectile.velocity = Vector2.Zero;
            landedTicks++;

            if (Collision.WetCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                HissOut(true);
                return;
            }
            if (landedTicks >= NightsEdgeTuning.DropLingerTicks)
            {
                Projectile.Kill(); // every machine removes its own copy
                return;
            }

            if (!Main.dedServ)
                Feed(true);
        }

        // Told by the owner to make room: the droplet fades out quickly.
        void AiGone()
        {
            Projectile.velocity = Vector2.Zero;
            if (goneTicks == 0 && !Main.dedServ)
                NightsEdgeFx.DropHiss(Projectile.Center, false);
            if (++goneTicks > 10)
            {
                Projectile.Kill();
                return;
            }
            if (!Main.dedServ)
                Feed(landed, 1f - goneTicks / 10f);
        }

        void HissOut(bool sound)
        {
            if (!Main.dedServ)
                NightsEdgeFx.DropHiss(Projectile.Center, sound);
            Projectile.Kill();
        }

        // The droplet's body for this tick: a round blob while it falls, a flat puddle of three merging blobs once it has landed that cools and shrinks
        // away over its last ticks. 'extraFade' is for a droplet that was told to go.
        void Feed(bool isLanded, float extraFade = 1f)
        {
            MetaballLayer body = MetaballLayers.Of<NightsEdgeBurstStyle>();
            Vector2 center = Projectile.Center;
            float radius = NightsEdgeTuning.DropRadius * Size * extraFade;

            if (!isLanded)
            {
                body.AddBlob(center, radius, NightsEdgeTuning.DropHeat);
                if (Main.rand.NextBool(4))
                    MetaballLayers.Of<NightsEdgeFlameStyle>().Spawn(center, Main.rand.NextVector2Circular(0.5f, 0.5f), Main.rand.Next(NightsEdgeTuning.FlameLifeMin, NightsEdgeTuning.FlameLifeMax + 1), Main.rand.NextFloat(1.6f, 2.6f) * Size);
                Lighting.AddLight(center, 0.4f, 0.15f, 0.8f);
                return;
            }

            float age = MathHelper.Clamp(landedTicks / (float)Math.Max(1, NightsEdgeTuning.DropLingerTicks), 0f, 1f);
            float fade = MathHelper.Clamp((NightsEdgeTuning.DropLingerTicks - landedTicks) / (float)Math.Max(1, NightsEdgeTuning.DropFadeTicks), 0f, 1f) * extraFade;
            float heat = MathHelper.Lerp(NightsEdgeTuning.DropHeat, NightsEdgeTuning.DropColdHeat, age);
            float wobble = 1f + 0.12f * (float)Math.Sin(ticks * 0.2f + Projectile.whoAmI);
            body.AddBlob(center + new Vector2(0f, 2f), radius * 1.05f * wobble * fade, heat);
            body.AddBlob(center + new Vector2(-radius * 0.95f, 3f), radius * 0.65f * fade, heat);
            body.AddBlob(center + new Vector2(radius * 0.95f, 3f), radius * 0.65f * fade, heat);
            float light = (1f - 0.6f * age) * fade;
            Lighting.AddLight(center, 0.35f * light, 0.12f * light, 0.7f * light);
        }

        // Owner's client only: every enemy that overlaps the droplet (a puddle is wider than its 10 px box) is set on Shadowflame. The buff is only
        // refreshed once it is below half, so a standing enemy does not send a packet every tick.
        void Touch()
        {
            if (!IsOwner)
                return;
            Rectangle zone = Projectile.Hitbox;
            if (landed)
            {
                int width = (int)NightsEdgeTuning.DropTouchWidth;
                int height = (int)NightsEdgeTuning.DropTouchHeight;
                zone = new Rectangle((int)Projectile.Center.X - width / 2, (int)Projectile.Bottom.Y + 2 - height, width, height);
            }
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || (!npc.CanBeChasedBy() && npc.type != NPCID.TargetDummy))
                    continue;
                if (!zone.Intersects(npc.Hitbox))
                    continue;
                int index = npc.FindBuffIndex(BuffID.ShadowFlame);
                if (index < 0 || npc.buffTime[index] < NightsEdgeTuning.DropShadowflameTicks / 2)
                    npc.AddBuff(BuffID.ShadowFlame, NightsEdgeTuning.DropShadowflameTicks);
                if (Projectile.damage > 0 && (Main.GameUpdateCount < lastHit[i] || Main.GameUpdateCount - lastHit[i] >= NightsEdgeTuning.DropHitCooldownTicks))
                {
                    lastHit[i] = Main.GameUpdateCount;
                    npc.SimpleStrikeNPC(Projectile.damage, npc.Center.X >= Projectile.Center.X ? 1 : -1, false, 0f, DamageClass.Melee);
                }
            }
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}