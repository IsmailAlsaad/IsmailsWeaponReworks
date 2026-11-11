using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Phaseblade
{
    /// <summary>
    /// The trail a Phaseblade swing leaves (a SweepTrail, drawn by the shared pixel line renderer): a fan behind the blade that opens down
    /// from the tip. The tip edge is the hot core (the blade's color pushed toward white), the middle is the blade's color and the side
    /// toward the hand fades out; the fan fades from its tail with age. Every number is read live from PhasebladeTuning.
    /// </summary>
    public class PhasebladeSweepTrailStyle : SweepTrailStyle
    {
        readonly Color color;

        public PhasebladeSweepTrailStyle(Color color)
        {
            this.color = color;
        }

        public override float Grid => PhasebladeTuning.PixelSize;
        public override int LifeTicks => PhasebladeTuning.TrailLifeTicks;
        public override int Rails => PhasebladeTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => PhasebladeTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * PhasebladeTuning.TrailOpacity;
            Color result = ThreeStop(across, PhasebladeTuning.CoreOf(color), color, color * 0.5f);
            return result * (fade * (1f - 0.6f * across));
        }
    }
}