using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords
{
    /// <summary>The trail a wooden sword swing leaves (a SweepTrail): a fan that starts as a thin line at the tip and widens down the blade, in the colors of the sword's wood, fading from its tail.</summary>
    public class WoodenSwordSweepTrailStyle : ChargedBladeTrailStyle
    {
        readonly WoodSpec wood;

        public WoodenSwordSweepTrailStyle(int column)
        {
            wood = WoodenSwordWoods.Spec(column);
        }

        public override float Grid => WoodenSwordTuning.PixelSize;
        public override int LifeTicks => WoodenSwordTuning.TrailLifeTicks;
        public override int Rails => WoodenSwordTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => WoodenSwordTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * WoodenSwordTuning.TrailOpacity;
            Color color = ThreeStop(across, wood.Core, wood.Mid, wood.Deep);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}