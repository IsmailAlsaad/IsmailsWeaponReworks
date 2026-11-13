using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The trail a Night's Edge swing leaves (a SweepTrail): a fan that starts as a thin line at the tip and widens down the blade, pale
    /// lilac on the tip edge, purple in the middle, near-black violet toward the hand, fading from its tail. The swing projectile sets 'Charge'
    /// (0..1) while it lays the fan: a charged sword leaves a stronger fan that reaches further down the blade.
    /// </summary>
    public class NightsEdgeSweepTrailStyle : ChargedBladeTrailStyle
    {
        public override float Grid => NightsEdgeTuning.PixelSize;
        public override int LifeTicks => NightsEdgeTuning.TrailLifeTicks;
        public override int Rails => NightsEdgeTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => NightsEdgeTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float opacity = MathHelper.Lerp(NightsEdgeTuning.TrailOpacityLow, NightsEdgeTuning.TrailOpacityHigh, MathHelper.Clamp(Charge, 0f, 1f));
            float fade = Falloff(age) * opacity;
            Color color = ThreeStop(across, NightsEdgeTuning.CoreColor, NightsEdgeTuning.PurpleColor, NightsEdgeTuning.DeepColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}