using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The streak a thrust leaves, as a SweepTrail style: the tip edge is the palette's bright core, the middle its body color and the
    /// side toward the hand its faint halo, fading from the tail. The numbers are captured when the style is made (once per thrust), so a
    /// hot reload applies from the next thrust on.
    /// </summary>
    public class ThrustTrailStyle : SweepTrailStyle
    {
        readonly ThrustPalette palette;
        readonly int life;
        readonly int rails;
        readonly float grid;
        readonly float opacity;

        public ThrustTrailStyle(ThrustPalette palette, int lifeTicks, int rails, float grid, float opacity)
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
            Color color = ThreeStop(across, palette.Core, palette.Mid, palette.Halo);
            return color * (Falloff(age) * opacity * (1f - 0.5f * across));
        }
    }
}