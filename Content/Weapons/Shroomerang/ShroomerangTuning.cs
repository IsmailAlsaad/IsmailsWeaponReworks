using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Shroomerang
{
    /// <summary>
    /// Every tunable number of the Shroomerang rework in one place (ticks: 60 = 1 second). Properties instead of fields, so
    /// hot reload can tweak them.
    /// </summary>
    public static class ShroomerangTuning
    {
        // ---- throw (a regular boomerang: fixed range, then back to the player) ----
        public static float ThrowSpeed => 14f;
        public static float ThrowRange => 20f * 16f;      // fixed distance (pixels) before it turns around
        public static float BrakeDistance => 70f;         // it slows down over the last part of the way out
        public static float BrakeMinSpeed => 4f;

        // ---- return ----
        public static float ReturnAcceleration => 4f;     // pull toward the player per tick (force-based return, see BoomerangFlight.StepForceReturn)
        public static float ReturnDrag => 0.95f;          // velocity multiplier per tick; keep it below 1 or the path orbits the player forever
        public static float ReturnMaxSpeed => 18f;
        public static float CatchDistance => 28f;
        public static int FlightMaxTicks => 900;          // safety net: a boomerang that never gets back is removed

        // ---- hits ----
        public static int HitCooldownTicks => 12;         // infinite pierce: the same enemy can be hit again after this long
        public static float SpinSpeed => 0.5f;
        public static int AfterimageTicks => 10;          // how long an afterimage lasts (it keeps fading after the boomerang is gone)

        // ---- mushrooms the boomerang drops ----
        public static int FirstSpawnDelayTicks => 4;
        public static int SpawnIntervalTicks => 18;
        public static int MaxMushrooms => 8;              // the 9th evicts the oldest, which explodes at once
        public static int MushroomLifeTicks => 150;       // 2.5 seconds, then it explodes
        public static int PopTicks => 12;                 // grow-in animation
        public static int WarnTicks => 40;                // blinks faster during the last stretch
        public static float InheritVelocity => 0.2f;      // share of the boomerang's velocity a new mushroom starts with
        public static float ScatterSpeed => 1.5f;         // random push so they do not stack in a line
        public static float Drag => 0.94f;                // velocity multiplier per tick (gradual stop)
        public static float MushroomSpinSpeed => 0.28f;   // radians per tick

        // ---- touch trigger (an enemy walking into a mushroom sets it off) ----
        public static int ArmTicks => 20;                 // a fresh mushroom ignores enemies for this long (the boomerang drops it inside them)
        public static float TriggerPadding => 6f;         // pixels around the mushroom's hitbox
        public static int TriggerCheckInterval => 2;

        // ---- spore burst (the explosion) ----
        public static float BurstRadius => 80f;
        public static float BurstDamageMultiplier => 0.8f; // of the boomerang's damage at the moment the mushroom was dropped
        public static int BurstLifeTicks => 12;

        // ---- look ----
        public static Color GlowColor => new Color(80, 160, 255, 0);
        public static Color CloudColor => new Color(110, 170, 255); // tint of the smoke in the spore cloud
    }
}