using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The shared FAN renderer: the area between an outer edge and an inner edge, drawn the way the Dark Lance strike line is drawn: a few
    /// nested layers (a wide halo, a body, a thin hot core), each one smoothed PixelLine laid along the middle of its band, shrinking as it ages. Where the two edges coincide the fan is a thin one-cell
    /// line; as the inner edge moves away it widens gradually and covers the area between them, so a swing can start as a line at the
    /// blade's tip and open into a fan. SweepTrail calls this for every held blade and spear; call it directly for any one-off fan.
    /// Client only; call it where a spriteBatch is already running in world space.
    ///
    /// outers/inners: the two edges, index 0 = the oldest sample. expands: per sample, 0 = a thin line on the outer edge, 1 = the full
    /// fan (null = all 1). ages: per sample 0..1, handed to 'color' (across, age) where across 0 = outer edge and 1 = inner edge; a color
    /// with alpha 0 draws additively. rails = how many parallel lines fill the fan.
    /// </summary>
    public enum FanLook
    {
        Soft,     // a triangle mesh: straight clean edges and a feathered falloff just past both of them (the default)
        Ribbon,   // nested smoothed PixelLine layers, the Dark Lance strike's look
        Filled,   // one solid area, a color per grid cell
        Rails,    // a few parallel thin PixelLines
    }

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
        /// Draws the fan. look = Ribbon (the default, used by every SweepTrail): the Dark Lance strike's way of drawing a line, see DrawRibbon.
        /// Filled: one solid area, a color per grid cell. Rails: 'rails' parallel thin PixelLines.
        /// ribbonLayers (1 to 3), ribbonShrink (1 = the ribbon shrinks to a point as it ages, like the Dark Lance line; 0 = keeps its width and
        /// only fades), ribbonWidthScale (a square-stamped line is wider than its width on a diagonal, so a little under 1 keeps the edges on the
        /// blade) and ribbonGain (the layers add up, so each one is dimmed) only matter for Ribbon.
        /// </summary>
        public static void Draw(Vector2[] outers, Vector2[] inners, float[] expands, float[] ages, int count, float grid, int rails, Func<float, float, Color> color, bool smooth = true,
            FanLook look = FanLook.Soft, int ribbonLayers = 3, float ribbonShrink = 1f, float ribbonWidthScale = 0.85f, float ribbonGain = 0.6f, float softFeatherPx = 5f)
        {
            if (look == FanLook.Soft)
                DrawSoft(outers, inners, expands, ages, count, color, smooth, ribbonShrink, softFeatherPx);
            else if (look == FanLook.Ribbon)
                DrawRibbon(outers, inners, expands, ages, count, grid, color, smooth, ribbonLayers, ribbonShrink, ribbonWidthScale, ribbonGain);
            else if (look == FanLook.Filled)
                DrawFilled(outers, inners, expands, ages, count, grid, color, smooth);
            else
                DrawRails(outers, inners, expands, ages, count, grid, rails, color, smooth);
        }

        /// <summary>
        /// The Dark Lance way: nested layers drawn one after the other with the shared PixelLine, the widest (the halo, reaching from the tip
        /// edge all the way to the inner edge) first and the thinnest (the core, hugging the tip edge) last. Each layer is ONE line along the
        /// middle of its band, as wide as its band, so the width follows the fan: a thin line while the fan is closed, widening as it opens.
        /// The layer's color is the style's color at the far edge of its band (so the halo takes the dim hand side, the core the bright tip).
        /// </summary>
        static void DrawRibbon(Vector2[] outers, Vector2[] inners, float[] expands, float[] ages, int count, float grid, Func<float, float, Color> color, bool smooth, int layers, float shrink, float widthScale, float gain)
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

            layers = Math.Max(1, Math.Min(3, layers));
            float[] gapValues = gaps;
            float[] ageValues = ages;
            for (int layer = 0; layer < layers; layer++)
            {
                float far = layers == 1 ? 1f : MathHelper.Lerp(1f, 0.25f, layer / (float)(layers - 1)); // how far this layer reaches from the tip edge toward the inner edge
                for (int i = 0; i < count; i++)
                {
                    float e = expands == null ? 1f : expands[i];
                    Vector2 edge = Vector2.Lerp(outers[i], inners[i], e);
                    railPoints[i] = Vector2.Lerp(outers[i], edge, far * 0.5f);
                }
                PixelLine.Draw(railPoints, railTs, count, grid,
                    t =>
                    {
                        float keep = 1f - shrink * Sample(ageValues, count, t);
                        return keep <= 0.02f ? 0f : Math.Max(grid, Sample(gapValues, count, t) * far * widthScale * keep);
                    },
                    t => color(far, Sample(ageValues, count, t)) * gain,
                    smooth);
            }
        }

        static BasicEffect softEffect;
        static VertexPositionColor[] softVertices = new VertexPositionColor[0];
        static short[] softIndices = new short[0];
        static Vector2[] softDirection = new Vector2[0];
        static readonly float[] softAcross = { 0f, 0.25f, 0.5f, 0.75f, 1f }; // where the style is asked for a color across the fan; the mesh blends between them
        const int SoftColumn = 7; // vertices across the fan at every sample: feather, 5 color stops, feather

        /// <summary>
        /// The soft look: the fan is a triangle mesh, not a pile of square cells, so both edges are clean straight lines (cut finely and bent
        /// along a Catmull-Rom curve when smoothing) and the color is blended across it. Just past each edge the color keeps going for
        /// 'featherPx' pixels and fades to nothing, so there is no hard cut line. 'shrink' pulls the inner edge toward the tip as a sample
        /// ages (1 = the old end narrows to a line, like the Dark Lance strike line). Colors with alpha 0 add up as glow, like everywhere else.
        /// The sprite batch is paused for the mesh and restarted afterwards with the usual world-space settings (same as BladeOfGrassVineDraw).
        /// </summary>
        static void DrawSoft(Vector2[] outers, Vector2[] inners, float[] expands, float[] ages, int count, Func<float, float, Color> color, bool smooth, float shrink, float featherPx)
        {
            if (Main.dedServ || count < 2 || color == null)
                return;

            if (fanEdge.Length < count)
                fanEdge = new Vector2[count + 16];
            for (int i = 0; i < count; i++)
            {
                float keep = Math.Max(0f, 1f - shrink * ages[i]);
                fanEdge[i] = Vector2.Lerp(outers[i], inners[i], (expands == null ? 1f : expands[i]) * keep);
            }

            // Cut both edges into fine pieces (the same cut for both).
            float step = 4f;
            int needed = 1;
            for (int i = 0; i < count - 1; i++)
                needed += Math.Min(12, Pieces(outers, fanEdge, i, step));
            if (needed * SoftColumn > 30000)
                return;
            if (fillOuter.Length < needed)
            {
                fillOuter = new Vector2[needed + 64];
                fillInner = new Vector2[needed + 64];
                fillAge = new float[needed + 64];
            }
            if (softDirection.Length < needed)
                softDirection = new Vector2[needed + 64];

            int n = 0;
            for (int i = 0; i < count - 1; i++)
            {
                int pieces = Math.Min(12, Pieces(outers, fanEdge, i, step));
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

            // Which way is "outward" (from the inner edge toward the tip edge) at every sample: that is the way the feather grows. Where the fan
            // is closed (no width) it takes the nearest sample that has one; with none at all it is the side of the path.
            bool any = false;
            for (int k = 0; k < n; k++)
            {
                Vector2 across = fillOuter[k] - fillInner[k];
                float length = across.Length();
                if (length > 0.5f)
                {
                    softDirection[k] = across / length;
                    any = true;
                }
                else
                    softDirection[k] = Vector2.Zero;
            }
            if (any)
            {
                Vector2 last = Vector2.Zero;
                for (int k = 0; k < n; k++)
                {
                    if (softDirection[k] == Vector2.Zero)
                        softDirection[k] = last;
                    else
                        last = softDirection[k];
                }
                last = Vector2.Zero;
                for (int k = n - 1; k >= 0; k--)
                {
                    if (softDirection[k] == Vector2.Zero)
                        softDirection[k] = last;
                    else
                        last = softDirection[k];
                }
            }
            else
            {
                for (int k = 0; k < n; k++)
                {
                    Vector2 tangent = fillOuter[Math.Min(k + 1, n - 1)] - fillOuter[Math.Max(k - 1, 0)];
                    tangent = tangent.LengthSquared() > 0.0001f ? Vector2.Normalize(tangent) : Vector2.UnitX;
                    softDirection[k] = new Vector2(-tangent.Y, tangent.X);
                }
            }

            int vertexCount = n * SoftColumn;
            int triangleCount = (n - 1) * (SoftColumn - 1) * 2;
            if (softVertices.Length < vertexCount)
                softVertices = new VertexPositionColor[vertexCount + 64];
            if (softIndices.Length < triangleCount * 3)
                softIndices = new short[triangleCount * 3 + 192];

            featherPx = Math.Max(0f, featherPx);
            for (int k = 0; k < n; k++)
            {
                int v = k * SoftColumn;
                Vector2 outward = softDirection[k] * featherPx;
                softVertices[v] = new VertexPositionColor(new Vector3(fillOuter[k] + outward - Main.screenPosition, 0f), Color.Transparent);
                for (int s = 0; s < softAcross.Length; s++)
                {
                    Vector2 position = Vector2.Lerp(fillOuter[k], fillInner[k], softAcross[s]);
                    softVertices[v + 1 + s] = new VertexPositionColor(new Vector3(position - Main.screenPosition, 0f), color(softAcross[s], fillAge[k]));
                }
                softVertices[v + SoftColumn - 1] = new VertexPositionColor(new Vector3(fillInner[k] - outward - Main.screenPosition, 0f), Color.Transparent);
            }

            int index = 0;
            for (int k = 0; k < n - 1; k++)
            {
                for (int b = 0; b < SoftColumn - 1; b++)
                {
                    int a = k * SoftColumn + b;
                    int c = (k + 1) * SoftColumn + b;
                    softIndices[index++] = (short)a;
                    softIndices[index++] = (short)c;
                    softIndices[index++] = (short)(a + 1);
                    softIndices[index++] = (short)(a + 1);
                    softIndices[index++] = (short)c;
                    softIndices[index++] = (short)(c + 1);
                }
            }

            GraphicsDevice device = Main.graphics.GraphicsDevice;
            if (softEffect == null || softEffect.IsDisposed)
                softEffect = new BasicEffect(device) { TextureEnabled = false, VertexColorEnabled = true };

            Main.spriteBatch.End();
            softEffect.World = Matrix.Identity;
            softEffect.View = Main.GameViewMatrix.TransformationMatrix;
            softEffect.Projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -1f, 1f);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            foreach (EffectPass pass in softEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, softVertices, 0, vertexCount, softIndices, 0, triangleCount);
            }
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
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