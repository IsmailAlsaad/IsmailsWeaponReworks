using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.FlamingMace
{
    /// <summary>
    /// The fiery trail of the Flaming Mace head (a PixelTrail, see Common/PixelTrail.cs). Tiers 1 and 2: a wide orange glow under a narrower
    /// hot core. Tier 3 (the special): a long, wide, slightly ragged streak of three layers, dark red glow, orange body, yellow core.
    /// Every number reads FlamingMaceTuning live.
    /// </summary>
    public class FlamingMaceTrailStyle : PixelTrailStyle
    {
        readonly int tier;

        public FlamingMaceTrailStyle(int tier)
        {
            this.tier = tier;
        }

        static FlamingMaceTuning Tuning => FlamingMaceTuning.Instance;
        bool Special => tier >= 3;

        public override float Grid => Tuning.PixelSize;
        public override int LifeTicks => Special ? Tuning.SpecialTrailLifeTicks : Tuning.TrailLifeTicks;
        public override int Layers => Special ? 3 : 2;
        public override float Jitter => Special ? Tuning.SpecialTrailJitter : Tuning.TrailJitter;

        public override float GetWidth(int layer, float age)
        {
            float width = Tuning.TrailWidth(tier);
            float scale = Special ? (layer == 0 ? 1.25f : layer == 1 ? 0.75f : 0.4f) : (layer == 0 ? 1f : 0.5f);
            return width * scale * (1f - age);
        }

        public override Color GetColor(int layer, float age)
        {
            Color color;
            float strength;
            if (Special)
            {
                color = layer == 0 ? Tuning.EmberColor : layer == 1 ? Tuning.GlowColor(tier) : Tuning.HotColor(tier);
                strength = layer == 0 ? 0.5f : layer == 1 ? 0.7f : 0.95f;
            }
            else
            {
                color = layer == 0 ? Tuning.GlowColor(tier) : Tuning.HotColor(tier);
                strength = layer == 0 ? 0.55f : 0.9f;
            }
            return color * ((1f - age * age) * strength); // alpha stays 0: additive
        }
    }
}