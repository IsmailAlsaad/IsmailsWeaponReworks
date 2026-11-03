using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>
    /// The crimson line each jaw tip leaves (a PixelTrail, see Common/PixelTrail.cs): during the thrust both tips lay the same line, and
    /// when the jaws open it forks into two arcs that come back together in the bite. A wide dark red glow under a narrower bright core;
    /// it shrinks away from its tail. Every number reads BloodButchererTuning live.
    /// </summary>
    public class BloodButchererJawTrailStyle : PixelTrailStyle
    {
        public override float Grid => BloodButchererTuning.PixelSize;
        public override int LifeTicks => BloodButchererTuning.TrailLifeTicks;
        public override int Layers => 2;

        public override float GetWidth(int layer, float age)
        {
            return BloodButchererTuning.TrailWidth * (layer == 0 ? 1f : 0.45f) * (1f - age);
        }

        public override Color GetColor(int layer, float age)
        {
            Color color = layer == 0 ? BloodButchererTuning.CrimsonColor : BloodButchererTuning.BrightColor;
            return color * ((1f - age * age) * (layer == 0 ? 0.6f : 0.9f)); // alpha stays 0: additive
        }
    }
}