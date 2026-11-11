using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Cascade
{
    /// <summary>
    /// The tiny hot explosion of a charged Pop the Clutch: spawned by the owner's client at the spot the yoyo hit an enemy (so tModLoader
    /// syncs it), ai[0] = the enemy that was hit. Its round area hurts every OTHER enemy near it once for a few ticks (the hit one already took
    /// the yoyo's hit) and sets them On Fire. Every machine plays the same burst, sound and camera punch on its first tick. The owner's client
    /// detects the hits. No custom packets. The numbers are in CascadeTuning.
    /// </summary>
    public class CascadeBlastProjectile : ModProjectile
    {
        // Never drawn (dust only): any vanilla projectile texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Fireball;

        readonly CascadeTuning tuning = new CascadeTuning();
        int ticks;

        int HitEnemy => (int)Projectile.ai[0];   // the enemy the yoyo hit: the blast skips it

        public override void SetDefaults()
        {
            Projectile.width = (int)(tuning.BlastRadiusPx * 2f);
            Projectile.height = (int)(tuning.BlastRadiusPx * 2f);
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = tuning.BlastTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;   // every enemy once
        }

        public override void AI()
        {
            if (ticks == 0)
            {
                Burst(Projectile.Center);
                CameraShake.Punch(Projectile.Center, tuning.BlastShake, 8, "CascadeBlast");
            }
            float fade = Projectile.timeLeft / (float)System.Math.Max(1, tuning.BlastTicks);
            Lighting.AddLight(Projectile.Center, 1.4f * fade, 0.55f * fade, 0.1f * fade);
            ticks++;
        }

        // Every machine: a burst of fire thrown outward, smoke and a flame sound.
        void Burst(Vector2 center)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f, Pitch = 0.2f, PitchVariance = 0.15f }, center);
            for (int i = 0; i < tuning.BlastSparks; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Dust dust = Dust.NewDustPerfect(center + direction * (tuning.BlastRadiusPx * 0.2f), DustID.Torch, direction * Main.rand.NextFloat(3f, 7f), 100, default, Main.rand.NextFloat(1.4f, 2.2f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 3; i++)
            {
                Dust core = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(6f, 6f), DustID.Torch, Main.rand.NextVector2Circular(1f, 1f), 100, default, Main.rand.NextFloat(2f, 2.6f));
                core.noGravity = true;
            }
            for (int i = 0; i < tuning.BlastSmoke; i++)
                Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(tuning.BlastRadiusPx * 0.5f, tuning.BlastRadiusPx * 0.5f), DustID.Smoke, Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0f, -1f), 150, default, Main.rand.NextFloat(1f, 1.5f));
        }

        // The enemy the yoyo hit is skipped.
        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == HitEnemy ? false : (bool?)null;
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float radius = tuning.BlastRadiusPx;
            return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
        }

        // Owner's client only.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, tuning.BlastOnFireTicks);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
