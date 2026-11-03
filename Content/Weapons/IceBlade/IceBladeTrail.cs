using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.IceBlade
{
    /// <summary>
    /// The freezing trail of a charged release swing (a SweepTrail, drawn by the shared pixel line renderer): it starts as a thin line at
    /// the blade's tip and expands down the blade toward the hand as the swing builds up, leaving a fan behind the blade. The tip edge is
    /// the bright core, the rest fades through the mid blue to the halo blue toward the hand. The tier sets the opacity and how far down
    /// the blade the fan reaches (IceBladeTuning); the fan fades from its tail with age. Every number is read live, so hot reload works.
    /// </summary>
    public class IceBladeTrailStyle : SweepTrailStyle
    {
        readonly int tier;

        public IceBladeTrailStyle(int tier)
        {
            this.tier = tier;
        }

        public override float Grid => IceBladeTuning.PixelSize;
        public override int LifeTicks => IceBladeTuning.TrailLifeTicks;
        public override int Rails => IceBladeTuning.TrailRails;

        public override SweepFade Fade => SweepFade.Uniform; // the whole fan fades away together

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * IceBladeTuning.TrailOpacity(tier);
            // Bright core on the tip edge, mid blue in the middle, deep halo blue toward the hand; the inner side is fainter.
            Color color = ThreeStop(across, IceBladeTuning.CoreColor, IceBladeTuning.MidColor, IceBladeTuning.HaloColor);
            return color * (fade * (1f - 0.65f * across));
        }
    }
}