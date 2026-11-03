using Microsoft.Xna.Framework;
using System;

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
        // A swing has three parts: a slow, heavy ANTICIPATION (the arm and wrist pull back past the start of the arc), a fast STRIKE that
        // is quickest right after the anticipation and decelerates until it ends (a little past the end of the arc), and a RECOVER that
        // settles back to the end of the arc, so the next swing chains from an exact pose.
        public static int AnticipateTicks => 8;
        public static float AnticipateDeg => 20f;     // how far past the start of the arc the arm pulls back
        public static float AnticipateTiltDeg => 10f; // and the wrist (the blade tilt) the same way
        public static int SwingTicks => 7;            // the strike (the only part that damages and bats)
        public static float SwingEasePower => 2.5f;   // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 6f;       // the strike carries this far past the end of the arc ...
        public static int RecoverTicks => 5;          // ... and settles back to the end of the arc over this long
        public static int SweepHitSteps => 4;         // the blade's hit test is checked at this many poses between two ticks (the strike is fast)
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
        public static float CallFrontDistance => 30f; // the star comes down this far in front of the player (within sword reach) ...
        public static float CallTargetHeight => -8f;  // ... and this far above the player's center (negative = up)
        public static float CallRestArmDeg => 25f;    // where a call starts when no swing came right before it (facing-local arm angle)
        public static int FlashTicks => 12;

        // ---- the called star: it is born at the blade tip, hops on a small parabola to the point in front of the player, then keeps falling ----
        public static float CallHopHeight => 50f;     // the hop rises this far above the blade tip (pixels); the flight time follows from it and from where the star must land
        public static int CallBlockGraceTicks => 8;   // the star ignores blocks this long after it is born (it starts above the player's head)
        public static float CalledStarDamageMultiplier => 0.35f; // deliberately unimpressive on its own
        public static float CalledStarKnockbackMultiplier => 0.5f;
        public static int StarLifeTicks => 300;
        public static int StarSize => 24;
        public static float StarSpin => 0.25f;
        public static int AfterimageCount => 9;
        public static float GenerationGrowth => 0.12f; // the star is drawn this much bigger per generation

        // ---- batting: a swing that touches a falling star launches it ----
        public static int MaxBats => 3;               // successful hits per star lineage; the 3rd one is the finisher
        public static int MaxStars => 16;             // safety ceiling of stars alive per player (a call is refused at the cap, a melee star is skipped)
        public static float BatPadding => 14f;        // the star's hitbox is this much bigger for the swing's hit test (forgiving on purpose)
        public static int BatGraceTicks => 12;        // a star that just bounced cannot be batted again for this long
        public static float BatTargetRange => 700f;   // a bat sends the star at the enemy closest to the player within this distance (pixels)
        public static bool BatPrefersBosses => true;  // a boss within that range is the target even when a smaller enemy is closer
        public static float LaunchSpeed(int generation) { return generation <= 1 ? 18f : generation == 2 ? 22f : 30f; }          // the 3rd hit is the fastest
        public static float LaunchDamageMultiplier(int generation) { return generation <= 1 ? 1f : generation == 2 ? 1.3f : 1.7f; } // times the weapon's damage
        public static float LaunchKnockbackMultiplier => 0.8f;
        public static int LaunchLifeTicks => 90;

        // ---- bounce: a launched star that hits an enemy arcs back to the player on a solved parabola ----
        public static float BounceFrontDistance => 30f; // it lands this far in front of the player (within sword reach) ...
        public static float BounceTargetHeight => -10f; // ... and this far above the player's center (negative = up)
        public static float BounceSpeedTarget => 9f;    // flight time = distance / this (pixels per tick), then clamped:
        public static int BounceMinTicks => 24;
        public static int BounceMaxTicks => 60;
        public static float BounceLead => 1f;           // 1 = aim where the player will be when the star arrives (their velocity times the flight time)
        public static float ArcMaxFallSpeed => 24f;     // a bounced star may fall faster than MaxFallSpeed, so the arc stays exact
        public static float Gravity => 0.32f;
        public static float MaxFallSpeed => 14f;
        public static int BounceImmunityTicks => 20;  // a star cannot hit the same enemy again for this long
        public static int BouncedLifeTicks => 240;

        // ---- the finisher: the star of the 3rd bat explodes on the enemy it hits ----
        public static int BlastSize => 200;                    // diameter of the explosion in pixels
        public static int BlastTicks => 16;                     // how long the explosion can hurt
        public static float BlastDamageMultiplier => 2.5f;     // times the weapon's damage, on top of the star's own hit
        public static float BlastKnockbackMultiplier => 2f;
        public static float BlastShake => 5f;                  // camera shake strength (pixels)

        // ---- charge: the first enemy the blade hits in a swing charges the sword; a right click tosses a star of the charged tier and spends it ----
        public static int HitsPerLevel => 4;          // swings that hit an enemy per charge level
        public static int MaxLevel => 2;              // the top level = the generation of the star it tosses; one bat makes it the exploding generation (keep it below MaxBats)
        public static int MaxHits => HitsPerLevel * MaxLevel;
        public static int LevelOf(int hits) { return Math.Min(Math.Max(0, hits) / Math.Max(1, HitsPerLevel), MaxLevel); }

        // ---- pixel look ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 22;
        // The trail gets stronger with the charge like the Ice Blade's tiers: three anchors (no charge, half, full) and a blend between them.
        public static float TrailOpacityLow => 0.4f;
        public static float TrailOpacityMid => 0.65f;
        public static float TrailOpacityHigh => 0.95f;
        public static float TrailOpacityAt(float charge) { return charge < 0.5f ? MathHelper.Lerp(TrailOpacityLow, TrailOpacityMid, charge * 2f) : MathHelper.Lerp(TrailOpacityMid, TrailOpacityHigh, (charge - 0.5f) * 2f); }
        public static float TailLifeScale => 0.45f;       // the first point of a swing's trail lives this fraction as long as the last one, so the trailing end fades out first
        public static int TrailRails => 7;                // parallel pixel lines that fill the fan
        public static float TrailExpandBy => 0.9f;        // the fan has fully expanded this far into the swing (0..1)
        // Fully expanded, the fan reaches down the blade to this fraction of its length (lower = a bigger fan), by charge like the opacity.
        public static float TrailInnerLow => 0.5f;
        public static float TrailInnerMid => 0.33f;
        public static float TrailInnerHigh => 0.12f;
        public static float TrailInnerAt(float charge) { return charge < 0.5f ? MathHelper.Lerp(TrailInnerLow, TrailInnerMid, charge * 2f) : MathHelper.Lerp(TrailInnerMid, TrailInnerHigh, (charge - 0.5f) * 2f); }
        public static int TrailSamplesPerTick => 8;       // trail points laid per tick (the strike is fast, so many keep the fan smooth)
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