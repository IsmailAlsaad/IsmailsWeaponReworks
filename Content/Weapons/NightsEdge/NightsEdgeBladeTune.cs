using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// Hands NightsEdgeTuning to the shared charged blade (Common/ChargedBlade). Every property forwards live, so hot reload of a
    /// NightsEdgeTuning number still takes effect. Add a line here only when IChargedBladeTuning gets a new member.
    /// </summary>
    public class NightsEdgeBladeTune : IChargedBladeTuning
    {
        public float ReadyArmDeg => NightsEdgeTuning.ReadyArmDeg;
        public float ReadyTiltDeg => NightsEdgeTuning.ReadyTiltDeg;
        public float BladeLength => NightsEdgeTuning.BladeLength;
        public float SwingLowDeg => NightsEdgeTuning.SwingLowDeg;
        public float SwingHighDeg => NightsEdgeTuning.SwingHighDeg;
        public float TiltLowDeg => NightsEdgeTuning.TiltLowDeg;
        public float TiltHighDeg => NightsEdgeTuning.TiltHighDeg;
        public int ChainAnticipateTicks => NightsEdgeTuning.ChainAnticipateTicks;
        public int AnticipateTicks => NightsEdgeTuning.AnticipateTicks;
        public float ChainAnticipateDeg => NightsEdgeTuning.ChainAnticipateDeg;
        public float AnticipateDeg => NightsEdgeTuning.AnticipateDeg;
        public int SwingTicks => NightsEdgeTuning.SwingTicks;
        public int RecoverTicks => NightsEdgeTuning.RecoverTicks;
        public float SwingEasePower => NightsEdgeTuning.SwingEasePower;
        public float OvershootDeg => NightsEdgeTuning.OvershootDeg;
        public float AnticipateTiltDeg => NightsEdgeTuning.AnticipateTiltDeg;
        public float SwordScale => NightsEdgeTuning.SwordScale;
        public float ChargedAt => NightsEdgeTuning.ChargedAt;
        public float ChargeSyncStep => NightsEdgeTuning.ChargeSyncStep;
        public float ChargeShowEase => NightsEdgeTuning.ChargeShowEase;
        public float OutlineFade => NightsEdgeTuning.OutlineFade;
        public int IdleBobTicks => NightsEdgeTuning.IdleBobTicks;
        public float IdleBobDeg => NightsEdgeTuning.IdleBobDeg;
        public float IdleEase => NightsEdgeTuning.IdleEase;
        public float StabBlendOut => NightsEdgeTuning.StabBlendOut;
        public float SparkleChance => NightsEdgeTuning.SparkleChance;
        public int FlashTicks => NightsEdgeTuning.FlashTicks;
        public int StabBufferTicks => NightsEdgeTuning.StabBufferTicks;
        public int StabTurnTicks => NightsEdgeTuning.StabTurnTicks;
        public int StabAimTicks => NightsEdgeTuning.StabAimTicks;
        public int StabPlungeTicks => NightsEdgeTuning.StabPlungeTicks;
        public float StabArmDeg => NightsEdgeTuning.StabArmDeg;
        public float StabChestOffsetY => NightsEdgeTuning.StabChestOffsetY;
        public float StabAimDistance => NightsEdgeTuning.StabAimDistance;
        public float StabDepth => NightsEdgeTuning.StabDepth;
        public float StabShake => NightsEdgeTuning.StabShake;
        public int StabHoldTicks => NightsEdgeTuning.StabHoldTicks;
        public float StabHitShake => NightsEdgeTuning.StabHitShake;
        public float MaxSpeedScale => NightsEdgeTuning.MaxSpeedScale;
        public float SpeedCurvePower => NightsEdgeTuning.SpeedCurvePower;
        public float AttackSpeedInfluence => NightsEdgeTuning.AttackSpeedInfluence;
        public float MinSpeed => NightsEdgeTuning.MinSpeed;
        public float MaxSpeed => NightsEdgeTuning.MaxSpeed;
        public int TrailSamplesPerTick => NightsEdgeTuning.TrailSamplesPerTick;
        public float TrailLagTicks => NightsEdgeTuning.TrailLagTicks;
        public float TrailOuter => NightsEdgeTuning.TrailOuter;
        public float TrailInnerLow => NightsEdgeTuning.TrailInnerLow;
        public float TrailInnerHigh => NightsEdgeTuning.TrailInnerHigh;
        public float TrailExpandBy => NightsEdgeTuning.TrailExpandBy;
        public int SweepHitSteps => NightsEdgeTuning.SweepHitSteps;
        public float HitLengthScale => NightsEdgeTuning.HitLengthScale;
        public float HitWidth => NightsEdgeTuning.HitWidth;
        public float DamageMultiplier => NightsEdgeTuning.DamageMultiplier;
        public float KnockbackMultiplier => NightsEdgeTuning.KnockbackMultiplier;
        public float HitShake => NightsEdgeTuning.HitShake;
        public float ChargePerHit => NightsEdgeTuning.ChargePerHit;
        public float GripOffset => NightsEdgeTuning.GripOffset;
        public float GlowMaxAlpha => NightsEdgeTuning.GlowMaxAlpha;
        public Color OutlineColor => NightsEdgeTuning.OutlineColor;
        public float GlowScale => NightsEdgeTuning.GlowScale;
        public int OutlinePulseTicks => NightsEdgeTuning.OutlinePulseTicks;
        public float OutlineMin => NightsEdgeTuning.OutlineMin;
        public float OutlineMax => NightsEdgeTuning.OutlineMax;
        public float OutlineWidth => NightsEdgeTuning.OutlineWidth;
        public Color OutlineCoreColor => NightsEdgeTuning.OutlineCoreColor;
        public Color CoreColor => NightsEdgeTuning.CoreColor;
        public bool ShowBladeDebug => NightsEdgeTuning.ShowBladeDebug;
    }
}