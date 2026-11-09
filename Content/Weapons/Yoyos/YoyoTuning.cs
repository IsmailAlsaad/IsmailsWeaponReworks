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
            return itemType == ItemID.WoodYoyo || itemType == ItemID.Code1;
        }

        // ---- rope length: each yoyo's own vanilla range (ProjectileID.Sets.YoyosMaximumRange of the yoyo the item shoots) x this ----
        public static float RopeLengthScale => 1f;

        public static float RopeLength(Item item)
        {
            float[] ranges = ProjectileID.Sets.YoyosMaximumRange;
            float range = item.shoot > ProjectileID.None && item.shoot < ranges.Length ? ranges[item.shoot] : 130f;
            return Math.Max(48f, range * RopeLengthScale);
        }

        // ---- rope (verlet, visual only: every machine simulates its own) ----
        public static int RopePoints => 16;               // points along the string (more = smoother and a bit slower)
        public static float WalkRopeSlackFactor => 1.015f; // while Out or Returning: the string is this much longer than the distance hand to yoyo (1.01 to 1.02 = a very slight bend)
        public static float WalkRopeExtraSlack => 0f;     // plus this many pixels (0 = never slack)
        public static float WalkRopeGravity => 0.08f;     // px per tick per tick: a small sag only
        public static float RopeMinLength => 10f;
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
        public static float IdleDropPx => 24f;
        public static float IdleSwayPx => 2f;
        public static float IdleSpinRestSpeed => 0.1f;    // radians per tick at the hand
        public static float IdleSpinSpeed => 0.55f;       // radians per tick at the bottom
        public static float IdleArmRestDeg => 20f;        // arm angle below the horizontal, facing-local (90 = straight down)
        public static float IdleArmDropDeg => 60f;
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
        public static float WalkSwitchDeadzonePx => 10f;  // while it is out, the side only switches when the cursor is this far past the player's center (no flicker over the player)
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

        public static bool UseSlopes => true;             // false: only plain block collision (try it if the roll behaves oddly on slopes)

        // ---- Forward Pass (Code 1, left click) ----
        public static int ForwardPassTicks => 32;         // one whole pass (out along the ellipse and back to the hand)
        public static float ForwardPassWidthRatio => 0.42f; // the ellipse's half width as a fraction of its half length (0.42 = a slim ellipse)
        public static float ForwardPassMinReach => 64f;   // the far end is at least this far from the player, even with the cursor on top of them
        public static float ForwardPassReachMargin => 30f; // the far end stops this far short of the string's full length
        public static float ForwardPassEase => 0.35f;     // 0 = constant speed along the ellipse, 1 = slow at both ends
        public static float ForwardPassHitShake => 0.5f;  // very subtle camera punch per enemy hit (0 = none)

        // ---- afterimages (the shared helper YoyoAfterimages in YoyoJuice.cs; a trick opts in with YoyoTrick.Afterimages) ----
        public static int AfterimageTicks => 10;          // how long one afterimage lives
        public static float AfterimageMinScale => 0.7f;   // it shrinks to this fraction of the yoyo's size while it fades
        public static float AfterimageAlpha => 0.6f;      // brightness of a fresh afterimage
        public static float AfterimageMinSpeed => 3f;     // none are laid while the yoyo is slower than this (px per tick)

        // ---- look ----
        public static int WoodDust => 7;                  // vanilla wood chip dust
        // His own art, when it exists: Content/Weapons/Yoyos/WoodenYoyoProjectile.png (about 16x16). Until then the vanilla sprite is used.
        public static string WoodenTexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/WoodenYoyoProjectile";
        // Code 1: Content/Weapons/Yoyos/Code1Projectile.png (same size as the vanilla sprite).
        public static string Code1TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/Code1Projectile";

        // The art path of a yoyo's own sprite (null = it has none planned and always uses the vanilla one).
        public static string CustomTexturePath(int itemType)
        {
            if (itemType == ItemID.WoodYoyo)
                return WoodenTexturePath;
            if (itemType == ItemID.Code1)
                return Code1TexturePath;
            return null;
        }

        // The vanilla projectile whose sprite a yoyo uses until its own art exists (the yoyo projectile the item shoots).
        public static int VisualProjectile(int itemType)
        {
            return ContentSamples.ItemsByType[itemType].shoot;
        }
    }
}