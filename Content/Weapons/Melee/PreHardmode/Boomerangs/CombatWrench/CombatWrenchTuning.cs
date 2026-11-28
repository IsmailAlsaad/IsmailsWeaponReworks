using IsmailsWeaponReworks.Common.Rope;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Boomerangs.CombatWrench
{
    /// <summary>
    /// Every tunable number of the Combat Wrench rework in one place (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile).
    /// Properties instead of fields, so hot reload can tweak them. Grouped: DAMAGE, THROW, RETURN, ARM, ROPE, THROWN TRAIL, FX.
    /// </summary>
    public static class CombatWrenchTuning
    {
        // ---- DAMAGE (every damage variable lives here; the wrench hurts only at the heavy hit and at the release, from W4 on) ----
        public static float HitDamageMultiplier => 2.5f;       // of the item's damage: the heavy swing's hit on a reeled-in enemy


        // ---- THROW (the Forward Pass ellipse toward the cursor) ----
        public static float RopeLengthPx => 350f;              // the string's length; the throw goes this far minus ReachMarginPx
        public static float ReachMarginPx => 30f;              // the far end of the ellipse is RopeLengthPx minus this
        public static int PassTicks => 36;                     // one whole pass (out along the ellipse and back to the hand)
        public static float PassWidthRatio => 0.42f;           // the ellipse's half width as a fraction of its half length
        public static float PassEase => 0.35f;                 // 0 = constant speed along the ellipse, 1 = slow at both ends
        public static float SpinPerTick => 0.55f;              // radians per tick the wrench spins while it flies the ellipse
        public static bool ThroughPlatforms => true;           // platforms and other top-solid blocks do not stop it
        public static float MaxDistancePx => 900f;             // farther than this from the player and the wrench is gone
        public static int HitboxSize => 24;                    // the wrench's box (it is stopped by blocks with this box)
        public static float CollisionStepPx => 6f;             // it moves in pieces of at most this many pixels, each checked against blocks

        // ---- CONTACT (W2: the head or the string touches an enemy, and the wrench snaps onto it) ----
        public static float HeadReachPx => 12f;               // from the sprite's center to the tip of the head (the head is pinned to the contact point)
        public static float HeadHitWidthPx => 16f;            // how thick the head's touch test is (swept from the center to the tip)
        public static float RopeHitWidthPx => 8f;             // how thick the string's touch test is (swept from the hand to the butt)
        public static float MaxHoldSize => 120f;              // an enemy wider or taller than this counts as immovable (like a boss)
        public static int SnapTicks => 6;                     // the wrench flies to the contact point and turns its head to the enemy in this many ticks


        // ---- REEL (W3: a movable enemy is held and slid to the player, then the wrench flies back to the hand) ----
        public static float ReelSpeedPx => 12f;              // the enemy is reeled at this constant speed (pixels per tick, no ease), so a longer way takes longer
        public static int MinReelTicks => 4;                  // but never faster than this many ticks
        public static float PickupDistancePx => 110f;         // the moment the reeled enemy is this close to the player, the wrench is picked up (it flies to the hand while the enemy keeps sliding to the stop point)
        public static float SwingDistancePx => 56f;           // the stop point: this far from the player along the line to where the enemy was latched
        public static int RetractTicks => 8;                  // the wrench flies from the enemy to the hand in this many ticks (it accelerates)
        public static int HandOverHoldTicks => 20;            // W3 only (W4 replaces it with the heavy swing): the wrench waits in the hand with the enemy held in front, then the enemy is let go

        // ---- STRAIN (an immovable enemy is not moved: the wrench drags back a little, and the moment the drag ends it reels straight back and deals the hit) ----
        public static float StrainDragFraction => 0.3f;       // how far the wrench drags toward the player, as a fraction of the wrench's own length (head tip to butt)
        public static int StrainDragTicks => 14;              // the drag back, on a cubic ease

        public static float StrikeShake => 0.9f;              // camera punch of that hit
        public static int StrikeSparkCount => 12;

        // ---- FACING ----
        public static float FaceDeadzonePx => 6f;             // the player always faces the wrench's side; closer to the player than this the facing is kept

        // ---- RETURN (a block stopped it, or the pass is over: pulled to the hand and caught) ----
        public static float ReturnStartSpeed => 7f;
        public static float ReturnAccel => 1.1f;
        public static float ReturnMaxSpeed => 20f;
        public static float CatchDistancePx => 14f;
        public static float PointFollow => 0.35f;              // from the halfway point on (and while returning) the wrench turns to point straight away from the hand: how fast (0..1 per tick, 1 = at once)

        // ---- ARM ----
        public static float ArmFollow => 0.3f;                 // how fast the arm turns to its target pose (0..1 per tick)
        public static float ThrowArmStartDeg => 40f;           // the arm's angle when a throw starts ...
        public static int ThrowArmBlendTicks => 10;            // ... and it turns to point at the wrench over this many ticks
        public static float CatchArmDeg => 10f;                // the arm's angle at the catch
        public static float CatchArmBlendDistance => 60f;      // the arm eases from "pointing at the wrench" to CatchArmDeg over the last this-many pixels

        // ---- ROPE (the string from the hand to the butt of the wrench) ----
        public static Color RopeColor => new Color(253, 58, 61);          // placeholder: Ismail sets the colors himself
        public static Color RopeAltColor => new Color(218, 2, 5);       // placeholder: the twist, every other piece of the string
        public static Color RopeOutlineColor => new Color(109, 13, 15);      // placeholder
        public static float RopeWidth => 1.8f;
        public static float RopeOutlineWidth => 3.2f;
        public static float RopeAttachPx => 10f;               // how far from the sprite's center the butt is (where the string is tied)
        public static float HeadAngleDeg => -45f;              // the direction of the wrench HEAD from the sprite's center when the rotation is 0 (the vanilla sprite points up-right = -45); the butt (where the string is tied) is the opposite end. Change it if the head points the wrong way
        public static int RopePoints => 16;                    // points along the string (more = smoother and a bit slower)
        public static float RopeSlackFactor => 1.015f;         // while Out: the string is this much longer than the distance hand to wrench
        public static float RopeExtraSlack => 0f;              // plus this many pixels (0 = never slack)
        public static float RopeGravity => 0.08f;              // px per tick per tick: a small sag only
        public static float RopeTightenSpeed => 0.35f;         // pulled taut (the way back): how far toward the straight line the string goes every tick
        public static float RopeMinLength => 10f;
        public static float RopeDamping => 0.985f;             // velocity kept per tick
        public static int RopeIterations => 10;                // constraint passes per tick (more = stiffer string)
        public static bool RopeCollides => true;               // the string lies on the ground instead of going through it
        public static int RopeSubdivisions => 4;               // smoothing: curve pieces drawn between two rope points
        public static int RopeStripePieces => 2;               // the string alternates its two colors every this many curve pieces (2 = the yoyo look; 1 = twice as many, thinner stripes; 4 = fewer, longer stripes). The string has (RopePoints - 1) x RopeSubdivisions pieces

        // ---- THROWN TRAIL (the pixel trail the flying wrench lays; the swing's fan trail comes with W4) ----
        public static float ThrowTrailGrid => 2f;              // pixel size of the trail
        public static int ThrowTrailLifeTicks => 0;           // how long a point of the trail lasts
        public static float ThrowTrailWidth => 0f;
        public static float ThrowTrailJitter => 0f;            // pixels of fixed random offset per point, for a ragged edge
        public static Color ThrowTrailOuterColor => new Color(170, 185, 210);
        public static Color ThrowTrailInnerColor => new Color(245, 248, 255);

        // ---- FX ----
        public static float FlightSparkChance => 0.4f;         // per tick: a spark sheds off the flying wrench
        public static int ThrowSparkCount => 5;
        public static int ImpactSparkCount => 8;
        public static int CatchSparkCount => 5;
        public static int SnapSparkCount => 8;                // chips thrown when the head clamps onto an enemy
    }

    /// <summary>The rope's look and feel for the VerletRope, read live from CombatWrenchTuning (so hot reload works).</summary>
    public class CombatWrenchRopeLook : IRopeLook
    {
        public int RopePoints => CombatWrenchTuning.RopePoints;
        public float RopeMinLength => CombatWrenchTuning.RopeMinLength;
        public float RopeDamping => CombatWrenchTuning.RopeDamping;
        public int RopeIterations => CombatWrenchTuning.RopeIterations;
        public bool RopeCollides => CombatWrenchTuning.RopeCollides;
        public int RopeSubdivisions => CombatWrenchTuning.RopeSubdivisions;
        public int RopeStripePieces => CombatWrenchTuning.RopeStripePieces;
        public float RopeWidth => CombatWrenchTuning.RopeWidth;
        public float RopeOutlineWidth => CombatWrenchTuning.RopeOutlineWidth;
        public Color RopeColor => CombatWrenchTuning.RopeColor;
        public Color RopeAltColor => CombatWrenchTuning.RopeAltColor;
        public Color RopeOutlineColor => CombatWrenchTuning.RopeOutlineColor;
    }
}