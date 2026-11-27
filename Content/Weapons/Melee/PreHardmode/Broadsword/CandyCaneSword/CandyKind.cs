using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>The four candies. The number is stored in a candy projectile's ai[0] and is the block of two rows in Candy.png (red 0-1, green 2-3, blue 4-5, yellow 6-7).</summary>
    public enum CandyKind
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Yellow = 3
    }

    /// <summary>Everything that differs per candy kind in one place: color, dust, lifetime, whether it is picked up.</summary>
    public static class CandyKinds
    {
        public static Color Tint(CandyKind kind)
        {
            switch (kind)
            {
                case CandyKind.Red: return new Color(235, 55, 75);
                case CandyKind.Green: return new Color(90, 215, 100);
                case CandyKind.Blue: return new Color(90, 160, 255);
                default: return new Color(255, 225, 90);
            }
        }

        public static int DustOf(CandyKind kind)
        {
            switch (kind)
            {
                case CandyKind.Red: return DustID.RedTorch;
                case CandyKind.Green: return DustID.GreenTorch;
                case CandyKind.Blue: return DustID.BlueTorch;
                default: return DustID.YellowTorch;
            }
        }

        public static int LifeTicks(CandyKind kind)
        {
            switch (kind)
            {
                case CandyKind.Red: return CandyCaneSwordTuning.RedLifeTicks;
                case CandyKind.Green: return CandyCaneSwordTuning.GreenLifeTicks;
                case CandyKind.Blue: return CandyCaneSwordTuning.BlueLifeTicks;
                default: return CandyCaneSwordTuning.YellowLifeTicks;
            }
        }

        // Green and yellow are picked up by players.
        public static bool IsPickup(CandyKind kind)
        {
            return kind == CandyKind.Green || kind == CandyKind.Yellow;
        }

        // Owner's client: a random kind, every enabled kind equally likely.
        public static CandyKind Pick()
        {
            int count = CandyCaneSwordTuning.BlueEnabled ? 4 : 3;
            int roll = Main.rand.Next(count);
            if (!CandyCaneSwordTuning.BlueEnabled && roll >= (int)CandyKind.Blue)
                roll++; // skip blue
            return (CandyKind)roll;
        }
    }
}