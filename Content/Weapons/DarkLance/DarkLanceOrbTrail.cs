using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// The trail an orb leaves while it flies out of the lance and turns around the player: a wide dark halo line with a thinner bright
    /// line in it, both shrinking and cooling to nothing as they age (the shared PixelTrail / PixelLine renderer). The orb hands the
    /// trail to DetachedTrails when it ends, so it fades out on its own. Visual only, nothing to sync; it does nothing to the world.
    /// </summary>
    public class DarkLanceOrbTrailStyle : PixelTrailStyle
    {
        public override float Grid => DarkLanceTuning.PixelSize;
        public override int LifeTicks => DarkLanceTuning.TrailLifeTicks;
        public override int Layers => 2;

        public override float GetWidth(int layer, float age)
        {
            float width = DarkLanceTuning.TrailWidth * (1f - 0.85f * age);
            return layer == 0 ? width : width * 0.45f;
        }

        public override Color GetColor(int layer, float age)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;
            float fade = 1f - age;
            if (layer == 0)
                return DarkLanceFx.Glow(palette.Halo) * (0.55f * fade * fade);
            return DarkLanceFx.Glow(Color.Lerp(palette.Core, palette.Mid, age)) * (0.75f * fade);
        }
    }
}
