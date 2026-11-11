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

        // A taut string round round poles (Rock the Baby): it runs from pole to pole along their outer common tangents and bends round
        // every pole whose radius is above zero (the hand and the yoyo are poles of radius zero). 'turn' (+1 or -1) is the side the poles
        // are on: the string turns that way at every corner, so the poles sit INSIDE the bends (a string round pegs). The caller clears
        // 'points'; this adds the whole path.
        public static void AddWrapped(List<Vector2> points, Vector2[] poles, float[] radii, int turn, float step, int arcSamples)
        {
            int count = poles.Length;
            if (count < 2)
                return;
            turn = turn >= 0 ? 1 : -1;
            Vector2[] leave = new Vector2[count];    // where the string leaves each pole
            Vector2[] arrive = new Vector2[count];   // where it arrives at each pole
            for (int i = 0; i < count; i++)
            {
                leave[i] = poles[i];
                arrive[i] = poles[i];
            }
            for (int i = 0; i < count - 1; i++)
            {
                Vector2 d = poles[i + 1] - poles[i];
                float length = d.Length();
                if (length < 0.01f)
                    continue;
                d /= length;
                Vector2 away = new Vector2(d.Y, -d.X) * turn;   // from a pole's center to where the string touches it: the side away from the turn
                float sine = MathHelper.Clamp((radii[i] - radii[i + 1]) / length, -0.98f, 0.98f);   // poles of different sizes: the tangent leans
                float cosine = (float)Math.Sqrt(1f - sine * sine);
                Vector2 normal = d * sine + away * cosine;
                leave[i] = poles[i] + normal * radii[i];
                arrive[i + 1] = poles[i + 1] + normal * radii[i + 1];
            }
            points.Add(leave[0]);
            for (int i = 1; i < count; i++)
            {
                AddLine(points, leave[i - 1], arrive[i], step);
                if (i == count - 1)
                    break;
                if (radii[i] <= 0.5f)
                {
                    points.Add(leave[i]);
                    continue;
                }
                Vector2 center = poles[i];
                Vector2 from = arrive[i] - center;
                Vector2 to = leave[i] - center;
                float start = (float)Math.Atan2(from.Y, from.X);
                float sweep = MathHelper.WrapAngle((float)Math.Atan2(to.Y, to.X) - start);
                if (turn > 0 && sweep < -0.05f)
                    sweep += MathHelper.TwoPi;       // the long way: the string turns the way the pole is on
                else if (turn < 0 && sweep > 0.05f)
                    sweep -= MathHelper.TwoPi;
                else if ((turn > 0 && sweep < 0f) || (turn < 0 && sweep > 0f))
                    sweep = 0f;                      // a straight run: no arc
                int samples = Math.Max(2, (int)(Math.Abs(sweep) / MathHelper.TwoPi * Math.Max(6, arcSamples)));
                for (int k = 1; k <= samples; k++)
                {
                    float angle = start + sweep * k / samples;
                    points.Add(center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radii[i]);
                }
            }
        }
    }
}