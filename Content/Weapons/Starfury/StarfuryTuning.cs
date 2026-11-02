using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>
    /// Every tunable number of the Starfury rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Arm angles are for a player facing right: 0 = forward, positive = down, negative = up.
    /// </summary>
    public static class StarfuryTuning
    {
        // ---- sword ----
        public static float BladeLength => 56f;       // hand to tip, in pixels at scale 1
        public static float SwordScale => 1.15f;

        // ---- swing: fixed ticks, NOT scaled by melee speed; left click alternates down, up, down ... ----
        public static int WindupTicks => 2;           // only used when the swing does not continue from the previous one
        public static int SwingTicks => 8;
        public static int RecoverTicks => 3;
        // All of these are relative to the AIM (the direction to the cursor when the swing starts): 0 = at the cursor, negative = above
        // it, positive = below it. The arc is symmetric, so the middle of every slash points exactly at the cursor.
        public static float RestArmDeg => 10f;        // where a fresh combo starts (just before the high end of the arc)
        public static float SwingHighDeg => -70f;     // top of the arc
        public static float SwingLowDeg => 70f;       // bottom of the arc
        public static float TiltHighDeg => -12f;      // blade tilt relative to the arm at the top (wrist) ...
        public static float TiltLowDeg => 12f;        // ... and at the bottom
        public static int ComboTicks => 30;           // a swing started within this long after the last one continues the alternation

        // ---- the call (right click): sword up, a star falls from offscreen along a straight line to a point in front of the player ----
        public static int CallLiftTicks => 6;         // the arm lifts with a cubic ease-out
        public static int CallSpawnTick => 7;         // the star is born on this tick of the call
        public static int CallTicks => 18;            // the whole pose; no left swing can start before it ends
        public static float CallArmDeg => -80f;
        public static float CallTiltDeg => -10f;      // arm plus tilt = the blade points straight up
        public static float CallKickDeg => 6f;        // the arm kicks up this much when the star is born
        public static int CallCooldownTicks => 24;    // from the right click to the next allowed right click
        public static float CallFrontDistance => 56f; // the star comes down this far in front of the player (within sword reach) ...
        public static float CallTargetHeight => -8f;  // ... and this far above the player's center (negative = up)
        public static float CallRestArmDeg => 25f;    // where a call starts when no swing came right before it (facing-local arm angle)
        public static int FlashTicks => 12;

        // ---- the called star ----
        public static float SpawnSideOffset => 140f;  // the star starts this far sideways of the target, on the side the player does NOT face
        public static float SpawnAboveScreen => 120f; // and this far above the top of the screen ...
        public static float MinFallDistance => 500f;  // ... or at least this far above the target
        public static float CalledStarSpeed => 10f;
        public static float CalledStarDamageMultiplier => 0.35f; // deliberately unimpressive on its own
        public static float CalledStarKnockbackMultiplier => 0.5f;
        public static int StarLifeTicks => 300;
        public static int StarSize => 24;
        public static float StarSpin => 0.25f;
        public static int AfterimageCount => 9;
        public static float GenerationGrowth => 0.12f; // the star is drawn this much bigger per generation

        // ---- batting: a swing that touches a falling star launches it ----
        public static int MaxBats => 3;               // successful hits per star lineage; the 3rd one is the finisher
        public static int MaxStars => 16;             // safety ceiling of stars alive per player (a call is refused at the cap, a doubling is skipped)
        public static float BatPadding => 14f;        // the star's hitbox is this much bigger for the swing's hit test (forgiving on purpose)
        public static float DoubleSpreadDeg => 5f;    // the two stars of a doubling leave this far to each side of the launch direction (fixed, not random)
        public static int BatGraceTicks => 12;        // a star that just bounced cannot be batted again for this long
        public static float BatTargetRange => 700f;   // a bat sends the star at the enemy closest to the player within this distance (pixels)
        public static float LaunchSpeed(int generation) { return generation <= 1 ? 18f : generation == 2 ? 22f : 30f; }          // the 3rd hit is the fastest
        public static float LaunchDamageMultiplier(int generation) { return generation <= 1 ? 1f : generation == 2 ? 1.3f : 1.7f; } // times the weapon's damage
        public static float LaunchKnockbackMultiplier => 0.8f;
        public static int LaunchLifeTicks => 90;

        // ---- bounce: a launched star that hits an enemy arcs back to the player on a solved parabola ----
        public static float BounceFrontDistance => 48f; // it lands this far in front of the player (within sword reach) ...
        public static float BounceTargetHeight => -10f; // ... and this far above the player's center (negative = up)
        public static float BounceSpeedTarget => 9f;    // flight time = distance / this (pixels per tick), then clamped:
        public static int BounceMinTicks => 24;
        public static int BounceMaxTicks => 60;
        public static float BounceLead => 1f;           // 1 = aim where the player will be when the star arrives (their velocity times the flight time)
        public static float ArcMaxFallSpeed => 24f;     // a bounced star may fall faster than MaxFallSpeed, so the arc stays exact
        public static float BounceSpreadDeg => 15f;     // random angle either side of straight up (the small pop of a star born at a melee hit)
        public static float Gravity => 0.32f;
        public static float MaxFallSpeed => 14f;
        public static int BounceImmunityTicks => 20;  // a star cannot hit the same enemy again for this long
        public static int BouncedLifeTicks => 240;

        // ---- the finisher: the star of the 3rd bat explodes on the enemy it hits ----
        public static int BlastSize => 200;                    // diameter of the explosion in pixels
        public static int BlastTicks => 8;                     // how long the explosion can hurt
        public static float BlastDamageMultiplier => 2.5f;     // times the weapon's damage, on top of the star's own hit
        public static float BlastKnockbackMultiplier => 2f;
        public static float BlastShake => 5f;                  // camera shake strength (pixels)

        // ---- melee: a swing that hits an enemy pops one falling star out of the impact point ----
        public static int MeleeStarsPerSwing => 1;             // so a swing through a crowd does not flood the screen
        public static float MeleeStarPopSpeed => 7f;
        public static int MeleeStarHitGraceTicks => 6;         // a freshly popped star cannot hit any enemy for this long ...
        public static int MeleeStarOriginImmunityTicks => 20;  // ... and cannot hit the enemy it popped out of for this long

        // ---- pixel look ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 22;
        public static float TrailThickness => 7f;
        public static float TrailOpacity => 0.9f;
        public static int TrailRails => 7;                // parallel pixel lines that fill the fan
        public static float TrailExpandBy => 0.6f;        // the fan has fully expanded this far into the swing (0..1)
        public static float TrailInnerFraction => 0.25f;  // fully expanded, the fan reaches down the blade to this fraction of its length
        public static Color GoldColor => new Color(255, 215, 110, 0);
        public static Color PinkColor => new Color(255, 140, 190, 0);
        public static Color CoreColor => new Color(255, 250, 225, 0);

        // How many bats a star has had: gold (a called star), ice blue, pink, hot orange (the finisher).
        public static Color GenerationColor(int generation)
        {
            return generation <= 0 ? GoldColor : generation == 1 ? new Color(120, 220, 255, 0) : generation == 2 ? PinkColor : new Color(255, 120, 70, 0);
        }
    }
}