using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Pose and drawing for a held spear (a vanilla spear item sprite: a 45 degree diagonal with the butt in the bottom-left corner),
    /// built on the same math as HeldSwordPose. 'aim' is an angle in the player's own frame (see HeldAim).
    /// </summary>
    public static class SpearPose
    {
        // Stretches the owner's front arm toward the aim.
        public static void Arm(Player owner, float aim)
        {
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
        }

        public static Vector2 Hand(Player owner, float aim)
        {
            return HeldSwordPose.HandWorld(owner, aim);
        }

        // Draws the item sprite with its butt at 'butt' pointing along 'worldRotation' (the world angle of the shaft).
        // centerScale: for a glow copy (a bigger sprite behind the real one) pass the REAL sprite's scale here. The copy then grows around the
        // middle of the sprite instead of around its pivot (the butt), so the glow is an even outline instead of sticking out at the tip only.
        public static void DrawSprite(Texture2D texture, Vector2 butt, float worldRotation, int spriteDirection, float scale, Color color, float extraRotation = 0f, Vector2 pivotShift = default, float centerScale = 0f)
        {
            HeldSwordPose.Sprite(texture, spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            origin += new Vector2(pivotShift.X * (spriteDirection >= 0 ? 1f : -1f), pivotShift.Y);   // <- new line
            float rotation = worldRotation + rotationOffset + extraRotation * (spriteDirection >= 0 ? 1f : -1f);
            Vector2 position = butt;
            if (centerScale > 0f && centerScale != scale)
            {
                Vector2 toCenter = texture.Size() * 0.5f - origin;   // pivot to sprite middle, in texture pixels (a flip mirrors the art, not the quad, so it needs no sign change)
                position += toCenter.RotatedBy(rotation) * (centerScale - scale);   // moves the copy so its middle sits where the real sprite's middle is
            }
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color, rotation, origin, scale, effects, 0);
        }
    }
}