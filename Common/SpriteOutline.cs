using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The pulsing pixel outline of a charged weapon (the Dark Lance's primed lance, the Night's Edge's charged blade), lifted out of the
    /// Dark Lance when a second weapon needed it: a soft glow copy behind the sprite, the sprite repeated in the eight directions around it
    /// and a thinner, brighter ring in the four straight directions. The caller supplies how to draw ONE copy of its sprite (it knows its
    /// own origin, rotation and flip), so this works for any sprite. The colors should have alpha 0 (additive). Visual only.
    /// </summary>
    public static class SpriteOutline
    {
        /// <summary>Draws one copy of the sprite. centerScale above 0 means: grow the copy around the MIDDLE of the sprite (the glow copy), where centerScale is the normal scale.</summary>
        public delegate void DrawCopy(Color color, float scale, float centerScale, Vector2 offset);

        static readonly Vector2[] Directions =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f)
        };

        // strength 0..1 (fade and pulse already applied), width = the outline's thickness in screen pixels, glowScale = size of the soft copy
        // behind it relative to 'scale' (the sprite's normal draw scale).
        public static void Draw(float strength, float width, float glowScale, float scale, Color outlineColor, Color coreColor, DrawCopy draw)
        {
            if (strength <= 0f)
                return;
            draw(outlineColor * (0.4f * strength), scale * glowScale, scale, default);
            for (int i = 0; i < Directions.Length; i++)
                draw(outlineColor * (0.9f * strength), scale, 0f, Directions[i] * width);
            for (int i = 0; i < 4; i++)
                draw(coreColor * (0.5f * strength), scale, 0f, Directions[i] * (width * 0.5f));
        }
    }
}