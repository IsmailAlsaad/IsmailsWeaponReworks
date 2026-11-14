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

        // ---- the flame wave (patch 4, back in patch 6a): every swing that STARTS with a fully charged sword throws one short wave toward the cursor; not while the Shadow Armor is on ----
        public static float WaveDamageMultiplier => 0.6f;   // of the swing's base damage; each enemy is hit once
        public static float WaveKnockbackMultiplier => 0.5f;
        public static float WaveSpeed => 13f;               // px per tick (it does not slow down)
        public static float WaveRangeTiles => 32f;          // how far it flies before it fades out
        public static float WaveSpawnFraction => 0.6f;      // it is born this far along the blade length from the player (a fraction of BladeLength)
        public static int WaveShadowflameTicks => 240;      // Shadowflame on every enemy it hits (4 s)
        public static float WaveRadius => 80f;              // radius of the crescent's circle at birth (px)
        public static float WaveThickness => 15f;           // thickness of the crescent in the middle (px)
        public static float WaveArcHalfDeg => 55f;          // half the arc's angle
        public static float WaveGrowth => 0.8f;             // the crescent grows by this fraction over its flight (fast at first)
        public static int WaveAfterimages => 6;
        public static float WaveFlameRate => 1.3f;          // metaball flames shed off the crescent per tick
        public static float WaveFlameSize => 2.4f;          // their radius at birth (px)
        public static float WaveDustChance => 0.55f;        // per tick: a vanilla dust mote off the crescent
        public static float WaveTipTrailWidth => 9f;        // the trail behind each pointed end of the crescent: its width at the start (px) ...
        public static int WaveTipTrailLifeTicks => 18;      // ... and how long a point of it lives (the flail head's trail look)

        // ---- the impale (patch 6a): damage ----
        public static float ImpaleStabDamageMultiplier => 1f;   // the stab itself, of the item's damage (a boss or an unmovable enemy takes only this)
        public static float ImpaleBeatDamageMultiplier => 0.3f; // each beat's pulse
        public static float ImpaleFinishDamageMultiplier => 4f; // the finisher's explosion (it also hurts what stands next to the enemy)
        public static float DropDamageMultiplier => 0.35f;      // a dark droplet or puddle hurts an enemy that touches it: of the Night's Edge's damage
        public static int DropHitCooldownTicks => 30;           // ... at most this often per enemy (counted across ALL droplets, so many puddles do not hit every tick)
        public static float ImpaleMaxSize => 120f;              // an enemy bigger than this (width or height) is stabbed but not impaled (the Blood Butcherer's limit)

        // ---- charge: true melee hits speed the sword up until it is full ----
        public static int HitsToFull => 15;             // the FIRST enemy each swing touches counts as one hit (a swing through a crowd = 1)
        public static float ChargePerHit => 1f / Math.Max(1, HitsToFull);
        public static float ChargedAt => 0.995f;        // from this charge on the sword counts as fully charged
        public static float ChargeShowEase => 0.15f;    // how fast the DRAWN charge follows the real one (per tick, 0..1)
        public static float ChargeSyncStep => 0.04f;    // the owner tells the other machines when the charge moved this much
        public static float MaxSpeedScale => 2.5f;      // NOT USED by the Night's Edge any more (the swing slows down with the charge, see FullTimeScale); the shared blade still reads it
        public static float SpeedCurvePower => 1f;      // 1 = the swing slows down evenly with the charge; above 1 it slows late
        public static float FullTimeScale => 2f;        // THE SWING SLOWS DOWN (the Volcano's pattern): a swing takes this many times as long at full charge (1 when empty, which is the fastest)
        public static float ArmorTimeScale => 1f;       // with the Shadow Armor on a swing takes this many times as long (1 = as fast as an empty sword)

        // ---- the charge fades (a FULL charge fades too): it starts when no swing has counted a hit for a while ----
        public static int FadeGraceTicks => 600;        // after the last counted hit the charge holds this long (3 s) ...
        public static int FadeDrainTicks => 600;        // ... then it drains from full to nothing over this long (3 s)

        // ---- the swing (a chop centered on the cursor, alternating up and down; scaled by the charge and by melee speed) ----
        public static float AttackSpeedInfluence => 1f; // 0 = ignore the player's melee attack speed, 1 = fully applied
        public static float MinSpeed => 0.3f;           // the final speed (charge x melee speed) is clamped to this range
        public static float MaxSpeed => 10f;
        public static int AnticipateTicks => 3;         // a swing that starts from the ready pose: the pull-back ...
        public static float AnticipateDeg => 18f;       // ... this far past the start of the arc
        public static int ChainAnticipateTicks => 9;    // a swing that FLOWS out of the previous one: the previous strike carries on through this wind-up ...
        public static float ChainAnticipateDeg => 24f;  // ... to this far past the end of the arc. Keep it above OvershootDeg, so the arm keeps drifting the way it was going
        public static float AnticipateTiltDeg => 10f;   // the wrist cocks back this much before every strike
        public static int SwingTicks => 6;             // the strike (the only part that damages)
        public static float SwingEasePower => 2f;       // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 8f;         // the strike carries this far past the end of the arc
        public static int RecoverTicks => 4;            // only when the button is let go: the arm settles back to the end of the arc over this long
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

        // ---- the self-stab (patch 5): right click at full charge; the shared blade plays it and reads these numbers ----
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

        // ---- the impale (patch 6a): right click below full charge; a dagger stab that spears an enemy, lifts it in beats and ends in a finisher ----
        public static int ImpaleBufferTicks => 10;      // a right click during a strike is remembered this long
        public static int ImpaleWindupTicks => 7;       // the pull-back before the thrust
        public static float ImpaleWindupArmDeg => 75f;  // the arm drops this far below the aim before the uppercut
        public static float ImpaleSwingEndDeg => 0f;  // the upward swing ends with the arm this far from the aim (negative = above the cursor line)
        public static float ImpaleBladeAngleDeg => 50f; // THE BLADE IS HELD THIS FAR OFF THE ARM: 90 = across the hand like a hook (the uppercut), 0 = straight along the arm (a plain stab)
        public static float ImpaleWindupPull => 13f;     // the sword is slid back along its length this far (px) in the wind-up
        public static int ImpaleSwingTicks => 7;        // the upward swing: the only part that can catch an enemy
        public static float ImpaleReach => 0f;          // the sword is slid out along its length this far (px) at the end of the swing and while it holds the enemy
        public static int ImpaleWaitTicks => 14;        // other machines wait this long for the owner's catch before they call it a miss
        public static int ImpaleMissTicks => 12;        // a stab that caught nothing holds its pose this long
        public static int ImpaleSeatTicks => 6;         // the caught enemy slides from where it was onto the blade over this long
        public static float ImpaleAnchorFraction => 0.8f; // the enemy sits this far along the blade (1 = at the tip)
        public static int ImpaleBeats => 3;             // beats; each ends in a pulse of damage
        public static int ImpaleBeatTicks => 26;        // one beat
        public static float ImpaleRisePortion => 0.7f;  // the lift happens in this first part of each beat, the rest is a pause before the pulse
        public static float ImpaleHeldAngleDeg => 60f;  // THE ARM LIFTS to this angle above the horizontal (90 = straight up) while the enemy is held; the blade keeps the angle it stabbed at (ImpaleBladeAngleDeg)
        public static float ImpaleShake => 1f;          // the blade trembles this much (px) while it holds the enemy
        public static int ImpaleFinishTicks => 8;       // the pose is held this long after the finisher
        public static float ImpaleHitShake => 2.5f;     // camera punch of the stab
        public static float ImpaleBeatShake => 1.5f;    // camera punch of a beat

        // ---- Shadow Armor (patch 5): the buff the self-stab gives ----
        public static int ShadowArmorTicks => 480;      // how long it lasts (8 s); the sword stays fully charged and its fade is paused meanwhile, the charge is 0 when it ends
        public static int ShadowArmorFadeTicks => 40;   // the body flames and the light ease out over the last this many ticks
        public static float ShadowArmorReduction => 0.15f; // damage reduction (Player.endurance)
        public static float ArmorFlameRate => 1.6f;     // dark flames born per tick off the body
        public static float ArmorFlameSize => 2.6f;     // their radius at birth (px)
        public static float ArmorDustChance => 0.5f;    // per tick: a vanilla dust mote off the body
        public static float ArmorLightStrength => 0.9f; // the purple light around the player
        public static int ArmorIgniteDroplets => 14;    // big droplets thrown from the body the moment the armor switches on

        // ---- the dark droplets: while the armor is on the player spits them up and out; they arc, land and lie as small dark puddles ----
        public static int DropIntervalTicks => 6;       // one droplet this often while the armor is on
        public static int StabBurstDrops => 10;         // the fan thrown at the moment of the stab
        public static float DropFanDeg => 60f;          // the fan opens this far to each side of straight up
        public static float DropSpeedMin => 3f;
        public static float DropSpeedMax => 7f;
        public static float DropGravity => 0.25f;
        public static float DropSizeMin => 0.8f;       // a droplet's size multiplier is random between these
        public static float DropSizeMax => 1.3f;
        public static float DropRadius => 3.2f;         // metaball radius at size 1 (px)
        public static float DropHeat => 0.7f;           // color heat of a fresh droplet, cooling to DropColdHeat on the ground
        public static float DropColdHeat => 0.2f;
        public static int DropFlightMaxTicks => 180;    // a droplet that never lands is removed after this long
        public static int DropLingerTicks => 180;       // a landed puddle lies this long (3 s) ...
        public static int DropFadeTicks => 40;          // ... and shrinks away over its last this many ticks
        public static int DropMax => 30;                // alive at once per player; a new droplet over this makes the oldest go
        public static int DropShadowflameTicks => 240;  // Shadowflame on an enemy that touches a droplet or a puddle (4 s); the damage is DropDamageMultiplier in the damage section
        public static float DropTouchWidth => 28f;      // a puddle's touch area (px)
        public static float DropTouchHeight => 12f;

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