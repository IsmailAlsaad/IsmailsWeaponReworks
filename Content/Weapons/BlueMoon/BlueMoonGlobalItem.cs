using IsmailsWeaponReworks.Common.Flail;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BlueMoon
{
    /// <summary>
    /// Overhauls the vanilla Blue Moon: pressing the attack button starts BlueMoonFlailProjectile, a held flail head that runs the whole
    /// hold-charge rhythm loop (see FlailProjectile). Everything else (use setup, one head at a time, the tooltip) is in FlailGlobalItem.
    /// </summary>
    public class BlueMoonGlobalItem : FlailGlobalItem
    {
        protected override int ItemType => ItemID.BlueMoon;
        protected override int ProjectileType => ModContent.ProjectileType<BlueMoonFlailProjectile>();
    }
}