using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.OreSwords
{
    /// <summary>The trail an ore broadsword swing leaves (a SweepTrail): a faint fan that starts as a thin line at the tip and widens down the blade, in the colors of the sword's metal, fading from its tail.</summary>
    public class OreSwordSweepTrailStyle : ChargedBladeTrailStyle
    {
        readonly OreSpec metal;

        public OreSwordSweepTrailStyle(int column)
        {
            metal = OreSwordMetals.Spec(column);
        }

        public override float Grid => OreSwordTuning.PixelSize;
        public override int LifeTicks => OreSwordTuning.TrailLifeTicks;
        public override int Rails => OreSwordTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => OreSwordTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * OreSwordTuning.TrailOpacity;
            Color color = ThreeStop(across, metal.Core, metal.Mid, metal.Deep);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}