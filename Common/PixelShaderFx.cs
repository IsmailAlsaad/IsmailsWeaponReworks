using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Everything a shader-drawn effect can be told. Unset fields keep their defaults. Fields that map to a standard uniform of
    /// Effects/PixelShared.fxh are set on every draw; anything shader-specific goes through Configure. Visual state only, never synced.
    /// </summary>
    public class PixelShaderParams
    {
        public float Grid = 2f;                      // uGrid: world px per shader pixel
        public float Time;                           // uTime
        public float Seed;                           // uSeed (use something every machine knows, like the projectile's whoAmI)
        public float Progress;                       // uProgress 0..1
        public float Intensity = 1f;                 // uIntensity
        public float Bands = 6f;                     // uBands
        public bool Ellipse;                         // uShape: false = rectangle mask, true = ellipse mask (shader calls quadMask)
        public Color ColorA = Color.DarkRed;         // uColorA
        public Color ColorB = Color.OrangeRed;       // uColorB
        public Color ColorC = Color.White;           // uColorC
        public Vector4 Params;                       // uParams
        public Vector4 Params2;                      // uParams2
        public Texture2D Ramp;                       // uRamp: hand-painted gradient, left to right; null = use ColorA/B/C
        public BlendState Blend = BlendState.AlphaBlend;   // the shader outputs premultiplied color, alpha 0 = additive
        public Color FallbackColor = Color.OrangeRed;      // the square drawn when the effect failed to load
        public Action<Effect> Configure;             // extra uniforms: fx => { PixelShaderFx.Set(fx, "uHead", head); }
    }

    /// <summary>
    /// The shared way to draw a pixel shader effect (Effects/&lt;name&gt;.fx, which includes Effects/PixelShared.fxh): ends the sprite batch,
    /// sets the standard uniforms, draws a world-anchored quad centered on a point, then restores the normal sprite batch.
    /// The quad is axis-aligned; rotate inside the shader (rotate2) with a value from Params or Configure.
    /// Call it from PreDraw / PostDraw of a projectile or a draw layer, on every machine, from state the machine already has.
    /// </summary>
    public static class PixelShaderFx
    {
        public static void Draw(string effectName, Vector2 center, int width, int height, PixelShaderParams p)
        {
            if (Main.dedServ)
                return;

            Vector2 topLeft = center - new Vector2(width / 2f, height / 2f) - Main.screenPosition;
            Rectangle dest = new Rectangle((int)Math.Floor(topLeft.X), (int)Math.Floor(topLeft.Y), width, height);
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            Main.spriteBatch.End();

            if (!ModContent.RequestIfExists<Effect>("IsmailsWeaponReworks/Effects/" + effectName, out Asset<Effect> asset, AssetRequestMode.ImmediateLoad))
            {
                // The shader did not compile or load: a plain square so the failure is visible.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(pixel, new Rectangle((int)Math.Floor(center.X - 10f - Main.screenPosition.X), (int)Math.Floor(center.Y - 10f - Main.screenPosition.Y), 20, 20), p.FallbackColor);
                Main.spriteBatch.End();
            }
            else
            {
                Effect fx = asset.Value;
                Set(fx, "uOrigin", new Vector2(dest.X, dest.Y) + Main.screenPosition);
                Set(fx, "uSize", new Vector2(width, height));
                Set(fx, "uGrid", Math.Max(p.Grid, 1f));
                Set(fx, "uTime", p.Time);
                Set(fx, "uSeed", p.Seed);
                Set(fx, "uProgress", p.Progress);
                Set(fx, "uIntensity", p.Intensity);
                Set(fx, "uBands", p.Bands);
                Set(fx, "uShape", p.Ellipse ? 1f : 0f);
                Set(fx, "uUseRamp", p.Ramp != null ? 1f : 0f);
                Set(fx, "uColorA", p.ColorA.ToVector3());
                Set(fx, "uColorB", p.ColorB.ToVector3());
                Set(fx, "uColorC", p.ColorC.ToVector3());
                Set(fx, "uParams", p.Params);
                Set(fx, "uParams2", p.Params2);
                p.Configure?.Invoke(fx);

                GraphicsDevice device = Main.graphics.GraphicsDevice;
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, p.Blend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, fx, Main.GameViewMatrix.TransformationMatrix);
                if (p.Ramp != null)
                {
                    device.Textures[1] = p.Ramp;
                    device.SamplerStates[1] = SamplerState.PointClamp;
                }
                Main.spriteBatch.Draw(pixel, dest, Color.White);
                Main.spriteBatch.End();
                if (p.Ramp != null)
                    device.Textures[1] = null;
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public static void Draw(string effectName, Vector2 center, int size, PixelShaderParams p)
        {
            Draw(effectName, center, size, size, p);
        }

        // A missing uniform is skipped (the compiler drops uniforms a shader does not use).
        public static void Set(Effect fx, string name, float value) { fx.Parameters[name]?.SetValue(value); }
        public static void Set(Effect fx, string name, Vector2 value) { fx.Parameters[name]?.SetValue(value); }
        public static void Set(Effect fx, string name, Vector3 value) { fx.Parameters[name]?.SetValue(value); }
        public static void Set(Effect fx, string name, Vector4 value) { fx.Parameters[name]?.SetValue(value); }
    }
}