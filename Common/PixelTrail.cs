using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// How a PixelTrail looks. A weapon subclasses this once (see Flamerang's MoltenTrailStyle) and every number reads the weapon's
    /// Tuning live, so hot reload works. 'age' runs from 0 (just laid) to 1 (about to vanish). Layer 0 is drawn first (the wide
    /// outer glow), higher layers on top of it (narrower, hotter).
    /// </summary>
    public abstract class PixelTrailStyle
    {
        public virtual float Grid => 2f;
        public virtual int LifeTicks => 40;
        public virtual int Layers => 1;
        public virtual float Jitter => 0f;   // pixels of fixed random offset per point, for a ragged edge
        public virtual bool Smoothing => true;    // a curve through the points (PixelLine smoothing, the Dark Lance look); override with false for a jagged line such as lightning

        public abstract float GetWidth(int layer, float age);
        public abstract Color GetColor(int layer, float age);

        /// <summary>Runs once per tick for the trail, while its projectile lives and after it has died (see DetachedTrails). Use it for
        /// embers or for anything the trail does to the world; check trail.IsOwner before doing anything that must only happen once.</summary>
        public virtual void Update(PixelTrail trail)
        {
        }
    }

    /// <summary>
    /// A trail of points laid down by a projectile, drawn as a PixelLine. Every point carries its own age, so a trail with no new points
    /// simply shrinks away from the tail: that is what makes it fade out gradually. A projectile keeps one, calls Add and Tick every
    /// tick and Draw in PreDraw; in OnKill it hands the trail to DetachedTrails.Hand so it keeps fading after the projectile is gone.
    /// Purely visual state, never synced: every machine builds its own from its own copy of the projectile. IsOwner marks the owner's
    /// machine, the only one that may apply buffs or damage from a style's Update.
    /// </summary>
    public class PixelTrail
    {
        public readonly PixelTrailStyle Style;
        public readonly bool IsOwner;

        readonly List<Vector2> positions = new List<Vector2>();
        readonly List<int> births = new List<int>();
        Vector2[] drawPoints = new Vector2[0];
        float[] drawAges = new float[0];
        int clock;

        public PixelTrail(PixelTrailStyle style, bool isOwner)
        {
            Style = style;
            IsOwner = isOwner;
        }

        public int Count => positions.Count;
        public bool IsEmpty => positions.Count == 0;
        public int Clock => clock;

        // Index 0 is the oldest point, Count - 1 the newest.
        public Vector2 PointAt(int index) => positions[index];

        public float AgeAt(int index) => MathHelper.Clamp((clock - births[index]) / (float)Math.Max(1, Style.LifeTicks), 0f, 1f);

        public void Add(Vector2 position)
        {
            positions.Add(position);
            births.Add(clock);
        }

        // Makes every point vanish within 'ticks' ticks from now (a trail that has to give way to another one). Points that are already
        // older than that are left alone, so nothing gets younger.
        public void FadeWithin(int ticks)
        {
            int latestBirth = clock - Style.LifeTicks + Math.Max(1, ticks);
            for (int i = 0; i < births.Count; i++)
                births[i] = Math.Min(births[i], latestBirth);
        }

        public void Tick()
        {
            clock++;
            int expired = 0;
            while (expired < births.Count && clock - births[expired] > Style.LifeTicks)
                expired++;
            if (expired > 0)
            {
                positions.RemoveRange(0, expired);
                births.RemoveRange(0, expired);
            }
            Style.Update(this);
        }

        public void Draw()
        {
            if (Main.dedServ || positions.Count < 2)
                return;

            int count = positions.Count;
            if (drawPoints.Length < count)
            {
                drawPoints = new Vector2[count + 16];
                drawAges = new float[count + 16];
            }

            float jitter = Style.Jitter;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = positions[i];
                if (jitter > 0f)
                    point += new Vector2((births[i] * 7 % 9 - 4) / 4f, (births[i] * 11 % 9 - 4) / 4f) * jitter;
                drawPoints[i] = point;
                drawAges[i] = AgeAt(i);
            }

            float grid = Style.Grid;
            for (int layer = 0; layer < Style.Layers; layer++)
            {
                int l = layer;
                PixelLine.Draw(drawPoints, drawAges, count, grid, age => Style.GetWidth(l, age), age => Style.GetColor(l, age), Style.Smoothing);
            }
        }
    }
}