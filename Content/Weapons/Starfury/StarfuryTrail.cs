using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>
    /// The trail the Starfury's tip leaves while it swings (a PixelTrail, drawn by the shared pixel line renderer). Two layers: a wide
    /// faint pink glow and a narrow bright gold core; the line tapers away toward its tail and fades with age. Every number is read
    /// live from StarfuryTuning, so hot reload works.
    /// </summary>
    public class StarfuryTrailStyle : PixelTrailStyle
    {
        public override float Grid => StarfuryTuning.PixelSize;
        public override int LifeTicks => StarfuryTuning.TrailLifeTicks;
        public override int Layers => 2;

        public override float GetWidth(int layer, float age)
        {
            return StarfuryTuning.TrailThickness * (layer == 0 ? 1.8f : 0.8f) * (1f - age);
        }

        public override Color GetColor(int layer, float age)
        {
            float fade = (1f - age) * (1f - age) * StarfuryTuning.TrailOpacity;
            return layer == 0 ? StarfuryTuning.PinkColor * (0.4f * fade) : StarfuryTuning.GoldColor * (0.9f * fade);
        }
    }
}