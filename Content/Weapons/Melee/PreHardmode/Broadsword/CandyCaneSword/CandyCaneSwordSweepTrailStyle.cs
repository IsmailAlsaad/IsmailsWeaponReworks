using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>The trail a Candy Cane Sword swing leaves (a SweepTrail): a faint fan from white at the tip through candy red, fading from its tail.</summary>
    public class CandyCaneSwordSweepTrailStyle : ChargedBladeTrailStyle
    {
        public override float Grid => CandyCaneSwordTuning.PixelSize;
        public override int LifeTicks => CandyCaneSwordTuning.TrailLifeTicks;
        public override int Rails => CandyCaneSwordTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => CandyCaneSwordTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * CandyCaneSwordTuning.TrailOpacity;
            Color color = ThreeStop(across, CandyCaneSwordTuning.TrailCore, CandyCaneSwordTuning.TrailMid, CandyCaneSwordTuning.TrailDeep);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}