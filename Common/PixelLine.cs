using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The shared pixel line renderer. Draws a polyline snapped to a grid of world pixels (grid-sized cells, so it looks like pixel art
    /// and moves in whole pixels), with a width and a color that can change along the line. It walks every segment in steps of one
    /// grid cell, so neighbouring cells always touch, builds one horizontal span per row and step, resolves overlaps and draws the result as few rectangles. Use it for any line or trail; for a trail that should outlive its
    /// projectile use PixelTrail + DetachedTrails instead of calling this directly.
    ///
    /// 't' is a value you give for every point (an age from 0 to 1, a position along the line, anything); it is interpolated between
    /// points and handed to the width and color callbacks. Width is in world pixels (rounded to whole cells, at least one when above
    /// zero); a color of alpha 0 draws additively (glow); a fully transparent color or a width of 0 or less skips that part.
    /// Client only. Call it where a spriteBatch is already running in world space (a projectile's PreDraw, or DetachedTrails).
    /// </summary>
    public static class PixelLine
    {
        struct Span
        {
            public int Y;
            public int X0;
            public int X1;
            public int Order;
            public Color Color;
        }

        static readonly List<Span> scratch = new List<Span>();
        static readonly Comparison<Span> byRow = (a, b) =>
        {
            int c = a.Y.CompareTo(b.Y);
            if (c != 0) return c;
            c = a.X0.CompareTo(b.X0);
            if (c != 0) return c;
            return a.Order.CompareTo(b.Order);
        };

        public static void Draw(Vector2[] points, float[] t, int count, float grid, Func<float, float> width, Func<float, Color> color)
        {
            if (Main.dedServ || count < 2 || width == null || color == null)
                return;

            grid = Math.Max(1f, grid);
            scratch.Clear();
            int order = 0;

            for (int i = 0; i < count - 1; i++)
            {
                Vector2 start = points[i];
                Vector2 edge = points[i + 1] - start;
                int steps = Math.Max(1, (int)Math.Ceiling(edge.Length() / grid));

                for (int s = i == 0 ? 0 : 1; s <= steps; s++)
                {
                    float f = s / (float)steps;
                    Vector2 p = start + edge * f;
                    float tt = MathHelper.Lerp(t[i], t[i + 1], f);

                    float w = width(tt);
                    if (w <= 0f)
                        continue;
                    Color c = color(tt);
                    if (c.PackedValue == 0)
                        continue;

                    int n = Math.Max(1, (int)Math.Round(w / grid));
                    int x0 = (int)Math.Floor(p.X / grid) - n / 2;
                    int y0 = (int)Math.Floor(p.Y / grid) - n / 2;
                    for (int row = 0; row < n; row++)
                        scratch.Add(new Span { Y = y0 + row, X0 = x0, X1 = x0 + n - 1, Order = order, Color = c });
                    order++;
                }
            }

            if (scratch.Count == 0)
                return;

            scratch.Sort(byRow);
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // Sweep every row from left to right. A span only paints the cells to the right of what an earlier span of the row already painted (spans that start further left, or at the same cell but laid earlier, win).
            int currentY = scratch[0].Y;
            int painted = int.MinValue;
            for (int i = 0; i < scratch.Count; i++)
            {
                Span span = scratch[i];
                if (span.Y != currentY)
                {
                    currentY = span.Y;
                    painted = int.MinValue;
                }

                int from = Math.Max(span.X0, painted + 1);
                if (from > span.X1)
                    continue;
                painted = span.X1;

                Main.spriteBatch.Draw(pixel, new Vector2(from * grid, span.Y * grid) - Main.screenPosition, new Rectangle(0, 0, 1, 1), span.Color, 0f,
                    Vector2.Zero, new Vector2((span.X1 - from + 1) * grid, grid), SpriteEffects.None, 0f);
            }
        }
    }
}