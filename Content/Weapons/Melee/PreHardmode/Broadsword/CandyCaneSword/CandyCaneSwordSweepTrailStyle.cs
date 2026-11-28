using System;
using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>The trail a Candy Cane Sword swing leaves (a SweepTrail): a faint fan from white at the tip through the color of the sword's current candy (read live, so a color change shows at once), fading from its tail.</summary>
    public class CandyCaneSwordSweepTrailStyle : ChargedBladeTrailStyle
    {
        readonly Func<CandyKind> kind;

        public CandyCaneSwordSweepTrailStyle(Func<CandyKind> kind)
        {
            this.kind = kind;
        }

        public override float Grid => CandyCaneSwordTuning.PixelSize;
        public override int LifeTicks => CandyCaneSwordTuning.TrailLifeTicks;
        public override int Rails => CandyCaneSwordTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => CandyCaneSwordTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * CandyCaneSwordTuning.TrailOpacity;
            Color tint = CandyKinds.Tint(kind());
            Color color = ThreeStop(across, CandyCaneSwordTuning.TrailCore, tint, Color.Lerp(Color.Black, tint, CandyCaneSwordTuning.TrailDeepScale));
            return color * (fade * (1f - 0.6f * across));
        }
    }
}