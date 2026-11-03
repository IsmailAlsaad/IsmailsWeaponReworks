using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>
    /// The crimson fan the spinning blade leaves (a SweepTrail, see Common/SweepTrail.cs and the Ice Blade's IceBladeTrailStyle): the tip edge
    /// is the bright core, the rest fades through crimson toward the hand. The swing projectile moves the inner edge down the blade with the
    /// blade's speed, so the fan starts as a thin line at the tip and fills the blade as the spin builds up, then thins out again as the
    /// blade slows down. The whole fan fades away together. Every number reads BloodButchererTuning live.
    /// </summary>
    public class BloodButchererSpinTrailStyle : SweepTrailStyle
    {
        public override float Grid => BloodButchererTuning.PixelSize;
        public override int LifeTicks => BloodButchererTuning.SpinTrailLifeTicks;
        public override int Rails => BloodButchererTuning.SpinTrailRails;
        public override SweepFade Fade => SweepFade.Uniform;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * BloodButchererTuning.SpinTrailOpacity;
            Color color = ThreeStop(across, BloodButchererTuning.BrightColor, BloodButchererTuning.CrimsonColor, BloodButchererTuning.CrimsonColor);
            return color * (fade * (1f - 0.5f * across)); // alpha stays 0: additive
        }
    }
}