using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// One crescent drawn by the shared pixel line renderer (PixelLine): the line follows a circle, thickest in the middle and shrinking to a
    /// single pixel at both tips. It sits on the pixel grid, so the shape looks like pixel art and crawls as it moves. Extracted from the Ice
    /// Blade's wave when the Night's Edge wave became the second user (the Ice Blade draws exactly as before). 'center' is the FRONT of the
    /// crescent, 'angle' the way it faces, 'half' the half arc in radians. Colors with alpha 0 are additive.
    /// </summary>
    public static class CrescentPainter
    {
        static Vector2[] points = new Vector2[0];
        static float[] ts = new float[0];

        public static void Draw(Vector2 center, float angle, float radius, float thickness, float half, Color color, float grid)
        {
            Vector2 origin = center - angle.ToRotationVector2() * radius;
            int count = Math.Min(64, Math.Max(5, (int)Math.Ceiling(radius * 2f * half / (grid * 4f)) + 1));
            if (points.Length < count)
            {
                points = new Vector2[count];
                ts = new float[count];
            }
            for (int i = 0; i < count; i++)
            {
                float f = i / (float)(count - 1);
                ts[i] = f;
                points[i] = origin + (angle + MathHelper.Lerp(-half, half, f)).ToRotationVector2() * radius;
            }
            PixelLine.Draw(points, ts, count, grid,
                f => Math.Max(grid, thickness * (float)Math.Pow(Math.Max(0.0, Math.Cos((f - 0.5f) * MathHelper.Pi)), 0.75)), // 1 in the middle, 0 at the tips
                f => color,
                true);
        }
    }
}