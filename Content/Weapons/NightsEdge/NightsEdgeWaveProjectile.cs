using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The short dark flame wave a fully charged Night's Edge throws with every swing: a purple crescent (Common/CrescentPainter, the Ice Blade
    /// wave's painter) with afterimages that sheds vanilla dust and metaball flames, flies straight toward the cursor at a constant speed for
    /// about NightsEdgeTuning.WaveRangeTiles tiles, stops at blocks and sets every enemy it hits on Shadowflame (each enemy once).
    /// MULTIPLAYER: spawned by the owner's blade (so it is synced like any projectile) and hit by the owner's client; every machine simulates
    /// the same motion and visuals from the synced spawn data. localAI[0] is the age in ticks (visual and motion only, each machine counts its own).
    /// </summary>
    public class NightsEdgeWaveProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        float Age => Projectile.localAI[0];
        float Life => Math.Max(4f, NightsEdgeTuning.WaveRangeTiles * 16f / Math.Max(1f, NightsEdgeTuning.WaveSpeed));
        float Progress => MathHelper.Clamp(Age / Life, 0f, 1f);
        float Fade => 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((Progress - 0.65f) / 0.35f, 0f, 1f));
        float CurrentRadius
        {
            get
            {
                float u = Progress;
                return NightsEdgeTuning.WaveRadius * (1f + NightsEdgeTuning.WaveGrowth * (1f - (1f - u) * (1f - u))); // expands fast at first, then slows
            }
        }
        float CurrentThickness => NightsEdgeTuning.WaveThickness * Math.Min(1f, (Age + 1f) / 4f) * (0.4f + 0.6f * Fade);

        AfterimageTrail afterimages; // visual only, never synced

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true; // stops at blocks
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120; // set from the range on the first tick
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
            float half = MathHelper.ToRadians(NightsEdgeTuning.WaveArcHalfDeg);
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
                Projectile.timeLeft = (int)Math.Ceiling(Life);
                NightsEdgeFx.PlayWave(Projectile.Center);
            }
            Projectile.localAI[0]++;

            float fade = Fade;
            Lighting.AddLight(Projectile.Center, 0.5f * fade, 0.18f * fade, 0.9f * fade);

            if (Main.dedServ || fade <= 0.05f)
                return;

            // Dust and metaball flames shed along the whole crescent, a few more from the two sharp tips.
            float radius = CurrentRadius;
            float half = MathHelper.ToRadians(NightsEdgeTuning.WaveArcHalfDeg);
            int flames = (int)(NightsEdgeTuning.WaveFlameRate * fade);
            if (Main.rand.NextFloat() < NightsEdgeTuning.WaveFlameRate * fade - flames)
                flames++;
            for (int i = 0; i < flames; i++)
            {
                float phi = Main.rand.NextFloat(-half, half);
                Vector2 outward = (Projectile.rotation + phi).ToRotationVector2();
                NightsEdgeFx.WaveShed(ArcPoint(phi, radius), outward * 0.6f + Projectile.velocity * 0.15f, fade, false, true);
            }
            if (Main.rand.NextFloat() < NightsEdgeTuning.WaveDustChance)
            {
                float phi = Main.rand.NextFloat(-half, half);
                Vector2 outward = (Projectile.rotation + phi).ToRotationVector2();
                NightsEdgeFx.WaveShed(ArcPoint(phi, radius), outward * Main.rand.NextFloat(0.8f, 2.5f) + Projectile.velocity * 0.2f, fade, true, false);
            }
            if (Main.rand.NextBool(3))
            {
                float phi = Main.rand.NextBool() ? half : -half;
                NightsEdgeFx.WaveShed(ArcPoint(phi, radius), (Projectile.rotation + phi).ToRotationVector2() * 2f, fade, true, false);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            NightsEdgeFx.WaveBreak(Projectile.Center, Projectile.rotation.ToRotationVector2());
            return true; // dies
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = Projectile.rotation.ToRotationVector2().X >= 0f ? 1 : -1;
        }

        // Owner's client only (NPC.AddBuff syncs).
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            NightsEdgeFx.HitSpark(target.Center, 0.5f);
            target.AddBuff(BuffID.ShadowFlame, NightsEdgeTuning.WaveShadowflameTicks);
        }

        // One crescent afterimage per tick (visual only); OnKill hands the trail to DetachedTrails so the leftover ones fade out on their own.
        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active)
                return;
            afterimages ??= new AfterimageTrail(NightsEdgeTuning.WaveAfterimages, PaintAfterimage);
            afterimages.Tick();
            afterimages.Add(Projectile.Center, Projectile.rotation, CurrentRadius, NightsEdgeTuning.PurpleColor * Fade, CurrentThickness);
        }

        // Older copies of the crescent, smaller and dimmer with age.
        static void PaintAfterimage(Afterimage image, float age)
        {
            float f = 1f - age;
            CrescentPainter.Draw(image.Position, image.Rotation, image.Scale, image.Width * (0.5f + 0.5f * f), MathHelper.ToRadians(NightsEdgeTuning.WaveArcHalfDeg),
                image.Color * (0.32f * f * f), NightsEdgeTuning.PixelSize);
        }

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(afterimages);
            afterimages = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            afterimages?.Draw();
            float fade = Fade;
            if (fade <= 0.01f)
                return false;
            float half = MathHelper.ToRadians(NightsEdgeTuning.WaveArcHalfDeg);
            float thickness = CurrentThickness;
            float angle = Projectile.rotation;
            Vector2 center = Projectile.Center;
            float radius = CurrentRadius;
            float grid = NightsEdgeTuning.PixelSize;
            CrescentPainter.Draw(center, angle, radius, thickness * 1.4f, half, NightsEdgeTuning.PurpleColor * (0.2f * fade), grid);  // halo
            CrescentPainter.Draw(center, angle, radius, thickness * 1.2f, half, NightsEdgeTuning.PurpleColor * (0.5f * fade), grid);
            CrescentPainter.Draw(center, angle, radius, thickness * 0.8f, half, NightsEdgeTuning.CoreColor * (0.95f * fade), grid);  // hot core
            return false;
        }
    }
}