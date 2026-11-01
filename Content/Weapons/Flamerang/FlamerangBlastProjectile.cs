using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Flamerang
{
    /// <summary>
    /// The fire explosion of the Flamerang: a short-lived circle that damages every enemy in it once and sets it on fire.
    /// MULTIPLAYER: spawned by the owner's client (so it is synced like any projectile); every machine plays the visuals and sound
    /// on its first tick, the owner's client does the damage and the burning, and the other machines add the hit sparks themselves.
    /// </summary>
    public class FlamerangBlastProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        float Age => Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = (int)(FlamerangTuning.BlastRadius * 2f);
            Projectile.height = (int)(FlamerangTuning.BlastRadius * 2f);
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = FlamerangTuning.BlastLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= FlamerangTuning.BlastRadius * FlamerangTuning.BlastRadius;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                FlamerangFx.Blast(Projectile.Center, FlamerangTuning.BlastRadius);
                if (Projectile.owner != Main.myPlayer)
                    RemoteHitTracker.TouchOnce(Projectile, npc => FlamerangFx.HitSparks(npc.Center)); // the owner gets these from OnHitNPC
            }
            Projectile.localAI[0]++;
            Lighting.AddLight(Projectile.Center, 1.4f, 0.8f, 0.25f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            FlamerangFx.HitSparks(target.Center);
            FlamerangBurn.Apply(target);
        }

        // An expanding ring of fire made of pixel squares, on top of the dust.
        public override bool PreDraw(ref Color lightColor)
        {
            float life = Math.Max(1, FlamerangTuning.BlastLifeTicks);
            float u = MathHelper.Clamp(Age / life, 0f, 1f);
            float fade = (1f - u) * (1f - u);
            float grid = FlamerangTuning.PixelSize;
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = FlamerangTuning.BlastRadius * (ring == 0 ? 1f : 0.65f) * (1f - (1f - u) * (1f - u));
                int count = Math.Max(12, (int)(MathHelper.TwoPi * radius / grid));
                Color color = (ring == 0 ? FlamerangTuning.FireColor : FlamerangTuning.HotColor) * (0.9f * fade);
                for (int i = 0; i < count; i++)
                {
                    Vector2 spot = Projectile.Center + (MathHelper.TwoPi * i / count).ToRotationVector2() * radius;
                    IceFx.DrawPixel(spot, grid * (1.5f + 3f * (1f - u)), color, grid);
                }
            }
            return false;
        }
    }
}