using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.BladeOfGrass
{
    /// <summary>Lays sprites along a polyline: the vines (hold projectile) and the coils (spine projectile) both draw this way.</summary>
    public static class BladeOfGrassVineDraw
    {
        // A hook head over the end of a vine: its tip is at 'end' (plus HeadForwardPx) and its back lies over the last links of the chain,
        // so nothing floats. The head sprites point up at rotation 0, hence the quarter turn.
        public static void DrawHead(Texture2D head, Vector2 end, float angle, float scale, Color color)
        {
            Vector2 direction = angle.ToRotationVector2();
            Vector2 center = end - direction * (head.Height * 0.5f * scale) + direction * BladeOfGrassTuning.HeadForwardPx;
            Main.EntitySpriteDraw(head, center - Main.screenPosition, null, color, angle + MathHelper.PiOver2, head.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }

        // Resamples the first 'count' points into positions 'spacing' pixels apart along the line (the first one at the start), each with
        // the angle of the line there. The lists are cleared first and reused by the caller, so drawing allocates nothing.
        public static void Resample(Vector2[] points, int count, float spacing, List<Vector2> positions, List<float> angles)
        {
            positions.Clear();
            angles.Clear();
            spacing = MathHelper.Max(1f, spacing);
            float next = 0f;
            for (int k = 1; k < count; k++)
            {
                Vector2 a = points[k - 1];
                Vector2 b = points[k];
                float length = Vector2.Distance(a, b);
                if (length < 0.001f)
                    continue;
                Vector2 direction = (b - a) / length;
                float angle = direction.ToRotation();
                while (next <= length)
                {
                    positions.Add(a + direction * next);
                    angles.Add(angle);
                    next += spacing;
                }
                next -= length;
            }
        }
    }
}