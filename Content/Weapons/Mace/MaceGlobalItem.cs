using IsmailsWeaponReworks.Common.Flail;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Mace
{
    /// <summary>
    /// Overhauls the vanilla Mace: pressing the attack button starts MaceFlailProjectile, a held flail head that runs the whole
    /// hold-charge rhythm loop (see FlailProjectile). Everything else (use setup, one head at a time, the tooltip) is in FlailGlobalItem.
    /// </summary>
    public class MaceGlobalItem : FlailGlobalItem
    {
        protected override int ItemType => ItemID.Mace;
        protected override int ProjectileType => ModContent.ProjectileType<MaceFlailProjectile>();
    }
}