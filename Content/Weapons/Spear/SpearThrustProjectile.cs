using IsmailsWeaponReworks.Common;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Spear
{
    /// <summary>
    /// The held Spear: a burst of thrusts toward the cursor, each a little off the aim. All behavior lives in HeldSpearProjectile; this only
    /// supplies the Spear's numbers from SpearTuning.
    /// </summary>
    public class SpearThrustProjectile : HeldSpearProjectile
    {
        protected override int ItemType => ItemID.Spear;
        protected override ThrustChain Chain => SpearTuning.Chain;
        protected override float Reach => SpearTuning.Reach;
        protected override ThrustPalette Palette => SpearTuning.Palette;
        protected override float SpearScale => SpearTuning.SpearScale;
        protected override float GripFraction => SpearTuning.GripFraction;
        protected override float HitWidth => SpearTuning.HitWidth;
        protected override int TrailLifeTicks => SpearTuning.TrailLifeTicks;
        protected override int TrailRails => SpearTuning.TrailRails;
        protected override float TrailOpacity => SpearTuning.TrailOpacity;
        protected override float PixelSize => SpearTuning.PixelSize;
    }
}