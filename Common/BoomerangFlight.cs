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
    }
}