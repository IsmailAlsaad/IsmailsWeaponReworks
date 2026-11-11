using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// Every tunable number of the Artery (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). The first part lists EVERY general
    /// parameter of YoyoTuning with this yoyo's value (the same as the general value unless it differs), so they are all in one place
    /// to read and change; the part after it holds the numbers of its trick (IForwardPassTuning) and anything else only this yoyo has. A new yoyo
    /// copies this file and keeps the whole list.
    /// </summary>
    public class ArteryTuning : YoyoTuning, IForwardPassTuning
    {
        // ======== general parameters (see YoyoTuning) ========
        // His own art, when it exists (for example "IsmailsWeaponReworks/Content/Weapons/Yoyos/WoodenYoyo/WoodenYoyoProjectile"); null or
        // a missing file = the vanilla sprite.
        public override string TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/Artery/ArteryProjectile";

        // ---- rope length ----
        // The string's maximum length in pixels (16 px = 1 tile). It is also how far a throw can reach: the Forward Pass goes this far
        // minus its margin, and a yoyo that is out comes back when it is this far from the hand. Every yoyo lists its own value.
        public override float RopeLengthPx => 235f;

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
        public override Color RopeColor => new Color(196, 52, 60);
        public override Color RopeAltColor => new Color(150, 28, 40); // the twist: every other piece of the string
        public override Color RopeOutlineColor => new Color(58, 10, 18);

        // ---- the yoyo body ----
        public override int HitboxSize => 16;
        public override int HitCooldownTicks => 14;         // the same enemy can be hit again after this long
        public override float ArmFollow => 0.3f;            // how fast the arm turns to its target pose (0..1 per tick)
        public override int SyncEveryTicks => 6;            // the owner re-sends the yoyo's position this often while it is out
        public override bool UseSlopes => true;             // false: only plain block collision (try it if a rolling trick behaves oddly on slopes)

        // ---- collision, hits and string wrap (patch 8) ----
        public override float CollisionStepPx => 6f;        // the yoyo moves in pieces of at most this many pixels, each checked against blocks, so a fast yoyo cannot clip a corner or jump a block (smaller = safer, a bit slower)
        public override bool SweepHits => true;             // the damage hitbox is the whole path flown since last tick, so a fast yoyo (or a pull back through the ground) cannot skip an enemy
        public override bool RopeWraps => true;             // while out, the string bends around block corners instead of passing through them
        public override int RopeWrapMaxPivots => 6;         // how many corners one string can be wrapped around
        public override float RopeWrapClearancePx => 2f;    // the string goes this far outside a corner
        public override float RopeWrapSnapSpeed => 0.7f;    // when the wrap changes, how fast the string moves onto its new path (0..1 per tick)

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
        public override float HitShake => 0.5f;               // very subtle camera punch per enemy hit (0 = none)
        public override bool Afterimages => true;          // afterimages while the yoyo is fast and out
        public override int AfterimageTicks => 10;          // how long one afterimage lives
        public override float AfterimageMinScale => 0.7f;   // it shrinks to this fraction of the yoyo's size while it fades
        public override float AfterimageMinSpeed => 3f;     // none are laid while the yoyo is slower than this (px per tick)
        // The color the sprite is multiplied by: Color.White = the same sprite (lit like the yoyo), Color.White * 0.5f = half faded,
        // Color.Cyan * 0.6f = a faded cyan tint, and so on.
        public override Color AfterimageColor => Color.White * 0.2f;

        // ---- Forward Pass (left click, the first half of Man on Trapeze) ----
        public int ForwardPassTicks => 32;                        // one whole pass (out along the ellipse and back to the hand)
        public float ForwardPassWidthRatio => 0.42f;              // the ellipse's half width as a fraction of its half length
        public float ForwardPassReachMargin => 30f;               // the far end is the string's full length minus this
        public float ForwardPassEase => 0.35f;                    // 0 = constant speed, 1 = slow at both ends
        public bool ForwardPassThroughPlatforms => true;          // platforms and other top-solid blocks do not stop it
        public bool ForwardPassBlockedReturnHurts => true;        // a block that stops it (in either half) does not make the way back harmless: it keeps hurting while it is pulled to the hand

        // ---- Man on Trapeze (left click): the lasso ----
        // The string (the line from the hand to the yoyo) or the yoyo itself touches an enemy that can be damaged while the pass is out
        // and the loop begins: the yoyo leaves the ellipse on a smooth curve around the enemy back to the string line, then walks along that
        // line toward the player while the noose around the enemy shrinks until it closes.
        public float LassoMinReachPx => 48f;                      // the yoyo must be at least this far from the hand before the string can catch anything (no loop on an enemy right at the player)
        public float LassoTouchPaddingPx => 6f;                   // how thick the string counts when it touches an enemy
        public int LoopCatchTicks => 14;                          // the curve from where the string caught the enemy to the string line: shorter = quicker
        public int LoopConstrictTicks => 50;                      // the walk back along the string line while the noose shrinks
        public float LoopWalkSpeedPx => 1.4f;                     // px per tick the yoyo walks toward the player while it constricts
        public float LoopGapPx => 14f;                            // where the yoyo arrives on the string line: this far outside the noose, on the player's side of the enemy
        public float LoopMinHandDistancePx => 40f;                // the yoyo never walks closer to the hand than this
        public float LoopRadiusPadPx => 10f;                      // the noose starts this much wider than half the enemy's size ...
        public float LoopMinRadiusPx => 14f;                      // ... but never smaller than this ...
        public float LoopMaxRadiusPx => 90f;                      // ... nor bigger than this (a huge boss is looped by a loop this big)
        public float LoopCloseRadiusPx => 4f;                     // the noose is this big when it has closed
        public float LoopSpinSpeed => 0.5f;                       // radians per tick the yoyo spins while it walks
        public float LoopStringStepPx => 5f;                      // the string is drawn in pieces about this long
        public int LoopArcSamples => 28;                          // points around the noose
        public float SnareMaxSizePx => 120f;                      // an enemy bigger than this is looped but not held (with bosses, immovable enemies and worm segments)

        // ---- Man on Trapeze (left click): the shred and the blood explosion (patch A2) ----
        // While the noose shrinks the enemy is shredded; when the loop closes (or the enemy dies inside it) the blood explosion goes off
        // at the enemy's position and makes everything in it bleed. A broken loop (the enemy got out of reach) does neither.
        public int ShredIntervalTicks => 10;                      // a shred hit this often while the noose shrinks (60 = once a second)
        public float ShredDamageFraction => 0.4f;                 // each shred hit, as a fraction of the yoyo's damage
        public float ShredShake => 0.5f;                          // camera punch per shred hit (0 = none), the owner only, like the yoyo's normal hit shake
        public float ExplosionRadiusPx => 90f;                    // the blood explosion's radius (an enemy's own size counts toward the reach)
        public float ExplosionDamageFraction => 3f;               // the explosion's hit, as a fraction of the yoyo's damage
        public float ExplosionKnockback => 6f;                    // the explosion's knockback (away from its center)
        public float ExplosionShake => 5f;                        // camera punch of the explosion, every machine (falls off with distance)
        public int BleedingTicks => 360;                          // how long the Bleeding debuff lasts on enemies the explosion hit
        public float BleedDamagePerSecond => 10f;                 // what Bleeding does to an enemy every second (vanilla Bleeding does nothing to enemies, ArteryBleedNPC adds it)
    }
}