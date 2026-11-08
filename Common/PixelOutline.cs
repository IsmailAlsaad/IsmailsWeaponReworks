using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The pulsing pixel outline of a held sword sprite: Muramasa's sheathed blue, the Katana's white "ready" glow and Light's Bane's
    /// purple full charge all use it. It is a soft glow copy of the sprite behind the sword, drawn around the CENTER of the blade sprite so
    /// it swells outward evenly (not from the pivot), plus the sprite repeated in the eight directions around it. The colors have alpha 0 =
    /// additive. Purely visual, every machine draws its own. Extracted from the Muramasa and Katana stance projectiles when Light's Bane
    /// needed the same thing; their output did not change.
    /// 'position' is the screen position the sword sprite itself is drawn at (already minus Main.screenPosition), 'rotation' includes the
    /// sprite's rotation offset and 'origin' / 'effects' come from HeldSwordPose.Sprite.
    /// </summary>
    public static class PixelOutline
    {
        static readonly Vector2[] Directions =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f)
        };

        // The pulse: a strength that swings between min and max once every pulseTicks.
        public static float Pulse(int ticks, int pulseTicks, float min, float max)
        {
            float wave = 0.5f + 0.5f * (float)Math.Sin(MathHelper.TwoPi * ticks / Math.Max(1, pulseTicks));
            return MathHelper.Lerp(min, max, wave);
        }

        // Only the soft glow: the sprite drawn bigger around the middle of the blade sprite, in 'color' (alpha 0 = additive).
        public static void DrawGlow(Texture2D texture, Vector2 position, float rotation, float scale, SpriteEffects effects, int spriteDirection, Color color, float glowScale)
        {
            Vector2 hiltToCenter = new Vector2(spriteDirection > 0 ? texture.Width * 0.5f : -texture.Width * 0.5f, -texture.Height * 0.5f);
            Vector2 centerPosition = position + (hiltToCenter * scale).RotatedBy(rotation);
            Vector2 centerOrigin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, centerPosition, null, color, rotation, centerOrigin, scale * glowScale, effects, 0);
        }

        // The whole outline: the glow, then the sprite in eight directions 'width' pixels away in the outline color, then four copies half as
        // far away in the brighter core color. 'strength' (0..1) is the already pulsed visibility.
        public static void Draw(Texture2D texture, Vector2 position, float rotation, Vector2 origin, float scale, SpriteEffects effects, int spriteDirection,
            float strength, float width, float glowScale, Color outlineColor, Color coreColor)
        {
            DrawGlow(texture, position, rotation, scale, effects, spriteDirection, outlineColor * (0.4f * strength), glowScale);

            for (int i = 0; i < Directions.Length; i++)
                Main.EntitySpriteDraw(texture, position + Directions[i] * width, null, outlineColor * (0.9f * strength), rotation, origin, scale, effects, 0);
            for (int i = 0; i < 4; i++)
                Main.EntitySpriteDraw(texture, position + Directions[i] * (width * 0.5f), null, coreColor * (0.5f * strength), rotation, origin, scale, effects, 0);
        }
    }
}