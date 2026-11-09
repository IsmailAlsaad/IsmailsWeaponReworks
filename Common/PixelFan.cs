using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The shared FAN renderer: the area between an outer edge and an inner edge, drawn as a few parallel smoothed PixelLines (the same
    /// pixel-snapped, Catmull-Rom smoothed renderer the Dark Lance strike uses). Where the two edges coincide the fan is a thin one-cell
    /// line; as the inner edge moves away it widens gradually and covers the area between them, so a swing can start as a line at the
    /// blade's tip and open into a fan. SweepTrail calls this for every held blade and spear; call it directly for any one-off fan.
    /// Client only; call it where a spriteBatch is already running in world space.
    ///
    /// outers/inners: the two edges, index 0 = the oldest sample. expands: per sample, 0 = a thin line on the outer edge, 1 = the full
    /// fan (null = all 1). ages: per sample 0..1, handed to 'color' (across, age) where across 0 = outer edge and 1 = inner edge; a color
    /// with alpha 0 draws additively. rails = how many parallel lines fill the fan.
    /// </summary>
    public static class PixelFan
    {
        static Vector2[] railPoints = new Vector2[0];
        static float[] railTs = new float[0];
        static float[] gaps = new float[0];

        /// <summary>
        /// Where the inner edge sits along the blade while a swing builds up, as a fraction of the blade (1 = at the tip = a thin line,
        /// 'innerFraction' = fully expanded). 'progress' is how far the swing is (0..1), the fan is fully open when 'expandBy' of it is done.
        /// Feed the result to SweepTrail.AddBlade as the 'inner' argument.
        /// </summary>
        public static float Expand(float progress, float expandBy, float innerFraction, float start = 1f)
        {
            // 'start' = where the inner edge begins (1 = the tip; pass the outer edge fraction when the fan's outer edge is not at 1)
            float open = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(progress / Math.Max(0.01f, expandBy), 0f, 1f));
            return MathHelper.Lerp(start, innerFraction, open);
        }

        // Linear lookup in a per-sample array at t (0 = first sample, 1 = last sample).
        static float Sample(float[] values, int count, float t)
        {
            float x = MathHelper.Clamp(t, 0f, 1f) * (count - 1);
            int i = Math.Min((int)x, count - 2);
            return MathHelper.Lerp(values[i], values[i + 1], x - i);
        }

        static Vector2[] fanEdge = new Vector2[0];
        static Vector2[] fillOuter = new Vector2[0];
        static Vector2[] fillInner = new Vector2[0];
        static float[] fillAge = new float[0];
        static readonly Dictionary<long, Color> cells = new Dictionary<long, Color>();
        static readonly List<long> cellOrder = new List<long>();

        /// <summary>
        /// Draws the fan. filled = true (the default, used by every SweepTrail): ONE solid ribbon, every grid cell of the area between the two
        /// edges gets its own color from color(across, age), so the fan is a smooth gradient with clean curved edges and no stripes or
        /// stamped boxes. filled = false: the old look, a few parallel PixelLine rails (rails of them).
        /// </summary>
        public static void Draw(Vector2[] outers, Vector2[] inners, float[] expands, float[] ages, int count, float grid, int rails, Func<float, float, Color> color, bool smooth = true, bool filled = true)
        {
            if (filled)
                DrawFilled(outers, inners, expands, ages, count, grid, color, smooth);
            else
                DrawRails(outers, inners, expands, ages, count, grid, rails, color, smooth);
        }

        static int Pieces(Vector2[] a, Vector2[] b, int i, float step)
        {
            float longest = Math.Max(Vector2.Distance(a[i], a[i + 1]), Vector2.Distance(b[i], b[i + 1]));
            return Math.Max(1, Math.Min(40, (int)Math.Ceiling(longest / step)));
        }

        static long CellKey(int x, int y)
        {
            return ((long)(y + 1048576) << 32) | (uint)(x + 1048576);
        }

        static void DrawFilled(Vector2[] outers, Vector2[] inners, float[] expands, float[] ages, int count, float grid, Func<float, float, Color> color, bool smooth)
        {
            if (Main.dedServ || count < 2 || color == null)
                return;

            grid = Math.Max(1f, grid);
            float step = grid * 0.5f; // samples at most half a cell apart, so the area has no holes

            if (fanEdge.Length < count)
                fanEdge = new Vector2[count + 16];
            for (int i = 0; i < count; i++)
                fanEdge[i] = Vector2.Lerp(outers[i], inners[i], expands == null ? 1f : expands[i]);

            // Cut both edges into fine pieces (a Catmull-Rom curve through the samples when smoothing), the same cut for both edges.
            int needed = 1;
            for (int i = 0; i < count - 1; i++)
                needed += Pieces(outers, fanEdge, i, step);
            if (fillOuter.Length < needed)
            {
                fillOuter = new Vector2[needed + 64];
                fillInner = new Vector2[needed + 64];
                fillAge = new float[needed + 64];
            }

            int n = 0;
            for (int i = 0; i < count - 1; i++)
            {
                int pieces = Pieces(outers, fanEdge, i, step);
                int i0 = Math.Max(i - 1, 0);
                int i3 = Math.Min(i + 2, count - 1);
                for (int s = 0; s < pieces; s++)
                {
                    float u = s / (float)pieces;
                    if (smooth && count >= 3)
                    {
                        fillOuter[n] = Vector2.CatmullRom(outers[i0], outers[i], outers[i + 1], outers[i3], u);
                        fillInner[n] = Vector2.CatmullRom(fanEdge[i0], fanEdge[i], fanEdge[i + 1], fanEdge[i3], u);
                    }
                    else
                    {
                        fillOuter[n] = Vector2.Lerp(outers[i], outers[i + 1], u);
                        fillInner[n] = Vector2.Lerp(fanEdge[i], fanEdge[i + 1], u);
                    }
                    fillAge[n] = MathHelper.Lerp(ages[i], ages[i + 1], u);
                    n++;
                }
            }
            fillOuter[n] = outers[count - 1];
            fillInner[n] = fanEdge[count - 1];
            fillAge[n] = ages[count - 1];
            n++;

            // Walk across the fan at every sample, inner end first and the outer edge last (the bright tip edge wins), newest sample last.
            // A cell only keeps one color, so overlapping samples never stack up.
            cells.Clear();
            for (int k = 0; k < n; k++)
            {
                Vector2 outer = fillOuter[k];
                Vector2 inner = fillInner[k];
                int m = Math.Max(2, (int)Math.Ceiling(Vector2.Distance(outer, inner) / step) + 1);
                for (int j = m - 1; j >= 0; j--)
                {
                    float across = j / (float)(m - 1);
                    Color c = color(across, fillAge[k]);
                    if (c.PackedValue == 0)
                        continue;
                    Vector2 p = Vector2.Lerp(outer, inner, across);
                    cells[CellKey((int)Math.Floor(p.X / grid), (int)Math.Floor(p.Y / grid))] = c;
                }
            }

            if (cells.Count == 0)
                return;

            // Row by row from left to right; neighbouring cells of the same color are drawn as one rectangle.
            cellOrder.Clear();
            foreach (long key in cells.Keys)
                cellOrder.Add(key);
            cellOrder.Sort();

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int index = 0;
            while (index < cellOrder.Count)
            {
                long key = cellOrder[index];
                Color c = cells[key];
                int x = (int)(key & 0xFFFFFFFFL) - 1048576;
                int y = (int)(key >> 32) - 1048576;
                int run = 1;
                while (index + run < cellOrder.Count && cellOrder[index + run] == key + run && cells[cellOrder[index + run]].PackedValue == c.PackedValue)
                    run++;
                Main.spriteBatch.Draw(pixel, new Vector2(x * grid, y * grid) - Main.screenPosition, new Rectangle(0, 0, 1, 1), c, 0f,
                    Vector2.Zero, new Vector2(run * grid, grid), SpriteEffects.None, 0f);
                index += run;
            }
        }

        static void DrawRails(Vector2[] outers, Vector2[] inners, float[] expands, float[] ages, int count, float grid, int rails, Func<float, float, Color> color, bool smooth)
        {
            if (Main.dedServ || count < 2 || color == null)
                return;

            if (railPoints.Length < count)
            {
                railPoints = new Vector2[count + 16];
                railTs = new float[count + 16];
                gaps = new float[count + 16];
            }

            for (int i = 0; i < count; i++)
            {
                railTs[i] = i / (float)(count - 1);
                float e = expands == null ? 1f : expands[i];
                gaps[i] = (outers[i] - inners[i]).Length() * e;
            }

            rails = Math.Max(2, rails);
            float[] gapValues = gaps;
            float[] ageValues = ages;

            // The inner rails first and the outer edge last, so the bright tip edge sits on top.
            for (int rail = rails - 1; rail >= 0; rail--)
            {
                float across = rail / (float)(rails - 1);
                for (int i = 0; i < count; i++)
                {
                    float e = expands == null ? 1f : expands[i];
                    railPoints[i] = Vector2.Lerp(outers[i], Vector2.Lerp(outers[i], inners[i], e), across);
                }
                PixelLine.Draw(railPoints, railTs, count, grid,
                    t => Math.Max(grid, Sample(gapValues, count, t) / (rails - 1) * 1.25f),
                    t => color(across, Sample(ageValues, count, t)),
                    smooth);
            }
        }
    }
}