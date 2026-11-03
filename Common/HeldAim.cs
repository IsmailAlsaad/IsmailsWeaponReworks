using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Capturing the cursor direction for a held-projectile weapon. The angle is kept in the PLAYER'S OWN FRAME (mirrored when gravity is
    /// flipped), the same convention the Starfury and Blood Butcherer use inline, so it can travel in a projectile's spawn packet and every
    /// machine poses the same arm. Capture reads Main.MouseWorld, so it only works on the owner's client (call it from Shoot).
    /// </summary>
    public static class HeldAim
    {
        public static float Capture(Player player)
        {
            Vector2 toMouse = Main.MouseWorld - player.MountedCenter;
            float aim = toMouse.LengthSquared() > 1f ? toMouse.ToRotation() : (player.direction >= 0 ? 0f : MathHelper.Pi);
            return player.gravDir == -1f ? -aim : aim;
        }

        // The world angle of an aim given in the player's own frame.
        public static float ToWorld(Player player, float aim)
        {
            return player.gravDir == -1f ? -aim : aim;
        }

        public static Vector2 ToWorldDirection(Player player, float aim)
        {
            return ToWorld(player, aim).ToRotationVector2();
        }

        // Which side the player faces for an aim: 1 = right, -1 = left (straight up or down counts as right).
        public static int Facing(float aim)
        {
            return Math.Cos(aim) >= 0.0 ? 1 : -1;
        }
    }
}