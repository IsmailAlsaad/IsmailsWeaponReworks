using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// Every tunable number of the Bee Keeper rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Swing arm angles are for a player facing right and RELATIVE TO THE AIM (the direction to the cursor when the
    /// swing starts): 0 = at the cursor, negative = above it, positive = below it.
    /// </summary>
    public static class BeeKeeperTuning
    {
        // ---- sword ----
        public static float BladeLength => 50f;       // hand to tip, in pixels at scale 1
        public static float SwordScale => 1.1f;

        // ---- swing (the Starfury's: alternates down and up, centered on the cursor; fixed ticks, NOT scaled by melee speed) ----
        public static int AnticipateTicks => 8;
        public static float AnticipateDeg => 20f;     // how far past the start of the arc the arm pulls back
        public static float AnticipateTiltDeg => 10f; // and the wrist (the blade tilt) the same way
        public static int SwingTicks => 7;            // the strike (the only part that damages)
        public static float SwingEasePower => 2.5f;   // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 6f;       // the strike carries this far past the end of the arc ...
        public static int RecoverTicks => 5;          // ... and settles back to the end of the arc over this long
        public static int SweepHitSteps => 4;         // the blade's hit test is checked at this many poses between two ticks
        public static float RestArmDeg => 10f;        // where a fresh combo starts (just before the high end of the arc)
        public static float SwingHighDeg => -70f;     // top of the arc
        public static float SwingLowDeg => 70f;       // bottom of the arc
        public static float TiltHighDeg => -12f;      // blade tilt relative to the arm at the top (wrist) ...
        public static float TiltLowDeg => 12f;        // ... and at the bottom
        public static int ComboTicks => 30;           // a swing started within this long after the last one continues the alternation
        public static float HitShake => 3f;           // camera punch on a hit (pixels)

        // ---- swing trail (a SweepTrail fan, drawn by the shared pixel line renderer) ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 22;
        public static float TrailOpacity => 0.8f;
        public static float TailLifeScale => 0.45f;   // the first point of a swing's trail lives this fraction as long as the last one
        public static int TrailRails => 7;            // parallel pixel lines that fill the fan
        public static float TrailExpandBy => 0.9f;    // the fan has fully expanded this far into the swing (0..1)
        public static float TrailInner => 0.35f;      // fully expanded, the fan reaches down the blade to this fraction of its length
        public static float TrailOuter => 1f;         // the outer edge of the fan, as a fraction of the blade length (1 = the tip)
        public static int TrailSamplesPerTick => 8;   // trail points laid per tick (the strike is fast, so many keep the fan smooth)
        public static Color CoreColor => new Color(255, 244, 180, 0);
        public static Color HoneyColor => new Color(255, 176, 40, 0);
        public static Color DeepColor => new Color(205, 95, 20, 0);

        // ---- the swarm ----
        public static int MaxBig => 5;
        public static int MaxSmall => 10;
        public static int SpawnIntervalTicks => 50;   // one new bee this often while the swarm is not full
        public static int LingerTicks => 600;         // the swarm stays this long after the sword is put away, then leaves
        public static int LeaveTicks => 24;           // a leaving bee shrinks and rises for this long, then is gone
        public static int BigSize => 22;              // hitbox of a big bee (square, pixels)
        public static int SmallSize => 14;
        public static float BigScale => 1f;           // drawn size
        public static float SmallScale => 0.85f;
        public static int PopTicks => 10;             // a new bee grows to full size over this long
        public static int FlapTicks => 3;             // ticks per wing frame
        public static int ArtFrames => 1;             // frames of YOUR bee art (the vanilla stand-in reads its own frame count)
        public static bool VanillaBeeFacesRight => false; // the stand-in vanilla bee sprite: flip this if it flies backwards

        // ---- hover: the bees circle above the player's head ----
        public static float HoverHeight => 38f;       // the middle of the swarm, above the player's top
        public static float BigRadiusX => 46f;        // how far sideways a big bee wanders
        public static float SmallRadiusX => 62f;
        public static float HoverRadiusY => 22f;
        public static float FlutterSpeed => 0.045f;   // radians per tick of the wander (big bees use 70%)
        public static float Spring => 0.02f;          // pull toward the wander point
        public static float Damping => 0.92f;         // velocity kept per tick
        public static float MaxSpeed => 12f;
        public static float BuzzJitter => 0.25f;      // random shake added to the velocity
        public static float SnapDistance => 900f;     // a bee further away than this jumps back to the swarm

        // ---- bees can be hurt (the owner's client decides) ----
        public static float BigHp => 30f;
        public static float SmallHp => 10f;
        public static float EnemyTouchDamage => 10f;  // an enemy body or hostile projectile that touches a bee costs it this much ...
        public static int HurtCooldownTicks => 30;    // ... and the bee cannot be hurt again for this long
        public static bool HostileProjectilesHurtBees => true;
        public static float HurtKnock => 4f;          // the bee is pushed away from what hit it
        public static int HurtFlashTicks => 10;

        // ---- poison cloud (a dying bee) ----
        public static float CloudRadius => 70f;       // a big bee's cloud (pixels); a small bee's is CloudSmallScale of it
        public static float CloudSmallScale => 0.65f;
        public static int CloudTicks => 150;
        public static int CloudPulseTicks => 20;      // enemies and players inside get the buff refreshed this often
        public static int PoisonTicks => 240;         // the Poisoned buff duration each pulse
        public static int CloudPuffsPerTick => 2;
        public static int CloudBurstPuffs => 14;
        public static float CloudMerge => 2.2f;
        public static float CloudOutline => 0.18f;
        public static float CloudGlow => 0.2f;
        public static float CloudRise => 0.02f;
        public static float CloudDrag => 0.96f;
        public static float CloudWobble => 0.04f;
        public static float CloudHeat => 0.9f;
        public static Color CloudFill(float heat)
        {
            Color dark = new Color(64, 96, 30);
            Color mid = new Color(120, 175, 45);
            Color bright = new Color(205, 235, 110);
            return heat < 0.5f ? Color.Lerp(dark, mid, heat * 2f) : Color.Lerp(mid, bright, (heat - 0.5f) * 2f);
        }
        public static Color CloudOutlineColor => new Color(40, 66, 20);
        public static Color CloudGlowColor => new Color(140, 210, 60, 0);
    }
}