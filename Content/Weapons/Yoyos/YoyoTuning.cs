using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// Every tunable number of the yoyo rework in one place (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). Properties instead
    /// of fields, so hot reload can tweak them. The shared groups (rope, idle, return) apply to every reworked yoyo; the trick groups are
    /// per trick (Walk the Dog is the Wooden Yoyo's). Split this per yoyo when the second yoyo shows what really differs.
    /// </summary>
    public static class YoyoTuning
    {
        // Which vanilla items this rework takes over. Add a yoyo here (and give it a trick in YoyoTricks) to rework it.
        public static bool IsReworked(int itemType)
        {
            return itemType == ItemID.WoodYoyo;
        }

        // ---- rope length: each yoyo's own vanilla range (ProjectileID.Sets.YoyosMaximumRange of the yoyo the item shoots) x this ----
        public static float RopeLengthScale => 1f;

        public static float RopeLength(Item item)
        {
            float[] ranges = ProjectileID.Sets.YoyosMaximumRange;
            float range = item.shoot > 0 && item.shoot < ranges.Length ? ranges[item.shoot] : 130f;
            return Math.Max(48f, range * RopeLengthScale);
        }

        // ---- rope (verlet, visual only: every machine simulates its own) ----
        public static int RopePoints => 16;               // points along the string (more = smoother and a bit slower)
        public static float RopeSlackFactor => 1.05f;     // the string is this much longer than the distance hand to yoyo (a little sag)
        public static float RopeExtraSlack => 4f;         // plus this many pixels
        public static float RopeMinLength => 10f;
        public static float RopeGravity => 0.32f;         // px per tick per tick
        public static float RopeDamping => 0.985f;        // velocity kept per tick
        public static int RopeIterations => 10;           // constraint passes per tick (more = stiffer string)
        public static bool RopeCollides => true;          // the string lies on the ground instead of going through it
        public static int RopeSubdivisions => 4;          // smoothing: curve pieces drawn between two rope points
        public static float RopeWidth => 1.8f;
        public static float RopeOutlineWidth => 3.2f;
        public static Color RopeColor => new Color(238, 228, 205);
        public static Color RopeAltColor => new Color(214, 200, 172); // the twist: every other piece of the string
        public static Color RopeOutlineColor => new Color(92, 74, 54);

        // ---- the yoyo body ----
        public static int HitboxSize => 16;
        public static int HitCooldownTicks => 14;         // the same enemy can be hit again after this long
        public static float ArmFollow => 0.3f;            // how fast the arm turns to its target pose (0..1 per tick)
        public static int SyncEveryTicks => 6;            // the owner re-sends the yoyo's position this often while it is out

        // ---- idle (every yoyo: held, nothing pressed) ----
        public static int IdleRestTicks => 8;             // the yoyo rests in the hand
        public static int IdleDropTicks => 22;            // it is let down gently
        public static int IdleSpinTicks => 38;            // it spins at the bottom
        public static int IdleRaiseTicks => 26;           // it is pulled back up
        public static float IdleDropPx => 44f;
        public static float IdleSwayPx => 2f;
        public static float IdleSpinRestSpeed => 0.1f;    // radians per tick at the hand
        public static float IdleSpinSpeed => 0.55f;       // radians per tick at the bottom
        public static float IdleArmRestDeg => 50f;        // arm angle below the horizontal, facing-local (90 = straight down)
        public static float IdleArmDropDeg => 82f;
        public static float ThrowArmStartDeg => 40f;      // the arm's angle when a throw starts ...
        public static int ThrowArmBlendTicks => 10;       // ... and it turns to point at the yoyo over this many ticks
        public static float CatchArmDeg => 10f;           // the arm's angle at the catch (50 = the idle rest pose)
        public static float CatchArmBlendDistance => 60f; // the arm eases from "pointing at the yoyo" to CatchArmDeg over the last this-many pixels

        // ---- throw rhythm and return (every yoyo) ----
        public static int ReleaseGraceTicks => 10;        // a release this soon after the throw is ignored, so a tap still throws
        public static int RethrowDelayTicks => 8;         // after a catch, a held button throws again after this long
        public static float ReturnStartSpeed => 7f;
        public static float ReturnAccel => 1.1f;
        public static float ReturnMaxSpeed => 20f;
        public static float CatchDistance => 14f;
        public static float ReturnSpinSpeed => 0.6f;
        public static float SpinPerPixel => 0.12f;        // rolling: radians of spin per pixel of horizontal travel

        // ---- Walk the Dog (Wooden Yoyo, left click) ----
        public static int WalkHitsPerThrow => 3;          // enemy hits before it retracts
        public static float WalkThrowDownSpeed => 5f;     // the throw: down ...
        public static float WalkThrowForwardSpeed => 3f;  // ... and a bit toward the cursor's side
        public static float WalkGravity => 0.45f;
        public static float WalkMaxFall => 14f;
        public static float WalkSpeed => 4.2f;            // rolling speed along the ground
        public static float WalkAccel => 0.35f;           // on the ground
        public static float WalkAirAccel => 0.06f;        // in the air
        public static float WalkLandBounce => 0.3f;       // fraction of the fall speed that bounces back up
        public static float WalkBounceMinImpact => 3f;    // softer landings do not bounce
        public static float WalkHopSpeed => 5.5f;         // a small hop when a block stops the roll (steps and slopes)
        public static int WalkHopCooldownTicks => 10;
        public static float WalkHitRecoil => 3.5f;        // pushed back from an enemy ...
        public static float WalkHitHop => 3.5f;           // ... and up
        public static float HitShake => 1.5f;
        public static bool UseSlopes => true;             // false: only plain block collision (try it if the roll behaves oddly on slopes)

        // ---- look ----
        public static int WoodDust => 7;                  // vanilla wood chip dust
        // His own art, when it exists: Content/Weapons/Yoyos/WoodenYoyoProjectile.png (about 16x16). Until then the vanilla sprite is used.
        public static string WoodenTexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/WoodenYoyoProjectile";
    }
}