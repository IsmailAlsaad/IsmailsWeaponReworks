using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// Hands BladeOfGrassTuning to the shared charged blade (Common/ChargedBlade). Every property forwards live, so hot reload of a
    /// BladeOfGrassTuning number still takes effect. Add a line here only when IChargedBladeTuning gets a new member.
    /// </summary>
    public class BladeOfGrassBladeTune : IChargedBladeTuning
    {
        public float ReadyArmDeg => BladeOfGrassTuning.ReadyArmDeg;
        public float ReadyTiltDeg => BladeOfGrassTuning.ReadyTiltDeg;
        public float BladeLength => BladeOfGrassTuning.BladeLength;
        public float SwingLowDeg => BladeOfGrassTuning.SwingLowDeg;
        public float SwingHighDeg => BladeOfGrassTuning.SwingHighDeg;
        public float TiltLowDeg => BladeOfGrassTuning.TiltLowDeg;
        public float TiltHighDeg => BladeOfGrassTuning.TiltHighDeg;
        public int ChainAnticipateTicks => BladeOfGrassTuning.ChainAnticipateTicks;
        public int AnticipateTicks => BladeOfGrassTuning.AnticipateTicks;
        public float ChainAnticipateDeg => BladeOfGrassTuning.ChainAnticipateDeg;
        public float AnticipateDeg => BladeOfGrassTuning.AnticipateDeg;
        public int SwingTicks => BladeOfGrassTuning.SwingTicks;
        public int RecoverTicks => BladeOfGrassTuning.RecoverTicks;
        public float SwingEasePower => BladeOfGrassTuning.SwingEasePower;
        public float OvershootDeg => BladeOfGrassTuning.OvershootDeg;
        public float AnticipateTiltDeg => BladeOfGrassTuning.AnticipateTiltDeg;
        public float SwordScale => BladeOfGrassTuning.SwordScale;
        public float ChargedAt => BladeOfGrassTuning.ChargedAt;
        public float ChargeSyncStep => BladeOfGrassTuning.ChargeSyncStep;
        public float ChargeShowEase => BladeOfGrassTuning.ChargeShowEase;
        public float OutlineFade => BladeOfGrassTuning.OutlineFade;
        public int IdleBobTicks => BladeOfGrassTuning.IdleBobTicks;
        public float IdleBobDeg => BladeOfGrassTuning.IdleBobDeg;
        public float IdleEase => BladeOfGrassTuning.IdleEase;
        public float StabBlendOut => BladeOfGrassTuning.StabBlendOut;
        public float SparkleChance => BladeOfGrassTuning.SparkleChance;
        public int FlashTicks => BladeOfGrassTuning.FlashTicks;
        public int StabBufferTicks => BladeOfGrassTuning.StabBufferTicks;
        public int StabTurnTicks => BladeOfGrassTuning.StabTurnTicks;
        public int StabAimTicks => BladeOfGrassTuning.StabAimTicks;
        public int StabPlungeTicks => BladeOfGrassTuning.StabPlungeTicks;
        public float StabArmDeg => BladeOfGrassTuning.StabArmDeg;
        public float StabChestOffsetY => BladeOfGrassTuning.StabChestOffsetY;
        public float StabAimDistance => BladeOfGrassTuning.StabAimDistance;
        public float StabDepth => BladeOfGrassTuning.StabDepth;
        public float StabShake => BladeOfGrassTuning.StabShake;
        public int StabHoldTicks => BladeOfGrassTuning.StabHoldTicks;
        public float StabHitShake => BladeOfGrassTuning.StabHitShake;
        public float MaxSpeedScale => BladeOfGrassTuning.MaxSpeedScale;
        public float SpeedCurvePower => BladeOfGrassTuning.SpeedCurvePower;
        public float AttackSpeedInfluence => BladeOfGrassTuning.AttackSpeedInfluence;
        public float MinSpeed => BladeOfGrassTuning.MinSpeed;
        public float MaxSpeed => BladeOfGrassTuning.MaxSpeed;
        public int TrailSamplesPerTick => BladeOfGrassTuning.TrailSamplesPerTick;
        public float TrailLagTicks => BladeOfGrassTuning.TrailLagTicks;
        public float TrailOuter => BladeOfGrassTuning.TrailOuter;
        public float TrailInnerLow => BladeOfGrassTuning.TrailInnerLow;
        public float TrailInnerHigh => BladeOfGrassTuning.TrailInnerHigh;
        public float TrailExpandBy => BladeOfGrassTuning.TrailExpandBy;
        public int SweepHitSteps => BladeOfGrassTuning.SweepHitSteps;
        public float HitLengthScale => BladeOfGrassTuning.HitLengthScale;
        public float HitWidth => BladeOfGrassTuning.HitWidth;
        public float DamageMultiplier => BladeOfGrassTuning.DamageMultiplier;
        public float KnockbackMultiplier => BladeOfGrassTuning.KnockbackMultiplier;
        public float HitShake => BladeOfGrassTuning.HitShake;
        public float ChargePerHit => BladeOfGrassTuning.ChargePerHit;
        public float GripOffset => BladeOfGrassTuning.GripOffset;
        public float GlowMaxAlpha => BladeOfGrassTuning.GlowMaxAlpha;
        public Color OutlineColor => BladeOfGrassTuning.OutlineColor;
        public float GlowScale => BladeOfGrassTuning.GlowScale;
        public int OutlinePulseTicks => BladeOfGrassTuning.OutlinePulseTicks;
        public float OutlineMin => BladeOfGrassTuning.OutlineMin;
        public float OutlineMax => BladeOfGrassTuning.OutlineMax;
        public float OutlineWidth => BladeOfGrassTuning.OutlineWidth;
        public Color OutlineCoreColor => BladeOfGrassTuning.OutlineCoreColor;
        public Color CoreColor => BladeOfGrassTuning.CoreColor;
        public bool ShowBladeDebug => BladeOfGrassTuning.ShowBladeDebug;
    }
}