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
        public virtual float TaperFraction => 0f; // 0 = even width; otherwise the oldest samples narrow to a point, widening to full width this far (0 to 1) along the trail
        public virtual bool Smoothing => true;    // the fan's edges are smooth curves (like the Dark Lance strike); false = straight pieces
        public virtual FanLook Look => FanLook.Soft;    // Soft = a smooth mesh with straight edges and a feathered falloff past them; Ribbon = nested PixelLine layers like the Dark Lance strike; Filled; Rails
        public virtual float SoftFeatherPx => 5f;      // Soft: how far past each edge the color keeps going while it fades to nothing (0 = a hard edge)
        public virtual int RibbonLayers => 3;           // Ribbon: 1 to 3 nested layers (halo, body, hot core)
        public virtual float RibbonShrink => 1f;        // Ribbon: 1 = narrows to a point as it ages (the Dark Lance line), 0 = keeps its width and only fades
        public virtual float RibbonWidthScale => 0.85f; // Ribbon: the layer's width as a fraction of its band (a square-stamped line is wider on a diagonal)
        public virtual float RibbonGain => 0.6f;        // Ribbon: every layer's color is multiplied by this (additive layers add up)

        // How the trail fades (see SweepFade). Uniform = the whole trail fades together (the Ice Blade); TailFirst = the tail fades out
        // first and the fade travels up to the tip (the Starfury). Override it in the weapon's style; nothing else has to change.
        public virtual SweepFade Fade => SweepFade.Uniform;
        public virtual float TailLifeScale => 0.45f; // TailFirst only: the first (oldest) point lives this fraction as long as the newest

        public abstract Color GetColor(float across, float age);

        // The usual fade-out curve for GetColor: 1 when just laid, 0 when about to vanish, falling fast at first.
        protected static float Falloff(float age)
        {
            return (1f - age) * (1f - age);
        }

        // The usual three-stop color across the fan: 'tip' on the outer edge (across 0), 'mid' in the middle, 'inner' on the inner edge (across 1).
        protected static Color ThreeStop(float across, Color tip, Color mid, Color inner)
        {
            return across < 0.5f
                ? Color.Lerp(tip, mid, across * 2f)
                : Color.Lerp(mid, inner, (across - 0.5f) * 2f);
        }
    }

    /// <summary>
    /// How a SweepTrail's samples age. Uniform: every sample lives the same LifeTicks, so a trail laid in a short swing fades away
    /// together. TailFirst: the sample laid at the start of the swing lives only Style.TailLifeScale of the life of the last one (the
    /// caller says where in the swing a sample is with the 'along' argument of Add), so the trailing end fades first and the fade
    /// travels up to the tip.
    /// </summary>
    public enum SweepFade
    {
        Uniform,
        TailFirst
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
        Vector2[] outerArray = new Vector2[0];
        Vector2[] innerArray = new Vector2[0];
        float[] expands = new float[0];
        float[] ages = new float[0];
        int clock;

        public SweepTrail(SweepTrailStyle style, bool isOwner)
        {
            Style = style;
            IsOwner = isOwner;
        }

        public int Count => outers.Count;
        public bool IsEmpty => outers.Count == 0;

        // 'along' (0..1) is where in the swing this sample is laid (0 = the start, 1 = the end). Only a TailFirst style uses it: the
        // sample then lives from Style.TailLifeScale (at 0) up to a full life (at 1), so the trailing end fades out first. A Uniform
        // style ignores it, so callers that do not care can leave the default.
        public void Add(Vector2 outer, Vector2 inner, float along = 1f)
        {
            float life = Style.Fade == SweepFade.TailFirst
                ? MathHelper.Lerp(Style.TailLifeScale, 1f, MathHelper.Clamp(along, 0f, 1f))
                : 1f;
            outers.Add(outer);
            inners.Add(inner);
            births.Add(clock);
            scales.Add(MathHelper.Clamp(life, 0.05f, 1f));
        }

        // The usual swing sample, so a weapon does not write the same line again: 'length' is the blade (or streak) length; 'outer' and
        // 'inner' say where the two edges sit along it as fractions of that length (1 = the tip, 0 = the hand; outer above 1 reaches past
        // the tip). For a fan that opens up during the swing pass PixelFan.Expand(...) as 'inner'. 'along' as in Add.
        public void AddBlade(Vector2 hand, Vector2 direction, float length, float outer, float inner, float along = 1f)
        {
            Add(hand + direction * length * outer, hand + direction * length * inner, along);
        }

        // Same idea as AddBlade for a shaft given by its two end points instead of a hand and a direction (a javelin, a spear, a spinning
        // fork): 'outer' and 'inner' are fractions of the way from 'from' (the butt) to 'to' (the tip). No direction is computed, so a
        // shaft of length zero is harmless.
        public void AddSegment(Vector2 from, Vector2 to, float outer, float inner, float along = 1f)
        {
            Vector2 shaft = to - from;
            Add(from + shaft * outer, from + shaft * inner, along);
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

        public void Draw()
        {
            if (Main.dedServ || outers.Count < 2)
                return;

            int count = outers.Count;
            float taperFraction = Style.TaperFraction;
            if (outerArray.Length < count)
            {
                outerArray = new Vector2[count + 16];
                innerArray = new Vector2[count + 16];
                expands = new float[count + 16];
                ages = new float[count + 16];
            }

            for (int i = 0; i < count; i++)
            {
                float along = i / (float)(count - 1);
                outerArray[i] = outers[i];
                innerArray[i] = inners[i];
                expands[i] = taperFraction > 0f ? MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(along / taperFraction, 0f, 1f)) : 1f; // 0 at the oldest sample
                ages[i] = MathHelper.Clamp((clock - births[i]) / Math.Max(1f, Style.LifeTicks * scales[i]), 0f, 1f);
            }

            PixelFan.Draw(outerArray, innerArray, expands, ages, count, Style.Grid, Style.Rails, Style.GetColor, Style.Smoothing, Style.Look, Style.RibbonLayers, Style.RibbonShrink, Style.RibbonWidthScale, Style.RibbonGain, Style.SoftFeatherPx);
        }
    }
}