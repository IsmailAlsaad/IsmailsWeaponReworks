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
        public static bool Applies(int itemType, bool isRework)
        {
            if (!isRework)
                return false;

            ModContent.GetInstance<ReworkRegistry>()?.Add(itemType);
            return IsActive(itemType);
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