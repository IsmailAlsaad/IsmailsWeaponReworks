using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.IceBlade
{
    /// <summary>
    /// The slash wave of the Ice Blade. A crescent drawn as a pixelated line: full thickness in the middle, tapering to a point at
    /// both ends, with afterimages, a halo and blue fog (all vanilla dust and pixel squares, no art file). It expands and slows down
    /// as it flies and fades at the end; its damage does NOT decay. It dies on tiles. Each enemy is hit once.
    /// MULTIPLAYER: spawned by the owner's IceBladeSwingProjectile (so it is synced like any projectile) and hit by the owner's client;
    /// every machine simulates the same motion and visuals from the synced spawn data.
    /// ai[0]: tier 1 to 3 (sent with the spawn). localAI[0]: age in ticks (visual and motion only, each machine counts its own).
    /// </summary>
    public class IceBladeWaveProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        int Tier => Math.Clamp((int)Projectile.ai[0], 1, 3);
        float Age => Projectile.localAI[0];
        float Life => Math.Max(1, IceBladeTuning.WaveLifeTicks(Tier));
        float Progress => MathHelper.Clamp(Age / Life, 0f, 1f);
        float ScaleAt(float age)
        {
            float u = MathHelper.Clamp(age / Life, 0f, 1f);
            return 1f + IceBladeTuning.WaveGrowth * (1f - (1f - u) * (1f - u)); // expands fast at first, then slows
        }
        float Fade => 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((Progress - 0.6f) / 0.4f, 0f, 1f));
        float CurrentRadius => IceBladeTuning.WaveRadius(Tier) * ScaleAt(Age);
        float CurrentThickness => IceBladeTuning.WaveThickness(Tier) * Math.Min(1f, (Age + 1f) / 4f) * (0.4f + 0.6f * Fade);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120; // set from the tier on the first tick
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // each enemy once
        }

        // A point on the crescent's circle; the front of the wave (phi = 0) is the projectile's center.
        Vector2 ArcPoint(float phi, float radius)
        {
            Vector2 origin = Projectile.Center - Projectile.rotation.ToRotationVector2() * radius;
            return origin + (Projectile.rotation + phi).ToRotationVector2() * radius;
        }

        // The crescent is not a rectangle: test a row of points along it.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = CurrentRadius;
            float reach = Math.Max(10f, CurrentThickness * 0.6f + 6f);
            float half = MathHelper.ToRadians(IceBladeTuning.WaveArcHalfDeg);
            int points = 11;
            for (int i = 0; i < points; i++)
            {
                Vector2 spot = ArcPoint(MathHelper.Lerp(-half, half, i / (float)(points - 1)), radius);
                Vector2 closest = new Vector2(MathHelper.Clamp(spot.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(spot.Y, targetHitbox.Top, targetHitbox.Bottom));
                if (Vector2.DistanceSquared(spot, closest) <= reach * reach)
                    return true;
            }
            return false;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation(); // the crescent always faces the way it was sent
                Projectile.timeLeft = IceBladeTuning.WaveLifeTicks(Tier);
            }
            Projectile.localAI[0]++;
            Projectile.velocity *= IceBladeTuning.WaveDrag;

            float fade = Fade;
            Lighting.AddLight(Projectile.Center, 0.25f * fade, 0.55f * fade, 0.9f * fade);

            if (Main.dedServ || fade <= 0.05f)
                return;

            // Blue fog and sparks along the whole crescent.
            float radius = CurrentRadius;
            float half = MathHelper.ToRadians(IceBladeTuning.WaveArcHalfDeg);
            for (int i = 0; i < Tier; i++)
            {
                float phi = Main.rand.NextFloat(-half, half);
                Vector2 outward = (Projectile.rotation + phi).ToRotationVector2();
                IceBladeFx.WaveFog(ArcPoint(phi, radius), outward * 0.7f + Projectile.velocity * 0.3f + Main.rand.NextVector2Circular(0.4f, 0.4f), 1.7f + 0.25f * Tier);
            }
            if (Main.rand.NextBool(2))
            {
                float phi = Main.rand.NextFloat(-half, half);
                Vector2 outward = (Projectile.rotation + phi).ToRotationVector2();
                IceBladeFx.WaveSpark(ArcPoint(phi, radius), outward * Main.rand.NextFloat(1f, 3.5f) + Projectile.velocity * 0.2f, 1.1f + 0.1f * Tier);
            }
            // Extra sparks shed from the two sharp tips.
            if (Main.rand.NextBool(3))
            {
                float phi = Main.rand.NextBool() ? half : -half;
                IceBladeFx.WaveSpark(ArcPoint(phi, radius), (Projectile.rotation + phi).ToRotationVector2() * 2f, 1f);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = Projectile.rotation.ToRotationVector2().X >= 0f ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            IceFx.Burst(target.Center, 5, 3f, 1f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true; // a wall ends the wave
        }

        public override void OnKill(int timeLeft)
        {
            IceBladeFx.PlayWaveShatter(Projectile.Center);
            if (Main.dedServ)
                return;
            float radius = CurrentRadius;
            float half = MathHelper.ToRadians(IceBladeTuning.WaveArcHalfDeg);
            for (int i = 0; i < 6; i++)
                IceFx.Burst(ArcPoint(MathHelper.Lerp(-half, half, i / 5f), radius), 2 + Tier, 3f, 1.1f);
        }

        // ---- drawing ----

        // One crescent made of pixel squares: the line follows the circle, the squares are thickest in the middle and shrink to a
        // single pixel at both tips. Each square sits on the pixel grid, so the shape looks like pixel art and crawls as it moves.
        void DrawCrescent(Vector2 center, float angle, float radius, float thickness, float half, Color color)
        {
            float grid = IceBladeTuning.PixelSize;
            Vector2 origin = center - angle.ToRotationVector2() * radius;
            float arcLength = radius * 2f * half;
            float distance = 0f;
            int guard = 0;
            while (distance < arcLength && guard++ < 400)
            {
                float f = distance / arcLength;
                float phi = MathHelper.Lerp(-half, half, f);
                float profile = (float)Math.Pow(Math.Max(0.0, Math.Cos((f - 0.5f) * MathHelper.Pi)), 0.75); // 1 in the middle, 0 at the tips
                float size = Math.Max(grid, thickness * profile);
                Vector2 spot = origin + (angle + phi).ToRotationVector2() * radius;
                IceFx.DrawPixel(spot, size, color, grid);
                distance += Math.Max(grid, (float)Math.Round(size / grid) * grid); // squares sit side by side, so they never stack up
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fade = Fade;
            if (fade <= 0.01f)
                return false;
            float half = MathHelper.ToRadians(IceBladeTuning.WaveArcHalfDeg);
            float baseRadius = IceBladeTuning.WaveRadius(Tier);
            float thickness = CurrentThickness;
            float angle = Projectile.rotation;

            // Afterimages: older copies of the crescent, smaller and dimmer.
            int count = Math.Min(IceBladeTuning.AfterimageCount, 7);
            for (int i = count; i >= 1; i--)
            {
                Vector2 old = Projectile.oldPos[i];
                if (old == Vector2.Zero)
                    continue;
                float f = 1f - i / (float)(count + 1);
                float radius = baseRadius * ScaleAt(Math.Max(0f, Age - i));
                DrawCrescent(old + Projectile.Size * 0.5f, angle, radius, thickness * (0.5f + 0.5f * f), half, IceBladeTuning.MidColor * (0.32f * f * f * fade));
            }

            Vector2 center = Projectile.Center;
            float currentRadius = CurrentRadius;
            DrawCrescent(center, angle, currentRadius, thickness * 2.4f, half, IceBladeTuning.HaloColor * (0.2f * fade));  // halo
            DrawCrescent(center, angle, currentRadius, thickness * 1.35f, half, IceBladeTuning.MidColor * (0.5f * fade));
            DrawCrescent(center, angle, currentRadius, thickness * 0.7f, half, IceBladeTuning.CoreColor * (0.95f * fade)); // hot core
            return false;
        }
    }
}