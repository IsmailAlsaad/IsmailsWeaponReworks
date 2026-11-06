using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// The evil explosion of a thrust that carries all the orbs: a short-lived circle that damages every enemy in it once and puts
    /// Shadowflame on them. Same pattern as the Enchanted Boomerang's StardustBurstProjectile.
    /// MULTIPLAYER: spawned by the owner's client (so it is synced like any projectile); every machine plays the visuals and sound from the
    /// synced spawn on its first tick, the owner's client does the damage and the buff (NPC.AddBuff syncs), and the other machines add the
    /// hit sparks themselves.
    /// </summary>
    public class DarkLanceBlastProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        float Age => Projectile.localAI[0];
        float Scale => Projectile.ai[1] > 0f ? Projectile.ai[1] : DarkLanceTuning.BlastScale((int)Projectile.ai[0]);   // ai[0] = gathered orbs, or ai[1] = an explicit size (fraction of the full blast, the flame embers use it); set in the spawn (every machine agrees)
        float Radius => DarkLanceTuning.BlastRadius * Scale;

        public override void SetDefaults()
        {
            Projectile.width = (int)(DarkLanceTuning.BlastRadius * 2f);
            Projectile.height = (int)(DarkLanceTuning.BlastRadius * 2f);
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Math.Max(2, DarkLanceTuning.BlastLifeTicks);
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right), MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= Radius * Radius;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                DarkLanceFx.Blast(Projectile.Center, Radius, Scale);
                if (Projectile.owner == Main.myPlayer)
                    AreaEffect.ApplyBuff(Projectile.Center, Radius, BuffID.ShadowFlame, DarkLanceTuning.BlastBuffTicks);
                else
                    RemoteHitTracker.TouchOnce(Projectile, npc => ThrustFx.HitSpark(npc.Center, DarkLanceTuning.Palette)); // the owner gets these from OnHitNPC
            }
            Projectile.localAI[0]++;
            Lighting.AddLight(Projectile.Center, DarkLanceTuning.Palette.Mid.ToVector3() * 1.5f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ThrustFx.HitSpark(target.Center, DarkLanceTuning.Palette);
        }

        // Two expanding rings of pixel squares and a big soft flash; the blobs of the burst are drawn by the metaball layer.
        public override bool PreDraw(ref Color lightColor)
        {
            float life = Math.Max(1, DarkLanceTuning.BlastLifeTicks);
            float u = MathHelper.Clamp(Age / life, 0f, 1f);
            float fade = (1f - u) * (1f - u);
            float grid = DarkLanceTuning.PixelSize;
            ThrustPalette palette = DarkLanceTuning.Palette;
            for (int ring = 0; ring < 2; ring++)
            {
                float radius = Radius * (ring == 0 ? 1f : 0.6f) * (1f - (1f - u) * (1f - u));
                int count = Math.Max(12, (int)(MathHelper.TwoPi * radius / grid));
                Color color = (ring == 0 ? palette.Mid : palette.Core) * (0.9f * fade);
                for (int i = 0; i < count; i++)
                {
                    Vector2 spot = Projectile.Center + (MathHelper.TwoPi * i / count).ToRotationVector2() * radius;
                    IceFx.DrawPixel(spot, grid * (1f + 2f * (1f - u)), color, grid);
                }
            }
            DarkLanceFx.DrawGlow(Projectile.Center, Radius * (0.5f + 0.5f * u), fade);
            return false;
        }
    }
}