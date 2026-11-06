using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// One drop of the Volcano's eruption (patch 3): thrown up in a fan, it falls under gravity, lands on the first solid tile and stays there
    /// as a puddle of merging lava for DropLingerTicks. Anything that touches it (flying or landed) is hurt a little and set on fire (Hellfire).
    /// Water ends it with a hiss. At most DropMax drops live per player: when the eruption would go over, the oldest are told to hiss out.
    /// ai[0] = phase (0 falling, 1 landed, 2 gone = told by the owner to hiss out), ai[1] = size (a multiplier, set by the owner at spawn).
    /// The lava is the blob layer of VolcanoBlobStyle (this projectile adds a blob every tick); it is drawn by the held or thrown Volcano, or
    /// after the tiles when none exists (MetaballLayers).
    /// MULTIPLAYER: the owner's client spawns the drops (so tModLoader syncs them with their velocity and size), decides the landing (it
    /// announces it with ai[0] and netUpdate: position and phase), the cap and the damage. Every machine also runs the same fall and landing
    /// test on its own tile data (visual only, so a drop never sinks through a floor while it waits for the owner's news), plays its own
    /// splash, hiss and lava, and removes its own copy when its linger time is over. No custom packets.
    /// </summary>
    public class VolcanoLavaDropProjectile : ModProjectile
    {
        // Never drawn (the lava is the metaball layer): any vanilla projectile texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Fireball;

        enum Phase { Falling = 0, Landed = 1, Gone = 2 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        float Size => Projectile.ai[1] > 0.1f ? Projectile.ai[1] : 1f;
        bool IsOwner => Projectile.owner == Main.myPlayer;

        bool landed;        // this machine has seen it land
        int ticks;
        int landedTicks;
        int goneTicks;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // the landing is tested by hand, so the drop rests exactly on the floor
            Projectile.ignoreWater = true;
            Projectile.timeLeft = VolcanoTuning.DropFlightMaxTicks + VolcanoTuning.DropLingerTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = VolcanoTuning.DropHitCooldown;
        }

        // Owner's client: the eruption's fan. Makes room first (at most DropMax alive per player), then throws EruptionDrops drops up and out.
        // 'axis' is the middle of the fan.
        public static void Rain(Projectile thrown, Vector2 point, Vector2 axis)
        {
            int count = VolcanoTuning.EruptionDrops;
            MakeRoom(thrown.owner, count);

            int damage = (int)(thrown.damage * VolcanoTuning.DropDamageMultiplier);
            int type = ModContent.ProjectileType<VolcanoLavaDropProjectile>();
            float half = MathHelper.ToRadians(VolcanoTuning.EruptionFanDeg);
            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? i / (float)(count - 1) : 0.5f;
                float angle = MathHelper.Lerp(-half, half, t) + Main.rand.NextFloat(-0.12f, 0.12f);
                Vector2 velocity = axis.RotatedBy(angle) * Main.rand.NextFloat(VolcanoTuning.DropSpeedMin, VolcanoTuning.DropSpeedMax);
                Projectile.NewProjectile(thrown.GetSource_FromThis(), point, velocity, type, damage, 0f, thrown.owner, 0f, Main.rand.NextFloat(0.8f, 1.4f));
            }
        }

        // The drops with the least time left are the oldest (a landed drop counts down, a falling one has not started). They are told to hiss out.
        static void MakeRoom(int owner, int incoming)
        {
            int type = ModContent.ProjectileType<VolcanoLavaDropProjectile>();
            List<Projectile> alive = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == owner && projectile.type == type && projectile.ai[0] < 2f)
                    alive.Add(projectile);
            }

            int excess = alive.Count + incoming - VolcanoTuning.DropMax;
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
        }

        void AiFalling()
        {
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + VolcanoTuning.DropGravity, 16f);

            if (Collision.WetCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                HissOut(true);
                return;
            }

            // Small steps, so a fast drop cannot pass through a floor: the last free step is where it lands.
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
                VolcanoFx.DropLand(Projectile.Center, Size);
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
            if (landedTicks >= VolcanoTuning.DropLingerTicks)
            {
                Projectile.Kill(); // every machine removes its own copy
                return;
            }

            if (!Main.dedServ)
            {
                Feed(true);
                if (Main.rand.NextBool(70))
                    VolcanoFx.Ember(Projectile.Center, Vector2.Zero);
            }
        }

        // Told by the owner to make room: the lava hisses out quickly.
        void AiGone()
        {
            Projectile.velocity = Vector2.Zero;
            if (goneTicks == 0 && !Main.dedServ)
                VolcanoFx.Hiss(Projectile.Center, Main.rand.NextBool(4));
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
                VolcanoFx.Hiss(Projectile.Center, sound);
            Projectile.Kill();
        }

        // The drop's lava for this tick: a round blob while it falls, a flat puddle of three merging blobs once it has landed that cools and
        // shrinks away over its last ticks. 'extraFade' is for the hiss-out of a drop that was told to go.
        void Feed(bool isLanded, float extraFade = 1f)
        {
            MetaballLayer body = MetaballLayers.Of<VolcanoBlobStyle>();
            Vector2 center = Projectile.Center;
            float radius = VolcanoTuning.DropRadius * Size * extraFade;

            if (!isLanded)
            {
                body.AddBlob(center, radius, VolcanoTuning.DropHeat);
                if (Main.rand.NextBool(4))
                    MetaballLayers.Of<VolcanoDripStyle>().Spawn(center, Main.rand.NextVector2Circular(0.5f, 0.5f), Main.rand.Next(18, 34), Main.rand.NextFloat(1.6f, 2.6f) * Size);
                Lighting.AddLight(center, 0.9f, 0.4f, 0.08f);
                return;
            }

            float age = MathHelper.Clamp(landedTicks / (float)Math.Max(1, VolcanoTuning.DropLingerTicks), 0f, 1f);
            float fade = MathHelper.Clamp((VolcanoTuning.DropLingerTicks - landedTicks) / (float)Math.Max(1, VolcanoTuning.DropFadeTicks), 0f, 1f) * extraFade;
            float heat = MathHelper.Lerp(VolcanoTuning.DropHeat, VolcanoTuning.DropColdHeat, age);
            float wobble = 1f + 0.12f * (float)Math.Sin(ticks * 0.2f + Projectile.whoAmI);
            body.AddBlob(center + new Vector2(0f, 2f), radius * 1.05f * wobble * fade, heat);
            body.AddBlob(center + new Vector2(-radius * 0.95f, 3f), radius * 0.65f * fade, heat);
            body.AddBlob(center + new Vector2(radius * 0.95f, 3f), radius * 0.65f * fade, heat);
            float light = (1f - 0.6f * age) * fade;
            Lighting.AddLight(center, 0.8f * light, 0.35f * light, 0.06f * light);
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return CurrentPhase == Phase.Gone ? false : (bool?)null;
        }

        // A puddle is wider than the 10 px box: it burns whatever stands in it.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!landed)
                return null;
            Rectangle puddle = new Rectangle(projHitbox.X - 8, projHitbox.Y - 2, projHitbox.Width + 16, projHitbox.Height + 4);
            return puddle.Intersects(targetHitbox);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
            modifiers.Knockback *= 0f;
        }

        // Owner's client only.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, VolcanoTuning.DropOnFireTicks);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
