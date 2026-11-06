using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Katana
{
    /// <summary>
    /// Every tunable number of the Katana rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload can
    /// tweak them. Arm angles are for a player facing right: 0 = forward, positive = down, negative = up; the blade angle is
    /// arm + tilt (0 = forward, 90 = straight down, 180 = straight back).
    /// </summary>
    public static class KatanaTuning
    {
        // ---- sword ----
        public static float BladeLength => 44f;       // hand to tip, in pixels at scale 1 (adjust to the real sprite)
        public static float SwordScale => 1.1f;

        // ---- the sheathed pose (copied from the Muramasa) ----
        public static float ReadyArmDeg => -20f;      // where the sword starts when it is first taken out (facing-local arm angle)
        public static float ReadyTiltDeg => -25f;
        public static float SheathArmDeg => 70f;      // the sheathed pose: the hand at the hip ...
        public static float SheathTiltDeg => 110f;    // ... and the blade pointing straight back (arm + tilt = 180)
        public static int SheathEaseTicks => 120;      // how long the sword takes to settle into the sheathed pose (taken out, or back from a throw)
        public static float ClickKickDeg => 8f;       // the hand kicks this much when the sword clicks shut
        public static int ClickFlashTicks => 12;
        public static int ClickDustCount => 8;
        public static float ClickShake => 1.5f;       // camera shake strength of the click (pixels)

        // ---- the throw: hold the button and the blade flies out along the aim line ----
        public static float MaxRangeTiles => 30f;     // the farthest the blade can fly
        public static int MaxChargeTicks => 10;       // holding this long (0.5 s) sends it the whole way
        public static float MinRangePixels => 48f;    // a tap still sends the blade this far (about 3 tiles)
        public static float ReachEasePower => 1f;     // 1 = even speed; below 1 = fast start, above 1 = slow start
        public static float StepPixels => 4f;         // the tile raycast steps this far at a time
        public static float SinkDepth => 6f;          // a blade stuck in a tile sinks this deep past the contact point
        public static float EmbedShake => 2.5f;       // how hard the stuck blade trembles right after it hits (pixels)
        public static float EmbedShakeDecay => 0.82f; // the tremble keeps this fraction of itself each tick
        public static float EmbedCameraShake => 1.5f;
        public static int VanishTicks => 10;          // after the release the guide line fades for this long

        // ---- the dash (a real teleport, but it looks and sounds like an extremely fast dash) ----
        public static int CooldownTicks => 60;        // after the teleport the sword cannot be thrown for this long
        public static int ImmuneTicks => 10;          // nothing can hurt the player for this long after the release
        public static float PushSpeed => 5f;          // added to the player's velocity, toward where he teleported
        public static float LandingStep => 4f;        // the landing search grows its circle by this many pixels at a time, up to the throw distance
        public static int LandingAngles => 24;        // spots tried on each circle (fanning out from the surface normal to both sides)
        public static int DashShadowTicks => 10;      // the player's vanilla afterimages trail behind him for this long after the dash

        // ---- the slash along the line (after the teleport) ----
        public static float SlashDamageMultiplier => 2.5f; // times the item's own damage
        public static float SlashWidth => 36f;        // how wide the cut is (pixels), for hits
        public static int SlashHitTicks => 2;         // the line hurts for this many ticks
        public static int SlashLingerTicks => 16;     // the drawn line fades out for this long after that
        public static float SlashLineWidth => 12f;    // how thick the drawn slash line is (pixels)
        public static float SlashShake => 2.5f;       // camera punch when the player arrives
        public static float SlashHitShake => 2f;      // camera punch when the slash hits something

        // ---- looks ----
        public static float PixelSize => 2f;
        public static int AfterimageTicks => 16;       // life of the flying blade's afterimages
        public static Color CoreColor => new Color(255, 255, 255, 0);
        public static Color GlowColor => new Color(205, 225, 255, 0);
        public static Color EdgeColor => new Color(130, 170, 235, 0);
    }
}