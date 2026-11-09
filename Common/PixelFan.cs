using System;
using Microsoft.Xna.Framework;
using Terraria;

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

        public static void Draw(Vector2[] outers, Vector2[] inners, float[] expands, float[] ages, int count, float grid, int rails, Func<float, float, Color> color, bool smooth = true)
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