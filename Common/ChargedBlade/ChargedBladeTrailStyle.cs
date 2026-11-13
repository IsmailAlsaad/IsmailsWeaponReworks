using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common.ChargedBlade
{
    /// <summary>
    /// The base of a charged blade's swing trail (a SweepTrail style). The blade sets 'Charge' (0..1) while it lays the fan, so a weapon's
    /// style can make a charged sword's fan stronger; everything else (grid, colors, fade) is the weapon's own.
    /// </summary>
    public abstract class ChargedBladeTrailStyle : SweepTrailStyle
    {
        public float Charge;
    }
}