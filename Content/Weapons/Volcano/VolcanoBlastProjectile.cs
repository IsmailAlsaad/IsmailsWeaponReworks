using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// The small lava explosion of a fully charged swing: every enemy the blade hits bursts, and the round area hurts everything in it once
    /// for a few ticks and sets it on fire. Spawned by the owner at the spot the blade hit (so tModLoader syncs it); every machine plays the
    /// same sparks, lava and sound on its first tick. The owner's client detects the hits. No custom packets.
    /// </summary>
    public class VolcanoBlastProjectile : ModProjectile
    {
        // Never drawn (dust and lava only): any vanilla projectile texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Fireball;

        int ticks;

        public override void SetDefaults()
        {
            Projectile.width = VolcanoTuning.BlastSize;
            Projectile.height = VolcanoTuning.BlastSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = VolcanoTuning.BlastTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        public override void AI()
        {
            if (ticks == 0)
            {
                VolcanoFx.Blast(Projectile.Center, VolcanoTuning.BlastSize * 0.5f);
                CameraShake.Punch(Projectile.Center, VolcanoTuning.BlastShake, 12, "VolcanoBlast");
            }
            float fade = Projectile.timeLeft / (float)System.Math.Max(1, VolcanoTuning.BlastTicks);
            Lighting.AddLight(Projectile.Center, 1.6f * fade, 0.7f * fade, 0.15f * fade);
            ticks++;
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float radius = VolcanoTuning.BlastSize * 0.5f;
            return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
        }

        // Owner's client only.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, VolcanoTuning.BlastOnFireTicks);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}