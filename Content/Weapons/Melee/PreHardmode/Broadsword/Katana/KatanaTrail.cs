using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.Katana
{
    /// <summary>
    /// The trail a Katana swing leaves (a SweepTrail, drawn by the shared pixel line renderer): a fan that starts as a thin line at the
    /// tip and widens down the blade; white core on the tip edge, pale blue in the middle, steel blue toward the hand. The tail fades
    /// out first. Every number is read live from KatanaTuning, so hot reload works.
    /// </summary>
    public class KatanaSweepTrailStyle : SweepTrailStyle
    {
        public override float Grid => KatanaTuning.PixelSize;
        public override int LifeTicks => KatanaTuning.TrailLifeTicks;
        public override int Rails => KatanaTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => KatanaTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * KatanaTuning.TrailOpacity;
            Color color = ThreeStop(across, KatanaTuning.CoreColor, KatanaTuning.GlowColor, KatanaTuning.EdgeColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}