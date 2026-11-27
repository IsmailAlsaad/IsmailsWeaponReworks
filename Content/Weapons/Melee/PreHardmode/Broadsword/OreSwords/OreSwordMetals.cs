using Microsoft.Xna.Framework;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.OreSwords
{
    /// <summary>What differs between the eight ore broadswords: the colors of the swing trail (additive, alpha 0: tip edge, middle, toward the hand) and the color the hit flare is tinted toward.</summary>
    public readonly struct OreSpec
    {
        public readonly int Column;
        public readonly Color Core;
        public readonly Color Mid;
        public readonly Color Deep;
        public readonly Color Tint;   // opaque: the flare moves toward this (by OreSwordTuning.FlareTint)

        public OreSpec(int column, Color core, Color mid, Color deep, Color tint)
        {
            Column = column;
            Core = core;
            Mid = mid;
            Deep = deep;
            Tint = tint;
        }
    }

    public static class OreSwordMetals
    {
        public const int MetalCount = 8;

        /// <summary>The metal's column (0..7: copper, tin, iron, lead, silver, tungsten, gold, platinum) for a vanilla item, or -1 when the item is not one of the eight ore broadswords.</summary>
        public static int ColumnOf(int itemType)
        {
            switch (itemType)
            {
                case ItemID.CopperBroadsword: return 0;
                case ItemID.TinBroadsword: return 1;
                case ItemID.IronBroadsword: return 2;
                case ItemID.LeadBroadsword: return 3;
                case ItemID.SilverBroadsword: return 4;
                case ItemID.TungstenBroadsword: return 5;
                case ItemID.GoldBroadsword: return 6;
                case ItemID.PlatinumBroadsword: return 7;
                default: return -1;
            }
        }

        public static OreSpec Spec(int column)
        {
            switch (column)
            {
                case 1: return new OreSpec(1, new Color(215, 210, 180, 0), new Color(165, 155, 120, 0), new Color(100, 92, 70, 0), new Color(215, 205, 160));     // tin
                case 2: return new OreSpec(2, new Color(210, 215, 225, 0), new Color(150, 155, 170, 0), new Color(85, 88, 100, 0), new Color(190, 195, 210));      // iron
                case 3: return new OreSpec(3, new Color(165, 180, 215, 0), new Color(95, 115, 160, 0), new Color(50, 62, 100, 0), new Color(130, 150, 205));       // lead
                case 4: return new OreSpec(4, new Color(235, 245, 255, 0), new Color(190, 205, 225, 0), new Color(120, 135, 160, 0), new Color(215, 230, 250));    // silver
                case 5: return new OreSpec(5, new Color(190, 235, 180, 0), new Color(110, 175, 100, 0), new Color(55, 100, 55, 0), new Color(130, 210, 120));      // tungsten
                case 6: return new OreSpec(6, new Color(255, 235, 140, 0), new Color(240, 195, 60, 0), new Color(170, 115, 25, 0), new Color(255, 215, 70));       // gold
                case 7: return new OreSpec(7, new Color(225, 245, 255, 0), new Color(150, 210, 235, 0), new Color(80, 135, 175, 0), new Color(150, 225, 250));     // platinum
                default: return new OreSpec(0, new Color(255, 190, 130, 0), new Color(225, 125, 60, 0), new Color(150, 70, 35, 0), new Color(240, 140, 70));      // copper
            }
        }
    }
}