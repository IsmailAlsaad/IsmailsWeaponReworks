using Microsoft.Xna.Framework;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords
{
    /// <summary>What differs between the eight wooden swords. Column = the wood's column in Splinters.png (left to right: wooden, boreal, palm, rich mahogany, shadewood, ebonwood, ash, cactus).</summary>
    public readonly struct WoodSpec
    {
        public readonly int Column;
        public readonly int MaxSplinters;
        public readonly int LifeTicks;
        public readonly Color Core;   // trail colors, additive (alpha 0): tip edge, middle, toward the hand
        public readonly Color Mid;
        public readonly Color Deep;

        public WoodSpec(int column, int maxSplinters, int lifeTicks, Color core, Color mid, Color deep)
        {
            Column = column;
            MaxSplinters = maxSplinters;
            LifeTicks = lifeTicks;
            Core = core;
            Mid = mid;
            Deep = deep;
        }
    }

    public static class WoodenSwordWoods
    {
        public const int WoodCount = 8;

        /// <summary>The wood's column (0..7) for a vanilla item, or -1 when the item is not one of the eight swords.</summary>
        public static int ColumnOf(int itemType)
        {
            switch (itemType)
            {
                case ItemID.WoodenSword: return 0;
                case ItemID.BorealWoodSword: return 1;
                case ItemID.PalmWoodSword: return 2;
                case ItemID.RichMahoganySword: return 3;
                case ItemID.ShadewoodSword: return 4;
                case ItemID.EbonwoodSword: return 5;
                case ItemID.AshWoodSword: return 6;
                case ItemID.CactusSword: return 7;
                default: return -1;
            }
        }

        /// <summary>The vanilla dust of a wood's column (the chips of the splinters). Palm, Rich Mahogany and Ash are plain wood dust (7) until their ids are checked on the wiki's Dust IDs page; change the numbers here.</summary>
        public static int DustOf(int column)
        {
            switch (column)
            {
                case 1: return 122;  // boreal wood
                case 2: return 7;    // palm wood (check)
                case 3: return 7;    // rich mahogany (check)
                case 4: return 121;  // shadewood
                case 5: return 77;   // ebonwood
                case 6: return 36;    // ash wood (check)
                case 7: return DustID.t_Cactus; // cactus (40)
                default: return DustID.WoodFurniture; // wood (7)
            }
        }

        public static WoodSpec Spec(int column)
        {
            int max = WoodenSwordTuning.MaxSplinters;
            int life = WoodenSwordTuning.LifeTicks;
            if (column == 7)
            {
                max = WoodenSwordTuning.CactusMaxSplinters;
                life = WoodenSwordTuning.CactusLifeTicks;
            }

            switch (column)
            {
                case 1: return new WoodSpec(1, max, life, new Color(230, 215, 190, 0), new Color(170, 140, 105, 0), new Color(95, 75, 55, 0));
                case 2: return new WoodSpec(2, max, life, new Color(245, 225, 170, 0), new Color(215, 175, 100, 0), new Color(140, 100, 50, 0));
                case 3: return new WoodSpec(3, max, life, new Color(225, 150, 120, 0), new Color(165, 80, 60, 0), new Color(90, 40, 35, 0));
                case 4: return new WoodSpec(4, max, life, new Color(200, 110, 120, 0), new Color(120, 50, 70, 0), new Color(55, 25, 45, 0));
                case 5: return new WoodSpec(5, max, life, new Color(190, 160, 230, 0), new Color(110, 85, 160, 0), new Color(50, 40, 85, 0));
                case 6: return new WoodSpec(6, max, life, new Color(255, 200, 120, 0), new Color(210, 110, 60, 0), new Color(100, 50, 35, 0));
                case 7: return new WoodSpec(7, max, life, new Color(200, 245, 150, 0), new Color(100, 190, 70, 0), new Color(40, 105, 45, 0));
                default: return new WoodSpec(0, max, life, new Color(235, 200, 140, 0), new Color(190, 140, 80, 0), new Color(110, 70, 35, 0));
            }
        }
    }
}