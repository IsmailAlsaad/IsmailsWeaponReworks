using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Boomerangs.CombatWrench
{
    /// <summary>
    /// The fan the Combat Wrench's heavy swing leaves: steel blue at the outer edge, a pale core toward the inner edge, the tail fading
    /// first. A SweepTrail, so it is handed to DetachedTrails when the swing ends and keeps fading. Purely visual.
    /// </summary>
    public class CombatWrenchSweepTrailStyle : SweepTrailStyle
    {
        public override float Grid => CombatWrenchTuning.TrailPixelSize;
        public override int LifeTicks => CombatWrenchTuning.TrailLifeTicks;
        public override int Rails => CombatWrenchTuning.TrailRails;
        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => CombatWrenchTuning.TrailTailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * CombatWrenchTuning.TrailOpacity;
            Color color = Color.Lerp(CombatWrenchTuning.TrailInnerColor, CombatWrenchTuning.TrailOuterColor, across);
            return color * (fade * (1f - 0.5f * across));
        }
    }
}