using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The trail an Enchanted Sword swing or thrust leaves (a SweepTrail): a fan that starts as a thin line at the tip and widens down the
    /// blade, in the Enchanted Boomerang's stardust colors (pale gold on the tip edge, blue in the middle, pink toward the hand), fading from
    /// its tail. The blade sets 'Stage' (0..1) while it lays the fan: a sword with more hits leaves a stronger fan.
    /// </summary>
    public class EnchantedSwordSweepTrailStyle : SweepTrailStyle
    {
        public float Stage;

        public override float Grid => EnchantedSwordTuning.PixelSize;
        public override int LifeTicks => EnchantedSwordTuning.TrailLifeTicks;
        public override int Rails => EnchantedSwordTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => EnchantedSwordTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float opacity = MathHelper.Lerp(EnchantedSwordTuning.TrailOpacityLow, EnchantedSwordTuning.TrailOpacityHigh, MathHelper.Clamp(Stage, 0f, 1f));
            float fade = Falloff(age) * opacity;
            Color color = ThreeStop(across, EnchantedSwordTuning.CoreColor, EnchantedSwordTuning.BlueColor, EnchantedSwordTuning.PinkColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}