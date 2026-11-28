using System;
using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// The Candy Cane Sword's purple easter egg, only in the getfixedboi seed (the Zenith seed, Main.zenithWorld, through EasterEgg.Zenith; to change
    /// which seed it is, change Active only). In that seed:
    /// - the sword is PurpleCandyCaneSword.png (44 x 70, one sprite): the held sword, the inventory icon and the item lying in the world, whatever the next candy color is;
    /// - every candy is drawn from PurpleCandy.png (160 x 80: 4 columns green, blue, yellow, red, each with 2 style rows) but still behaves as its own kind;
    /// - the tooltip gets a quote in purple (CandyCaneSwordGlobalItem.ModifyTooltips);
    /// - picking up a green or a yellow candy also gives the Grape buff (CandyPickup.Apply, GrapeBuff, CandyCaneSwordPlayer.UpdateDyes).
    /// A missing PNG falls back to the normal art. Both PNGs sit next to this file.
    /// MULTIPLAYER: the art and the tooltip are visuals only, nothing synced: every machine reads the world's own seed flag (the server sends it on join).
    /// </summary>
    public static class CandyCaneSwordPurple
    {
        public static bool Active => EasterEgg.Zenith;

        static string Folder => "IsmailsWeaponReworks/Content/Weapons/Melee/PreHardmode/Broadsword/CandyCaneSword/";

        static Asset<Texture2D> candies;
        static bool candiesTried;
        static Asset<Texture2D> sword;
        static bool swordTried;

        // PurpleCandy.png while the seed is on and the file exists, otherwise null. Loaded at once, so a candy never shows its normal art for a moment first.
        public static Texture2D CandySheet()
        {
            if (!Active || Main.dedServ)
                return null;
            return EasterEgg.Load(Folder + "PurpleCandy", ref candies, ref candiesTried, AssetRequestMode.ImmediateLoad);
        }

        // PurpleCandyCaneSword.png while the seed is on and the file exists, otherwise null.
        public static Texture2D Sword()
        {
            if (!Active || Main.dedServ)
                return null;
            return EasterEgg.Load(Folder + "PurpleCandyCaneSword", ref sword, ref swordTried, AssetRequestMode.ImmediateLoad);
        }

        // The cell of PurpleCandy.png for a candy of this kind and style (the style is the candy's ai[1], 0 to StyleCount - 1; it only picks the row here).
        public static Rectangle Cell(CandyKind kind, int style)
        {
            int size = CandyCaneSwordTuning.CellSize;
            int row = Math.Max(0, style) % Math.Max(1, CandyCaneSwordTuning.PurpleStyleCount);
            return new Rectangle(Column(kind) * size, row * size, size, size);
        }

        // PurpleCandy.png's column order is not Candy.png's: green, blue, yellow, red.
        static int Column(CandyKind kind)
        {
            switch (kind)
            {
                case CandyKind.Green: return 0;
                case CandyKind.Blue: return 1;
                case CandyKind.Yellow: return 2;
                default: return 3;
            }
        }

        // Called from CandyCaneSwordGlobalItem.Unload so a reload never keeps a texture of the unloaded mod.
        public static void Unload()
        {
            candies = null;
            candiesTried = false;
            sword = null;
            swordTried = false;
        }
    }
}
