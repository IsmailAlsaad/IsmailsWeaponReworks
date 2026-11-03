using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>
    /// Every tunable number of the Blood Butcherer rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot
    /// reload can tweak them. All lengths are pixels at scale 1. The bite is one fixed timeline (not scaled by melee speed):
    /// WINDUP (pull back) > THRUST (lunge along the aim) > OPEN (the jaws pivot open) > GAPE (hangs open, the forgiving part)
    /// > BITE (the jaws slam shut; the only part that damages and catches) > CLAMP (held shut, only when something was caught)
    /// > RECOVER (retract).
    /// </summary>
    public static class BloodButchererTuning
    {
        // ---- sword ----
        public static float BladeLength => 56f;       // hand to tip
        public static float HandleLength => 12f;      // hand to the jaw pivot; the jaws are the rest of the blade
        public static float SwordScale => 1f;

        // ---- timeline ----
        public static int WindupTicks => 8;
        public static int ThrustTicks => 6;
        public static int OpenTicks => 8;
        public static int GapeTicks => 3;
        public static int BiteTicks => 4;
        public static int ClampTicks => 6;            // held shut: only for an enemy that cannot be gripped (boss, worm, immovable, too big); patch 3 replaces it with the chomp
        public static int RecoverTicks => 12;
        public static int DecideWaitTicks => 20;      // another machine waits this long for the owner's catch decision, then assumes a miss

        // ---- grip, spin and launch (patch 2): an enemy that can be gripped is pinned in the shut jaws, spun a full circle and thrown at the cursor ----
        public static int PullTicks => 6;             // the blade drags the enemy in from where it was caught to the middle of the mouth
        public static int SpinTicks => 34;            // the full circle (ease-in: it gets faster until the throw)
        public static int LockTicks => 4;             // the aim stops following the cursor this many ticks before the throw
        public static int WindDownTicks => 26;        // after the throw (or when the enemy dies first) the blade keeps spinning, slowing evenly to a stop, and comes back to the player
        public static int WindDownRecoverTicks => 4;  // a short pause with the blade back in the hand before the next bite can start
        public static int ReleaseHoldTicks => 6;      // the jaws stay open this long after they fly open, then close while the blade slows
        public static int ReleaseOpenTicks => 3;      // how fast they fly open
        public static float SpinOffset => 50f;        // how far the blade is pushed out from the hand while it spins
        public static float SpinFollow => 0.5f;       // radians per tick the sweep may change while it follows the cursor
        public static float PinRatio => 0.6f;         // where in the shut jaws the enemy is pinned (0 = pivot, 1 = tips)
        public static float LaunchSpeed => 15f;       // pixels per tick toward the cursor
        public static int LaunchAiOffTicks => 25;    // the thrown enemy cannot steer or attack for this long (normal gravity and tile collision)
        public static float LaunchGravity => 0.3f;
        public static float LaunchMaxFall => 12f;
        public static float GripTickDamageMultiplier => 0.3f; // while it is held
        public static int GripTickInterval => 3;      // ticks between those hits
        public static float LaunchDamageMultiplier => 1.6f;   // the throw
        public static float MaxGrabSize => 120f;      // an enemy bigger than this (width or height) is clamped, not gripped
        public static float LaunchShake => 6f;
        public static float SpinWhooshPitchLow => 0f;
        public static float SpinWhooshPitchHigh => 0.7f;

        // ---- spin trail (patch 2b): a fan along the blade that fills it up as the blade speeds up and thins out as it slows down ----
        public static float TrailFullSpeed => 0.4f;   // the fan covers the whole blade at this angular speed (radians per tick)
        public static float TrailFillTo => 0.15f;     // fully expanded, the fan reaches down to this fraction of the blade length (0 = the hand)
        public static int SpinTrailLifeTicks => 16;
        public static int SpinTrailRails => 7;
        public static float SpinTrailOpacity => 0.9f;
        public static int SpinTrailSamplesPerTick => 5;

        // ---- blood tether (patch 2): a crimson strand from the player's real hand to the handle of the blade ----
        public static float TetherRestLength => ThrustDistance + BiteLungeDistance + 1f; // closer than this and the strand droops
        public static float TetherWidth => 5f;
        public static float TetherThinWidth => 5f;    // how thin it gets at full stretch
        public static float TetherSag => 1f;
        public static float TetherMaxSag => 12f;
        public static float TetherTrailing => 0f;    // a spinning blade drags the middle of the strand behind it: pixels per (radian per tick)
        public static float TetherMaxTrailing => 20f;
        public static float TetherWobble => 1.5f;
        public static int TetherSegments => 14;
        public static Color TetherDarkColor => new Color(105, 6, 16);    // opaque body
        public static Color TetherMidColor => new Color(190, 18, 30);    // opaque inner stripe
        public static Color TetherGlowColor => new Color(255, 90, 70, 0); // additive core

        // ---- movement ----
        public static float WindupPullback => 12f;    // how far the blade is pulled back along the aim before the thrust
        public static float ThrustDistance => 26f;    // how far it lunges forward along the aim
        public static float BiteLungeDistance => 6f;  // an extra push forward while the jaws slam shut
        public static float JawOpenDeg => 34f;        // each jaw opens this far from the blade's axis

        // ---- catching and damage ----
        public static float CatchPadding => 10f;      // the catch wedge (the triangle between the pivot and the two open jaw tips) is this much more forgiving
        public static float BiteDamageMultiplier => 1f;   // the enemy the bite caught
        public static float OtherDamageMultiplier => 0.6f; // any other enemy inside the jaws
        public static float ClampShake => 5f;         // camera punch strength when the jaws clamp on something

        // ---- look ----
        public static float PixelSize => 2f;
        public static int TeethCount => 6;
        public static float ToothLength => 9f;
        public static int FlashTicks => 10;
        public static int TrailLifeTicks => 14;
        public static float TrailWidth => 6f;
        public static int TrailSamplesPerTick => 3;
        public static Color CrimsonColor => new Color(150, 10, 25, 0);
        public static Color BrightColor => new Color(255, 80, 60, 0);
        public static Color BoneColor => new Color(236, 228, 208);

        // Art (BloodButchererHandle / BloodButchererJawUpper / BloodButchererJawLower .png): drawn like a vanilla sword, tip to the upper right at 45 degrees. Every
        // sprite's pivot is the CENTRE of its own canvas, and that centre is placed on the jaw joint (where the handle meets the jaws), so pad each canvas
        // with transparent pixels until the joint is in the middle. Until all three exist the vanilla sprite is used. HandleLength below is the distance from
        // the joint back to the end of the handle (where the blood tether is tied), so match it to your handle art.
    }
}