using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common.Flail
{
    /// <summary>
    /// The default flail head trail (a PixelTrail, see PixelTrail.cs): a wide glow under a narrower hot core, thicker for higher tiers.
    /// Every number reads the flail's FlailTuning live. A flail with its own look overrides FlailProjectile.CreateTrailStyle.
    /// </summary>
    public class FlailTrailStyle : PixelTrailStyle
    {
        readonly FlailTuning tuning;
        readonly int tier;

        public FlailTrailStyle(FlailTuning tuning, int tier)
        {
            this.tuning = tuning;
            this.tier = tier;
        }

        public override float Grid => tuning.PixelSize;
        public override int LifeTicks => tuning.TrailLifeTicks;
        public override int Layers => 2;
        public override float Jitter => tuning.TrailJitter;

        public override float GetWidth(int layer, float age)
        {
            return tuning.TrailWidth(tier) * (layer == 0 ? 1f : 0.5f) * (1f - age);
        }

        public override Color GetColor(int layer, float age)
        {
            Color color = layer == 0 ? tuning.GlowColor(tier) : tuning.HotColor(tier);
            return color * ((1f - age * age) * (layer == 0 ? 0.55f : 0.9f)); // alpha stays 0: additive
        }
    }
}