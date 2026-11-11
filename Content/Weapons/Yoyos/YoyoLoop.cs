using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The geometry of the Man on Trapeze loop (pure math, no state): the smooth curve the yoyo takes from where it caught the enemy back to the
    /// string line, and the noose, the taut string around the enemy.
    /// THE NOOSE: the yoyo is the apex; the string leaves it along one tangent of a circle around the enemy, follows the far side of the
    /// circle and comes back along the other tangent. That is exactly the shape of a pulled-tight loop, and shrinking the circle shrinks it.
    /// </summary>
    public static class YoyoLoop
    {
        // A cubic Bezier curve (u from 0 to 1).
        public static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u)
        {
            float v = 1f - u;
            return p0 * (v * v * v) + p1 * (3f * v * v * u) + p2 * (3f * v * u * u) + p3 * (u * u * u);
        }

        public static float Smooth(float u)
        {
            u = MathHelper.Clamp(u, 0f, 1f);
            return u * u * (3f - 2f * u);
        }

        // Straight pieces from 'from' to 'to', about 'step' pixels long (the string's twist is drawn per piece).
        public static void AddLine(List<Vector2> points, Vector2 from, Vector2 to, float step)
        {
            int count = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(from, to) / Math.Max(1f, step)));
            for (int i = 0; i <= count; i++)
                points.Add(Vector2.Lerp(from, to, i / (float)count));
        }

        // The noose: apex (the yoyo) -> tangent -> the far side of the circle -> tangent -> apex. Nothing is added while the circle is
        // tiny or the apex is not outside it.
        public static void AddNoose(List<Vector2> points, Vector2 apex, Vector2 center, float radius, float step, int arcSamples)
        {
            Vector2 toApex = apex - center;
            float distance = toApex.Length();
            if (radius < 1.5f || distance <= radius + 0.5f)
                return;
            float theta = (float)Math.Atan2(toApex.Y, toApex.X);
            float phi = (float)Math.Acos(radius / distance);
            Vector2 a = center + new Vector2((float)Math.Cos(theta + phi), (float)Math.Sin(theta + phi)) * radius;
            Vector2 b = center + new Vector2((float)Math.Cos(theta - phi), (float)Math.Sin(theta - phi)) * radius;
            AddLine(points, apex, a, step);
            int samples = Math.Max(6, arcSamples);
            float sweep = MathHelper.TwoPi - 2f * phi;   // the long way round, through the side opposite the apex
            for (int i = 1; i < samples; i++)
            {
                float angle = theta + phi + sweep * i / samples;
                points.Add(center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius);
            }
            AddLine(points, b, apex, step);
        }
    }
}