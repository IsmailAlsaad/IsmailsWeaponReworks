using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// The line the Dark Lance's strike leaves behind its tip (the shared PixelTrail / PixelLine renderer, so it is one smooth pixel-snapped
    /// ribbon and not a pile of separate boxes): three layers, a wide dark halo, a purple body and a thin hot core, all thick at the newest end and
    /// shrinking to a point as they age. 'widthScale' makes the thrust's line thinner than the sweep's. Visual only, nothing to sync.
    /// </summary>
    public class DarkLanceStrikeTrailStyle : PixelTrailStyle
    {
        readonly float widthScale;

        public DarkLanceStrikeTrailStyle(float widthScale = 1f)
        {
            this.widthScale = widthScale;
        }

        public override float Grid => DarkLanceTuning.StrikeTrailGrid;
        public override int LifeTicks => DarkLanceTuning.StrikeTrailLifeTicks;
        public override int Layers => 3;
        public override bool Smoothing => DarkLanceTuning.StrikeTrailSmoothing;

        public override float GetWidth(int layer, float age)
        {
            float full = DarkLanceTuning.StrikeTrailWidth * widthScale * (1f - age);
            return layer == 0 ? full : layer == 1 ? full * 0.6f : full * 0.25f;
        }

        public override Color GetColor(int layer, float age)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;
            float fade = 1f - age;
            if (layer == 0)
                return DarkLanceFx.Glow(palette.Halo) * (0.7f * fade * fade);
            if (layer == 1)
                return DarkLanceFx.Glow(palette.Mid) * (0.75f * fade);
            return DarkLanceFx.Glow(Color.Lerp(palette.Core, palette.Mid, age)) * (0.95f * fade);
        }
    }
}