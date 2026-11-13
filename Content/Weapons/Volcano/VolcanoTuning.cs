using Microsoft.Xna.Framework;
using System;
using Terraria;

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
        public static float BladeLength => 140f;      // joint to the tip of the lava blade
        public static float SwordScale => 1f;
        public static float HandleCrop => 0.32f;     // stand-in art only: the part of the vanilla sprite (from its hilt corner) drawn as the handle
        public static bool ShowBladeDebug => false;  // true: magenta square on the tip the hit test uses, green square on the joint

        // ---- heat (0 = the weak baseline flame, 1 = fully charged) ----
        public static float HeatPerHit => 0.1f;      // the FIRST true melee hit of each swing adds this (10 swings = fully charged)
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
        public static float BlobSpacing => 3f;       // px between the round blobs along the blade
        public static float BlobMerge => 2.7f;
        public static float BlobOutline => 0f;
        public static float BlobGlow => 0.1f;
        public static float EdgeCool => 0f;        // thickness of the cooled rim of the lava (field units; 0 = off). Higher = darker, thicker rim; the white band only shows deeper inside
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
        public static float BodyEndLow => 0.8f;        // where the lava body is cut off at heat 0 (1 = the tip); lower it until the lone pixels at the tip are gone ...
        public static float BodyEndHigh => 0.8f;       // ... and the same at full heat; in between it is interpolated by the heat
        public static float BodyScatter => 0.35f;       // how far from the middle line the body blobs sit (fraction of the blob radius)
        public static float CoreScatter => 0.6f;        // the same for the white-hot spots
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

        // ---- patch 2: the right-click throw (only at full heat; the whole sword flies tip first, then sticks and melts into what it touched) ----
        public static float ThrowSpeed => 24f;               // px per tick, straight toward the cursor
        public static int ThrowMaxTicks => 100;               // no contact by then: it bursts in the air (about 60 tiles at this speed)
        public static float ThrowHitWidth => 24f;            // thickness of the tip's path in the contact test (px at scale 1)
        public static float ThrowDamageMultiplier => 2.5f;   // the impact hit on the enemy it latches onto (the eruption of patch 3 is separate)
        public static float ThrowKnockbackMultiplier => 0.3f;
        public static float ThrowShake => 2f;
        public static int ThrowArmTicks => 8;                // the throwing arm stays pointed along the sword this long
        public static float FlightDripRate => 1.2f;          // lava drips left behind per tick while it flies

        // ---- the fuse: stuck in a tile or on an enemy ----
        public static int FuseTicks => 60;                   // how long it sits before the end (patch 3: the eruption)
        public static float MeltPower => 1.2f;               // the pumping: 1 = even; higher = slow start, quick end
        public static float SinkDepth => 12f;                // at the end the handle has pushed this far past the contact point (px at scale 1)
        public static float FuseShake => 2.4f;               // shake of the sword at the end (px at scale 1)
        public static float FuseSwell => 0.9f;               // the remaining blade grows this much thicker by the end
        public static float FuseBrighten => 0.35f;           // color heat added to the blade by the end
        public static float PoolRadiusStart => 5f;           // the pool of lava at the contact point (px at scale 1) ...
        public static float PoolRadiusEnd => 15f;            // ... swells to this
        public static float FuseDripRate => 0.7f;            // lava drips off the pool per tick at the end
        public static int SizzleTicks => 12;
        public static float StickSizzleVolume => 0.9f;       // the sizzle when the sword attaches to an enemy or a tile and starts to melt
        public static float StickShake => 3f;                // camera punch when it sticks
        public static int RegrowTicks => 0;                  // after the handle is caught the hand stays empty this long before the blade forms (0 = at once)

        // ---- charged swing: every enemy the blade hits bursts into a small lava explosion ----
        public static int BlastSize => 112;                  // diameter of the explosion
        public static int BlastTicks => 4;
        public static float BlastDamageMultiplier => 0.6f;   // of the swing's base damage, on everything inside (the enemy that was hit too)
        public static int BlastOnFireTicks => 240;
        public static float BlastShake => 2.5f;
        public static int BlastCores => 6;                   // big slow blobs in the middle: they merge into one glob that shrinks away
        public static float BlastCoreSizeMin => 6f;
        public static float BlastCoreSizeMax => 10f;
        public static float BlastCoreSpeed => 1.6f;
        public static int BlastDroplets => 26;               // droplets thrown out and braked by BurstDrag
        public static float BlastSpeedMin => 2.5f;
        public static float BlastSpeedMax => 8f;
        public static float BlastDropMin => 2.4f;            // radius of a droplet
        public static float BlastDropMax => 5f;
        public static float BurstMerge => 2.6f;              // how far the droplets' fields reach (bigger = they bridge and merge more)
        public static float BurstGravity => 0.1f;
        public static float BurstDrag => 0.9f;               // velocity kept per tick: low = thrown out and stopped quickly
        public static float BurstHeat => 0.95f;              // color heat at birth, cools to about a quarter of it

        // ---- patch 3: the eruption (the end of the fuse; it also fires when the thrown sword bursts in the air) ----
        public static int EruptionSize => 260;               // diameter of the shockwave (px), a bit over twice the charged blast
        public static float EruptionDamageMultiplier => 2f;  // of the thrown sword's base damage, on everything inside (once)
        public static int EruptionOnFireTicks => 300;        // Hellfire
        public static float EruptionShake => 6f;
        public static int EruptionCores => 12;               // the same metaball burst as the charged blast, scaled up by the size
        public static int EruptionDroplets => 48;
        public static int EruptionDrops => 18;               // real lava drops that rain down in a fan (VolcanoLavaDropProjectile)
        public static float EruptionFanDeg => 55f;           // half the width of the fan; its middle is 'up', leaning away from the way the sword came in
        public static float DropSpeedMin => 6f;
        public static float DropSpeedMax => 12f;
        public static float DropGravity => 0.28f;
        public static float DropRadius => 4.5f;              // the blob of a drop (px; each drop is 0.8 to 1.4 times this)
        public static float DropDamageMultiplier => 0.3f;    // of the thrown sword's base damage, per hit
        public static int DropHitCooldown => 30;             // a drop (or a puddle) hurts the same enemy at most this often
        public static int DropOnFireTicks => 240;            // Hellfire on touch
        public static int DropFlightMaxTicks => 240;         // a drop that never lands is removed after this
        public static int DropLingerTicks => 300;            // how long a landed puddle stays (6 s)
        public static int DropFadeTicks => 50;               // it shrinks away over the last of it
        public static int DropMax => 40;                     // at most this many alive per player (the oldest hiss out first)
        public static float DropHeat => 0.85f;               // color heat of a fresh drop ...
        public static float DropColdHeat => 0.25f;           // ... cooling to this while it lingers

        // ---- patch 3: the handle is thrown out of the eruption, spins, bounces and flies home ----
        public static float HandleSpinOffset => 1f;         // the handle spins around the point this far back from the joint (px at scale 1: about the middle of your handle art)
        public static float BounceKickSpeed => 9f;           // thrown out of the contact point, back the way it came in
        public static float BounceKickUp => 4f;
        public static int BounceArcTicks => 16;              // this long it only flies free (kick, gravity, drag): the bounce
        public static float BounceGravity => 0.35f;
        public static float BounceDrag => 0.98f;
        public static float BounceSpinDeg => 20f;            // spin per tick at the start (it eases off while it homes)
        public static float ReturnAccel => 0.9f;             // then it homes in on the player and speeds up by this per tick ...
        public static float ReturnSpeedMax => 26f;           // ... up to this (px per tick)
        public static float ReturnTurn => 0.18f;             // how sharply it steers toward the player (0..1)
        public static float CatchDistance => 30f;            // closer than this to the player: caught
        public static int ReturnMaxTicks => 150;             // it never flies home longer than this
        public static int ReturnCoolTicks => 45;             // the handle glows and drips lava for this long after the eruption

        // ---- colors (alpha 0 = additive) ----
        public static Color CoreColor => new Color(255, 240, 170, 0);
        public static Color OrangeColor => new Color(255, 120, 25, 0);
        public static Color DeepColor => new Color(170, 30, 10, 0);
        public static Color OutlineColor => OrangeColor * 0f;
        public static Color GlowColor => OrangeColor;
        public static Color HandleGlowColor => OrangeColor;//new Color(255, 120, 30, 0);

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