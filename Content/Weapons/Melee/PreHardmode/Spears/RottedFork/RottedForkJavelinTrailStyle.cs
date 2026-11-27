using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Spears.RottedFork
{
    /// <summary>
    /// The javelin's trail: the same palette, life, rails, pixel size and opacity as the spin's ThrustTrailStyle (no taper), but the fan is
    /// opened across the flight path, so its middle (across 0.5) is the bright core and both edges are the faint halo.
    /// </summary>
    public class RottedForkJavelinTrailStyle : SweepTrailStyle
    {
        readonly ThrustPalette palette;
        readonly int life;
        readonly int rails;
        readonly float grid;
        readonly float opacity;

        public RottedForkJavelinTrailStyle(ThrustPalette palette, int lifeTicks, int rails, float grid, float opacity)
        {
            this.palette = palette;
            life = lifeTicks;
            this.rails = rails;
            this.grid = grid;
            this.opacity = opacity;
        }

        public override float Grid => grid;
        public override int LifeTicks => life;
        public override int Rails => rails;

        public override Color GetColor(float across, float age)
        {
            float fromMiddle = System.Math.Abs(across * 2f - 1f); // 0 in the middle, 1 on either edge
            return ThreeStop(fromMiddle, palette.Core, palette.Mid, palette.Halo) * (Falloff(age) * opacity);
        }
    }
}