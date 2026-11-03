using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Hit testing for a weapon that is a moving line (a spear shaft, a lance): is a target's box touched by the segment from..to with a
    /// given thickness, also swept from where the segment was last tick, so a fast thrust cannot skip over a thin enemy.
    /// </summary>
    public static class SegmentHit
    {
        public static bool Overlaps(Rectangle box, Vector2 from, Vector2 to, float thickness)
        {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), from, to, thickness, ref point);
        }

        // The segment moved from (prevFrom, prevTo) to (from, to) this tick; test a few steps in between.
        public static bool Sweep(Rectangle box, Vector2 prevFrom, Vector2 prevTo, Vector2 from, Vector2 to, float thickness, int steps)
        {
            steps = Math.Max(1, steps);
            for (int i = 1; i <= steps; i++)
            {
                float fraction = i / (float)steps;
                if (Overlaps(box, Vector2.Lerp(prevFrom, from, fraction), Vector2.Lerp(prevTo, to, fraction), thickness))
                    return true;
            }
            return false;
        }

        // The point of the segment closest to a position (where a hit spark should appear).
        public static Vector2 ClosestPoint(Vector2 from, Vector2 to, Vector2 position)
        {
            Vector2 line = to - from;
            float lengthSquared = line.LengthSquared();
            if (lengthSquared < 0.0001f)
                return from;
            float t = MathHelper.Clamp(Vector2.Dot(position - from, line) / lengthSquared, 0f, 1f);
            return from + line * t;
        }

        // Every enemy may be hit again (a new thrust starts).
        public static void ClearLocalImmunity(Projectile projectile)
        {
            for (int i = 0; i < projectile.localNPCImmunity.Length; i++)
                projectile.localNPCImmunity[i] = 0;
        }
    }
}