using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The trail an ethereal sword strike leaves (a SweepTrail): a fan along the blade, pale white-blue on the tip edge, blue in the
    /// middle, deep blue toward the hand, fading together.
    /// </summary>
    public class EtherealSwordTrailStyle : SweepTrailStyle
    {
        public override float Grid => EnchantedSwordTuning.PixelSize;
        public override int LifeTicks => EnchantedSwordTuning.TrailLifeTicks;
        public override int Rails => EnchantedSwordTuning.TrailRails;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * EnchantedSwordTuning.TrailOpacity;
            Color color = ThreeStop(across, EnchantedSwordTuning.CoreColor, EnchantedSwordTuning.BlueColor, EnchantedSwordTuning.DeepColor);
            return color * (fade * (1f - 0.5f * across));
        }
    }
}