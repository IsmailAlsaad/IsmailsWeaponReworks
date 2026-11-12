using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Meatball
{
    /// <summary>
    /// The Meatball's flail head: the shared flail loop (Common/Flail/FlailProjectile.cs) plus the Meatball's own parts: its numbers and
    /// effects, and the tier 3 special: the head flies straight at the cursor (hurting enemies on the way); at the apex (the cursor, or the
    /// first tile it touches before that) the ball tears loose into a MeatballHeadProjectile that keeps flying in the same direction, and the
    /// snapped chain whips back to the hand with the head hidden. The loop then ends (it never restarts on hold), and the hand stays empty
    /// for MeatballTuning.RegrowTicks.
    /// MULTIPLAYER: the shared loop's rules. The special's flight is deterministic on every machine (a straight line to the target); the
    /// owner's client decides the detach and spawns the ball as a normal synced projectile, and counts the regrow time (MeatballPlayer).
    /// </summary>
    public class MeatballFlailProjectile : FlailProjectile
    {
        // Vanilla art until a MeatballFlailProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.TheMeatball;

        protected override int ItemType => ItemID.TheMeatball;
        protected override FlailTuning Tuning => MeatballTuning.Instance;
        protected override FlailFx Fx => MeatballFx.Instance;
        protected override bool SweepsPath => CurrentPhase == Phase.Charge || CurrentPhase == Phase.Special;  // the special's head is far faster than it is wide
        protected override bool HeadHidden => CurrentPhase == Phase.Retract;   // the ball is gone: only the snapped chain whips back
        protected override bool RestartsOnHold => false;                       // the hand stays empty after the special

        static MeatballTuning Meat => MeatballTuning.Instance;

        Vector2 specialTarget;

        // ---- the special ----

        protected override void BeginSpecial(Player owner)
        {
            specialTarget = owner.MountedCenter + AimAngle.ToRotationVector2() * AimReach;
        }

        protected override void UpdateSpecial(Player owner)
        {
            Vector2 toTarget = specialTarget - Projectile.Center;
            float speed = Meat.SpecialSpeed;
            bool arrived = toTarget.Length() <= speed;
            Vector2 oldCenter = Projectile.Center;
            Projectile.Center = arrived ? specialTarget : Projectile.Center + toTarget.SafeNormalize(Vector2.UnitX) * speed;
            trail?.Add(Projectile.Center);
            Fx.FlightDust(Projectile.Center, Projectile.Center - oldCenter, 3);
            Lighting.AddLight(Projectile.Center, 1.1f, 0.12f, 0.15f);

            if (!IsOwner)
                return;
            bool blocked = phaseTimer > 2 && Collision.SolidCollision(Projectile.Center - new Vector2(6f), 12, 12);
            if (arrived || blocked)
                Detach(owner, blocked ? oldCenter : Projectile.Center);
        }

        // Owner only: the ball tears loose where the head is (a step back from the tile if the head ran into one), flying straight on in the
        // direction of the throw. If three balls are already stuck somewhere, the oldest one goes off to make room. The chain whips back.
        void Detach(Player owner, Vector2 at)
        {
            MeatballHeadProjectile.MakeRoom(Projectile.owner, Meat.MaxBalls);
            Vector2 direction = AimAngle.ToRotationVector2();
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, direction * Meat.BallSpeed, ModContent.ProjectileType<MeatballHeadProjectile>(),
                Math.Max(1, lockedDamage), Projectile.knockBack, Projectile.owner);
            owner.GetModPlayer<MeatballPlayer>().RegrowTicks = Meat.RegrowTicks;
            EndSpecial();
        }
    }
}