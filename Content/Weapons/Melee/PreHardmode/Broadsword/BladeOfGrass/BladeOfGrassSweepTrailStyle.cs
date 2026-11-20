using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// The trail a Blade of Grass swing leaves (a SweepTrail): a fan that starts as a thin line at the tip and widens down the blade, pale
    /// lime on the tip edge, green in the middle, deep green toward the hand, fading from its tail. A blade with longer vines (more charge)
    /// leaves a stronger fan.
    /// </summary>
    public class BladeOfGrassSweepTrailStyle : ChargedBladeTrailStyle
    {
        public override float Grid => BladeOfGrassTuning.PixelSize;
        public override int LifeTicks => BladeOfGrassTuning.TrailLifeTicks;
        public override int Rails => BladeOfGrassTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => BladeOfGrassTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float opacity = MathHelper.Lerp(BladeOfGrassTuning.TrailOpacityLow, BladeOfGrassTuning.TrailOpacityHigh, MathHelper.Clamp(Charge, 0f, 1f));
            float fade = Falloff(age) * opacity;
            Color color = ThreeStop(across, BladeOfGrassTuning.TrailCoreColor, BladeOfGrassTuning.TrailGreenColor, BladeOfGrassTuning.TrailDeepColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}