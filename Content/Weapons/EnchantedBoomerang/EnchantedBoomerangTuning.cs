using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedBoomerang
{
    /// <summary>
    /// Every tunable number of the Enchanted Boomerang rework in one place (ticks: 60 = 1 second). Properties instead of fields, so
    /// hot reload can tweak them. Left click throws the normal boomerang, right click the ethereal one; both use the same numbers.
    /// </summary>
    public static class EnchantedBoomerangTuning
    {
        // ---- throw ----
        public static float ThrowSpeed => 14f;
        public static float ThrowRange => 18f * 16f;      // fixed distance (pixels) before it turns around
        public static float BrakeDistance => 70f;         // it slows down over the last part of the way out
        public static float BrakeMinSpeed => 4f;

        // ---- return ----
        public static float ReturnAcceleration => 1f;     // pull toward the player per tick (force-based return, see BoomerangFlight.StepForceReturn)
        public static float ReturnDrag => 0.95f;          // velocity multiplier per tick; keep it below 1 or the path orbits the player forever
        public static float ReturnMaxSpeed => 18f;
        public static float CatchDistance => 28f;
        public static int FlightMaxTicks => 900;          // safety net: a boomerang that never gets back is removed

        // ---- hits ----
        public static int HitCooldownTicks => 12;         // infinite pierce: the same enemy can be hit again after this long
        public static float SpinSpeed => 0.5f;

        // ---- the two kinds hitting each other ----
        public static float CollideRadius => 20f;         // each boomerang counts as a circle this big (forgiving on purpose)
        public static float BounceSpeed => 13f;           // sideways impulse (90 degrees off the travel direction); the return pull bends it into an arc

        // ---- stardust burst at the contact point ----
        public static float BurstRadius => 72f;
        public static float BurstDamageMultiplier => 1.5f;
        public static int BurstLifeTicks => 12;

        // ---- look ----
        public static Color NormalColor => new Color(255, 215, 120, 0);
        public static Color EtherealColor => new Color(120, 220, 255, 0);
        public static float EtherealOpacity => 0.6f;
        public static float PixelSize => 2f;
    }
}