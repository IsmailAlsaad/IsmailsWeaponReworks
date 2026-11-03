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
        public static void DrawSprite(Texture2D texture, Vector2 butt, float worldRotation, int spriteDirection, float scale, Color color)
        {
            HeldSwordPose.Sprite(texture, spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            Main.EntitySpriteDraw(texture, butt - Main.screenPosition, null, color, worldRotation + rotationOffset, origin, scale, effects, 0);
        }
    }
}