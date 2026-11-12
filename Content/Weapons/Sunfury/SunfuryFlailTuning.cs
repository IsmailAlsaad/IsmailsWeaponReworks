using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>
    /// The Sunfury's numbers for the shared flail loop. Every value forwards to SunfuryTuning, which stays the one place to tune the Sunfury
    /// (the loop reads this class, the Sunfury's own explosion and effects keep reading SunfuryTuning).
    /// </summary>
    public class SunfuryFlailTuning : FlailTuning
    {
        public override int ChargeTicks(int tier) { return SunfuryTuning.ChargeTicks(tier); }
        public override float MinReach => SunfuryTuning.MinReach;
        public override float MaxReach => SunfuryTuning.MaxReach;
        public override float EllipseWidthRatio => SunfuryTuning.EllipseWidthRatio;
        public override float ThrowSpeed(int tier) { return SunfuryTuning.ThrowSpeed(tier); }
        public override int ReleaseBlendTicks => SunfuryTuning.ReleaseBlendTicks;
        public override int MinHalfTicks => SunfuryTuning.MinHalfTicks;
        public override float TierDamageMultiplier(int tier) { return SunfuryTuning.TierDamageMultiplier(tier); }
        public override int HitCooldownTicks => SunfuryTuning.HitCooldownTicks;
        public override float SpinHitPeriodFraction => SunfuryTuning.SpinHitPeriodFraction;
        public override float SpinDamageMultiplier(int tier) { return SunfuryTuning.SpinDamageMultiplier(tier); }
        public override float HeadSize => SunfuryTuning.HeadSize;
        public override float SpinRadiusMin => SunfuryTuning.SpinRadiusMin;
        public override float SpinRadiusMax => SunfuryTuning.SpinRadiusMax;
        public override float SpinRadiusSmoothing => SunfuryTuning.SpinRadiusSmoothing;
        public override float SpinOmegaStart(int tier) { return SunfuryTuning.SpinOmegaStart(tier); }
        public override float SpinOmegaEnd(int tier) { return SunfuryTuning.SpinOmegaEnd(tier); }
        public override float SpecialMinReach => SunfuryTuning.SpecialMinReach;
        public override float SpecialMaxReach => SunfuryTuning.SpecialMaxReach;
        public override float SpecialPathDamageMultiplier => SunfuryTuning.SpecialPathDamageMultiplier;
        public override int SpecialHangTicks => SunfuryTuning.SpecialHangTicks;
        public override float RetractSpeed => SunfuryTuning.RetractSpeed;
        public override float CatchDistance => SunfuryTuning.CatchDistance;
        public override float FailRetractSpeed => SunfuryTuning.FailRetractSpeed;
        public override float PixelSize => SunfuryTuning.PixelSize;
        public override int TrailLifeTicks => SunfuryTuning.TrailLifeTicks;
        public override float TrailWidth(int tier) { return SunfuryTuning.TrailWidth(tier); }
        public override float TrailJitter => SunfuryTuning.TrailJitter;
        public override int AfterimageTicks => SunfuryTuning.AfterimageTicks;
        public override int FlashTicks => SunfuryTuning.FlashTicks;
        public override Color GlowColor(int tier) { return SunfuryTuning.GlowColor(tier); }
        public override Color HotColor(int tier) { return SunfuryTuning.HotColor(tier); }
        public override string ChainName => SunfuryTuning.ChainName;
    }
}