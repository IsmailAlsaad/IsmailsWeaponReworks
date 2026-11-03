using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Evenly spaced things turning around a point: the Rotted Fork's spin-up ring now, the Dark Lance's flame balls in patch 6. Pure
    /// math with no state, so every machine computes the same ring from the same numbers and nothing has to be synced.
    /// (Patch 6 adds Rank, a projectile's place among its owner's projectiles of the same type, when the flame balls need it.)
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
    }
}