using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// Every tunable number of the Night's Edge rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Arm angles are for a player facing right and RELATIVE TO THE AIM (the direction to the cursor when the swing
    /// starts): 0 = at the cursor, negative = above it, positive = below it; the ready pose is relative to the player instead (0 = forward,
    /// positive = down). The blade angle is arm + tilt. The shared charged blade (Common/ChargedBlade) reads these through NightsEdgeBladeTune.
    /// </summary>
    public static class NightsEdgeTuning
    {
        // ---- sword ----
        public static float BladeLength => 70f;       // hand to tip, px at scale 1: a first guess, tune it with ShowBladeDebug until the magenta square sits on the sprite's tip
        public static float SwordScale => 1.1f;
        public static float GripOffset => 0f;         // draw only: moves the sprite this far (px at scale 1) back along the blade, so the hand holds it higher or lower
        public static bool ShowBladeDebug => false;   // true: a magenta square on the tip the hit test uses

        // ---- damage: everything that decides what a swing does to an enemy ----
        public static float DamageMultiplier => 1f;     // of the item's damage (the speed is the reward for charging, so 1)
        public static float KnockbackMultiplier => 1f;
        public static float HitLengthScale => 1.07f;     // how far the sword HITS along the blade, as a fraction of BladeLength (1.5 = 50% further). Does not change what you see
        public static float HitWidth => 18f;            // how thick the hit line is (pixels at scale 1; the line is that wide around the blade). Does not change what you see
        public static int SweepHitSteps => 6;           // the hit test is checked at this many poses between two ticks (a fast strike needs more)
        public static float BlastDamageMultiplier => 0.6f; // the dark flame explosion of a charged hit: of the swing's base damage, on everything inside (the enemy that was hit too)
        public static int BlastSize => 104;             // diameter of that explosion (px): the area it hurts
        public static int BlastTicks => 4;              // how long the area lasts (every enemy inside is hit once)
        public static int BlastShadowflameTicks => 240; // Shadowflame on everything it hits (4 s)

        // ---- the flame wave (patch 4): every swing that STARTS with a fully charged sword throws one short wave toward the cursor ----
        public static float WaveDamageMultiplier => 0.6f;   // of the swing's base damage; each enemy is hit once
        public static float WaveKnockbackMultiplier => 0.5f;
        public static float WaveSpeed => 13f;               // px per tick (it does not slow down)
        public static float WaveRangeTiles => 12f;          // how far it flies before it fades out
        public static float WaveSpawnFraction => 0.8f;      // it is born this far along the blade length from the player (a fraction of BladeLength)
        public static int WaveShadowflameTicks => 240;      // Shadowflame on every enemy it hits (4 s)
        public static float WaveRadius => 40f;              // radius of the crescent's circle at birth (px)
        public static float WaveThickness => 11f;           // thickness of the crescent in the middle (px)
        public static float WaveArcHalfDeg => 55f;          // half the arc's angle
        public static float WaveGrowth => 0.4f;             // the crescent grows by this fraction over its flight (fast at first)
        public static int WaveAfterimages => 6;
        public static float WaveFlameRate => 1.3f;          // metaball flames shed off the crescent per tick
        public static float WaveFlameSize => 2.4f;          // their radius at birth (px)
        public static float WaveDustChance => 0.55f;        // per tick: a vanilla dust mote off the crescent

        // ---- charge: true melee hits speed the sword up until it is full ----
        public static int HitsToFull => 20;             // the FIRST enemy each swing touches counts as one hit (a swing through a crowd = 1)
        public static float ChargePerHit => 1f / Math.Max(1, HitsToFull);
        public static float ChargedAt => 0.995f;        // from this charge on the sword counts as fully charged
        public static float ChargeShowEase => 0.15f;    // how fast the DRAWN charge follows the real one (per tick, 0..1)
        public static float ChargeSyncStep => 0.04f;    // the owner tells the other machines when the charge moved this much
        public static float MaxSpeedScale => 2.5f;      // swing speed at full charge, as a multiple of the normal speed
        public static float SpeedCurvePower => 1f;      // 1 = the speed rises evenly with the charge; above 1 it rises late

        // ---- the charge fades (a FULL charge fades too): it starts when no swing has counted a hit for a while ----
        public static int FadeGraceTicks => 300;        // after the last counted hit the charge holds this long (3 s) ...
        public static int FadeDrainTicks => 600;        // ... then it drains from full to nothing over this long (3 s)

        // ---- the swing (a chop centered on the cursor, alternating up and down; scaled by the charge and by melee speed) ----
        public static float AttackSpeedInfluence => 1f; // 0 = ignore the player's melee attack speed, 1 = fully applied
        public static float MinSpeed => 0.3f;           // the final speed (charge x melee speed) is clamped to this range
        public static float MaxSpeed => 10f;
        public static int AnticipateTicks => 5;         // a swing that starts from the ready pose: the pull-back ...
        public static float AnticipateDeg => 18f;       // ... this far past the start of the arc
        public static int ChainAnticipateTicks => 9;    // a swing that FLOWS out of the previous one: the previous strike carries on through this wind-up ...
        public static float ChainAnticipateDeg => 24f;  // ... to this far past the end of the arc. Keep it above OvershootDeg, so the arm keeps drifting the way it was going
        public static float AnticipateTiltDeg => 10f;   // the wrist cocks back this much before every strike
        public static int SwingTicks => 10;             // the strike (the only part that damages)
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
        public static float TrailInnerHigh => 0.1f;     // ... and with a full charge
        public static float TrailOuter => 1f;        // the outer edge of the fan, as a fraction of the blade length (1 = the tip)
        public static float TrailLagTicks => 0.2f;      // how many ticks the trail runs BEHIND the sprite (0 = exactly on it)
        public static int TrailSamplesPerTick => 8;     // trail points laid per tick (the strike is fast, so many keep the fan smooth)
        public static float TrailOpacityLow => 0.3f;    // how strong the fan is with no charge ...
        public static float TrailOpacityHigh => 0.8f;   // ... and with a full charge

        // ---- charge look: a soft glow that grows with the charge, and at full charge the pulsing outline (like the Dark Lance's charged outline) ----
        public static float GlowMaxAlpha => 0.3f;       // opacity of the soft glow just before it is full
        public static float GlowScale => 1.18f;         // the glow copy is this much bigger than the sword, grown from the MIDDLE of the sprite
        public static float OutlineWidth => 2f;         // screen pixels
        public static int OutlinePulseTicks => 48;      // one full pulse
        public static float OutlineMin => 0.45f;        // the pulse runs between these two strengths
        public static float OutlineMax => 1f;
        public static float OutlineFade => 0.3f;        // how fast the outline fades in and out (per tick, 0..1)
        public static int FlashTicks => 12;             // the white flash when a strike starts and when the sword fills
        public static float HitShake => 0.6f;           // camera punch on a hit (grows with the charge)
        public static float SparkleChance => 0.3f;      // per tick, while fully charged: a mote somewhere along the blade

        // ---- the stab (not used yet: right click does nothing until patch 5 and 6; the shared blade reads these numbers already) ----
        public static int StabTurnTicks => 12;
        public static int StabAimTicks => 5;
        public static int StabPlungeTicks => 3;
        public static int StabHoldTicks => 7;
        public static int StabBufferTicks => 10;
        public static float StabArmDeg => -10f;
        public static float StabAimDistance => 10f;
        public static float StabDepth => 6f;
        public static float StabChestOffsetY => -2f;
        public static float StabShake => 1.2f;
        public static float StabBlendOut => 0.2f;
        public static float StabHitShake => 2.5f;

        // ---- the dark flame look (patch 3): shows with the full-charge outline; the edge is fed from NightsEdgeBladeMask.png ----
        public static float EdgeRadius => 2.2f;         // radius of one edge blob (px at scale 1)
        public static int EdgeBlobStride => 3;          // one blob on every Nth opaque mask pixel (1 = all of them, a lot of blobs)
        public static float EdgeMerge => 2.4f;          // how far a blob's field reaches, in its radii
        public static float EdgeOutline => 0f;       // thickness of the edge's outline (field units)
        public static float EdgeGlow => 0.15f;
        public static float EdgeHeat => 0.55f;          // color heat of the edge (0 = deep violet, 1 = pale lilac)
        public static float EdgeHeatFlicker => 0.2f;
        public static float EdgeWobble => 0.6f;         // each blob slides sideways this far (px) ...
        public static float EdgeWobbleSpeed => 5f;      // ... at this speed
        public static float FallbackStart => 0.3f;      // without the mask PNG the effect runs along the blade from this fraction of its length to the tip
        public static float FlameRate => 1.4f;          // flames born per tick at full charge
        public static float FlameSize => 2f;            // radius at birth (px at scale 1)
        public static int FlameLifeMin => 16;
        public static int FlameLifeMax => 28;
        public static float FlameRise => 0.05f;
        public static float FlameDrag => 0.95f;
        public static float FlameWobble => 0.04f;
        public static float FlameHeat => 0.8f;          // a flame starts this hot and cools
        public static float FlameMerge => 2.7f;
        public static float FlameOutline => 0f;
        public static float FlameGlow => 0.2f;
        public static float EmberDustChance => 0.35f;   // per tick at full charge: a vanilla Shadowflame / purple dust mote off the edge
        public static float LightStrength => 1f;        // the purple light along the blade
        public static float FlameDieCharge => 0.35f;    // the flame look follows the charge: it is full at full charge, thins and shrinks as the charge drains and is gone at this charge
        public static float FlameFollow => 0.12f;       // how fast the flame strength eases toward that (per tick, 0..1)
        public static int FlipCalmTicks => 3;           // after the sword flips to the other side the flames and embers ignore the blade's motion this long (the flip is instant)
        public static float BlastShake => 2.2f;         // camera punch of a charged hit's explosion
        public static int BlastCores => 6;              // big slow blobs in the middle of the explosion: they merge into one glob that shrinks away
        public static float BlastCoreSizeMin => 6f;
        public static float BlastCoreSizeMax => 10f;
        public static float BlastCoreSpeed => 1.6f;
        public static int BlastDroplets => 26;          // droplets thrown out and braked by BurstDrag
        public static float BlastSpeedMin => 2.5f;
        public static float BlastSpeedMax => 8f;
        public static float BlastDropMin => 2.4f;       // radius of a droplet
        public static float BlastDropMax => 5f;
        public static float BurstMerge => 2.6f;         // how far the droplets' fields reach (bigger = they bridge and merge more)
        public static float BurstGravity => 0.1f;
        public static float BurstDrag => 0.9f;          // velocity kept per tick: low = thrown out and stopped quickly
        public static float BurstHeat => 0.95f;         // color heat at birth, cools to about a quarter of it

        // ---- colors (alpha 0 = additive) ----
        public static Color OutlineColor => PurpleColor;
        public static Color OutlineCoreColor => PurpleColor;//new Color(235, 200, 255, 0);
        public static Color CoreColor => new Color(235, 205, 255, 0);
        public static Color PurpleColor => new Color(150, 60, 240, 0);
        public static Color DeepColor => PurpleColor;// new Color(60, 15, 130, 0);
    }
}