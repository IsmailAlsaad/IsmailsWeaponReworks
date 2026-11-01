using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.ThornChakram
{
    /// <summary>
    /// Every tunable number of the Thorn Chakram rework in one place (ticks: 60 = 1 second). Properties instead of fields, so
    /// hot reload can tweak them.
    /// </summary>
    public static class ThornChakramTuning
    {
        // ---- throw (a regular boomerang: fixed range, then back to the player) ----
        public static float ThrowSpeed => 13f;
        public static float ThrowRange => 18f * 16f;      // fixed distance (pixels) before it turns around
        public static float BrakeDistance => 70f;         // it slows down over the last part of the way out
        public static float BrakeMinSpeed => 4f;

        // ---- return ----
        public static float ReturnAcceleration => 4f;     // pull toward the player per tick (force-based return, see BoomerangFlight.StepForceReturn)
        public static float ReturnDrag => 0.95f;          // velocity multiplier per tick; keep it below 1 or the path orbits the player forever
        public static float ReturnMaxSpeed => 17f;
        public static float CatchDistance => 28f;
        public static int FlightMaxTicks => 900;          // safety net: a chakram that never gets back is removed

        // ---- hits ----
        public static int HitCooldownTicks => 12;         // the same enemy can be hit again after this long (in flight)
        public static float SpinSpeed => 0.5f;

        // ---- stinger rings while it flies ----
        public static int BurstFirstDelayTicks => 6;
        public static int BurstIntervalTicks => 30;
        public static int BurstCount => 8;                // equally spaced around the circle
        public static float BurstRadius => 14f;           // pixels: where on the chakram's outer circle the stingers start
        public static float BurstSpeed => 8f;
        public static int BurstStingerLifeTicks => 20;
        public static float BurstDamageMultiplier => 0.5f; // of the chakram's damage, per stinger (each hits one enemy, then it is gone)

        // ---- latch (the first enemy it hits: it sticks to it and shreds it) ----
        public static int LatchTicks => 60;               // 1 second
        public static float LatchOffset => 8f;            // pixels from the enemy's center, toward the side it came from
        public static int ShredIntervalTicks => 4;        // the same enemy is hit again this often while latched
        public static float ShredDamageMultiplier => 0.4f; // of the chakram's damage, per shred hit
        public static float LatchSpinMaxMultiplier => 1.2f; // spin speed at the end of the latch, relative to SpinSpeed

        // ---- final explosion (when the latch ends) ----
        public static int ExplosionCount => 8;
        public static float ExplosionSpeed => 10f;
        public static int ExplosionStingerLifeTicks => 28;
        public static float ExplosionDamageMultiplier => 0.9f; // of the chakram's damage, per hit
        public static int ExplosionHitCooldownTicks => 10; // the piercing stingers can hit the same enemy again after this long

        // ---- look ----
        public static Color GlowColor => new Color(90, 220, 80, 0);
        public static float StingerSpriteRotationOffset => 1.5707964f; // the vanilla stinger sprite points up: this turns it along its flight (change it if they fly sideways)
        public static int StingerSpriteId => 55;          // vanilla stinger projectile sprite (Projectile_55)
        public static int LeafDust => 2;                  // vanilla grass dust
        public static int PoisonDust => 46;               // vanilla jungle-green poison dust
        public static int BloodDust => 5;                 // vanilla blood dust
    }
}