using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The trail a Night's Edge swing leaves (a SweepTrail): a fan that starts as a thin line at the tip and widens down the blade; pale
    /// violet on the tip edge, purple in the middle, deep violet toward the hand, fading from its tail. A higher charge leaves a stronger trail.
    /// </summary>
    public class NightsEdgeSweepTrailStyle : SweepTrailStyle
    {
        readonly float charge;

        // charge = 0..1 (swings in the combo / ChargeSwings) when the swing started.
        public NightsEdgeSweepTrailStyle(float charge)
        {
            this.charge = charge;
        }

        public override float Grid => NightsEdgeTuning.PixelSize;
        public override int LifeTicks => NightsEdgeTuning.TrailLifeTicks;
        public override int Rails => NightsEdgeTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => NightsEdgeTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float opacity = MathHelper.Lerp(NightsEdgeTuning.TrailOpacityLow, NightsEdgeTuning.TrailOpacityHigh, charge);
            float fade = Falloff(age) * opacity;
            Color color = ThreeStop(across, NightsEdgeTuning.CoreColor, NightsEdgeTuning.MidColor, NightsEdgeTuning.DeepColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}