using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common.Rework
{
    /// <summary>
    /// The one place every rework GlobalItem asks "do I apply to this item?". Used from AppliesToEntity:
    /// <c>return ReworkToggle.Applies(entity.type, entity.type == ItemID.X);</c>. It registers the item (so the config grid can list it)
    /// and then answers false when the config has the rework switched off.
    /// </summary>
    public static class ReworkToggle
    {
        /// <summary>
        /// folder is only needed from a GlobalItem that lives outside its weapon's folder (a shared base class): pass GetType().Namespace.
        /// Otherwise the compiler fills in the source file's path, and the folder (class / progression / subclass) is read from it.
        /// </summary>
        public static bool Applies(int itemType, bool isRework, string folder = null, [CallerFilePath] string callerFile = "")
        {
            if (!isRework)
                return false;

            ModContent.GetInstance<ReworkRegistry>()?.Add(itemType, WeaponFolder(folder ?? callerFile));
            return IsActive(itemType);
        }

        /// <summary>
        /// ".../Content/Weapons/Melee/PreHardmode/Broadsword/Starfury/StarfuryGlobalItem.cs" or the namespace
        /// "IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Flails.Mace" gives "Melee/PreHardmode/Broadsword" / "Melee/PreHardmode/Flails".
        /// Null when the text has no Content/Weapons part (the grid then files the item under Others).
        /// </summary>
        public static string WeaponFolder(string source)
        {
            if (string.IsNullOrEmpty(source))
                return null;

            string[] parts = source.Replace('\\', '/').Replace('.', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i + 3 < parts.Length; i++)
            {
                if (parts[i] == "Weapons" && parts[i - 1] == "Content")
                    return parts[i + 1] + "/" + parts[i + 2] + "/" + parts[i + 3];
            }
            return null;
        }

        public static bool IsActive(int itemType)
        {
            ReworkRegistry registry = ModContent.GetInstance<ReworkRegistry>();
            return registry == null || !registry.IsDisabled(itemType);
        }

        /// <summary>For ModPlayers: the player holds this item AND its rework is switched on (a switched-off item is plain vanilla).</summary>
        public static bool Holds(Player player, int itemType)
        {
            return player.HeldItem.type == itemType && IsActive(itemType);
        }
    }
}