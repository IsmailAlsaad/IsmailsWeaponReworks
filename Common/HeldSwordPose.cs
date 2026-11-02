using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Shared pose math for a held-projectile sword (the Ice Blade and the Starfury use it): where the hand is for a given arm angle,
    /// which way the blade points, and how the vanilla item sprite is oriented when drawn. Angles are "local": for a player facing
    /// right, 0 = forward and positive = down; ToWorld mirrors them when the player faces left. Extracted from the Ice Blade's
    /// swing projectile when the Starfury needed the same thing.
    /// </summary>
    public static class HeldSwordPose
    {
        public static float ToWorld(int facing, float localRadians)
        {
            return facing == 1 ? localRadians : MathHelper.Pi - localRadians;
        }

        public static Vector2 Hand(Player owner, int facing, float arm)
        {
            Vector2 hand = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, ToWorld(facing, arm) - MathHelper.PiOver2);
            if (owner.gravDir == -1f)
                hand.Y = owner.Bottom.Y + (owner.position.Y - hand.Y);
            hand.Y += owner.gfxOffY;
            return hand;
        }

        public static Vector2 BladeDirection(Player owner, int facing, float arm, float tilt)
        {
            float angle = ToWorld(facing, arm + tilt);
            if (owner.gravDir == -1f)
                angle = -angle;
            return angle.ToRotationVector2();
        }

        // The same two for an arm and blade angle given directly as WORLD angles: a swing that is aimed at something is not relative to the
        // facing. Added for the Starfury; the Ice Blade keeps using the facing-local versions above (its behavior is unchanged).
        public static Vector2 HandWorld(Player owner, float worldArm)
        {
            Vector2 hand = owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, worldArm - MathHelper.PiOver2);
            if (owner.gravDir == -1f)
                hand.Y = owner.Bottom.Y + (owner.position.Y - hand.Y);
            hand.Y += owner.gfxOffY;
            return hand;
        }

        public static Vector2 BladeDirectionWorld(Player owner, float worldBlade)
        {
            return (owner.gravDir == -1f ? -worldBlade : worldBlade).ToRotationVector2();
        }

        // Origin, rotation offset and flip for drawing a vanilla sword item sprite (hilt in the bottom-left corner) along Projectile.rotation.
        public static void Sprite(Texture2D texture, int spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects)
        {
            if (spriteDirection > 0)
            {
                origin = new Vector2(0f, texture.Height); // the hilt is the bottom-left corner of the sprite
                rotationOffset = MathHelper.ToRadians(45f);
                effects = SpriteEffects.None;
            }
            else
            {
                origin = new Vector2(texture.Width, texture.Height);
                rotationOffset = MathHelper.ToRadians(135f);
                effects = SpriteEffects.FlipHorizontally;
            }
        }
    }
}