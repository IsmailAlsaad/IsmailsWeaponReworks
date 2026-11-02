using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>
    /// The sun-like explosion of the Sunfury's special: a short-lived circle that damages every enemy in it once and sets it on fire.
    /// MULTIPLAYER: spawned by the owner's client (so it is synced like any projectile); every machine plays the visuals, sound and camera
    /// shake on its first tick, the owner's client does the damage and the burning, and the other machines add the hit sparks themselves.
    /// </summary>
    public class SunfuryBlastProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        float Age => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = (int)(SunfuryTuning.BlastRadius * 2f);
            Projectile.height = (int)(SunfuryTuning.BlastRadius * 2f);
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = SunfuryTuning.BlastLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= SunfuryTuning.BlastRadius * SunfuryTuning.BlastRadius;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                SunfuryFx.SunBlast(Projectile.Center, SunfuryTuning.BlastRadius);
                if (Projectile.owner != Main.myPlayer)
                    RemoteHitTracker.TouchOnce(Projectile, npc => SunfuryFx.HitSparks(npc.Center, 3)); // the owner gets these from OnHitNPC
            }
            Projectile.localAI[0]++;
            float fade = 1f - MathHelper.Clamp(Age / Math.Max(1, SunfuryTuning.BlastLifeTicks), 0f, 1f);
            Lighting.AddLight(Projectile.Center, 2.2f * fade + 0.2f, 1.5f * fade + 0.1f, 0.5f * fade);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SunfuryFx.HitSparks(target.Center, 3);
            target.AddBuff(BuffID.OnFire3, SunfuryTuning.BlastBurnTicks);
        }

        // A sun: a white-hot core that swells and fades, a corona ring that races outward, and rays, all drawn from pixel squares and lines.
        public override bool PreDraw(ref Color lightColor)
        {
            float life = Math.Max(1, SunfuryTuning.BlastLifeTicks);
            float u = MathHelper.Clamp(Age / life, 0f, 1f);
            float grow = 1f - (1f - u) * (1f - u) * (1f - u);
            float fade = (1f - u) * (1f - u);
            float radius = SunfuryTuning.BlastRadius;
            float grid = SunfuryTuning.PixelSize;
            Vector2 center = Projectile.Center;

            // Disc layers, outermost first: orange haze, fire, hot yellow, white core (the core shrinks back faster than the haze).
            SunfuryFx.DrawDisc(center, radius * grow, SunfuryTuning.GlowColor(1) * (0.35f * fade), grid);
            SunfuryFx.DrawDisc(center, radius * 0.8f * grow, SunfuryTuning.GlowColor(2) * (0.5f * fade), grid);
            SunfuryFx.DrawDisc(center, radius * 0.55f * grow * (1f - 0.4f * u), SunfuryTuning.HotColor(2) * (0.7f * fade), grid);
            SunfuryFx.DrawDisc(center, radius * 0.3f * grow * (1f - 0.7f * u), SunfuryTuning.HotColor(3) * (0.9f * fade), grid);

            // The corona: a thin ring of pixel squares on the edge.
            float ringRadius = radius * grow;
            int count = Math.Max(24, (int)(MathHelper.TwoPi * ringRadius / (grid * 2f)));
            for (int i = 0; i < count; i++)
            {
                Vector2 spot = center + (MathHelper.TwoPi * i / count).ToRotationVector2() * ringRadius;
                IceFx.DrawPixel(spot, grid * (2f + 3f * (1f - u)), SunfuryTuning.HotColor(3) * (0.9f * fade), grid);
            }

            // Rays shooting out of the sun.
            int rays = 14;
            for (int i = 0; i < rays; i++)
            {
                float angle = MathHelper.TwoPi * i / rays + 0.2f;
                float reach = radius * (0.9f + 0.5f * (i % 3) / 2f) * grow;
                Vector2[] points = { center + angle.ToRotationVector2() * radius * 0.5f * grow, center + angle.ToRotationVector2() * reach };
                float[] t = { 0f, 1f };
                float fadeCopy = fade;
                PixelLine.Draw(points, t, 2, grid, x => 8f * (1f - x), x => SunfuryTuning.HotColor(2) * (0.8f * fadeCopy * (1f - x)));
            }
            return false;
        }
    }
}