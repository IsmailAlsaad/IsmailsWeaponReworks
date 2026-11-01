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

        // ---- steering toward the cursor (owner only; Outbound and Hold) ----
        public static float SteerTurn => 0.02f;            // most radians per tick the flight direction bends toward the cursor (weak on purpose)
        public static float HoldSteerSpeed => 3f;          // drift speed toward the cursor while holding
        public static float HoldSteerSmoothing => 0.08f;   // how fast the drift velocity follows (0 to 1 per tick)
        public static float HoldSteerDeadzone => 24f;      // within this distance of the cursor it stops drifting
        public static int SteerSyncTicks => 5;             // the owner sends position and velocity this often while steering

        // ---- spin: starts slow, speeds up (ease-in) to the cap ----
        public static float SpinStart => 0.12f;            // radians per tick
        public static float SpinCap => 0.45f;
        public static int SpinRampTicks => 180;

        // ---- shredding: the faster it spins, the faster it hits the same enemy again ----
        public static int HitCooldownSlow => 18;
        public static int HitCooldownFast => 8;
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
        public static float EtherealLaunchSpeed => 11f;    // the impulse it is shot out with (no homing: the return pull bends it into an arc)
        public static float EtherealReturnAcceleration => 1f; // pull toward the player per tick (BoomerangFlight.StepForceReturn)
        public static float EtherealReturnDrag => 0.95f;   // keep below 1 or it orbits the player forever
        public static float EtherealReturnMaxSpeed => 18f;
        public static int EtherealHitCooldownTicks => 10;
        public static int EtherealLifeTicks => 360;        // safety net: it fizzles out if it has not reached the player by then
        public static float GoldenAngleDeg => 137.5f;      // consecutive ethereal forms fan out around the main boomerang

        // ---- look ----
        public static Color MainColor => new Color(185, 115, 255, 0);
        public static Color EtherealColor => new Color(120, 205, 255, 0);
        public static float EtherealOpacity => 0.6f;
    }
}