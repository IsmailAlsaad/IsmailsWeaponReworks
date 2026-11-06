using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// Every tunable number of the Volcano rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Arm angles are for a player facing right: 0 = forward, positive = down, negative = up; the blade angle is
    /// arm + tilt (0 = forward, 90 = straight down, -90 = straight up).
    /// </summary>
    public static class VolcanoTuning
    {
        // ---- sword: the sprite is only the handle, the blade is procedural lava ----
        public static float HandleLength => 1f;     // hand to the JOINT (where the lava blade starts), px at scale 1: match your handle art
        public static float BladeLength => 90f;      // joint to the tip of the lava blade
        public static float SwordScale => 1.1f;
        public static float HandleCrop => 0.32f;     // stand-in art only: the part of the vanilla sprite (from its hilt corner) drawn as the handle
        public static bool ShowBladeDebug => false;  // true: magenta square on the tip the hit test uses, green square on the joint

        // ---- heat (0 = the weak baseline flame, 1 = fully charged) ----
        public static float HeatPerHit => 0.1f;      // every true melee hit of the swing adds this (10 hits = fully charged)
        public static int HeatHoldTicks => 300;      // the heat does not cool for this long after the last hit
        public static float HeatDecayPerTick => 0.0015f; // then it cools by this much per tick (full to cold in about 11 s)
        public static float HeatShowEase => 0.12f;   // how fast the DRAWN heat follows the real one (per tick, 0..1)
        public static float HeatSyncStep => 0.04f;   // the owner tells the other machines when the heat moved this much
        public static float ChargedAt => 0.995f;     // from this heat on the sword counts as fully charged (the burst plays when it gets there)

        // ---- the swing (a chop centered on the cursor; fixed ticks, scaled by melee speed) ----
        public static int AnticipateTicks => 4;
        public static float AnticipateDeg => 16f;    // how far past the start of the arc the arm pulls back
        public static float AnticipateTiltDeg => 8f;
        public static int SwingTicks => 6;           // the strike (the only part that damages)
        public static float SwingEasePower => 2f;    // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 6f;
        public static int RecoverTicks => 6;
        public static int SweepHitSteps => 4;        // the hit test is checked at this many poses between two ticks
        public static float HitWidth => 20f;         // thickness of the blade line the hit test uses (px at scale 1)
        public static float SwingHighDeg => -70f;    // relative to the aim: negative = above, positive = below
        public static float SwingLowDeg => 70f;
        public static float TiltHighDeg => -14f;
        public static float TiltLowDeg => 14f;
        public static float MinSpeed => 0.3f;        // melee speed is clamped to this range
        public static float MaxSpeed => 3f;

        // ---- heavy: the more lava on the blade, the slower and harder the swing (the heat at the click decides; 0 = cold, 1 = full) ----
        public static float HeavyTimeScale => 2f;            // the swing takes this many times as long at full heat
        public static float HeavyDamageMultiplier => 2f;     // damage at full heat (1 when cold)
        public static float HeavyKnockbackMultiplier => 1.5f;
        public static int TurnNoInheritTicks => 8;           // when the player turns around, flames and drips ignore the blade's movement for this long

        // ---- the ready pose: ReadyArmDeg/ReadyTiltDeg when uncharged, ChargedReadyArmDeg/ChargedReadyTiltDeg when fully charged; the arm blends between them with the heat ----
        public static float ChargedReadyArmDeg => -110f;
        public static float ChargedReadyTiltDeg => -30f;
        public static float ReadyArmDeg => 80f;
        public static float ReadyTiltDeg => 90f;
        public static float IdleBobDeg => 3f;
        public static int IdleBobTicks => 100;
        public static float IdleEase => 0.12f;       // how fast the arm settles into the ready pose after a swing (per tick, 0..1)

        // ---- on hit ----
        public static int OnFireTicks => 180;        // the vanilla Volcano sets enemies on fire
        public static int OnFireHeatBonusTicks => 240; // extra burn time at full heat
        public static float HitShake => 1f;          // camera punch of a hit, grows with the heat
        public static int FlashTicks => 10;

        // ---- the lava blade ----
        public static float PixelSize => 2f;
        public static float BlobSpacing => 4f;       // px between the round blobs along the blade
        public static float BlobMerge => 2.2f;
        public static float BlobOutline => 0f;
        public static float BlobGlow => 0.15f;
        public static float EdgeRadiusLow => 2.4f;   // blade blob radius at heat 0 (a thin burning edge) ...
        public static float EdgeRadiusHigh => 7f;    // ... and at full heat (a fat slab of lava)
        public static float WobbleLow => 0.6f;       // sideways wobble of the blobs, px
        public static float WobbleHigh => 3.2f;
        public static float WobbleSpeedLow => 4f;
        public static float WobbleSpeedHigh => 11f;
        public static float HeatLow => 0.28f;        // color heat of the blade at baseline (deep red) ...
        public static float HeatHigh => 0.7f;        // ... and at full heat (amber)
        public static float HeatFlicker => 0.2f;     // how much the color heat flickers
        public static float CoreFrom => 0.3f;        // from this heat on a white-hot core runs down the blade
        public static float CoreHeat => 0.98f;
        public static float FlameHeat => 0.8f;       // a flame particle starts this hot and cools

        // ---- where along the lava blade each thing starts and ends (0 = the joint, 1 = the tip; may go past 1) ----
        public static float BodyStart => 0.08f;         // the round blobs of the lava body (and the white-hot core)
        public static float BodyEnd => 1f;
        public static float FlameStart => 0.05f;     // the rising flames
        public static float FlameEnd => 1f;
        public static float DripStart => 0.15f;      // the falling drips
        public static float DripEnd => 1f;
        public static float SpitStart => 0.3f;       // the flung spits
        public static float SpitEnd => 1f;

        // ---- flames (rise off the blade), lava drips (fall) and spits (flung out, then fall) ----
        public static float FlameRise => 0.05f;
        public static float FlameDrag => 0.95f;
        public static float FlameWobble => 0.04f;
        public static float FlameRateLow => 0.3f;    // flames per tick at heat 0 ...
        public static float FlameRateHigh => 1.8f;   // ... and at full heat
        public static float FlameSize => 1.8f;
        public static float FlameSizeHeat => 2.2f;   // extra size at full heat
        public static int FlameLifeMin => 18;
        public static int FlameLifeMax => 30;
        public static float DripGravity => 0.16f;
        public static float DripHeat => 0.8f;
        public static float DripMerge => 2.4f;
        public static float DripFrom => 0.25f;       // drips start at this heat
        public static float DripRateHigh => 0.5f;    // drips per tick at full heat
        public static float DripSizeMin => 2.2f;
        public static float DripSizeMax => 3.6f;
        public static float SpitFrom => 0.6f;        // lava spits start at this heat
        public static float SpitRateHigh => 0.16f;
        public static float SpitSpeedMin => 2.5f;
        public static float SpitSpeedMax => 5.5f;
        public static float EmberDustFrom => 0.5f;   // vanilla fire dust sparks start at this heat
        public static float EmberDustChance => 0.3f; // per tick at full heat

        // ---- swing trail ----
        public static int TrailLifeTicks => 12;
        public static float TailLifeScale => 0.45f;
        public static int TrailRails => 8;
        public static float TrailExpandBy => 0.5f;
        public static float TrailInner => 0.3f;      // fully expanded, the fan reaches down the blade to this fraction of its length
        public static int TrailSamplesPerTick => 6;
        public static float TrailLagTicks => 0.2f;
        public static float TrailLengthScale => 1.1f;
        public static float TrailOpacityLow => 0.55f;
        public static float TrailOpacityHigh => 1f;

        // ---- colors (alpha 0 = additive) ----
        public static Color CoreColor => new Color(255, 240, 170, 0);
        public static Color OrangeColor => new Color(255, 120, 25, 0);
        public static Color DeepColor => new Color(170, 30, 10, 0);
        public static Color OutlineColor => new Color(95, 18, 8);
        public static Color GlowColor => new Color(255, 110, 20);
        public static Color HandleGlowColor => new Color(255, 120, 30, 0);

        // The lava fill by heat: dark crust, deep red, orange, amber, white-yellow (the five hard bands of the metaball layer).
        public static Color LavaFill(float heat)
        {
            Color[] stops =
            {
                new Color(70, 14, 8), new Color(170, 35, 10), new Color(240, 95, 15), new Color(255, 165, 35), new Color(255, 240, 160)
            };
            float x = MathHelper.Clamp(heat, 0f, 1f) * (stops.Length - 1);
            int i = Math.Min((int)x, stops.Length - 2);
            return Color.Lerp(stops[i], stops[i + 1], x - i);
        }
    }
}