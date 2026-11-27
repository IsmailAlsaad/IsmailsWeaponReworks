using IsmailsWeaponReworks.Common;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.OreSwords
{
    /// <summary>The look of the ore swords' hit flare: every number reads OreSwordTuning live.</summary>
    public class OreSwordFlareStyle : FlareParticleStyle
    {
        public override string AssetPath => "IsmailsWeaponReworks/Assets/Effects/SmallFlare";
        public override int LifeTicks => OreSwordTuning.FlareLifeTicks;
        public override float PeakScale => OreSwordTuning.FlarePeakScale;
        public override float GrowFraction => OreSwordTuning.FlareGrowFraction;
        public override float SpinMin => OreSwordTuning.FlareSpinMin;
        public override float SpinMax => OreSwordTuning.FlareSpinMax;
        public override float Opacity => OreSwordTuning.FlareOpacity;
        public override float Light => OreSwordTuning.FlareLight;
        public override int MaxParticles => OreSwordTuning.FlareMax;
    }
}