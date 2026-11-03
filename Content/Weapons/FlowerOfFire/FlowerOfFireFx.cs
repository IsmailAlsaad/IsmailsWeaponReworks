using IsmailsWeaponReworks.Common;
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
            PixelShaderParams p = new PixelShaderParams
            {
                Grid = FlowerOfFireTuning.PixelSize,
                Time = time,
                Bands = FlowerOfFireTuning.ColorBands,
                Configure = fx =>
                {
                    PixelShaderFx.Set(fx, "uHead", head);
                    PixelShaderFx.Set(fx, "uScale", FlowerOfFireTuning.UnitScale);
                    PixelShaderFx.Set(fx, "uAngle", angle);
                    PixelShaderFx.Set(fx, "uBurst", burst);
                }
            };
            PixelShaderFx.Draw("PixelFireball", head, FlowerOfFireTuning.QuadSize, p);
        }
    }
}