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
        public static float SwordScale => 1.1f;

        // ---- timeline ----
        public static int WindupTicks => 8;
        public static int ThrustTicks => 6;
        public static int OpenTicks => 8;
        public static int GapeTicks => 3;
        public static int BiteTicks => 4;
        public static int ClampTicks => 6;            // patch 1: held shut. Patch 2 replaces this with the spin.
        public static int RecoverTicks => 12;
        public static int DecideWaitTicks => 20;      // another machine waits this long for the owner's catch decision, then assumes a miss

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

        // Art (BloodButchererHandle / BloodButchererJawUpper / BloodButchererJawLower .png): the sword drawn horizontal, pointing right, all three on the
        // same canvas, with this pixel as the jaw pivot (and the handle's join). Until all three exist the vanilla sprite is used.
        public static Vector2 ArtPivot => new Vector2(12f, 16f);
    }
}