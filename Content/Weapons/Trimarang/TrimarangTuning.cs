using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Trimarang
{
    /// <summary>
    /// Every tunable number of the Trimarang rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them.
    /// </summary>
    public static class TrimarangTuning
    {
        // ---- throw: it slows down so that it stops exactly at HoldRange, then spins there until the button is released ----
        public static float ThrowSpeed => 16f;
        public static float HoldRange => 20f * 16f;        // 20 tiles
        public static float MinThrowSpeed => 1.2f;         // so the slowdown never stalls short of the stop point
        public static float StopSnapDistance => 3f;
        public static float LeashDistance => 45f * 16f;    // it comes back on its own if the player gets this far away
        public static int FlightMaxTicks => 7200;          // safety net: 2 minutes

        // ---- spin: starts slow, speeds up (ease-in) to the cap ----
        public static float SpinStart => 0.12f;            // radians per tick
        public static float SpinCap => 0.95f;
        public static int SpinRampTicks => 180;

        // ---- shredding: the faster it spins, the faster it hits the same enemy again ----
        public static int HitCooldownSlow => 14;
        public static int HitCooldownFast => 5;
        public static float HitRadius => 30f;
        public static float HoldKnockbackMultiplier => 0.3f; // enemies stay in the blades

        // ---- return ----
        public static float ReturnStartSpeed => 6f;
        public static float ReturnAcceleration => 1f;
        public static float ReturnMaxSpeed => 20f;
        public static float CatchDistance => 28f;

        // ---- ethereal forms ----
        public static int HitsPerEthereal => 10;            // every 10th hit of the main boomerang
        public static float EtherealDamageMultiplier => 0.7f;
        public static int EtherealMaxAlive => 8;           // the oldest fizzles when a new one would be the 9th
        public static int EtherealHitsToExplode => 8;
        public static float EtherealLaunchSpeed => 11f;
        public static int EtherealLaunchTicks => 10;       // it flies straight out this long before it starts to home
        public static float EtherealHomingSpeed => 15f;
        public static float EtherealTurn => 0.18f;         // how sharply it steers (0 to 1 per tick)
        public static float EtherealSearchRange => 44f * 16f;
        public static int EtherealRetargetTicks => 15;
        public static int EtherealHitCooldownTicks => 10;
        public static int EtherealLifeTicks => 360;        // fizzles out if it never finds anything
        public static float GoldenAngleDeg => 137.5f;      // consecutive ethereal forms fan out around the main boomerang

        // ---- look ----
        public static Color MainColor => new Color(185, 115, 255, 0);
        public static Color EtherealColor => new Color(120, 205, 255, 0);
        public static float EtherealOpacity => 0.6f;
    }
}