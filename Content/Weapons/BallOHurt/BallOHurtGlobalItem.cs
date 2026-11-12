using IsmailsWeaponReworks.Common.Flail;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BallOHurt
{
    /// <summary>
    /// Overhauls the vanilla Ball O' Hurt: pressing the attack button starts BallOHurtFlailProjectile, a held flail head that runs the whole
    /// hold-charge rhythm loop (see FlailProjectile). Everything else (use setup, one head at a time, the tooltip) is in FlailGlobalItem.
    /// </summary>
    public class BallOHurtGlobalItem : FlailGlobalItem
    {
        protected override int ItemType => ItemID.BallOHurt;
        protected override int ProjectileType => ModContent.ProjectileType<BallOHurtFlailProjectile>();
    }
}