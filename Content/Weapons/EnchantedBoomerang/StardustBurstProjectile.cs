using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedBoomerang
{
    /// <summary>
    /// The stardust explosion where a normal and an ethereal boomerang meet. A short-lived circle that damages every enemy in it once.
    /// MULTIPLAYER: spawned by the owner's client (so it is synced like any projectile); every machine plays the visuals and sound from
    /// the synced spawn on its first tick, the owner's client does the damage, and the other machines add the hit sparks themselves.
    /// </summary>
    public class StardustBurstProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        float Age => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = (int)(EnchantedBoomerangTuning.BurstRadius * 2f);
            Projectile.height = (int)(EnchantedBoomerangTuning.BurstRadius * 2f);
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = EnchantedBoomerangTuning.BurstLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= EnchantedBoomerangTuning.BurstRadius * EnchantedBoomerangTuning.BurstRadius;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                EnchantedBoomerangFx.Burst(Projectile.Center, EnchantedBoomerangTuning.BurstRadius);
                if (Projectile.owner != Main.myPlayer)
                    RemoteHitTracker.TouchOnce(Projectile, npc => EnchantedBoomerangFx.HitSparks(npc.Center, false)); // the owner gets these from OnHitNPC
            }
            Projectile.localAI[0]++;
            Lighting.AddLight(Projectile.Center, 1f, 0.9f, 0.6f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            EnchantedBoomerangFx.HitSparks(target.Center, false);
        }

        // Two expanding rings of pixel squares (additive), a quick flash on top of the dust.
        public override bool PreDraw(ref Color lightColor)
        {
            float life = Math.Max(1, EnchantedBoomerangTuning.BurstLifeTicks);
            float u = MathHelper.Clamp(Age / life, 0f, 1f);
            float fade = (1f - u) * (1f - u);
            float grid = EnchantedBoomerangTuning.PixelSize;
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = EnchantedBoomerangTuning.BurstRadius * (ring == 0 ? 1f : 0.6f) * (1f - (1f - u) * (1f - u));
                int count = Math.Max(12, (int)(MathHelper.TwoPi * radius / grid));
                Color color = (ring == 0 ? EnchantedBoomerangTuning.NormalColor : EnchantedBoomerangTuning.EtherealColor) * (0.9f * fade);
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