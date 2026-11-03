using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// How a SweepTrail looks. A weapon subclasses this once (see the Ice Blade's IceBladeTrailStyle) and every number reads the weapon's
    /// Tuning live, so hot reload works. 'across' runs from 0 (the outer edge, the blade's tip side) to 1 (the inner edge, toward the
    /// hand); 'age' runs from 0 (just laid) to 1 (about to vanish). Alpha 0 colors draw additively (glow).
    /// </summary>
    public abstract class SweepTrailStyle
    {
        public virtual float Grid => 2f;
        public virtual int LifeTicks => 30;
        public virtual int Rails => 7;   // parallel pixel lines that fill the area between the outer and the inner edge

        public abstract Color GetColor(float across, float age);
    }

    /// <summary>
    /// The area a swinging blade has swept, for a held sword: every sample is a pair of points (the outer edge, usually the tip, and the
    /// inner edge, somewhere closer to the hand). The caller moves the inner edge from the tip toward the hand while the swing
    /// builds up, so the trail starts as a thin line at the tip and EXPANDS down the blade into a fan. Drawn as a few parallel
    /// PixelLines between the two edges, so it keeps the pixel look. Every sample carries its own age, so the fan fades away from its tail.
    /// Same rules as PixelTrail: purely visual, never synced (each machine builds its own), call Add and Tick every tick, Draw in PreDraw,
    /// and hand it to DetachedTrails in OnKill so it keeps fading after the projectile is gone.
    /// </summary>
    public class SweepTrail
    {
        public readonly SweepTrailStyle Style;
        public readonly bool IsOwner;

        readonly List<Vector2> outers = new List<Vector2>();
        readonly List<Vector2> inners = new List<Vector2>();
        readonly List<int> births = new List<int>();
        readonly List<float> scales = new List<float>(); // per sample: how long it lives, as a fraction of Style.LifeTicks
        Vector2[] points = new Vector2[0];
        float[] ts = new float[0];
        float[] gaps = new float[0];
        float[] ages = new float[0];
        int clock;

        public SweepTrail(SweepTrailStyle style, bool isOwner)
        {
            Style = style;
            IsOwner = isOwner;
        }

        public int Count => outers.Count;
        public bool IsEmpty => outers.Count == 0;

        // lifeScale (0..1] shortens this sample's life: a swing that lays its tail with a small scale and its tip with 1 makes the
        // trailing end fade out first, and the fade then travels up to the tip. The default keeps every other trail exactly as it was.
        public void Add(Vector2 outer, Vector2 inner, float lifeScale = 1f)
        {
            outers.Add(outer);
            inners.Add(inner);
            births.Add(clock);
            scales.Add(MathHelper.Clamp(lifeScale, 0.05f, 1f));
        }

        public void Tick()
        {
            clock++;
            int expired = 0;
            while (expired < births.Count && clock - births[expired] > Style.LifeTicks * scales[expired])
                expired++;
            if (expired > 0)
            {
                outers.RemoveRange(0, expired);
                inners.RemoveRange(0, expired);
                births.RemoveRange(0, expired);
                scales.RemoveRange(0, expired);
            }
        }

        // Linear lookup in a per-point array at t (0 = first point, 1 = last point).
        static float Sample(float[] values, int count, float t)
        {
            float x = MathHelper.Clamp(t, 0f, 1f) * (count - 1);
            int i = Math.Min((int)x, count - 2);
            return MathHelper.Lerp(values[i], values[i + 1], x - i);
        }

        public void Draw()
        {
            if (Main.dedServ || outers.Count < 2)
                return;

            int count = outers.Count;
            if (points.Length < count)
            {
                points = new Vector2[count + 16];
                ts = new float[count + 16];
                gaps = new float[count + 16];
                ages = new float[count + 16];
            }

            for (int i = 0; i < count; i++)
            {
                ts[i] = i / (float)(count - 1);
                gaps[i] = (outers[i] - inners[i]).Length();
                ages[i] = MathHelper.Clamp((clock - births[i]) / Math.Max(1f, Style.LifeTicks * scales[i]), 0f, 1f);
            }

            float grid = Style.Grid;
            int rails = Math.Max(2, Style.Rails);
            float[] gapValues = gaps;
            float[] ageValues = ages;

            // The inner rails first and the outer edge last, so the bright tip edge sits on top.
            for (int rail = rails - 1; rail >= 0; rail--)
            {
                float across = rail / (float)(rails - 1);
                for (int i = 0; i < count; i++)
                    points[i] = Vector2.Lerp(outers[i], inners[i], across);
                PixelLine.Draw(points, ts, count, grid,
                    t => Math.Max(grid, Sample(gapValues, count, t) / (rails - 1) * 1.25f),
                    t => Style.GetColor(across, Sample(ageValues, count, t)));
            }
        }
    }
}