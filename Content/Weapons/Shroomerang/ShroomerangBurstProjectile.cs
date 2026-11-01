using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Shroomerang
{
    /// <summary>
    /// The puff of glowing spores a mushroom explodes into. A short-lived circle that damages every enemy in it once.
    /// MULTIPLAYER: spawned by the owner's client (so it is synced like any projectile); every machine plays the visuals and sound from
    /// the synced spawn on its first tick, the owner's client does the damage, and the other machines add the hit sparks themselves.
    /// localAI[0] = age (visual only).
    /// </summary>
    public class ShroomerangBurstProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        float Age => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = (int)(ShroomerangTuning.BurstRadius * 2f);
            Projectile.height = (int)(ShroomerangTuning.BurstRadius * 2f);
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = ShroomerangTuning.BurstLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= ShroomerangTuning.BurstRadius * ShroomerangTuning.BurstRadius;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                ShroomerangFx.PlayBurst(Projectile.Center);
                ShroomerangFx.SporePuff(Projectile.Center, 22, 6f, 1.5f);
                ShroomerangFx.SporeRing(Projectile.Center, 24, 10f, 5f);
                ShroomerangFx.SporeRing(Projectile.Center, 16, 36f, 2.5f);
                if (Projectile.owner != Main.myPlayer)
                    RemoteHitTracker.TouchOnce(Projectile, npc => ShroomerangFx.HitSparks(npc.Center)); // the owner gets these from OnHitNPC
            }
            Projectile.localAI[0]++;

            float fade = 1f - Age / Math.Max(1, ShroomerangTuning.BurstLifeTicks);
            Lighting.AddLight(Projectile.Center, 0.3f * fade, 0.7f * fade, 1.2f * fade);
            ShroomerangFx.SporeCloud(Projectile.Center, ShroomerangTuning.BurstRadius * 0.8f, 3); // spores keep drifting while it lasts
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ShroomerangFx.HitSparks(target.Center);
        }

        // Two expanding rings of pixel squares (additive), a quick flash on top of the dust.
        public override bool PreDraw(ref Color lightColor)
        {
            float life = Math.Max(1, ShroomerangTuning.BurstLifeTicks);
            float u = MathHelper.Clamp(Age / life, 0f, 1f);
            float fade = (1f - u) * (1f - u);
            float grid = ShroomerangTuning.PixelSize;
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = ShroomerangTuning.BurstRadius * (ring == 0 ? 1f : 0.6f) * (1f - (1f - u) * (1f - u));
                int count = Math.Max(12, (int)(MathHelper.TwoPi * radius / grid));
                Color color = ShroomerangTuning.GlowColor * (0.9f * fade);
                for (int i = 0; i < count; i++)
                {
                    Vector2 spot = Projectile.Center + (MathHelper.TwoPi * i / count).ToRotationVector2() * radius;
                    IceFx.DrawPixel(spot, grid * (1f + 2f * (1f - u)), color, grid);
                }
            }
            return false;
        }
    }
}