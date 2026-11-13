using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.FlamingMace
{
    /// <summary>
    /// The Flaming Mace's flail head: the shared flail loop (Common/Flail/FlailProjectile.cs) plus the Flaming Mace's own parts: its numbers
    /// and effects, the burn on every hit, and the tier 3 special: the regular elliptical throw (apex at the cursor, reach up to 30 tiles),
    /// just much faster, with a burning trail. The head pierces every enemy on its way and a small fireball (FlamingMaceBlastProjectile)
    /// explodes on each enemy it passes through. After the throw the head is at the hand and the loop ends, or restarts if the button is held.
    /// MULTIPLAYER: the owner's client decides everything (the shared loop's rules). Where the head is during the special is a pure
    /// function of the aim (ai[1] / ai[2]) and the ticks since the special began, so every machine draws the same throw with no per-tick
    /// sync; only the owner's client spawns the fireballs (normal synced projectiles) and ends the special.
    /// </summary>
    public class FlamingMaceFlailProjectile : FlailProjectile
    {
        // Vanilla art until a FlamingMaceFlailProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FlamingMace;

        protected override int ItemType => ItemID.FlamingMace;
        protected override FlailTuning Tuning => FlamingMaceTuning.Instance;
        protected override FlailFx Fx => FlamingMaceFx.Instance;
        protected override PixelTrailStyle CreateTrailStyle(int tier) { return new FlamingMaceTrailStyle(tier); }
        protected override bool SweepsPath => CurrentPhase == Phase.Charge || CurrentPhase == Phase.Special;  // the special is far faster than the head is wide

        static FlamingMaceTuning Mace => FlamingMaceTuning.Instance;

        int specialTicks;   // per machine: how long the whole throw takes, from the aim and the special speed

        // ---- the special ----

        protected override void BeginSpecial(Player owner)
        {
            // The whole ellipse (out and back) divided by the special speed.
            float a = AimReach * 0.5f;
            float b = a * Tuning.EllipseWidthRatio;
            float perimeter = MathHelper.Pi * (3f * (a + b) - (float)Math.Sqrt((3f * a + b) * (a + 3f * b)));
            specialTicks = Math.Max(Math.Max(1, Mace.SpecialMinTicks), (int)Math.Ceiling(perimeter / Math.Max(1f, Mace.SpecialThrowSpeed)));
            if (IsOwner)
                Projectile.localNPCHitCooldown = Math.Max(1, (int)(specialTicks * Mace.SpecialHitCooldownFraction));
        }

        protected override void UpdateSpecial(Player owner)
        {
            if (phaseTimer >= specialTicks)
            {
                Projectile.Center = owner.MountedCenter;
                if (IsOwner)
                    EndSpecial();
                return;
            }

            // The regular throw's ellipse (hand, apex at the cursor, hand), flown in specialTicks.
            float u = (phaseTimer + 1) / (float)specialTicks;
            Vector2 oldCenter = Projectile.Center;
            Projectile.Center = EllipsePoint(u * MathHelper.TwoPi, owner.MountedCenter);
            trail?.Add(Projectile.Center);
            Fx.FlightDust(Projectile.Center, Projectile.Center - oldCenter, 3);
            if (phaseTimer == specialTicks / 2)
                Fx.ApexJingle(Projectile.Center, 3);
            Lighting.AddLight(Projectile.Center, 1.4f, 0.8f, 0.25f);
        }

        // ---- hits ----

        // Owner's client: every hit burns; during the special every enemy the head passes through also gets a small fireball.
        protected override void OnFlailHit(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, Mace.BurnTicks);
            if (CurrentPhase != Phase.Special)
                return;
            int damage = Math.Max(1, (int)(lockedDamage * Mace.BlastDamageMultiplier));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, ModContent.ProjectileType<FlamingMaceBlastProjectile>(),
                damage, Projectile.knockBack * Mace.BlastKnockbackMultiplier, Projectile.owner);
        }
    }
}