using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// The trail a Light's Bane swing leaves (a SweepTrail): a fan that starts as a thin line at the tip and widens down the blade, pale
    /// lilac on the tip edge, purple in the middle, deep violet toward the hand, fading from its tail. The swing projectile sets 'Charge'
    /// (0..1) while it lays the fan: a charged sword leaves a stronger fan that reaches further down the blade (the reach is set where the
    /// points are laid, the strength here).
    /// </summary>
    public class LightsBaneSweepTrailStyle : ChargedBladeTrailStyle
    {
        public override float Grid => LightsBaneTuning.PixelSize;
        public override int LifeTicks => LightsBaneTuning.TrailLifeTicks;
        public override int Rails => LightsBaneTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => LightsBaneTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float opacity = MathHelper.Lerp(LightsBaneTuning.TrailOpacityLow, LightsBaneTuning.TrailOpacityHigh, MathHelper.Clamp(Charge, 0f, 1f));
            float fade = Falloff(age) * opacity;
            Color color = ThreeStop(across, LightsBaneTuning.CoreColor, LightsBaneTuning.PurpleColor, LightsBaneTuning.DeepColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}