using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// The path the owner draws with the cursor during the Muramasa's draw window (patch 2). Owner's client only: it is never synced.
    /// Begin starts it, Tick samples the cursor once per tick (a point is only laid when the cursor moved DrawSampleSpacing; there is no
    /// distance limit) and returns true on the tick the window ends (button released or time up).
    /// When it ends the path is simplified (Douglas-Peucker, the tolerance grows until at most DrawMaxStrokes strokes are left) into
    /// Strokes: a polyline whose points are the stroke ends (MuramasaIaidoProjectile.Launch turns them into slashes). Draw shows the
    /// glowing path with the time ring at the cursor while the window is open.
    /// </summary>
    public class MuramasaDrawState
    {
        readonly List<Vector2> path = new List<Vector2>();
        List<Vector2> strokes = new List<Vector2>();
        Vector2 cursor;
        int ticks;
        Vector2[] points = new Vector2[0];
        float[] ts = new float[0];
        Vector2[] ringPoints = new Vector2[0];
        float[] ringTs = new float[0];

        public bool Finished { get; private set; }
        public float PathLength { get; private set; }
        // The simplified path: the points between the strokes (Count - 1 strokes). Empty until the window has ended.
        public IList<Vector2> Strokes => strokes;

        float TimeLeft => MathHelper.Clamp(1f - ticks / (float)Math.Max(1, MuramasaTuning.DrawWindowTicks), 0f, 1f);

        // The cursor in the world. There is no distance limit.
        static Vector2 CursorWorld()
        {
            return Main.MouseWorld;
        }

        public void Begin(Player owner)
        {
            cursor = CursorWorld();
            path.Add(cursor);
        }

        // One tick of the window. Returns true on the tick it ends.
        public bool Tick(Player owner, bool held)
        {
            ticks++;
            cursor = CursorWorld();
            if (Vector2.Distance(path[path.Count - 1], cursor) >= Math.Max(1f, MuramasaTuning.DrawSampleSpacing))
            {
                path.Add(cursor);
                MuramasaFx.DrawDust(cursor);
            }
            Lighting.AddLight(cursor, 0.2f, 0.45f, 0.9f);

            if (held && ticks < MuramasaTuning.DrawWindowTicks)
                return false;

            if (path[path.Count - 1] != cursor)
                path.Add(cursor);
            PathLength = 0f;
            for (int i = 1; i < path.Count; i++)
                PathLength += Vector2.Distance(path[i - 1], path[i]);
            strokes = Simplify(path, MuramasaTuning.DrawSimplifyTolerance, MuramasaTuning.DrawMaxStrokes);
            Finished = true;
            return true;
        }

        // ---- simplification (Douglas-Peucker) ----

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.LengthSquared();
            if (lengthSquared < 0.0001f)
                return Vector2.Distance(p, a);
            float t = MathHelper.Clamp(Vector2.Dot(p - a, ab) / lengthSquared, 0f, 1f);
            return Vector2.Distance(p, a + ab * t);
        }

        static void Reduce(List<Vector2> pts, int first, int last, float tolerance, bool[] keep)
        {
            float farthest = 0f;
            int index = -1;
            for (int i = first + 1; i < last; i++)
            {
                float distance = DistanceToSegment(pts[i], pts[first], pts[last]);
                if (distance > farthest)
                {
                    farthest = distance;
                    index = i;
                }
            }
            if (index >= 0 && farthest > tolerance)
            {
                keep[index] = true;
                Reduce(pts, first, index, tolerance, keep);
                Reduce(pts, index, last, tolerance, keep);
            }
        }

        // Keeps the points that matter: starts at the tolerance and loosens it until at most maxStrokes strokes (maxStrokes + 1 points) are left.
        static List<Vector2> Simplify(List<Vector2> pts, float tolerance, int maxStrokes)
        {
            int n = pts.Count;
            if (n <= 2)
                return new List<Vector2>(pts);
            maxStrokes = Math.Max(1, maxStrokes);
            float epsilon = Math.Max(1f, tolerance);
            for (int attempt = 0; attempt < 30; attempt++)
            {
                bool[] keep = new bool[n];
                keep[0] = true;
                keep[n - 1] = true;
                Reduce(pts, 0, n - 1, epsilon, keep);
                List<Vector2> result = new List<Vector2>();
                for (int i = 0; i < n; i++)
                    if (keep[i])
                        result.Add(pts[i]);
                if (result.Count - 1 <= maxStrokes)
                    return result;
                epsilon *= 1.4f;
            }
            return new List<Vector2> { pts[0], pts[n - 1] };
        }

        // ---- drawing (client only, called from the stance's PreDraw) ----

        void Fill(IList<Vector2> source, int extra, Vector2 last)
        {
            int count = source.Count + extra;
            if (points.Length < count)
            {
                points = new Vector2[count + 16];
                ts = new float[count + 16];
            }
            for (int i = 0; i < source.Count; i++)
                points[i] = source[i];
            if (extra > 0)
                points[source.Count] = last;
            for (int i = 0; i < count; i++)
                ts[i] = count > 1 ? i / (float)(count - 1) : 1f;
        }

        public void Draw()
        {
            if (Main.dedServ)
                return;
            if (!Finished)
                DrawLive();
        }

        void DrawLive()
        {
            float grid = MuramasaTuning.PixelSize;
            float width = MuramasaTuning.DrawLineWidth;
            bool extra = path[path.Count - 1] != cursor;
            Fill(path, extra ? 1 : 0, cursor);
            int count = path.Count + (extra ? 1 : 0);
            if (count == 1)
            {
                points[1] = points[0]; // a single point is drawn as a dot
                ts[1] = 1f;
                count = 2;
            }

            // Wide soft glow, the blue body, then a thin hot core; the newest part is the thickest and brightest.
            PixelLine.Draw(points, ts, count, grid, t => width * (1f + t), t => MuramasaTuning.DeepColor * (0.3f + 0.4f * t));
            PixelLine.Draw(points, ts, count, grid, t => width * (0.4f + 0.6f * t), t => Color.Lerp(MuramasaTuning.BlueColor, MuramasaTuning.CoreColor, t) * (0.55f + 0.45f * t));
            PixelLine.Draw(points, ts, count, grid, t => Math.Max(grid, width * 0.3f), t => MuramasaTuning.CoreColor * (0.4f + 0.6f * t));

            // The time ring at the cursor: it shrinks as the window runs out and blinks near the end.
            float left = TimeLeft;
            int segments = 20;
            if (ringPoints.Length < segments + 1)
            {
                ringPoints = new Vector2[segments + 1];
                ringTs = new float[segments + 1];
            }
            float radius = MathHelper.Lerp(MuramasaTuning.DrawRingMin, MuramasaTuning.DrawRingMax, left);
            for (int i = 0; i <= segments; i++)
            {
                ringPoints[i] = cursor + (MathHelper.TwoPi * i / segments).ToRotationVector2() * radius;
                ringTs[i] = i / (float)segments;
            }
            float blink = left < MuramasaTuning.DrawWarnFraction && (ticks / 3) % 2 == 0 ? 0.3f : 1f;
            PixelLine.Draw(ringPoints, ringTs, segments + 1, grid, t => grid, t => MuramasaTuning.CoreColor * blink);
            points[0] = cursor;
            points[1] = cursor;
            ts[0] = 0f;
            ts[1] = 1f;
            PixelLine.Draw(points, ts, 2, grid, t => grid * 2f, t => MuramasaTuning.CoreColor * blink);
        }

    }
}