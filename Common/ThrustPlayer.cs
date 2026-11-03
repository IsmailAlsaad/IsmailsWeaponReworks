using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The little state every thrusting weapon keeps between uses, on the owner's machine only (never synced: the owner decides and the
    /// held projectile carries what the other machines need). Right now: the cooldown after a burst, remembered per item type so
    /// swapping to another spear does not inherit it. Every spear's CanUseItem calls ThrustPlayer.CanUse.
    /// </summary>
    public class ThrustPlayer : ModPlayer
    {
        public uint CooldownUntil;       // Main.GameUpdateCount the cooldown ends at
        public int CooldownItem = -1;    // the item type it belongs to

        public void StartCooldown(int itemType, int ticks)
        {
            if (ticks <= 0)
            {
                CooldownItem = -1;
                return;
            }
            CooldownItem = itemType;
            CooldownUntil = Main.GameUpdateCount + (uint)ticks;
        }

        public bool OnCooldown(int itemType)
        {
            return CooldownItem == itemType && Main.GameUpdateCount < CooldownUntil;
        }

        // The one gate for a held-spear weapon: its projectile is not out and its burst cooldown is over.
        public static bool CanUse(Player player, int itemType, int projectileType)
        {
            return player.ownedProjectileCounts[projectileType] <= 0 && !player.GetModPlayer<ThrustPlayer>().OnCooldown(itemType);
        }
    }
}