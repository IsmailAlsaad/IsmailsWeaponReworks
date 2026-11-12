using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BlueMoon
{
    /// <summary>
    /// The Blue Moon's flail head: the shared flail loop (Common/Flail/FlailProjectile.cs) plus the Blue Moon's own parts: its numbers and
    /// effects, and the tier 3 special: SIX very fast throws from the hand toward the cursor, alternating above and below the aim line (up
    /// first), each an out-and-back stab; every enemy a throw hits is hit by a water magic explosion (BlueMoonBlastProjectile). After the
    /// last throw the head is at the hand and the loop ends, or restarts if the button is held.
    /// MULTIPLAYER: the owner's client decides everything (the shared loop's rules). Where the head is during the special is a pure function
    /// of the aim (ai[1] / ai[2]) and the ticks since the special began, so every machine draws the same six throws with no per-tick sync;
    /// only the owner's client spawns the water explosions (normal synced projectiles) and ends the special.
    /// </summary>
    public class BlueMoonFlailProjectile : FlailProjectile
    {
        // Vanilla art until a BlueMoonFlailProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BlueMoon;

        protected override int ItemType => ItemID.BlueMoon;
        protected override FlailTuning Tuning => BlueMoonTuning.Instance;
        protected override FlailFx Fx => BlueMoonFx.Instance;
        protected override bool SweepsPath => CurrentPhase == Phase.Charge || CurrentPhase == Phase.Special;  // the special's throws are far faster than the head is wide

        static BlueMoonTuning Blue => BlueMoonTuning.Instance;

        // ---- the special ----

        protected override void BeginSpecial(Player owner)
        {
            if (IsOwner)
                Projectile.localNPCHitCooldown = Math.Max(1, Blue.SpecialHitCooldownTicks); // every throw can hit the same enemy again
        }

        protected override void UpdateSpecial(Player owner)
        {
            int ticks = Math.Max(1, Blue.SpecialThrowTicks);
            int index = phaseTimer / ticks;
            if (index >= Blue.SpecialThrows)
            {
                Projectile.Center = owner.MountedCenter;
                if (IsOwner)
                    EndSpecial();
                return;
            }

            int step = phaseTimer % ticks;
            if (step == 0)
            {
                NewTrail(3);                                 // each throw gets its own trail
                Fx.ThrowWhoosh(Projectile.Center, 3);
            }

            // Out and back along a line above (even throws) or below (odd throws) the aim line.
            float u = (step + 1) / (float)ticks;
            int upSign = AimAngle.ToRotationVector2().X >= 0f ? -1 : 1; // rotating a right-pointing aim by a negative angle turns it up, a left-pointing one by a positive angle
            float side = index % 2 == 0 ? 1f : -1f;
            float angle = AimAngle + upSign * side * Blue.SpecialSpread;
            float extend = (float)Math.Sin(u * MathHelper.Pi);
            Vector2 oldCenter = Projectile.Center;
            Projectile.Center = owner.MountedCenter + angle.ToRotationVector2() * AimReach * extend;
            trail?.Add(Projectile.Center);
            Fx.FlightDust(Projectile.Center, Projectile.Center - oldCenter, 3);
            Lighting.AddLight(Projectile.Center, 0.3f, 0.7f, 1.3f);
        }

        // ---- hits ----

        // Owner's client: a water magic explosion on every enemy the special's head hits.
        protected override void OnFlailHit(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CurrentPhase != Phase.Special)
                return;
            int damage = Math.Max(1, (int)(lockedDamage * Blue.BlastDamageMultiplier));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<BlueMoonBlastProjectile>(),
                damage, Projectile.knockBack * Blue.BlastKnockbackMultiplier, Projectile.owner);
        }
    }
}