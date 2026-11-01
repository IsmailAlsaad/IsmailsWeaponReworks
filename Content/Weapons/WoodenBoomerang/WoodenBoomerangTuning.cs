using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.WoodenBoomerang
{
    /// <summary>
    /// Every tunable number of the Wooden Boomerang rework in one place (ticks: 60 = 1 second). Properties instead of fields, so
    /// hot reload can tweak them.
    /// </summary>
    public static class WoodenBoomerangTuning
    {
        // ---- throw (a regular boomerang: fixed range, then back to the player) ----
        public static float ThrowSpeed => 12f;
        public static float ThrowRange => 14f * 16f;      // fixed distance (pixels) before it turns around
        public static float BrakeDistance => 60f;         // it slows down over the last part of the way out
        public static float BrakeMinSpeed => 4f;

        // ---- return ----
        public static float ReturnAcceleration => 3.5f;   // pull toward the player per tick (force-based return, see BoomerangFlight.StepForceReturn)
        public static float ReturnDrag => 0.95f;          // velocity multiplier per tick; keep it below 1 or the path orbits the player forever
        public static float ReturnMaxSpeed => 16f;
        public static float CatchDistance => 28f;
        public static int FlightMaxTicks => 900;          // safety net: a boomerang that never gets back is removed
        public static float SpinSpeed => 0.45f;

        // ---- shards (it breaks in half on the first enemy it hits) ----
        public static float ShardDamageMultiplier => 0.6f; // of the boomerang's damage, per hit
        public static int ShardLifeTicks => 100;           // how long they linger (a bit under 2 s)
        public static int ShardFadeTicks => 25;            // they fade out during the last stretch
        public static int ShardHitCooldownTicks => 20;     // the same enemy can be hurt again after this long
        public static float ShardSpeed => 5.5f;            // pixels per tick right after the break
        public static float ShardSpreadAngle => 1.1f;      // radians: how far from the flight direction each half is thrown
        public static float ShardHop => 2f;                // upward kick so they tumble
        public static float ShardGravity => 0.18f;
        public static float ShardMaxFallSpeed => 7f;
        public static float ShardAirDrag => 0.985f;        // horizontal velocity multiplier per tick
        public static float ShardBounceX => 0.4f;          // share of the speed kept when it bounces off a wall
        public static float ShardBounceY => 0.3f;          // share of the speed kept when it bounces off the floor/ceiling
        public static float ShardGroundFriction => 0.9f;   // horizontal velocity multiplier per tick while it rests on something

        // ---- look ----
        public static Color GlowColor => new Color(255, 190, 110, 0);
        public static int WoodDust => 7;                   // vanilla wood chip dust
    }
}