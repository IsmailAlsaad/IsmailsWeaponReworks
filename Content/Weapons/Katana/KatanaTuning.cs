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

        // ---- the dash: the player flies to the blade at DashSpeed, invincible while he travels ----
        public static int CooldownTicks => 60;        // the sword cannot be thrown again for this long after the dash starts
        public static float DashSpeed => 40f;         // pixels per tick (30 tiles take 12 ticks)
        public static float MinDashPixels => 8f;      // a blade closer than this does nothing (no dash, no cooldown)
        public static float DashGraze => 0.3f;        // a wall hit at a shallower angle than this (cosine) slides the dash along it; a head-on hit stops it
        public static int ImmuneTailTicks => 10;      // nothing can hurt the player for this long after the dash ends, however it ends
        public static int DashShadowTicks => 8;       // the player's vanilla afterimages trail behind him for this long after the dash

        // ---- the slash along the dash (the line from where he started to where he is) ----
        public static float SlashDamageMultiplier => 2.5f; // times the item's own damage
        public static float SlashWidth => 36f;        // how wide the cut is (pixels), for hits
        public static int SlashHitTicks => 2;         // the line keeps hurting for this many ticks after the dash ends
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