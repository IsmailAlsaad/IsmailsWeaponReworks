using IsmailsWeaponReworks.Common.Flail;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Meatball
{
    /// <summary>
    /// Overhauls the vanilla Meatball: pressing the attack button starts MeatballFlailProjectile, a held flail head that runs the whole
    /// hold-charge rhythm loop (see FlailProjectile). Everything else (use setup, one head at a time, the tooltip) is in FlailGlobalItem.
    /// The only addition: after the tier 3 special tears the ball off, the hand stays empty for MeatballTuning.RegrowTicks before a new
    /// loop can start (MeatballPlayer counts it).
    /// </summary>
    public class MeatballGlobalItem : FlailGlobalItem
    {
        protected override int ItemType => ItemID.TheMeatball;
        protected override int ProjectileType => ModContent.ProjectileType<MeatballFlailProjectile>();

        public override bool CanUseItem(Item item, Player player)
        {
            return base.CanUseItem(item, player) && player.GetModPlayer<MeatballPlayer>().RegrowTicks <= 0;
        }
    }
}