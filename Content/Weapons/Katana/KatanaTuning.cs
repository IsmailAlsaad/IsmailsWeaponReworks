using IsmailsWeaponReworks.Common;
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
        public static float GripOffset => 10f;         // where the hand holds the sword: pixels up from the end of the handle, at scale 1 (0 = the very end; try 6-10 for the middle of the handle). The sprite moves, the hit geometry and BladeLength do not: if the tip looks too short, lower BladeLength by the same amount

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
        public static float DashArrivePixels => 28f; // a dash into a tile is over once the player's center is this close to the blade (a tile in the way before that is slid along, never a stop)
        public static int DashStuckTicks => 3;        // pressed against a tile without moving for this many ticks: the dash ends
        public static float DashImpulseKeep => 0.4f;  // however the dash ends the player keeps this fraction of DashSpeed, aimed from his center at the blade
        public static bool VanillaShadow => false;    // the three vanilla shadow copies of the player (off: the afterimages below replace them)
        public static int DashShadowTicks => 8;       // vanilla shadow only: how long they trail behind him after the dash

        // ---- the slash along the dash (the line from where he started to where he is) ----
        public static float SlashDamageMultiplier => 2.5f; // times the item's own damage
        public static float SlashWidth => 36f;        // how wide the cut is (pixels), for hits
        public static int SlashHitTicks => 2;         // the line keeps hurting for this many ticks after the dash ends
        public static int TrailFollowTicks => 12;     // the line's head keeps following the player this long after the dash ends (its tail catches up meanwhile)
        public static int SlashLingerTicks => 16;     // the drawn line fades out for this long after that
        public static float SlashLineWidth => 12f;    // how thick the drawn slash line is (pixels)
        public static float SlashShake => 2.5f;       // camera punch when the player arrives
        public static float SlashHitShake => 2f;      // camera punch when the slash hits something

        // ---- the ready glow: a white pulsing outline on the sword once the cooldown is over ----
        public static float ReadyOutlineWidth => 2f;  // screen pixels
        public static int ReadyPulseTicks => 40;      // one full pulse
        public static float ReadyOutlineMin => 0.45f; // the pulse runs between these two strengths
        public static float ReadyOutlineMax => 1f;
        public static float ReadyFade => 0.3f;        // how fast the glow fades in (per tick, 0..1)
        public static float ReadyGlowScale => 1.18f;  // the soft glow copy behind the outline
        public static int ReadyFlashTicks => 14;      // the white flash of the moment the sword is ready again
        public static float ReadySparkleChance => 0.12f; // per tick, a mote drifts off the ready sword
        public static Color ReadyOutlineColor => new Color(225, 235, 255, 0);
        public static Color ReadyCoreColor => new Color(255, 255, 255, 0);

        // ---- afterimages: copies of the player left along the dash ----
        public static float AfterimageSpacing => 22f; // pixels along the path between two copies
        public static int PlayerAfterimageTicks => 14; // how long one copy lasts
        public static float AfterimageShadow => 0.5f; // how see-through the freshest copy is (0 = solid, 1 = invisible); it fades to 1

        // ---- hit-stop and hit marks on the enemies the cut hits ----
        public static int HitStopTicks => 8;          // a hit enemy freezes this long (its knockback is kept and released afterwards)
        public static float HitStopFlash => 0.7f;     // how white the frozen enemy is at the start (fades while frozen)
        public static int HitMarkTicks => 16;         // life of one hit mark
        public static int HitMarkCount => 2;          // marks laid across each enemy
        public static float HitMarkLength => 72f;     // pixels, the main mark (the others are shorter)
        public static float HitMarkWidth => 8f;       // pixels at its thickest
        public static float HitMarkAngleDeg => 25f;   // each mark is 90 degrees to the cut, give or take up to this
        public static int MaxHitMarks => 24;
        public static float HitMarkCoverage => 1.15f; // on a big enemy a mark grows to cover this much of its size across the mark (HitMarkLength stays the minimum)
        public static float HitMarkWidthGrow => 0.5f; // how much of that growth the mark's thickness follows (0 = none, 1 = all)
        public static float HitMarkMaxScale => 6f;    // the most a mark grows, times its normal size
        public static HitMarkStyle HitMarks => new HitMarkStyle(HitMarkTicks, HitMarkCount, MaxHitMarks, HitMarkLength, HitMarkWidth, HitMarkAngleDeg, HitMarkCoverage, HitMarkWidthGrow, HitMarkMaxScale, PixelSize, EdgeColor, GlowColor, CoreColor);

        // ---- the stuck blade: stays in its tile, drops and falls when the tile is mined ----
        public static int EmbedStayTicks => 600;      // a blade stuck in a tile stays this long (10 s), then fades
        public static int EmbedFadeTicks => 30;       // the fade at the end of that
        public static int EmbedVanishTicks => 8;      // a stuck blade fades out this fast once the dash that came to it is over (EmbedStayTicks is only the safety cap)
        public static float DropGravity => 0.45f;     // pixels per tick per tick
        public static float DropMaxFallSpeed => 12f;
        public static float DropKick => 1.5f;         // the blade pops loose backward at this speed
        public static float DropSpin => 0.22f;        // radians per tick, it tumbles
        public static float DropBounceKeep => 0.35f;  // the fraction of its speed a bounce keeps
        public static int DropMaxBounces => 2;        // after this many bounces it lies still
        public static int DropMaxTicks => 150;        // a blade still falling after this long just fades (a pit, lava)
        public static int DropFadeTicks => 24;        // the fade once it lies still

        // ---- looks ----
        public static float PixelSize => 2f;
        public static int AfterimageTicks => 16;       // life of the flying blade's afterimages
        public static Color CoreColor => new Color(255, 255, 255, 0);
        public static Color GlowColor => new Color(205, 225, 255, 0);
        public static Color EdgeColor => new Color(130, 170, 235, 0);
    }
}