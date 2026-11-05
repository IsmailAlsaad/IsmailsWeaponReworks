using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// The trail a Muramasa swing leaves (a SweepTrail, drawn by the shared pixel line renderer): a fan that starts as a thin line at the
    /// tip and widens down the blade; icy white-blue core on the tip edge, blue in the middle, deep blue toward the hand. The tail fades
    /// out first. Every number is read live from MuramasaTuning, so hot reload works.
    /// </summary>
    public class MuramasaSweepTrailStyle : SweepTrailStyle
    {
        public override float Grid => MuramasaTuning.PixelSize;
        public override int LifeTicks => MuramasaTuning.TrailLifeTicks;
        public override int Rails => MuramasaTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => MuramasaTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * MuramasaTuning.TrailOpacity;
            Color color = ThreeStop(across, MuramasaTuning.CoreColor, MuramasaTuning.BlueColor, MuramasaTuning.DeepColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}