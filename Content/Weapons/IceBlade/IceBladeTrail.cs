using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.IceBlade
{
    /// <summary>
    /// The freezing trail the Ice Blade's tip leaves while a charged release swings (a PixelTrail, drawn by the shared pixel line
    /// renderer). Two layers: a wide faint glow and a narrow bright core. Thickness and opacity grow with the tier (IceBladeTuning); the
    /// line tapers away toward its tail and fades with age. Every number is read live, so hot reload works.
    /// </summary>
    public class IceBladeTrailStyle : PixelTrailStyle
    {
        readonly int tier;

        public IceBladeTrailStyle(int tier)
        {
            this.tier = tier;
        }

        public override float Grid => IceBladeTuning.PixelSize;
        public override int LifeTicks => IceBladeTuning.TrailLifeTicks;
        public override int Layers => 2;

        public override float GetWidth(int layer, float age)
        {
            float thickness = IceBladeTuning.TrailThickness(tier) * (layer == 0 ? 1.8f : 0.8f);
            return thickness * (1f - age);
        }

        public override Color GetColor(int layer, float age)
        {
            float fade = (1f - age) * (1f - age) * IceBladeTuning.TrailOpacity(tier);
            return layer == 0 ? IceBladeTuning.MidColor * (0.45f * fade) : IceBladeTuning.CoreColor * (0.85f * fade);
        }
    }
}