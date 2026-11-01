using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Shared boomerang motion. The return-and-catch step was moved here from EnchantedBoomerangBase when the Trimarang needed the
    /// same thing.
    /// </summary>
    public static class BoomerangFlight
    {
        // One tick of a returning boomerang: speed up and steer at the owner's center. Returns true when it has reached the owner;
        // the caller then plays its catch effect and kills its own copy (every machine does, so no kill packet is needed).
        public static bool StepReturn(Projectile projectile, Player owner, float startSpeed, float acceleration, float maxSpeed, float catchDistance)
        {
            float speed = Math.Min(maxSpeed, Math.Max(projectile.velocity.Length(), startSpeed) + acceleration);
            Vector2 toOwner = owner.Center - projectile.Center;
            if (toOwner.Length() <= Math.Max(speed, catchDistance))
                return true;
            projectile.velocity = Vector2.Normalize(toOwner) * speed;
            return false;
        }

        // One tick of a FORCE-based return: instead of pointing the velocity at the owner, add an acceleration toward the owner's
        // CURRENT position to whatever velocity the projectile already has (an impulse from a throw, a bounce or an explosion), scale it by
        // 'drag' (1 = none; a bit below 1 makes the path spiral in instead of orbiting) and clamp it to maxSpeed. The result is a smooth arc
        // that bends toward wherever the owner is now. Returns true when it has reached the owner (same catch rule as StepReturn).
        public static bool StepForceReturn(Projectile projectile, Player owner, float acceleration, float maxSpeed, float drag, float catchDistance)
        {
            Vector2 toOwner = owner.Center - projectile.Center;

            if (toOwner.Length() <= Math.Max(projectile.velocity.Length(), catchDistance))
                return true;
            projectile.velocity += Vector2.Normalize(toOwner) * acceleration;
            projectile.velocity *= drag;
            if (projectile.velocity.Length() > maxSpeed)
                projectile.velocity = Vector2.Normalize(projectile.velocity) * maxSpeed;
            return false;
        }
    }
}