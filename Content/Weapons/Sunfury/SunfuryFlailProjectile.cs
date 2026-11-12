using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>
    /// The Sunfury's flail head: the shared flail loop (Common/Flail/FlailProjectile.cs) plus the Sunfury's own parts: its numbers and
    /// effects, the burn on every hit, and the tier 3 special: the head flies straight at the cursor and detonates in a sun-like explosion
    /// (SunfuryBlastProjectile), hangs hidden inside it, and comes back (the loop ends, or restarts if the button is held when it is caught).
    /// MULTIPLAYER: the shared loop's rules. The special's flight is deterministic on every machine (a straight line to the target); the
    /// owner's client decides the detonation and spawns the explosion as a normal projectile.
    /// </summary>
    public class SunfuryFlailProjectile : FlailProjectile
    {
        // Vanilla art until a SunfuryFlailProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Sunfury;

        SunfuryFlailTuning tuning;
        SunfuryFlailFx fx;
        Vector2 specialTarget;

        protected override int ItemType => ItemID.Sunfury;
        protected override FlailTuning Tuning => tuning ??= new SunfuryFlailTuning();
        protected override FlailFx Fx => fx ??= new SunfuryFlailFx();
        protected override PixelTrailStyle CreateTrailStyle(int tier) { return new SunfuryTrailStyle(tier); }

        // ---- the special ----

        protected override void BeginSpecial(Player owner)
        {
            specialTarget = owner.MountedCenter + AimAngle.ToRotationVector2() * AimReach;
        }

        protected override void UpdateSpecial(Player owner)
        {
            Vector2 toTarget = specialTarget - Projectile.Center;
            float speed = SunfuryTuning.SpecialSpeed;
            bool arrived = toTarget.Length() <= speed;
            Vector2 oldCenter = Projectile.Center;
            Projectile.Center = arrived ? specialTarget : Projectile.Center + toTarget.SafeNormalize(Vector2.UnitX) * speed;
            trail?.Add(Projectile.Center);
            SunfuryFx.FlightDust(Projectile.Center, Projectile.Center - oldCenter, 3);
            Lighting.AddLight(Projectile.Center, 1.4f, 0.9f, 0.3f);

            if (!IsOwner)
                return;
            bool blocked = SunfuryTuning.SpecialStopsOnTiles && phaseTimer > 2 && Collision.SolidCollision(Projectile.Center - new Vector2(6f), 12, 12);
            if (arrived || blocked)
                Detonate();
        }

        // Owner only: the sun goes off where the head is, and the head starts its way home.
        void Detonate()
        {
            int damage = System.Math.Max(1, (int)(lockedDamage * SunfuryTuning.BlastDamageMultiplier));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SunfuryBlastProjectile>(),
                damage, Projectile.knockBack * SunfuryTuning.BlastKnockbackMultiplier, Projectile.owner);
            EndSpecial();
        }

        // ---- hits ----

        protected override void OnFlailHit(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, SunfuryTuning.BurnTicks);
        }
    }
}