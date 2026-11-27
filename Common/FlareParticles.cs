using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// How a flare particle looks (a FlareParticleSet draws it). A weapon subclasses this once and every number reads the weapon's Tuning live,
    /// so hot reload works. The PNG is an additive glow: black = nothing, white = full, with a gradient between (it is drawn with alpha 0, so
    /// black adds nothing whether or not the file has an alpha channel).
    /// </summary>
    public abstract class FlareParticleStyle
    {
        public abstract string AssetPath { get; }               // like "IsmailsWeaponReworks/Assets/Effects/SmallFlare" (no extension)
        public virtual int LifeTicks => 18;
        public virtual float PeakScale => 0.9f;                 // the size at its biggest (1 = the PNG's own size)
        public virtual float GrowFraction => 0.3f;              // the first this much (0..1) of its life it grows, then it shrinks to 0
        public virtual float SpinMin => 0.02f;                  // radians per tick; every particle picks one constant spin between Min and Max, random direction
        public virtual float SpinMax => 0.06f;
        public virtual float Opacity => 1f;
        public virtual float Light => 0.3f;                     // light it gives while it is big (0 = none)
        public virtual int MaxParticles => 24;                  // per set: one more removes the oldest
    }

    /// <summary>
    /// A bunch of small flares: add one where something hits, call Tick every tick and Draw from PreDraw (a spritebatch must be running). Every
    /// flare starts at a random rotation, spins at a constant slight speed, grows quickly to its peak size and then shrinks to nothing.
    /// Purely visual, never synced: every machine adds its own when it sees the hit. Hand the set to DetachedFlares in OnKill so the flares
    /// finish their life after the projectile that owns the set is gone.
    /// </summary>
    public class FlareParticleSet
    {
        class Flare
        {
            public Vector2 Position;
            public float Rotation;
            public float Spin;
            public int Age;
            public int Life;
            public Color Color;
        }

        public readonly FlareParticleStyle Style;
        readonly List<Flare> flares = new List<Flare>();
        Asset<Texture2D> art;
        bool artTried;

        public FlareParticleSet(FlareParticleStyle style)
        {
            Style = style;
        }

        public bool IsEmpty => flares.Count == 0;

        public void Add(Vector2 position, Color color)
        {
            if (Main.dedServ)
                return;

            float spin = Main.rand.NextFloat(Style.SpinMin, Math.Max(Style.SpinMin, Style.SpinMax)) * (Main.rand.NextBool() ? 1f : -1f);
            flares.Add(new Flare
            {
                Position = position,
                Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                Spin = spin,
                Age = 0,
                Life = Math.Max(1, Style.LifeTicks),
                Color = color
            });
            if (flares.Count > Math.Max(1, Style.MaxParticles))
                flares.RemoveAt(0);
        }

        // 0..1: grows fast (ease out) over GrowFraction of the life, then shrinks smoothly to 0 at the end.
        float Size(Flare flare)
        {
            float t = flare.Age / (float)flare.Life;
            float grow = MathHelper.Clamp(Style.GrowFraction, 0.01f, 0.99f);
            if (t < grow)
            {
                float x = t / grow;
                return 1f - (1f - x) * (1f - x);
            }
            return MathHelper.SmoothStep(1f, 0f, (t - grow) / (1f - grow));
        }

        public void Tick()
        {
            for (int i = flares.Count - 1; i >= 0; i--)
            {
                Flare flare = flares[i];
                flare.Age++;
                flare.Rotation += flare.Spin;
                if (flare.Age >= flare.Life)
                {
                    flares.RemoveAt(i);
                    continue;
                }
                float size = Size(flare);
                if (Style.Light > 0f && size > 0.05f)
                    Lighting.AddLight(flare.Position, flare.Color.ToVector3() * (Style.Light * size));
            }
        }

        public void Draw()
        {
            if (Main.dedServ || flares.Count == 0)
                return;

            if (!artTried)
            {
                artTried = true;
                ModContent.RequestIfExists(Style.AssetPath, out art);
            }
            Texture2D texture = art != null && art.IsLoaded ? art.Value : null;

            for (int i = 0; i < flares.Count; i++)
            {
                Flare flare = flares[i];
                float scale = Style.PeakScale * Size(flare);
                if (scale <= 0.01f)
                    continue;

                Color color = flare.Color;
                color.A = 0; // alpha 0 = additive: black adds nothing
                color = color * Style.Opacity;
                Vector2 position = flare.Position - Main.screenPosition;
                if (texture != null)
                    Main.EntitySpriteDraw(texture, position, null, color, flare.Rotation, new Vector2(texture.Width, texture.Height) * 0.5f, scale, SpriteEffects.None, 0);
                else // no PNG yet: a small spinning square in the same color
                    Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, position, new Rectangle(0, 0, 1, 1), color, flare.Rotation, new Vector2(0.5f), new Vector2(7f) * scale, SpriteEffects.None, 0);
            }
        }
    }

    /// <summary>
    /// Keeps FlareParticleSets alive after their projectile has died, so the flares finish shrinking instead of vanishing. A projectile calls
    /// DetachedFlares.Hand(set) from OnKill, on every machine. Client only, nothing is synced. Drawn after the tiles (behind enemies); these are
    /// only the last few ticks of flares, so it is not noticeable.
    /// </summary>
    public class DetachedFlares : ModSystem
    {
        readonly List<FlareParticleSet> sets = new List<FlareParticleSet>();

        static int MaxSets => 32;

        public static void Hand(FlareParticleSet set)
        {
            if (Main.dedServ || set == null || set.IsEmpty)
                return;

            DetachedFlares system = ModContent.GetInstance<DetachedFlares>();
            system.sets.Add(set);
            if (system.sets.Count > MaxSets)
                system.sets.RemoveAt(0);
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gameMenu || Main.gamePaused)
                return;

            for (int i = sets.Count - 1; i >= 0; i--)
            {
                sets[i].Tick();
                if (sets[i].IsEmpty)
                    sets.RemoveAt(i);
            }
        }

        public override void PostDrawTiles()
        {
            if (Main.dedServ || sets.Count == 0)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < sets.Count; i++)
                sets[i].Draw();
            Main.spriteBatch.End();
        }

        public override void OnWorldUnload()
        {
            sets.Clear();
        }

        public override void Unload()
        {
            sets.Clear();
        }
    }
}