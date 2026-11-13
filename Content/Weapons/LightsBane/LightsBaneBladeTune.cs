using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// Hands LightsBaneTuning to the shared charged blade (Common/ChargedBlade). Every property forwards live, so hot reload of a
    /// LightsBaneTuning number still takes effect. Add a line here only when IChargedBladeTuning gets a new member.
    /// </summary>
    public class LightsBaneBladeTune : IChargedBladeTuning
    {
        public float ReadyArmDeg => LightsBaneTuning.ReadyArmDeg;
        public float ReadyTiltDeg => LightsBaneTuning.ReadyTiltDeg;
        public float BladeLength => LightsBaneTuning.BladeLength;
        public float SwingLowDeg => LightsBaneTuning.SwingLowDeg;
        public float SwingHighDeg => LightsBaneTuning.SwingHighDeg;
        public float TiltLowDeg => LightsBaneTuning.TiltLowDeg;
        public float TiltHighDeg => LightsBaneTuning.TiltHighDeg;
        public int ChainAnticipateTicks => LightsBaneTuning.ChainAnticipateTicks;
        public int AnticipateTicks => LightsBaneTuning.AnticipateTicks;
        public float ChainAnticipateDeg => LightsBaneTuning.ChainAnticipateDeg;
        public float AnticipateDeg => LightsBaneTuning.AnticipateDeg;
        public int SwingTicks => LightsBaneTuning.SwingTicks;
        public int RecoverTicks => LightsBaneTuning.RecoverTicks;
        public float SwingEasePower => LightsBaneTuning.SwingEasePower;
        public float OvershootDeg => LightsBaneTuning.OvershootDeg;
        public float AnticipateTiltDeg => LightsBaneTuning.AnticipateTiltDeg;
        public float SwordScale => LightsBaneTuning.SwordScale;
        public float ChargedAt => LightsBaneTuning.ChargedAt;
        public float ChargeSyncStep => LightsBaneTuning.ChargeSyncStep;
        public float ChargeShowEase => LightsBaneTuning.ChargeShowEase;
        public float OutlineFade => LightsBaneTuning.OutlineFade;
        public int IdleBobTicks => LightsBaneTuning.IdleBobTicks;
        public float IdleBobDeg => LightsBaneTuning.IdleBobDeg;
        public float IdleEase => LightsBaneTuning.IdleEase;
        public float StabBlendOut => LightsBaneTuning.StabBlendOut;
        public float SparkleChance => LightsBaneTuning.SparkleChance;
        public int FlashTicks => LightsBaneTuning.FlashTicks;
        public int StabBufferTicks => LightsBaneTuning.StabBufferTicks;
        public int StabTurnTicks => LightsBaneTuning.StabTurnTicks;
        public int StabAimTicks => LightsBaneTuning.StabAimTicks;
        public int StabPlungeTicks => LightsBaneTuning.StabPlungeTicks;
        public float StabArmDeg => LightsBaneTuning.StabArmDeg;
        public float StabChestOffsetY => LightsBaneTuning.StabChestOffsetY;
        public float StabAimDistance => LightsBaneTuning.StabAimDistance;
        public float StabDepth => LightsBaneTuning.StabDepth;
        public float StabShake => LightsBaneTuning.StabShake;
        public int StabHoldTicks => LightsBaneTuning.StabHoldTicks;
        public float StabHitShake => LightsBaneTuning.StabHitShake;
        public float MaxSpeedScale => LightsBaneTuning.MaxSpeedScale;
        public float SpeedCurvePower => LightsBaneTuning.SpeedCurvePower;
        public float AttackSpeedInfluence => LightsBaneTuning.AttackSpeedInfluence;
        public float MinSpeed => LightsBaneTuning.MinSpeed;
        public float MaxSpeed => LightsBaneTuning.MaxSpeed;
        public int TrailSamplesPerTick => LightsBaneTuning.TrailSamplesPerTick;
        public float TrailLagTicks => LightsBaneTuning.TrailLagTicks;
        public float TrailOuter => LightsBaneTuning.TrailOuter;
        public float TrailInnerLow => LightsBaneTuning.TrailInnerLow;
        public float TrailInnerHigh => LightsBaneTuning.TrailInnerHigh;
        public float TrailExpandBy => LightsBaneTuning.TrailExpandBy;
        public int SweepHitSteps => LightsBaneTuning.SweepHitSteps;
        public float HitLengthScale => LightsBaneTuning.HitLengthScale;
        public float HitWidth => LightsBaneTuning.HitWidth;
        public float DamageMultiplier => LightsBaneTuning.DamageMultiplier;
        public float KnockbackMultiplier => LightsBaneTuning.KnockbackMultiplier;
        public float HitShake => LightsBaneTuning.HitShake;
        public float ChargePerHit => LightsBaneTuning.ChargePerHit;
        public float GripOffset => LightsBaneTuning.GripOffset;
        public float GlowMaxAlpha => LightsBaneTuning.GlowMaxAlpha;
        public Color OutlineColor => LightsBaneTuning.OutlineColor;
        public float GlowScale => LightsBaneTuning.GlowScale;
        public int OutlinePulseTicks => LightsBaneTuning.OutlinePulseTicks;
        public float OutlineMin => LightsBaneTuning.OutlineMin;
        public float OutlineMax => LightsBaneTuning.OutlineMax;
        public float OutlineWidth => LightsBaneTuning.OutlineWidth;
        public Color OutlineCoreColor => LightsBaneTuning.OutlineCoreColor;
        public Color CoreColor => LightsBaneTuning.CoreColor;
        public bool ShowBladeDebug => LightsBaneTuning.ShowBladeDebug;
    }
}