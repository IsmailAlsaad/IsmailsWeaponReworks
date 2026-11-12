using IsmailsWeaponReworks.Common.Flail;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>
    /// Overhauls the vanilla Sunfury: pressing the attack button starts SunfuryFlailProjectile, a held flail head that runs the whole
    /// hold-charge rhythm loop (see FlailProjectile). Everything else (use setup, one head at a time, the tooltip) is in FlailGlobalItem.
    /// </summary>
    public class SunfuryGlobalItem : FlailGlobalItem
    {
        protected override int ItemType => ItemID.Sunfury;
        protected override int ProjectileType => ModContent.ProjectileType<SunfuryFlailProjectile>();
    }
}