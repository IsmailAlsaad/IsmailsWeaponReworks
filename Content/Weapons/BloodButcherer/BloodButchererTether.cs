using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>
    /// The crimson strand that ties the blade's handle to the player's real hand, so the sword no longer looks like it leaps out of the
    /// hand during the lunge and the spin. One per swing projectile; the projectile gives it the two ends and a "bend" (droop under gravity
    /// when slack, dragged behind a spinning blade when taut). Drawn with Common/PixelLine as three stacked lines (opaque dark body, opaque
    /// bright stripe, additive core), thick at both ends and pulsing along its length as if blood flowed toward the blade. Visual only:
    /// every machine draws its own from the same timeline, nothing is synced. Client only.
    /// </summary>
    public class BloodButchererTether
    {
        Vector2[] points = new Vector2[0];
        float[] along = new float[0];

        public void Draw(Vector2 start, Vector2 end, Vector2 bend, float stretch, float flash, int clock, Color light)
        {
            if (Main.dedServ)
                return;
            Vector2 axis = end - start;
            float length = axis.Length();
            if (length < 2.5f)
                return;

            int segments = Math.Max(4, BloodButchererTuning.TetherSegments);
            int count = segments + 1;
            if (points.Length != count)
            {
                points = new Vector2[count];
                along = new float[count];
            }

            // A quadratic curve whose middle sits 'bend' away from the straight line, plus a small wobble that grows when the bite lands.
            Vector2 across = new Vector2(-axis.Y, axis.X) / length;
            Vector2 control = (start + end) * 0.5f + bend * 2f;
            float wobble = BloodButchererTuning.TetherWobble * (0.4f + flash * 1.5f);
            for (int i = 0; i < count; i++)
            {
                float s = i / (float)segments;
                float inverse = 1f - s;
                Vector2 p = inverse * inverse * start + 2f * inverse * s * control + s * s * end;
                p += across * ((float)Math.Sin(s * MathHelper.TwoPi + clock * 0.5f) * wobble * (float)Math.Sin(s * MathHelper.Pi));
                points[i] = p;
                along[i] = s;
            }

            float thickness = MathHelper.Lerp(BloodButchererTuning.TetherWidth, BloodButchererTuning.TetherThinWidth, stretch) * (1f + 0.6f * flash);
            Color bright = Color.Lerp(light, Color.White, 0.4f);
            Color dark = Tint(BloodButchererTuning.TetherDarkColor, bright);
            Color mid = Tint(BloodButchererTuning.TetherMidColor, bright);
            Color glow = BloodButchererTuning.TetherGlowColor * (0.35f + 0.5f * flash);
            float grid = BloodButchererTuning.PixelSize;

            PixelLine.Draw(points, along, count, grid, s => thickness * Profile(s, clock), s => dark);
            PixelLine.Draw(points, along, count, grid, s => thickness * 0.5f * Profile(s, clock), s => mid);
            PixelLine.Draw(points, along, count, grid, s => thickness * 0.25f * Profile(s, clock), s => glow);
        }

        // Thicker at both ends, with a ripple that runs toward the blade.
        static float Profile(float s, int clock)
        {
            float ends = 1f + 0.5f * (1f - (float)Math.Sin(s * Math.PI));
            float flow = 1f + 0.22f * (float)Math.Sin(s * 11f - clock * 0.6f);
            return ends * flow;
        }

        static Color Tint(Color color, Color light)
        {
            return new Color(color.ToVector3() * light.ToVector3());
        }
    }
}