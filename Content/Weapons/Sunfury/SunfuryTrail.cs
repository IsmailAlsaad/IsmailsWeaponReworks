using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>
    /// The fiery trail of the Sunfury head (a PixelTrail, see Common/PixelTrail.cs): a wide orange glow under a narrower hot core,
    /// thicker and brighter for higher tiers. Every number reads SunfuryTuning live.
    /// </summary>
    public class SunfuryTrailStyle : PixelTrailStyle
    {
        readonly int tier;

        public SunfuryTrailStyle(int tier)
        {
            this.tier = tier;
        }

        public override float Grid => SunfuryTuning.PixelSize;
        public override int LifeTicks => SunfuryTuning.TrailLifeTicks;
        public override int Layers => 2;
        public override float Jitter => SunfuryTuning.TrailJitter;

        public override float GetWidth(int layer, float age)
        {
            float width = SunfuryTuning.TrailWidth(tier) * (layer == 0 ? 1f : 0.5f);
            return width * (1f - age);
        }

        public override Color GetColor(int layer, float age)
        {
            Color color = layer == 0 ? SunfuryTuning.GlowColor(tier) : SunfuryTuning.HotColor(tier);
            return color * ((1f - age * age) * (layer == 0 ? 0.55f : 0.9f)); // alpha stays 0: additive
        }
    }
}