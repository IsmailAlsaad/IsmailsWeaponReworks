using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// Every tunable number of the Light's Bane rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Arm angles are for a player facing right and RELATIVE TO THE AIM (the direction to the cursor when the swing
    /// starts): 0 = at the cursor, negative = above it, positive = below it; the ready pose is relative to the player instead (0 = forward,
    /// positive = down). The blade angle is arm + tilt.
    /// </summary>
    public static class LightsBaneTuning
    {
        // ---- sword ----
        public static float BladeLength => 54f;       // hand to tip, px at scale 1: a first guess, tune it with ShowBladeDebug until the magenta square sits on the sprite's tip
        public static float SwordScale => 1.1f;
        public static float GripOffset => 0f;         // draw only: moves the sprite this far (px at scale 1) back along the blade, so the hand holds it higher or lower
        public static bool ShowBladeDebug => false;   // true: a magenta square on the tip the hit test uses

        // ---- damage: everything that decides what a swing does to an enemy ----
        public static float DamageMultiplier => 1f;     // of the item's damage (the speed is the reward for charging, so 1)
        public static float KnockbackMultiplier => 1f;
        public static float HitLengthScale => 1.3f;     // how far the sword HITS along the blade, as a fraction of BladeLength (1.5 = 50% further). Does not change what you see
        public static float HitWidth => 18f;            // how thick the hit line is (pixels at scale 1; the line is that wide around the blade). Does not change what you see
        public static int SweepHitSteps => 6;           // the hit test is checked at this many poses between two ticks (a fast strike needs more)

        // ---- charge: true melee hits speed the sword up until it is full ----
        public static int HitsToFull => 20;             // the FIRST enemy each swing touches counts as one hit (a swing through a crowd = 1)
        public static float ChargePerHit => 1f / Math.Max(1, HitsToFull);
        public static int ChargeHoldTicks => 180;       // the charge (a full one too) does not cool for this long after the last counted hit
        public static float ChargeDecayPerTick => 0.003f; // then it cools by this much per tick (a full bar in about 5.5 s; 0.0056 = 3 s like the Night's Edge). A full charge cools too, except while imbued
        public static float ChargedAt => 0.995f;        // from this charge on the sword counts as fully charged
        public static float ChargeShowEase => 0.15f;    // how fast the DRAWN charge follows the real one (per tick, 0..1)
        public static float ChargeSyncStep => 0.04f;    // the owner tells the other machines when the charge moved this much
        public static float MaxSpeedScale => 4f;      // swing speed at full charge, as a multiple of the normal speed
        public static float SpeedCurvePower => 1f;      // 1 = the speed rises evenly with the charge; above 1 it rises late

        // ---- the swing (a chop centered on the cursor, alternating up and down; scaled by the charge and by melee speed) ----
        public static float AttackSpeedInfluence => 1f; // 0 = ignore the player's melee attack speed, 1 = fully applied
        public static float MinSpeed => 0.3f;           // the final speed (charge x melee speed) is clamped to this range
        public static float MaxSpeed => 6f;
        public static int AnticipateTicks => 5;         // a swing that starts from the ready pose: the pull-back ...
        public static float AnticipateDeg => 18f;       // ... this far past the start of the arc
        public static int ChainAnticipateTicks => 9;    // a swing that FLOWS out of the previous one: the previous strike carries on through this wind-up ...
        public static float ChainAnticipateDeg => 24f;  // ... to this far past the end of the arc. Keep it above OvershootDeg, so the arm keeps drifting the way it was going
        public static float AnticipateTiltDeg => 10f;   // the wrist cocks back this much before every strike
        public static int SwingTicks => 10;              // the strike (the only part that damages)
        public static float SwingEasePower => 2f;       // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 8f;         // the strike carries this far past the end of the arc
        public static int RecoverTicks => 7;            // only when the button is let go: the arm settles back to the end of the arc over this long
        public static float SwingHighDeg => -70f;       // top of the arc
        public static float SwingLowDeg => 70f;         // bottom of the arc
        public static float TiltHighDeg => -14f;        // blade tilt relative to the arm at the top (wrist) ...
        public static float TiltLowDeg => 14f;          // ... and at the bottom

        // ---- the ready pose (between swings): the sword held up at the shoulder, bobbing a little ----
        public static float ReadyArmDeg => 30f;
        public static float ReadyTiltDeg => -60f;
        public static float IdleBobDeg => 2f;
        public static int IdleBobTicks => 90;
        public static float IdleEase => 0.15f;          // how fast the arm settles into the ready pose after a swing (per tick, 0..1)

        // ---- swing trail (a SweepTrail fan, drawn by the shared pixel line renderer; it grows with the charge) ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 16;
        public static float TailLifeScale => 0.45f;     // the first point of a swing's trail lives this fraction as long as the last one
        public static int TrailRails => 7;              // parallel pixel lines that fill the fan
        public static float TrailExpandBy => 0.8f;      // the fan has fully expanded this far into the strike (0..1)
        public static float TrailInnerLow => 0.8f;      // fully expanded, the fan reaches down the blade to this fraction of its length: with no charge ...
        public static float TrailInnerHigh => 0.3f;     // ... and with a full charge
        public static float TrailOuter => 1.25f;        // the outer edge of the fan, as a fraction of the blade length (1 = the tip)
        public static float TrailLagTicks => 0.4f;      // how many ticks the trail runs BEHIND the sprite (0 = exactly on it)
        public static int TrailSamplesPerTick => 8;     // trail points laid per tick (the strike is fast, so many keep the fan smooth)
        public static float TrailOpacityLow => 0.3f;    // how strong the fan is with no charge ...
        public static float TrailOpacityHigh => 0.75f;  // ... and with a full charge

        // ---- charge look: a soft glow that grows with the charge, and at full charge the pulsing outline ----
        public static float GlowMaxAlpha => 0.3f;       // opacity of the soft glow just before it is full
        public static float GlowScale => 1.18f;         // the glow copy is this much bigger than the sword, grown from the MIDDLE of the sprite
        public static float OutlineWidth => 2f;         // screen pixels
        public static int OutlinePulseTicks => 40;      // one full pulse
        public static float OutlineMin => 0.5f;         // the pulse runs between these two strengths
        public static float OutlineMax => 1f;
        public static float OutlineFade => 0.25f;       // how fast the outline fades in and out (per tick, 0..1)
        public static int FlashTicks => 12;             // the white flash when a strike starts and when the sword fills
        public static float HitShake => 0.6f;           // camera punch on a hit (grows with the charge)
        public static float SparkleChance => 0.25f;     // per tick, while fully charged: a mote somewhere along the blade

        // ---- the stab (right click at full charge): the blade turns to the chest, is held by its sharp end and driven in, and the player is imbued ----
        // Arm angles are facing-local (0 = forward, negative = up). The blade is much longer than the player's arm reach, so during the stab the
        // sword (full size) is slid back through the fist until the fist holds it near the tip (the hand itself never moves off the arm): the tip
        // stands StabAimDistance in front of the chest while aiming and the fist slides up the blade during the plunge.
        public static int StabTurnTicks => 12;          // the arm goes to StabArmDeg, the blade turns to point at the chest and slides back through the fist
        public static int StabAimTicks => 5;            // a short held aim: the blade trembles and sparkles
        public static int StabPlungeTicks => 3;         // the plunge (fast)
        public static int StabHoldTicks => 7;           // the blade stays buried while the burst plays
        public static int StabBufferTicks => 10;        // a right click during a strike is remembered this long and fires as soon as the strike is over
        public static float StabArmDeg => -10f;         // the arm while stabbing (facing-local: 0 = forward, negative = up)
        public static float StabAimDistance => 10f;     // while aiming, the tip stands this far in front of the chest (px)
        public static float StabDepth => 6f;            // when plunged, the tip is this far PAST the center of the chest (px)
        public static float StabChestOffsetY => -2f;    // the chest point the blade aims at, relative to the player's center (px, negative = up)
        public static float StabShake => 1.2f;          // tremble of the blade while aiming and buried (px; half as much while aiming)
        public static float StabBlendOut => 0.2f;       // after the stab the sword slides back out of the chest into the hand by this much per tick (0..1)
        public static float StabHitShake => 2.5f;       // camera punch of the impact

        // ---- the imbue (after the stab): a speed boost with purple dust and light ----
        public static int ImbueTicks => 480;            // how long it lasts (8 seconds)
        public static int ImbueFadeTicks => 40;         // the boost eases out over the last this many ticks
        public static float ImbueSpeedMultiplier => 1.6f; // max run speed and top speed in the air (maxRunSpeed and accRunSpeed)
        public static float ImbueAccelMultiplier => 1.6f; // how fast the player gets there (runAcceleration)
        public static float ImbueDustChance => 0.6f;    // per tick while moving: a purple mote at the feet
        public static float ImbueDustMinSpeed => 2f;    // the player must move at least this fast (px per tick) for the dust
        public static int ImbueFlickerCycles => 0;      // over the last ImbueFadeTicks the Phase dye look blinks on and off, faster and faster: this many blinks
        public static float ImbueAfterimageSpacing => 18f; // pixels the player travels between two afterimages
        public static int ImbueAfterimageTicks => 16;   // how long one afterimage lasts
        public static float ImbueAfterimageShadow => 0.45f; // how see-through the freshest afterimage is (0 = solid, 1 = invisible); it fades to 1
        public static int ImbueBuffGraceTicks => 30;    // the imbue's visuals ignore a missing buff this long after they start (on other machines the buff can arrive a moment late)

        // ---- colors (alpha 0 = additive) ----
        public static Color OutlineColor => new Color(150, 70, 255, 0);
        public static Color OutlineCoreColor => new Color(225, 190, 255, 0);
        public static Color CoreColor => new Color(235, 215, 255, 0);
        public static Color PurpleColor => new Color(165, 85, 255, 0);
        public static Color DeepColor => new Color(85, 35, 165, 0);
    }
}