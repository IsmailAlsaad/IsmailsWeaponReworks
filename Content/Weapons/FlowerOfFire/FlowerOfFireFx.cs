using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.FlowerOfFire
{
    /// <summary>
    /// Sound and drawing for the Flower of Fire shader fireball. DrawFireball draws Effects/PixelFireball.fx over a square quad centered
    /// on the fireball (visual only, never synced, every machine draws it from the projectile's own state).
    /// </summary>
    public static class FlowerOfFireFx
    {
        public static void Burst(Vector2 center)
        {
            SoundEngine.PlaySound(SoundID.Item74, center);
        }

        public static void DrawFireball(Vector2 head, float angle, float time, float burst)
        {
            if (Main.dedServ)
                return;

            int size = FlowerOfFireTuning.QuadSize;
            Vector2 topLeft = head - new Vector2(size / 2f) - Main.screenPosition;
            Rectangle dest = new Rectangle((int)Math.Floor(topLeft.X), (int)Math.Floor(topLeft.Y), size, size);
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            Main.spriteBatch.End();

            if (!ModContent.RequestIfExists<Effect>("IsmailsWeaponReworks/Effects/PixelFireball", out Asset<Effect> asset, AssetRequestMode.ImmediateLoad))
            {
                // The shader did not compile or load: a plain orange square so the failure is visible.
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(pixel, new Rectangle((int)Math.Floor(head.X - 10f - Main.screenPosition.X), (int)Math.Floor(head.Y - 10f - Main.screenPosition.Y), 20, 20), Color.OrangeRed);
                Main.spriteBatch.End();
            }
            else
            {
                Effect fx = asset.Value;
                Set(fx, "uOrigin", new Vector2(dest.X, dest.Y) + Main.screenPosition);
                Set(fx, "uSize", new Vector2(size, size));
                Set(fx, "uHead", head);
                Set(fx, "uScale", FlowerOfFireTuning.UnitScale);
                Set(fx, "uGrid", FlowerOfFireTuning.PixelSize);
                Set(fx, "uTime", time);
                Set(fx, "uAngle", angle);
                Set(fx, "uBands", FlowerOfFireTuning.ColorBands);
                Set(fx, "uDither", FlowerOfFireTuning.Dither);
                Set(fx, "uBurst", burst);

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, fx, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(pixel, dest, Color.White);
                Main.spriteBatch.End();
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        static void Set(Effect fx, string name, float value) { fx.Parameters[name]?.SetValue(value); }
        static void Set(Effect fx, string name, Vector2 value) { fx.Parameters[name]?.SetValue(value); }
    }
}