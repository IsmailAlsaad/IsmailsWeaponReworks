using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common.ChargedBlade
{
    /// <summary>
    /// Every tunable number the shared charged-blade projectile (ChargedBladeProjectile) reads. A weapon implements it with one property per
    /// line that forwards to its own Tuning class (`=> LightsBaneTuning.X`), so hot reload still works and every weapon keeps its numbers in
    /// its own `<Weapon>Tuning`. Meanings: see LightsBaneTuning (the first weapon) for what each knob does.
    /// </summary>
    public interface IChargedBladeTuning
    {
        float ReadyArmDeg { get; }
        float ReadyTiltDeg { get; }
        float BladeLength { get; }
        float SwingLowDeg { get; }
        float SwingHighDeg { get; }
        float TiltLowDeg { get; }
        float TiltHighDeg { get; }
        int ChainAnticipateTicks { get; }
        int AnticipateTicks { get; }
        float ChainAnticipateDeg { get; }
        float AnticipateDeg { get; }
        int SwingTicks { get; }
        int RecoverTicks { get; }
        float SwingEasePower { get; }
        float OvershootDeg { get; }
        float AnticipateTiltDeg { get; }
        float SwordScale { get; }
        float ChargedAt { get; }
        float ChargeSyncStep { get; }
        float ChargeShowEase { get; }
        float OutlineFade { get; }
        int IdleBobTicks { get; }
        float IdleBobDeg { get; }
        float IdleEase { get; }
        float StabBlendOut { get; }
        float SparkleChance { get; }
        int FlashTicks { get; }
        int StabBufferTicks { get; }
        int StabTurnTicks { get; }
        int StabAimTicks { get; }
        int StabPlungeTicks { get; }
        float StabArmDeg { get; }
        float StabChestOffsetY { get; }
        float StabAimDistance { get; }
        float StabDepth { get; }
        float StabShake { get; }
        int StabHoldTicks { get; }
        float StabHitShake { get; }
        float MaxSpeedScale { get; }
        float SpeedCurvePower { get; }
        float AttackSpeedInfluence { get; }
        float MinSpeed { get; }
        float MaxSpeed { get; }
        int TrailSamplesPerTick { get; }
        float TrailLagTicks { get; }
        float TrailOuter { get; }
        float TrailInnerLow { get; }
        float TrailInnerHigh { get; }
        float TrailExpandBy { get; }
        int SweepHitSteps { get; }
        float HitLengthScale { get; }
        float HitWidth { get; }
        float DamageMultiplier { get; }
        float KnockbackMultiplier { get; }
        float HitShake { get; }
        float ChargePerHit { get; }
        float GripOffset { get; }
        float GlowMaxAlpha { get; }
        Color OutlineColor { get; }
        float GlowScale { get; }
        int OutlinePulseTicks { get; }
        float OutlineMin { get; }
        float OutlineMax { get; }
        float OutlineWidth { get; }
        Color OutlineCoreColor { get; }
        Color CoreColor { get; }
        bool ShowBladeDebug { get; }
    }
}