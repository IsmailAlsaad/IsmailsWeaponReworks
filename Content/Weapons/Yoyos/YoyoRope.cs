using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The yoyo's string: a verlet rope pinned to the hand at one end and the yoyo at the other. Its length follows the distance between
    /// the two (a little slack, so it sags), it hangs under gravity and lies on the ground instead of going through it. Drawn SMOOTH, not
    /// pixelated: a Catmull-Rom curve through the rope points, laid down as thin rotated quads at sub-pixel positions (outline, then a
    /// twisted body), lit by the world light. Visual only: every machine simulates and draws its own from the hand and the yoyo
    /// positions, nothing is synced. Client only.
    /// </summary>
    public class YoyoRope
    {
        Vector2[] pos;
        Vector2[] prev;
        Vector2[] last;
        bool ready;
        readonly List<Vector2> curve = new List<Vector2>();

        public void Update(Vector2 anchor, Vector2 end)
        {
            if (Main.dedServ)
                return;
            int n = Math.Max(4, YoyoTuning.RopePoints);
            if (pos == null || pos.Length != n)
            {
                pos = new Vector2[n];
                prev = new Vector2[n];
                last = new Vector2[n];
                ready = false;
            }

            float distance = Vector2.Distance(anchor, end);
            float length = Math.Max(YoyoTuning.RopeMinLength, distance * YoyoTuning.RopeSlackFactor + YoyoTuning.RopeExtraSlack);
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

            Vector2 gravity = new Vector2(0f, YoyoTuning.RopeGravity);
            for (int i = 1; i < n - 1; i++)
            {
                last[i] = pos[i];
                Vector2 velocity = (pos[i] - prev[i]) * YoyoTuning.RopeDamping;
                prev[i] = pos[i];
                pos[i] += velocity + gravity;
            }
            pos[0] = anchor;
            prev[0] = anchor;
            pos[n - 1] = end;
            prev[n - 1] = end;

            int iterations = Math.Max(1, YoyoTuning.RopeIterations);
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

            if (YoyoTuning.RopeCollides)
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

        static bool Solid(Vector2 point)
        {
            return Collision.SolidCollision(point - new Vector2(1.5f), 3, 3);
        }

        public void Draw(Texture2D pixel)
        {
            if (Main.dedServ || !ready || pos == null)
                return;
            int n = pos.Length;
            int subdivisions = Math.Max(1, YoyoTuning.RopeSubdivisions);

            // The smooth curve: Catmull-Rom between every two rope points.
            curve.Clear();
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

            for (int pass = 0; pass < 2; pass++)
            {
                for (int j = 0; j < curve.Count - 1; j++)
                {
                    Vector2 a = curve[j];
                    Vector2 b = curve[j + 1];
                    Color light = Lighting.GetColor((int)((a.X + b.X) * 0.5f / 16f), (int)((a.Y + b.Y) * 0.5f / 16f));
                    if (pass == 0)
                        Piece(pixel, a, b, YoyoTuning.RopeOutlineWidth, Tint(YoyoTuning.RopeOutlineColor, light));
                    else
                    {
                        bool twist = (j / 2) % 2 == 1;
                        Piece(pixel, a, b, YoyoTuning.RopeWidth, Tint(twist ? YoyoTuning.RopeAltColor : YoyoTuning.RopeColor, light));
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