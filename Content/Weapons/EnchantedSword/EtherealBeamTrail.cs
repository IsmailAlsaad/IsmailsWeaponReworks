using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The trail the thrown ethereal beam lays (a PixelTrail, so it is handed to DetachedTrails when the beam dies and keeps fading): a wide
    /// soft blue-to-pink glow with a narrow pale-gold core, both thinning and fading with age, in the Enchanted Boomerang's stardust colors.
    /// </summary>
    public class EtherealBeamTrailStyle : PixelTrailStyle
    {
        public override float Grid => EnchantedSwordTuning.PixelSize;
        public override int LifeTicks => EnchantedSwordTuning.BeamTrailLifeTicks;
        public override int Layers => 2;

        public override float GetWidth(int layer, float age)
        {
            float width = EnchantedSwordTuning.BeamTrailWidth * (1f - 0.85f * age);
            return layer == 0 ? width : width * 0.45f;
        }

        public override Color GetColor(int layer, float age)
        {
            if (layer == 0)
                return Color.Lerp(EnchantedSwordTuning.BlueColor, EnchantedSwordTuning.PinkColor, age) * (0.5f * (1f - age));
            return Color.Lerp(EnchantedSwordTuning.CoreColor, EnchantedSwordTuning.BlueColor, age) * (0.85f * (1f - age));
        }
    }
}
