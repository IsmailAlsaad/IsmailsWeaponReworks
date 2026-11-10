using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>Everything a weapon decides about how its coil looks: the textures and the numbers. Each weapon builds one from its own tuning.</summary>
    public struct VineCoilStyle
    {
        public Texture2D SmoothChain;   // the smooth vine (Ivy Whip chain), fades out
        public Texture2D SpinyChain;    // the spiny vine (Thorn Hook chain), fades in
        public Texture2D SmoothHead;
        public Texture2D SpinyHead;
        public float SmoothScale;
        public float SpinyScale;
        public float HeadScale;
        public float HeadForwardPx;     // the head sticks out this far beyond the end of the vine
        public float ChainEndTrimPx;    // the chain stops this far before each end (the head covers the rest)
        public float RadiusScale;       // coil half-size relative to the enemy's half-size
        public float RadiusPad;
        public float MaxLoops;
        public float Drift;             // how far the coil climbs up the enemy (fraction of its height)
        public int Samples;
    }

    /// <summary>
    /// A section of vine coiled around an enemy (moved here from the Blade of Grass spine projectile when the Amazon needed it): the vine
    /// winds around the enemy's outline, the smooth vine fades into the spiny one, and it has a head on each end. Pure drawing: the
    /// caller owns the timing (visible = how much of the section is wound on, spines = how spiny it has become, both 0..1) and the
    /// buffer (kept per projectile so nothing is allocated while drawing).
    /// </summary>
    public static class VineCoil
    {
        public static void Draw(NPC npc, in VineCoilStyle style, ref Vector2[] coil, float sectionLength, float seed, int age, float visible, float spines)
        {
            int samples = Math.Max(8, style.Samples);
            if (coil == null || coil.Length != samples)
                coil = new Vector2[samples];
            float rx = npc.width * 0.5f * style.RadiusScale + style.RadiusPad;
            float ry = npc.height * 0.5f * style.RadiusScale + style.RadiusPad;
            float circumference = MathHelper.TwoPi * (rx + ry) * 0.5f;
            float loops = MathHelper.Clamp(sectionLength / Math.Max(1f, circumference), 1f, Math.Max(1f, style.MaxLoops));
            for (int j = 0; j < samples; j++)
            {
                float u = j / (float)(samples - 1) * visible;
                float angle = seed + u * loops * MathHelper.TwoPi;
                float wobble = (float)Math.Sin(age * 0.2f + u * 9f) * 2f;
                coil[j] = npc.Center + new Vector2((float)Math.Cos(angle) * (rx + wobble), (float)Math.Sin(angle) * (ry + wobble) * 0.85f + (u - 0.5f) * npc.height * style.Drift);
            }

            // The coil has a head on EACH end: the smooth head while it is still smooth, the spiny head once it is spiny.
            Vector2 coilStart = coil[0];
            Vector2 coilEnd = coil[samples - 1];
            bool hasStart = Vector2.DistanceSquared(coilStart, coil[2]) > 0.01f;
            bool hasEnd = Vector2.DistanceSquared(coilEnd, coil[samples - 3]) > 0.01f;
            float startAngle = (coilStart - coil[2]).ToRotation(); // pointing out of the coil at its start
            float endAngle = (coilEnd - coil[samples - 3]).ToRotation();
            Color startLight = Lighting.GetColor(coilStart.ToTileCoordinates());
            Color endLight = Lighting.GetColor(coilEnd.ToTileCoordinates());
            float trim = style.ChainEndTrimPx;

            // The smooth vine fades out while the spiny one fades in; each is one mesh, see VineDraw.DrawStrip.
            VineDraw.BeginMesh();
            VineDraw.DrawStrip(style.SmoothChain, coil, samples, style.SmoothScale, style.SmoothScale, 1f - spines, trim, trim);
            VineDraw.DrawStrip(style.SpinyChain, coil, samples, style.SpinyScale, style.SpinyScale, spines, trim, trim);
            VineDraw.EndMesh();

            if (spines < 1f)
            {
                if (hasStart)
                    VineDraw.DrawHead(style.SmoothHead, coilStart, startAngle, style.HeadScale, startLight * (1f - spines), style.HeadForwardPx);
                if (hasEnd)
                    VineDraw.DrawHead(style.SmoothHead, coilEnd, endAngle, style.HeadScale, endLight * (1f - spines), style.HeadForwardPx);
            }
            if (spines > 0f)
            {
                if (hasStart)
                    VineDraw.DrawHead(style.SpinyHead, coilStart, startAngle, style.HeadScale, startLight * spines, style.HeadForwardPx);
                if (hasEnd)
                    VineDraw.DrawHead(style.SpinyHead, coilEnd, endAngle, style.HeadScale, endLight * spines, style.HeadForwardPx);
            }
        }
    }
}