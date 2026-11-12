using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BlueMoon
{
    /// <summary>
    /// The water magic explosion on an enemy the Blue Moon's special hits: a short-lived circle that damages every enemy in it once and
    /// soaks it (Wet). MULTIPLAYER: spawned by the owner's client (so it is synced like any projectile); every machine plays the visuals
    /// and sound on its first tick, the owner's client does the damage, and the other machines add the hit sparks themselves.
    /// </summary>
    public class BlueMoonBlastProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        static BlueMoonTuning Tuning => BlueMoonTuning.Instance;
        float Age => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = (int)(Tuning.BlastRadius * 2f);
            Projectile.height = (int)(Tuning.BlastRadius * 2f);
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Tuning.BlastLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= Tuning.BlastRadius * Tuning.BlastRadius;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                BlueMoonFx.Instance.WaterBlast(Projectile.Center, Tuning.BlastRadius);
                if (Projectile.owner != Main.myPlayer)
                    RemoteHitTracker.TouchOnce(Projectile, npc => BlueMoonFx.Instance.HitSparks(npc.Center, 3)); // the owner gets these from OnHitNPC
            }
            Projectile.localAI[0]++;
            float fade = 1f - MathHelper.Clamp(Age / Math.Max(1, Tuning.BlastLifeTicks), 0f, 1f);
            Lighting.AddLight(Projectile.Center, 0.2f + 0.3f * fade, 0.5f + 0.6f * fade, 1.1f * fade + 0.3f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BlueMoonFx.Instance.HitSparks(target.Center, 3);
            target.AddBuff(BuffID.Wet, Tuning.BlastWetTicks);
        }

        // A water burst: a translucent blue disc and a bright ring of pixel squares racing outward, in the pixel look of the other explosions.
        public override bool PreDraw(ref Color lightColor)
        {
            float life = Math.Max(1, Tuning.BlastLifeTicks);
            float u = MathHelper.Clamp(Age / life, 0f, 1f);
            float grow = 1f - (1f - u) * (1f - u) * (1f - u);
            float fade = (1f - u) * (1f - u);
            float radius = Tuning.BlastRadius;
            float grid = Tuning.PixelSize;
            Vector2 center = Projectile.Center;

            // The ring on the edge, and a second smaller one behind it.
            for (int layer = 0; layer < 2; layer++)
            {
                float ringRadius = radius * grow * (layer == 0 ? 1f : 0.65f);
                Color color = (layer == 0 ? Tuning.HotColor(3) : Tuning.GlowColor(3)) * ((layer == 0 ? 0.9f : 0.6f) * fade);
                int count = Math.Max(20, (int)(MathHelper.TwoPi * ringRadius / (grid * 2f)));
                for (int i = 0; i < count; i++)
                {
                    Vector2 spot = center + (MathHelper.TwoPi * i / count).ToRotationVector2() * ringRadius;
                    IceFx.DrawPixel(spot, grid * (2f + 3f * (1f - u)), color, grid);
                }
            }
            return false;
        }
    }
}