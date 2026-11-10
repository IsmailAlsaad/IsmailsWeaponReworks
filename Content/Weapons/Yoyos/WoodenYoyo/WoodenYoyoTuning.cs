using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.WoodenYoyo
{
    /// <summary>
    /// Every tunable number of the WoodenYoyo (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). The first part lists EVERY general
    /// parameter of YoyoTuning with this yoyo's value (the same as the general value unless it differs), so they are all in one place
    /// to read and change; the part after it holds the numbers of its trick (IWalkTheDogTuning) and anything else only this yoyo has. A new yoyo
    /// copies this file and keeps the whole list.
    /// </summary>
    public class WoodenYoyoTuning : YoyoTuning, IWalkTheDogTuning
    {
        // ======== general parameters (see YoyoTuning) ========
        // His own art, when it exists (for example "IsmailsWeaponReworks/Content/Weapons/Yoyos/WoodenYoyo/WoodenYoyoProjectile"); null or
        // a missing file = the vanilla sprite.
        public override string TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/WoodenYoyo/WoodenYoyoProjectile";

        // ---- rope length ----
        // The string's maximum length in pixels (16 px = 1 tile). It is also how far a throw can reach: the Forward Pass goes this far
        // minus its margin, and a yoyo that is out comes back when it is this far from the hand. Every yoyo lists its own value.
        public override float RopeLengthPx => 130f;

        // ---- rope (verlet, visual only: every machine simulates its own) ----
        public override int RopePoints => 16;               // points along the string (more = smoother and a bit slower)
        public override float RopeSlackFactor => 1.015f;    // while Out: the string is this much longer than the distance hand to yoyo (1.01 to 1.02 = a very slight bend)
        public override float RopeExtraSlack => 0f;         // plus this many pixels (0 = never slack)
        public override float RopeGravity => 0.08f;         // px per tick per tick: a small sag only
        public override float RopeTightenSpeed => 0.35f;    // pulled taut (the way back): how far toward the straight line the string goes every tick (0.35 = about 6 ticks, 1 = at once)
        public override float RopeMinLength => 10f;
        public override float RopeDamping => 0.985f;        // velocity kept per tick
        public override int RopeIterations => 10;           // constraint passes per tick (more = stiffer string)
        public override bool RopeCollides => true;          // the string lies on the ground instead of going through it
        public override int RopeSubdivisions => 4;          // smoothing: curve pieces drawn between two rope points
        public override float RopeWidth => 1.8f;
        public override float RopeOutlineWidth => 3.2f;
        public override Color RopeColor => new Color(238, 228, 205);
        public override Color RopeAltColor => new Color(214, 200, 172); // the twist: every other piece of the string
        public override Color RopeOutlineColor => new Color(92, 74, 54);

        // ---- the yoyo body ----
        public override int HitboxSize => 16;
        public override int HitCooldownTicks => 14;         // the same enemy can be hit again after this long
        public override float ArmFollow => 0.3f;            // how fast the arm turns to its target pose (0..1 per tick)
        public override int SyncEveryTicks => 6;            // the owner re-sends the yoyo's position this often while it is out
        public override bool UseSlopes => true;             // false: only plain block collision (try it if a rolling trick behaves oddly on slopes)

        // ---- idle (held, nothing pressed) ----
        public override int IdleRestTicks => 8;             // the yoyo rests in the hand
        public override int IdleDropTicks => 22;            // it is let down gently
        public override int IdleSpinTicks => 38;            // it spins at the bottom
        public override int IdleRaiseTicks => 26;           // it is pulled back up
        public override float IdleDropPx => 24f;
        public override float IdleSwayPx => 2f;
        public override float IdleSpinRestSpeed => 0.1f;    // radians per tick at the hand
        public override float IdleSpinSpeed => 0.55f;       // radians per tick at the bottom
        public override float IdleArmRestDeg => 20f;        // arm angle below the horizontal, facing-local (90 = straight down)
        public override float IdleArmDropDeg => 60f;
        public override float ThrowArmStartDeg => 40f;      // the arm's angle when a throw starts ...
        public override int ThrowArmBlendTicks => 10;       // ... and it turns to point at the yoyo over this many ticks
        public override float CatchArmDeg => 10f;           // the arm's angle at the catch (50 = the idle rest pose)
        public override float CatchArmBlendDistance => 60f; // the arm eases from "pointing at the yoyo" to CatchArmDeg over the last this-many pixels

        // ---- throw rhythm and return ----
        public override int ReleaseGraceTicks => 10;        // a release this soon after the throw is ignored, so a tap still throws
        public override int RethrowDelayTicks => 8;         // after a catch, a held button throws again after this long
        public override float ReturnStartSpeed => 7f;
        public override float ReturnAccel => 1.1f;
        public override float ReturnMaxSpeed => 20f;
        public override float CatchDistance => 14f;
        public override float ReturnSpinSpeed => 0.6f;
        public override float SpinPerPixel => 0.12f;        // rolling: radians of spin per pixel of travel

        // ---- juice (off by default; the shared helpers in YoyoJuice.cs) ----
        public override float HitShake => 0f;               // very subtle camera punch per enemy hit (0 = none)
        public override bool Afterimages => false;          // afterimages while the yoyo is fast and out
        public override int AfterimageTicks => 10;          // how long one afterimage lives
        public override float AfterimageMinScale => 0.7f;   // it shrinks to this fraction of the yoyo's size while it fades
        public override float AfterimageMinSpeed => 3f;     // none are laid while the yoyo is slower than this (px per tick)
        // The color the sprite is multiplied by: Color.White = the same sprite (lit like the yoyo), Color.White * 0.5f = half faded,
        // Color.Cyan * 0.6f = a faded cyan tint, and so on.
        public override Color AfterimageColor => Color.White * 0.6f;

        // ---- Walk the Dog (left click) ----
        public int WalkHitsPerThrow => 3;          // enemy hits before it retracts
        public float WalkThrowDownSpeed => 5f;     // the throw: down ...
        public float WalkThrowForwardSpeed => 3f;  // ... and a bit toward the cursor's side
        public float WalkSwitchDeadzonePx => 10f;  // while it is out, the side only switches when the cursor is this far past the player's center
        public float WalkGravity => 0.45f;
        public float WalkMaxFall => 14f;
        public float WalkSpeed => 4.2f;            // rolling speed along the ground
        public float WalkAccel => 0.35f;           // on the ground
        public float WalkAirAccel => 0.06f;        // in the air
        public float WalkLandBounce => 0.3f;       // fraction of the fall speed that bounces back up
        public float WalkBounceMinImpact => 3f;    // softer landings do not bounce
        public float WalkHopSpeed => 5.5f;         // a small hop when a block stops the roll (steps and slopes)
        public int WalkHopCooldownTicks => 10;
        public float WalkHitRecoil => 3.5f;        // pushed back from an enemy ...
        public float WalkHitHop => 3.5f;           // ... and up
    }
}