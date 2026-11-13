using IsmailsWeaponReworks.Common.Flail;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.FlamingMace
{
    /// <summary>
    /// Overhauls the vanilla Flaming Mace: pressing the attack button starts FlamingMaceFlailProjectile, a held flail head that runs the
    /// whole hold-charge rhythm loop (see FlailProjectile). Everything else (use setup, one head at a time, the tooltip) is in FlailGlobalItem.
    /// </summary>
    public class FlamingMaceGlobalItem : FlailGlobalItem
    {
        protected override int ItemType => ItemID.FlamingMace;
        protected override int ProjectileType => ModContent.ProjectileType<FlamingMaceFlailProjectile>();
    }
}