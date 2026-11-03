using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>
    /// The trail a Starfury swing leaves (a SweepTrail, drawn by the shared pixel line renderer): it starts as a thin line at the blade's
    /// tip and expands down the blade toward the hand as the swing builds up, leaving a fan behind the blade. The tip edge is the bright
    /// cream core, the middle is gold and the side toward the hand fades into pink; the fan fades from its tail with age. Every number
    /// is read live from StarfuryTuning, so hot reload works.
    /// </summary>
    public class StarfurySweepTrailStyle : SweepTrailStyle
    {
        readonly float charge;

        // charge = 0..1 (hits / StarfuryTuning.MaxHits) when the swing started: a higher charge leaves a stronger trail.
        public StarfurySweepTrailStyle(float charge)
        {
            this.charge = charge;
        }

        public override float Grid => StarfuryTuning.PixelSize;
        public override int LifeTicks => StarfuryTuning.TrailLifeTicks;
        public override int Rails => StarfuryTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;       // the tail fades out first, the fade travels up to the tip
        public override float TailLifeScale => StarfuryTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * StarfuryTuning.TrailOpacityAt(charge);
            Color color = ThreeStop(across, StarfuryTuning.CoreColor, StarfuryTuning.GoldColor, StarfuryTuning.PinkColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}