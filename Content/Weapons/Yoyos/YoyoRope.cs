using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The yoyo's string. IDLE: a plain straight line from the hand to the yoyo (Straight, no simulation). OUT and RETURN: a verlet rope
    /// pinned to the hand at one end and the yoyo at the other (Update), starting from the straight line. Its length follows the distance
    /// between the two every tick (the caller gives how much slack and gravity), it lies on the ground instead of going through it, and it
    /// is simulated in the hand's frame: every tick all points shift by how far the hand moved, so the player's own movement (running,
    /// jumping, knockback, the arm turning) adds no inertia while the yoyo's motion still shapes the string. Drawn SMOOTH, not
    /// pixelated: a Catmull-Rom curve through the rope points, laid down as thin rotated quads at sub-pixel positions (outline, then a
    /// twisted body), lit by the world light. Visual only: every machine simulates and draws its own from the hand and the yoyo
    /// positions, nothing is synced. Client only.
    /// </summary>
    public class YoyoRope
    {
        readonly YoyoTuning tuning;

        public YoyoRope(YoyoTuning tuning)
        {
            this.tuning = tuning;
        }

        Vector2[] pos;
        Vector2[] prev;
        Vector2[] last;
        bool ready;                    // the verlet rope is running (false = the straight line is drawn)
        bool hasLine;
        Vector2[] offset;              // Tighten: how far each point still is from the straight line (shrinks every tick)
        bool tightening;               // the last call was Tighten (the offsets are running)
        Vector2 lineA;
        Vector2 lineB;
        Vector2[] basePts;             // where the string would lie if it were pulled taut along its path (hand, corners, yoyo)
        int lastPivotCount;            // the number of corners of the last Tighten: a change starts the offsets again
        readonly List<Vector2> pathNodes = new List<Vector2>();
        readonly float[] segLength = new float[16];
        readonly int[] segCount = new int[16];
        readonly float[] segFrac = new float[16];
        readonly List<Vector2> curve = new List<Vector2>();

        // Idle: no simulation, just a straight line. The next Update starts the rope from this line.
        public void Straight(Vector2 anchor, Vector2 end)
        {
            if (Main.dedServ)
                return;
            ready = false;
            tightening = false;
            hasLine = true;
            lineA = anchor;
            lineB = end;
        }

        // Out and Returning: one tick of the simulated rope. slackFactor and extraSlack set how much longer the string is than the
        // distance (1.01 and 0 = tight), gravity how much it sags.
        public void Update(Vector2 anchor, Vector2 end, float slackFactor, float extraSlack, float ropeGravity)
        {
            if (Main.dedServ)
                return;
            int n = Math.Max(4, tuning.RopePoints);
            if (pos == null || pos.Length != n)
            {
                pos = new Vector2[n];
                prev = new Vector2[n];
                last = new Vector2[n];
                ready = false;
            }

            tightening = false;
            float distance = Vector2.Distance(anchor, end);
            float length = Math.Max(tuning.RopeMinLength, distance * slackFactor + extraSlack);
            float piece = length / (n - 1);

            // First tick, or the player teleported: lay the string straight between the two ends.
            if (!ready || Vector2.DistanceSquared(pos[0], anchor) > 600f * 600f)
            {
                for (int i = 0; i < n; i++)
                {
                    Vector2 p = Vector2.Lerp(anchor, end, i / (float)(n - 1));
                    pos[i] = p;
                    prev[i] = p;
                    last[i] = p;
                }
                ready = true;
            }
            else
            {
                // The hand's frame: pos[0] is where the hand was last tick (the ends are pinned only AFTER the integration below), so the
                // shift is how far the hand moved. Moving every point, its previous position and its last position by it keeps the
                // velocities, so the hand's movement whips nothing.
                Vector2 shift = anchor - pos[0];
                if (shift != Vector2.Zero)
                {
                    for (int i = 0; i < n; i++)
                    {
                        pos[i] += shift;
                        prev[i] += shift;
                        last[i] += shift;
                    }
                }
            }

            Vector2 gravity = new Vector2(0f, ropeGravity);
            for (int i = 1; i < n - 1; i++)
            {
                last[i] = pos[i];
                Vector2 velocity = (pos[i] - prev[i]) * tuning.RopeDamping;
                prev[i] = pos[i];
                pos[i] += velocity + gravity;
            }
            pos[0] = anchor;
            prev[0] = anchor;
            pos[n - 1] = end;
            prev[n - 1] = end;

            int iterations = Math.Max(1, tuning.RopeIterations);
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    Vector2 d = pos[i + 1] - pos[i];
                    float dl = d.Length();
                    if (dl < 0.0001f)
                        continue;
                    Vector2 correction = d * ((dl - piece) / dl);
                    bool firstPinned = i == 0;
                    bool lastPinned = i + 1 == n - 1;
                    if (firstPinned && lastPinned)
                        continue;
                    if (firstPinned)
                        pos[i + 1] -= correction;
                    else if (lastPinned)
                        pos[i] += correction;
                    else
                    {
                        pos[i] += correction * 0.5f;
                        pos[i + 1] -= correction * 0.5f;
                    }
                }
            }

            if (tuning.RopeCollides)
            {
                for (int i = 1; i < n - 1; i++)
                {
                    if (!Solid(pos[i]))
                        continue;
                    // Slide along the surface when one axis is free, else stay where the point was last tick.
                    Vector2 slideX = new Vector2(pos[i].X, last[i].Y);
                    Vector2 slideY = new Vector2(last[i].X, pos[i].Y);
                    if (!Solid(slideX))
                        pos[i] = slideX;
                    else if (!Solid(slideY))
                        pos[i] = slideY;
                    else
                        pos[i] = last[i];
                    prev[i] = pos[i];
                }
            }
        }

        // Pulled taut (the way back, the Return): no simulation. Every tick each rope point moves a fraction ('speed') of the way to the
        // straight line from the hand to the yoyo, so a sagging or bent string visibly pulls tight over a few ticks and then stays a
        // straight line while the hand and the yoyo move. It keeps the hand's frame like Update, and the next Update continues from here.
        public void Tighten(Vector2 anchor, Vector2 end, float speed)
        {
            TightenAlong(anchor, null, end, speed);
        }

        // The same, along a path that bends around block corners: hand, the corners (YoyoRopeWrap.Pivots), yoyo. Every corner is one of
        // the rope points, so the string bends exactly there. When the number of corners changes the string moves onto the new path at
        // 'speed' per tick instead of jumping.
        public void TightenAlong(Vector2 anchor, IReadOnlyList<Vector2> pivots, Vector2 end, float speed)
        {
            if (Main.dedServ)
                return;
            int n = Math.Max(4, tuning.RopePoints);
            if (pos == null || pos.Length != n)
            {
                pos = new Vector2[n];
                prev = new Vector2[n];
                last = new Vector2[n];
                ready = false;
            }
            if (!ready || Vector2.DistanceSquared(pos[0], anchor) > 600f * 600f)
            {
                // No rope yet (or the player teleported): the path at once.
                BuildPath(anchor, pivots, end, n);
                lastPivotCount = pivots == null ? 0 : pivots.Count;
                for (int i = 0; i < n; i++)
                {
                    Vector2 p = basePts[i];
                    pos[i] = p;
                    prev[i] = p;
                    last[i] = p;
                }
                ready = true;
                return;
            }

            // Every point sits on TODAY's straight line plus an offset that shrinks every tick (moving each point a fraction toward the
            // line lagged behind a swinging yoyo: the string pointed at the yoyo's last position and then jerked toward the new one).
            Vector2 shift = anchor - pos[0];   // the hand's frame, like Update
            float keep = 1f - MathHelper.Clamp(speed, 0.01f, 1f);
            int pivotCount = pivots == null ? 0 : pivots.Count;
            if (pivotCount != lastPivotCount)
            {
                tightening = false;   // the path changed (a wrap began or ended): the offsets start again from where the string is now
                lastPivotCount = pivotCount;
            }
            BuildPath(anchor, pivots, end, n);
            if (offset == null || offset.Length != n)
            {
                offset = new Vector2[n];
                tightening = false;
            }
            for (int i = 0; i < n; i++)
            {
                Vector2 line = basePts[i];
                if (!tightening)
                    offset[i] = pos[i] + shift - line;   // the first tick: how far the bent string is from the straight line
                offset[i] *= keep;
                Vector2 p = line + offset[i];
                pos[i] = p;
                prev[i] = p;   // no leftover speed: the next Update starts from rest
                last[i] = p;
            }
            tightening = true;
            pos[0] = anchor;
            prev[0] = anchor;
            pos[n - 1] = end;
            prev[n - 1] = end;
        }

        // The path the taut string follows, as n points: the hand, the corners, the yoyo. Every corner is one of the points; the others are
        // shared out along the pieces by length. Without corners it is the straight line (the same points as before).
        void BuildPath(Vector2 anchor, IReadOnlyList<Vector2> pivots, Vector2 end, int n)
        {
            if (basePts == null || basePts.Length != n)
                basePts = new Vector2[n];
            int p = pivots == null ? 0 : Math.Min(pivots.Count, Math.Min(n - 3, segLength.Length - 2));
            if (p <= 0)
            {
                for (int i = 0; i < n; i++)
                    basePts[i] = Vector2.Lerp(anchor, end, i / (float)(n - 1));
                return;
            }
            pathNodes.Clear();
            pathNodes.Add(anchor);
            for (int i = 0; i < p; i++)
                pathNodes.Add(pivots[i]);
            pathNodes.Add(end);
            int pieces = p + 1;
            float total = 0f;
            for (int k = 0; k < pieces; k++)
            {
                segLength[k] = Vector2.Distance(pathNodes[k], pathNodes[k + 1]);
                total += segLength[k];
            }
            int interior = n - (p + 2);
            int given = 0;
            for (int k = 0; k < pieces; k++)
            {
                float exact = total > 0.001f ? interior * segLength[k] / total : interior / (float)pieces;
                segCount[k] = (int)exact;
                segFrac[k] = exact - segCount[k];
                given += segCount[k];
            }
            while (given < interior)
            {
                int best = 0;
                for (int k = 1; k < pieces; k++)
                {
                    if (segFrac[k] > segFrac[best])
                        best = k;
                }
                segCount[best]++;
                segFrac[best] = -1f;
                given++;
            }
            int w = 0;
            for (int k = 0; k < pieces; k++)
            {
                basePts[w++] = pathNodes[k];
                for (int j = 1; j <= segCount[k]; j++)
                    basePts[w++] = Vector2.Lerp(pathNodes[k], pathNodes[k + 1], j / (float)(segCount[k] + 1));
            }
            basePts[w] = end;
        }

        static bool Solid(Vector2 point)
        {
            return Collision.SolidCollision(point - new Vector2(1.5f), 3, 3);
        }

        public void Draw(Texture2D pixel)
        {
            if (Main.dedServ || (!ready && !hasLine))
                return;
            int subdivisions = Math.Max(1, tuning.RopeSubdivisions);
            curve.Clear();
            if (!ready)
            {
                // Idle: the straight line, cut into as many pieces as the rope curve so the twist looks the same.
                int count = (Math.Max(4, tuning.RopePoints) - 1) * subdivisions;
                for (int j = 0; j <= count; j++)
                    curve.Add(Vector2.Lerp(lineA, lineB, j / (float)count));
            }
            else
            {
                // The smooth curve: Catmull-Rom between every two rope points.
                int n = pos.Length;
                for (int i = 0; i < n - 1; i++)
                {
                    Vector2 p0 = pos[Math.Max(i - 1, 0)];
                    Vector2 p1 = pos[i];
                    Vector2 p2 = pos[i + 1];
                    Vector2 p3 = pos[Math.Min(i + 2, n - 1)];
                    for (int s = 0; s < subdivisions; s++)
                        curve.Add(Vector2.CatmullRom(p0, p1, p2, p3, s / (float)subdivisions));
                }
                curve.Add(pos[n - 1]);
            }

            for (int pass = 0; pass < 2; pass++)
            {
                for (int j = 0; j < curve.Count - 1; j++)
                {
                    Vector2 a = curve[j];
                    Vector2 b = curve[j + 1];
                    Color light = Lighting.GetColor((int)((a.X + b.X) * 0.5f / 16f), (int)((a.Y + b.Y) * 0.5f / 16f));
                    if (pass == 0)
                        Piece(pixel, a, b, tuning.RopeOutlineWidth, Tint(tuning.RopeOutlineColor, light));
                    else
                    {
                        bool twist = (j / 2) % 2 == 1;
                        Piece(pixel, a, b, tuning.RopeWidth, Tint(twist ? tuning.RopeAltColor : tuning.RopeColor, light));
                    }
                }
            }
        }

        // One straight piece of the curve: a stretched, rotated 1x1 pixel (the ends overlap a little so no seams show).
        static void Piece(Texture2D pixel, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 d = b - a;
            float length = d.Length();
            if (length < 0.01f)
                return;
            Main.spriteBatch.Draw(pixel, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, d.ToRotation(), new Vector2(0f, 0.5f),
                new Vector2(length + width * 0.35f, width), SpriteEffects.None, 0f);
        }

        static Color Tint(Color color, Color light)
        {
            return new Color(color.ToVector3() * light.ToVector3());
        }
    }
}