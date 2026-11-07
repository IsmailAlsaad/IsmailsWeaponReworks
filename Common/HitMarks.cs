using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// How one weapon's hit marks look (its Tuning builds one, see KatanaTuning.HitMarks). A mark is never smaller than Length x Width;
    /// on a big enemy it grows so it covers about Coverage of the enemy's size across the mark, up to MaxScale times, and its thickness
    /// follows that growth by WidthGrow (0 = none, 1 = all).
    /// </summary>
    public readonly struct HitMarkStyle
    {
        public readonly int Ticks;
        public readonly int Count;
        public readonly int MaxMarks;
        public readonly float Length;
        public readonly float Width;
        public readonly float AngleDeg;
        public readonly float Coverage;
        public readonly float WidthGrow;
        public readonly float MaxScale;
        public readonly float Grid;
        public readonly Color Edge;
        public readonly Color Glow;
        public readonly Color Core;

        public HitMarkStyle(int ticks, int count, int maxMarks, float length, float width, float angleDeg, float coverage, float widthGrow, float maxScale, float grid, Color edge, Color glow, Color core)
        {
            Ticks = ticks;
            Count = count;
            MaxMarks = maxMarks;
            Length = length;
            Width = width;
            AngleDeg = angleDeg;
            Coverage = coverage;
            WidthGrow = widthGrow;
            MaxScale = maxScale;
            Grid = grid;
            Edge = edge;
            Glow = glow;
            Core = core;
        }
    }

    /// <summary>
    /// The streaks that cross an enemy when a cut hits it (pointed pixel lines that shoot out and fade), shared by the Katana and the
    /// Muramasa. A projectile owns one set: Add on a hit, Tick every tick, Draw in PreDraw. Visual only, nothing is synced (every machine
    /// adds its own marks from the hit it sees).
    /// </summary>
    public class HitMarkSet
    {
        struct Mark
        {
            public Vector2 Center;
            public float Angle;
            public float Length;
            public float Width;
            public int Age;
        }

        readonly List<Mark> marks = new List<Mark>();
        readonly Vector2[] points = new Vector2[8];
        readonly float[] ts = new float[8];

        // Lays the marks across an enemy: the main one at about 90 degrees to the cut (give or take AngleDeg), the others shorter, each
        // at its own angle. Returns the main angle (for the weapon's own sound and sparks).
        public float Add(HitMarkStyle style, Vector2 at, Vector2 cut, Rectangle hitbox)
        {
            Vector2 line = cut.SafeNormalize(Vector2.UnitX);
            float baseAngle = line.ToRotation() + MathHelper.PiOver2;
            if (Main.dedServ)
                return baseAngle;
            float variance = MathHelper.ToRadians(style.AngleDeg);
            int count = Math.Max(1, style.Count);
            for (int i = 0; i < count; i++)
            {
                if (marks.Count >= Math.Max(1, style.MaxMarks))
                    marks.RemoveAt(0);
                float size = i == 0 ? 1f : Main.rand.NextFloat(0.5f, 0.8f);
                float angle = baseAngle + Main.rand.NextFloatDirection() * variance;
                float grow = Grow(style, angle, hitbox);
                marks.Add(new Mark
                {
                    Center = at + (i == 0 ? Vector2.Zero : line * (Main.rand.NextFloatDirection() * 10f * MathHelper.Lerp(1f, grow, 0.5f))) + Main.rand.NextVector2Circular(4f, 4f),
                    Angle = angle,
                    Length = style.Length * size * Main.rand.NextFloat(0.85f, 1.15f) * grow,
                    Width = style.Width * size * MathHelper.Lerp(1f, grow, MathHelper.Clamp(style.WidthGrow, 0f, 1f)),
                    Age = 0
                });
            }
            return baseAngle;
        }

        // 1 for a small enemy (the mark keeps its normal size); more when the enemy is wider across the mark than the mark is long.
        static float Grow(HitMarkStyle style, float angle, Rectangle hitbox)
        {
            Vector2 axis = angle.ToRotationVector2();
            float extent = Math.Abs(axis.X) * hitbox.Width + Math.Abs(axis.Y) * hitbox.Height;
            float scale = extent * style.Coverage / Math.Max(1f, style.Length);
            return MathHelper.Clamp(scale, 1f, Math.Max(1f, style.MaxScale));
        }

        public void Tick(HitMarkStyle style)
        {
            for (int i = marks.Count - 1; i >= 0; i--)
            {
                Mark mark = marks[i];
                mark.Age++;
                if (mark.Age >= Math.Max(1, style.Ticks))
                    marks.RemoveAt(i);
                else
                    marks[i] = mark;
            }
        }

        // A pointed streak that shoots out to its full length in a quarter of its life, then thins and fades.
        public void Draw(HitMarkStyle style)
        {
            if (Main.dedServ)
                return;
            int count = points.Length;
            for (int m = 0; m < marks.Count; m++)
            {
                Mark mark = marks[m];
                float t = MathHelper.Clamp(mark.Age / (float)Math.Max(1, style.Ticks), 0f, 1f);
                float quick = Math.Min(1f, t * 4f);
                float grow = 1f - (1f - quick) * (1f - quick);
                float fade = (1f - t) * (1f - t);
                float half = mark.Length * 0.5f * grow;
                float width = mark.Width * (1f - 0.6f * t);
                float grid = style.Grid;
                Vector2 axis = mark.Angle.ToRotationVector2();
                for (int i = 0; i < count; i++)
                {
                    ts[i] = i / (float)(count - 1);
                    points[i] = mark.Center + axis * MathHelper.Lerp(-half, half, ts[i]);
                }
                PixelLine.Draw(points, ts, count, grid, u => width * 2f * Taper(u), u => style.Edge * (0.5f * fade));
                PixelLine.Draw(points, ts, count, grid, u => width * Taper(u), u => style.Glow * fade);
                PixelLine.Draw(points, ts, count, grid, u => Math.Max(grid, width * 0.4f * Taper(u)), u => style.Core * fade);
            }
        }

        // 0 at both ends, 1 in the middle (a pointed streak).
        static float Taper(float u)
        {
            return (float)Math.Pow(Math.Max(0f, 1f - Math.Abs(2f * u - 1f)), 0.7f);
        }
    }
}