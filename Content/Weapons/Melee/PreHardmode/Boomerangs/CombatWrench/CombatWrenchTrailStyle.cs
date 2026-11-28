using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Boomerangs.CombatWrench
{
    /// <summary>
    /// The thrown wrench's pixel trail: a steel-blue glow with a pale core that thins out as it ages. Because it is a PixelTrail it
    /// is handed to DetachedTrails when the wrench is caught, so it keeps fading. Purely visual.
    /// </summary>
    public class CombatWrenchTrailStyle : PixelTrailStyle
    {
        public override float Grid => CombatWrenchTuning.ThrowTrailGrid;
        public override int LifeTicks => CombatWrenchTuning.ThrowTrailLifeTicks;
        public override int Layers => 2;
        public override float Jitter => CombatWrenchTuning.ThrowTrailJitter;

        public override float GetWidth(int layer, float age)
        {
            float width = CombatWrenchTuning.ThrowTrailWidth * (1f - 0.8f * age);
            return layer == 0 ? width : width * 0.5f;
        }

        public override Color GetColor(int layer, float age)
        {
            if (layer == 0)
                return CombatWrenchTuning.ThrowTrailOuterColor * (0.5f * (1f - age));
            return Color.Lerp(CombatWrenchTuning.ThrowTrailInnerColor, CombatWrenchTuning.ThrowTrailOuterColor, age) * (0.8f * (1f - age));
        }
    }
}