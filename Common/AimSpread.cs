using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Random-looking angle variance that every machine computes identically from a seed (carried in the projectile's spawn packet) and
    /// an index (which thrust or shot this is), so nothing about the jitter needs syncing. Successive indexes lean to opposite sides, so a
    /// burst fans out instead of stabbing the same line twice, and the size varies between 35% and 100% of the maximum.
    /// </summary>
    public static class AimSpread
    {
        // Radians, in the same frame as the aim it is added to.
        public static float Offset(int seed, int index, float maxDegrees)
        {
            unchecked
            {
                uint first = Mix((uint)seed * 2654435761u + 0x9E3779B9u);
                bool positive = ((first & 1u) == 0u) == (index % 2 == 0);
                uint hash = Mix(first ^ ((uint)index * 0x85EBCA6Bu + 1u));
                float magnitude = 0.35f + 0.65f * ((hash & 0xFFFFu) / 65535f);
                return MathHelper.ToRadians(maxDegrees) * magnitude * (positive ? 1f : -1f);
            }
        }

        static uint Mix(uint x)
        {
            unchecked
            {
                x ^= x >> 16;
                x *= 0x7FEB352Du;
                x ^= x >> 15;
                x *= 0x846CA68Bu;
                x ^= x >> 16;
                return x;
            }
        }
    }
}