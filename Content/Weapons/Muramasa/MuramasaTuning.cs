using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// Every tunable number of the Muramasa rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Arm angles are for a player facing right: 0 = forward, positive = down, negative = up; the blade angle is
    /// arm + tilt (0 = forward, 90 = straight down, 180 = straight back).
    /// </summary>
    public static class MuramasaTuning
    {
        // ---- sword ----
        public static float BladeLength => 60f;       // hand to tip, in pixels at scale 1 (adjust to the real sprite)
        public static float SwordScale => 1.1f;

        // ---- sheathing: after every attack (and when the sword is taken out) the player slowly lowers the sword to the hip ----
        public static int SheathTicks => 150;          // fixed ticks, NOT scaled by melee speed
        public static float ReadyArmDeg => -20f;      // where the sword starts when it is first taken out (facing-local arm angle)
        public static float ReadyTiltDeg => -25f;
        public static float SheathArmDeg => 70f;      // the sheathed pose: the hand at the hip ...
        public static float SheathTiltDeg => 110f;    // ... and the blade pointing straight back (arm + tilt = 180)
        public static float ScrapeAt => 0.35f;        // sheath progress (0..1) at which the quiet scrape sound plays
        public static float ClickKickDeg => 8f;       // the hand kicks this much when the sword clicks shut
        public static int ClickFlashTicks => 14;      // how long the click flash and kick last
        public static int ClickDustCount => 14;
        public static float ClickShake => 2f;         // camera shake strength of the click (pixels)

        // ---- the sheathed outline: a pulsing blue pixel outline around the sprite ----
        public static float OutlineWidth => 2f;       // screen pixels
        public static int OutlinePulseTicks => 48;    // one full pulse
        public static float OutlineMin => 0.45f;      // the pulse runs between these two strengths
        public static float OutlineMax => 1f;
        public static float OutlineFade => 0.3f;      // how fast the outline fades in and out (per tick, 0..1)
        public static float GlowScale => 1.18f;       // the soft glow copy behind the outline
        public static Color OutlineColor => new Color(70, 160, 255, 0);
        public static Color OutlineCoreColor => new Color(160, 220, 255, 0);

        // ---- the plain swing (a click before the sword is fully sheathed): anticipation, strike, recover; fixed ticks ----
        public static int AnticipateTicks => 3;
        public static float AnticipateDeg => 14f;     // how far past the start of the arc the arm pulls back
        public static float AnticipateTiltDeg => 8f;
        public static int SwingTicks => 5;            // the strike (the only part that damages)
        public static float SwingEasePower => 2.2f;   // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 5f;
        public static int RecoverTicks => 6;
        public static int SweepHitSteps => 4;         // the blade's hit test is checked at this many poses between two ticks
        // Relative to the AIM (the direction to the cursor when the swing starts): 0 = at the cursor, negative = above, positive = below.
        public static float SwingHighDeg => -65f;     // top of the arc
        public static float SwingLowDeg => 65f;       // bottom of the arc
        public static float TiltHighDeg => -12f;
        public static float TiltLowDeg => 12f;
        public static float HitShake => 1f;
        public static int FlashTicks => 10;

        // ---- pixel look of the swing trail ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 10;
        public static float TailLifeScale => 0.45f;
        public static int TrailRails => 8;
        public static float TrailExpandBy => 0.5f;
        public static float TrailInner => 0.3f;       // fully expanded, the fan reaches down the blade to this fraction of its length
        public static float TrailOpacity => 1f;
        public static int TrailSamplesPerTick => 8;
        public static Color CoreColor => new Color(215, 240, 255, 0);
        public static Color BlueColor => new Color(80, 170, 255, 0);
        public static Color DeepColor => new Color(40, 80, 220, 0);

        // ---- trail alignment: if the trail does not sit on the sprite, tune these (they are live under hot reload) ----
        public static float TrailLagTicks => 0f;      // how many ticks the trail runs BEHIND the sprite (0 = exactly on it; the old fixed value was 0.8)
        public static float TrailLengthScale => 1f;   // the trail's outer edge sits at BladeLength * this (the old fixed value was 0.9)
        public static bool ShowBladeDebug => false;   // true: magenta square = the hit-test tip, green square = where the trail's outer edge is laid

        // ---- the draw window (patch 2): hold the button on a fully sheathed sword and draw a path with the cursor ----
        public static int DrawWindowTicks => 40;       // how long the path can be drawn
        public static float DrawMaxRadiusTiles => 25f; // the path is clamped to this radius around the player
        public static float DrawSampleSpacing => 6f;   // a new path point is only laid when the cursor moved this far (pixels)
        public static float DrawMinPathPixels => 32f;  // a path shorter than this (about 2 tiles) is a plain swing at the cursor instead
        public static int DrawMaxStrokes => 8;         // the path is simplified into at most this many straight strokes
        public static float DrawSimplifyTolerance => 12f; // start tolerance of the simplification (pixels); it grows until the stroke limit holds
        public static int DrawCooldownTicks => 20;     // pause after a draw before the next one can begin
        public static int DrawPreviewTicks => 24;      // patch 2 only: how long the simplified strokes stay on screen after the release
        public static float DrawLineWidth => 6f;       // thickness of the drawn path (pixels)
        public static float DrawRingMax => 22f;        // the time ring at the cursor: radius with the whole window left ...
        public static float DrawRingMin => 5f;         // ... and with none left
        public static float DrawWarnFraction => 0.25f; // the ring blinks when this fraction of the window is left
    }
}