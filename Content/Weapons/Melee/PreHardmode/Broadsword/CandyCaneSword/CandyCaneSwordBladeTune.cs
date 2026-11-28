using IsmailsWeaponReworks.Common.ChargedBlade;
using IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// Hands the swing numbers to the shared charged blade (Common/ChargedBlade). Everything is the wooden swords' (WoodenSwordBladeTune, which also
    /// measures the blade length from the item sprite), so the Candy Cane Sword swings exactly like them; only the trail geometry (TrailOuter, TrailInnerLow,
    /// TrailInnerHigh, TrailExpandBy, TrailSamplesPerTick, and the grip offset) comes from CandyCaneSwordTuning (the trail's lag too, one number per swing direction, through the blade). Every property forwards live, so hot reload
    /// of a number still takes effect. Add a line here only when IChargedBladeTuning gets a new member.
    /// </summary>
    public class CandyCaneSwordBladeTune : IChargedBladeTuning
    {
        readonly WoodenSwordBladeTune inner;

        public CandyCaneSwordBladeTune(int itemType)
        {
            inner = new WoodenSwordBladeTune(itemType);
        }

        // Hand to tip: the diagonal of one cell of CandyCaneSword.png once it exists, otherwise measured from the vanilla item sprite.
        public float BladeLength
        {
            get
            {
                Vector2 cell = CandyCaneSwordSprites.CellSize;
                if (cell.X > 0f && cell.Y > 0f)
                    return cell.Length() * WoodenSwordTuning.BladeLengthFactor;
                return inner.BladeLength;
            }
        }
        public float ReadyArmDeg => inner.ReadyArmDeg;
        public float ReadyTiltDeg => inner.ReadyTiltDeg;
        public float SwingLowDeg => inner.SwingLowDeg;
        public float SwingHighDeg => inner.SwingHighDeg;
        public float TiltLowDeg => inner.TiltLowDeg;
        public float TiltHighDeg => inner.TiltHighDeg;
        public int ChainAnticipateTicks => inner.ChainAnticipateTicks;
        public int AnticipateTicks => inner.AnticipateTicks;
        public float ChainAnticipateDeg => inner.ChainAnticipateDeg;
        public float AnticipateDeg => inner.AnticipateDeg;
        public int SwingTicks => inner.SwingTicks;
        public int RecoverTicks => inner.RecoverTicks;
        public float SwingEasePower => inner.SwingEasePower;
        public float OvershootDeg => inner.OvershootDeg;
        public float AnticipateTiltDeg => inner.AnticipateTiltDeg;
        public float SwordScale => inner.SwordScale;
        public float ChargedAt => inner.ChargedAt;
        public float ChargeSyncStep => inner.ChargeSyncStep;
        public float ChargeShowEase => inner.ChargeShowEase;
        public float OutlineFade => inner.OutlineFade;
        public int IdleBobTicks => inner.IdleBobTicks;
        public float IdleBobDeg => inner.IdleBobDeg;
        public float IdleEase => inner.IdleEase;
        public float StabBlendOut => inner.StabBlendOut;
        public float SparkleChance => inner.SparkleChance;
        public int FlashTicks => inner.FlashTicks;
        public int StabBufferTicks => inner.StabBufferTicks;
        public int StabTurnTicks => inner.StabTurnTicks;
        public int StabAimTicks => inner.StabAimTicks;
        public int StabPlungeTicks => inner.StabPlungeTicks;
        public float StabArmDeg => inner.StabArmDeg;
        public float StabChestOffsetY => inner.StabChestOffsetY;
        public float StabAimDistance => inner.StabAimDistance;
        public float StabDepth => inner.StabDepth;
        public float StabShake => inner.StabShake;
        public int StabHoldTicks => inner.StabHoldTicks;
        public float StabHitShake => inner.StabHitShake;
        public float MaxSpeedScale => inner.MaxSpeedScale;
        public float SpeedCurvePower => inner.SpeedCurvePower;
        public float AttackSpeedInfluence => inner.AttackSpeedInfluence;
        public float MinSpeed => inner.MinSpeed;
        public float MaxSpeed => inner.MaxSpeed;
        public int TrailSamplesPerTick => CandyCaneSwordTuning.TrailSamplesPerTick;
        // The blade overrides the lag per swing direction (TrailLagTicksUp / TrailLagTicksDown); this is only the interface's default.
        public float TrailLagTicks => CandyCaneSwordTuning.TrailLagTicksDown;
        public float TrailOuter => CandyCaneSwordTuning.TrailOuter;
        public float TrailInnerLow => CandyCaneSwordTuning.TrailInnerLow;
        public float TrailInnerHigh => CandyCaneSwordTuning.TrailInnerHigh;
        public float TrailExpandBy => CandyCaneSwordTuning.TrailExpandBy;
        public int SweepHitSteps => inner.SweepHitSteps;
        public float HitLengthScale => inner.HitLengthScale;
        public float HitWidth => inner.HitWidth;
        public float DamageMultiplier => inner.DamageMultiplier;
        public float KnockbackMultiplier => inner.KnockbackMultiplier;
        public float HitShake => inner.HitShake;
        public float ChargePerHit => inner.ChargePerHit;
        public float GripOffset => CandyCaneSwordTuning.GripOffset;
        public float GlowMaxAlpha => inner.GlowMaxAlpha;
        public Color OutlineColor => inner.OutlineColor;
        public float GlowScale => inner.GlowScale;
        public int OutlinePulseTicks => inner.OutlinePulseTicks;
        public float OutlineMin => inner.OutlineMin;
        public float OutlineMax => inner.OutlineMax;
        public float OutlineWidth => inner.OutlineWidth;
        public Color OutlineCoreColor => inner.OutlineCoreColor;
        public Color CoreColor => inner.CoreColor;
        public bool ShowBladeDebug => inner.ShowBladeDebug;
    }
}