using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The geometry helpers of the Artery's loop (pure math, no state): straight pieces of string and a smooth spline through points.
    /// The loop itself is not drawn from a formula: it is the yoyo's own flight path (ManOnTrapezeTrick), drawn as a spline.
    /// </summary>
    public static class ArteryLoop
    {
        // Straight pieces from 'from' to 'to', about 'step' pixels long (the string's twist is drawn per piece).
        public static void AddLine(List<Vector2> points, Vector2 from, Vector2 to, float step)
        {
            int count = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(from, to) / Math.Max(1f, step)));
            for (int i = 0; i <= count; i++)
                points.Add(Vector2.Lerp(from, to, i / (float)count));
        }

        // A smooth Catmull-Rom curve through 'through' (the first and the last point are the curve's ends), 'steps' pieces between two points.
        public static void AddSpline(List<Vector2> points, List<Vector2> through, int steps)
        {
            int n = through.Count;
            if (n == 0)
                return;
            steps = Math.Max(1, steps);
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 p0 = through[Math.Max(i - 1, 0)];
                Vector2 p1 = through[i];
                Vector2 p2 = through[i + 1];
                Vector2 p3 = through[Math.Min(i + 2, n - 1)];
                for (int s = 0; s < steps; s++)
                    points.Add(Vector2.CatmullRom(p0, p1, p2, p3, s / (float)steps));
            }
            points.Add(through[n - 1]);
        }
    }
}