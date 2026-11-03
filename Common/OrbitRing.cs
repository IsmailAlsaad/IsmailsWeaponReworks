using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Evenly spaced things turning around a point: the Rotted Fork's spin-up ring now, the Dark Lance's flame balls in patch 6. Pure
    /// math with no state, so every machine computes the same ring from the same numbers and nothing has to be synced.
    /// Rank gives a projectile its place among its owner's projectiles of the same type (the Dark Lance's flame balls use it).
    /// </summary>
    public static class OrbitRing
    {
        // The angle (radians, in the world's y-down frame, positive = clockwise on screen) of 'slot' among 'count' evenly spaced
        // things after 'age' ticks of turning 'spin' radians per tick.
        public static float Angle(float age, int slot, int count, float spin)
        {
            return age * spin + MathHelper.TwoPi * slot / Math.Max(1, count);
        }

        public static Vector2 Position(Vector2 center, float radius, float angle)
        {
            return center + angle.ToRotationVector2() * radius;
        }

        // The place of 'projectile' (0 = the oldest) among its owner's live projectiles of the same type, ordered by the spawn serial in
        // ai[1] (the owner writes the same serial into the spawn on every machine, so every machine agrees), and how many there are.
        // 'counts' can leave some out (for example orbs that are already popping); the caller must not ask for a projectile it left out.
        public static int Rank(Projectile projectile, out int count, Func<Projectile, bool> counts = null)
        {
            int rank = 0;
            count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != projectile.owner || other.type != projectile.type)
                    continue;
                if (counts != null && !counts(other))
                    continue;
                count++;
                if (other.whoAmI != projectile.whoAmI && (other.ai[1] < projectile.ai[1] || (other.ai[1] == projectile.ai[1] && other.whoAmI < projectile.whoAmI)))
                    rank++;
            }
            return rank;
        }
    }
}