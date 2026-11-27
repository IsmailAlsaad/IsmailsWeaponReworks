using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;
using Terraria.GameContent;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords
{
    /// <summary>
    /// Hands WoodenSwordTuning (and the sword sprite for the length) to the shared charged blade (Common/ChargedBlade). Every property forwards live, so hot reload of a
    /// WoodenSwordTuning number still takes effect. Add a line here only when IChargedBladeTuning gets a new member.
    /// </summary>
    public class WoodenSwordBladeTune : IChargedBladeTuning
    {
        public float ReadyArmDeg => WoodenSwordTuning.ReadyArmDeg;
        public float ReadyTiltDeg => WoodenSwordTuning.ReadyTiltDeg;
        readonly int item;

        public WoodenSwordBladeTune(int itemType)
        {
            item = itemType;
        }

        // Hand to tip: the diagonal of the item sprite (a vanilla sword has its hilt in the bottom-left corner and the tip in the top-right one).
        public float BladeLength
        {
            get
            {
                var asset = TextureAssets.Item[item];
                if (asset == null || !asset.IsLoaded)
                    return WoodenSwordTuning.BladeLengthFallback;
                float width = asset.Value.Width;
                float height = asset.Value.Height;
                return (float)System.Math.Sqrt(width * width + height * height) * WoodenSwordTuning.BladeLengthFactor;
            }
        }
        public float SwingLowDeg => WoodenSwordTuning.SwingLowDeg;
        public float SwingHighDeg => WoodenSwordTuning.SwingHighDeg;
        public float TiltLowDeg => WoodenSwordTuning.TiltLowDeg;
        public float TiltHighDeg => WoodenSwordTuning.TiltHighDeg;
        public int ChainAnticipateTicks => WoodenSwordTuning.ChainAnticipateTicks;
        public int AnticipateTicks => WoodenSwordTuning.AnticipateTicks;
        public float ChainAnticipateDeg => WoodenSwordTuning.ChainAnticipateDeg;
        public float AnticipateDeg => WoodenSwordTuning.AnticipateDeg;
        public int SwingTicks => WoodenSwordTuning.SwingTicks;
        public int RecoverTicks => WoodenSwordTuning.RecoverTicks;
        public float SwingEasePower => WoodenSwordTuning.SwingEasePower;
        public float OvershootDeg => WoodenSwordTuning.OvershootDeg;
        public float AnticipateTiltDeg => WoodenSwordTuning.AnticipateTiltDeg;
        public float SwordScale => WoodenSwordTuning.SwordScale;
        public float ChargedAt => WoodenSwordTuning.ChargedAt;
        public float ChargeSyncStep => WoodenSwordTuning.ChargeSyncStep;
        public float ChargeShowEase => WoodenSwordTuning.ChargeShowEase;
        public float OutlineFade => WoodenSwordTuning.OutlineFade;
        public int IdleBobTicks => WoodenSwordTuning.IdleBobTicks;
        public float IdleBobDeg => WoodenSwordTuning.IdleBobDeg;
        public float IdleEase => WoodenSwordTuning.IdleEase;
        public float StabBlendOut => WoodenSwordTuning.StabBlendOut;
        public float SparkleChance => WoodenSwordTuning.SparkleChance;
        public int FlashTicks => WoodenSwordTuning.FlashTicks;
        public int StabBufferTicks => WoodenSwordTuning.StabBufferTicks;
        public int StabTurnTicks => WoodenSwordTuning.StabTurnTicks;
        public int StabAimTicks => WoodenSwordTuning.StabAimTicks;
        public int StabPlungeTicks => WoodenSwordTuning.StabPlungeTicks;
        public float StabArmDeg => WoodenSwordTuning.StabArmDeg;
        public float StabChestOffsetY => WoodenSwordTuning.StabChestOffsetY;
        public float StabAimDistance => WoodenSwordTuning.StabAimDistance;
        public float StabDepth => WoodenSwordTuning.StabDepth;
        public float StabShake => WoodenSwordTuning.StabShake;
        public int StabHoldTicks => WoodenSwordTuning.StabHoldTicks;
        public float StabHitShake => WoodenSwordTuning.StabHitShake;
        public float MaxSpeedScale => WoodenSwordTuning.MaxSpeedScale;
        public float SpeedCurvePower => WoodenSwordTuning.SpeedCurvePower;
        public float AttackSpeedInfluence => WoodenSwordTuning.AttackSpeedInfluence;
        public float MinSpeed => WoodenSwordTuning.MinSpeed;
        public float MaxSpeed => WoodenSwordTuning.MaxSpeed;
        public int TrailSamplesPerTick => WoodenSwordTuning.TrailSamplesPerTick;
        public float TrailLagTicks => WoodenSwordTuning.TrailLagTicks;
        public float TrailOuter => WoodenSwordTuning.TrailOuter;
        public float TrailInnerLow => WoodenSwordTuning.TrailInnerLow;
        public float TrailInnerHigh => WoodenSwordTuning.TrailInnerHigh;
        public float TrailExpandBy => WoodenSwordTuning.TrailExpandBy;
        public int SweepHitSteps => WoodenSwordTuning.SweepHitSteps;
        public float HitLengthScale => WoodenSwordTuning.HitLengthScale;
        public float HitWidth => WoodenSwordTuning.HitWidth;
        public float DamageMultiplier => WoodenSwordTuning.DamageMultiplier;
        public float KnockbackMultiplier => WoodenSwordTuning.KnockbackMultiplier;
        public float HitShake => WoodenSwordTuning.HitShake;
        public float ChargePerHit => WoodenSwordTuning.ChargePerHit;
        public float GripOffset => WoodenSwordTuning.GripOffset;
        public float GlowMaxAlpha => WoodenSwordTuning.GlowMaxAlpha;
        public Color OutlineColor => WoodenSwordTuning.OutlineColor;
        public float GlowScale => WoodenSwordTuning.GlowScale;
        public int OutlinePulseTicks => WoodenSwordTuning.OutlinePulseTicks;
        public float OutlineMin => WoodenSwordTuning.OutlineMin;
        public float OutlineMax => WoodenSwordTuning.OutlineMax;
        public float OutlineWidth => WoodenSwordTuning.OutlineWidth;
        public Color OutlineCoreColor => WoodenSwordTuning.OutlineCoreColor;
        public Color CoreColor => WoodenSwordTuning.CoreColor;
        public bool ShowBladeDebug => WoodenSwordTuning.ShowBladeDebug;
    }
}