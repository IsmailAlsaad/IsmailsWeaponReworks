using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.CameraModifiers;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// One camera punch for every weapon (Sunfury, Starfury and the Blood Butcherer use it; never write a new PunchCameraModifier call).
    /// Every machine calls it for the same event and the strength falls off with the distance from 'center', so a far-away player
    /// barely feels it. Visual only, never synced. 'uniqueId' lets two different punches stack instead of replacing each other.
    /// </summary>
    public static class CameraShake
    {
        public static void Punch(Vector2 center, float strength, int frames = 20, string uniqueId = "IsmailsWeaponReworksPunch")
        {
            if (Main.dedServ)
                return;
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(center, Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2(), strength, 7f, frames, 1400f, uniqueId));
        }
    }
}