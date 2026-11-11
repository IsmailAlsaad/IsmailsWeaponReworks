using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Valor
{
    /// <summary>
    /// Every tunable number of the Valor (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). The first part lists EVERY general
    /// parameter of YoyoTuning with this yoyo's value (the same as the general value unless it differs), so they are all in one place
    /// to read and change; the part after it holds the numbers of its tricks (IPopTheClutchTuning and IForwardPassTuning for the left click,
    /// IAroundTheWorldTuning for the right click) and anything else only this yoyo has. A new yoyo copies this file and keeps the whole list.
    /// </summary>
    public class ValorTuning : YoyoTuning, IPopTheClutchTuning, IForwardPassTuning, IAroundTheWorldTuning
    {
        // ======== general parameters (see YoyoTuning) ========
        // His own art, when it exists (for example "IsmailsWeaponReworks/Content/Weapons/Yoyos/Valor/ValorProjectile"); null or
        // a missing file = the vanilla sprite.
        public override string TexturePath => null;

        // ---- rope length ----
        // The string's maximum length in pixels (16 px = 1 tile). It is also how far a throw can reach: Pop the Clutch's passes go this far
        // minus ForwardPassReachMargin, Around the World's circle has this radius minus its margin, and a yoyo that is out comes back when it
        // is this far from the hand. Every yoyo lists its own value.
        public override float RopeLengthPx => 300f;         // A GUESS: verify ProjectileID.Sets.YoyosMaximumRange[ProjectileID.Valor] in the vanilla source and tune by feel

        // ---- rope (verlet, visual only: every machine simulates its own) ----
        public override int RopePoints => 16;               // points along the string (more = smoother and a bit slower)
        public override float RopeSlackFactor => 1.015f;    // while Out: the string is this much longer than the distance hand to yoyo (1.01 to 1.02 = a very slight bend)
        public override float RopeExtraSlack => 0f;         // plus this many pixels (0 = never slack)
        public override float RopeGravity => 0.08f;         // px per tick per tick: a small sag only
        public override float RopeTightenSpeed => 0.45f;    // pulled taut (the dangle, the way back, Around the World): how far toward the straight line the string goes every tick (0.45 = about 4 ticks, 1 = at once)
        public override float RopeMinLength => 10f;
        public override float RopeDamping => 0.985f;        // velocity kept per tick
        public override int RopeIterations => 10;           // constraint passes per tick (more = stiffer string)
        public override bool RopeCollides => true;          // the string lies on the ground instead of going through it
        public override int RopeSubdivisions => 4;          // smoothing: curve pieces drawn between two rope points
        public override float RopeWidth => 1.8f;
        public override float RopeOutlineWidth => 3.2f;
        public override Color RopeColor => new Color(214, 222, 235);
        public override Color RopeAltColor => new Color(168, 182, 204); // the twist: every other piece of the string
        public override Color RopeOutlineColor => new Color(58, 66, 84);

        // ---- the yoyo body ----
        public override int HitboxSize => 36;               // the yoyo's hitbox (a square, in pixels) and the width of its swept hit line; the sprite is not scaled. Read when the yoyo appears: swap the item away and back after a change
        public override int HitCooldownTicks => 14;         // the same enemy can be hit again after this long (Pop the Clutch also resets it at the start of every pass)
        public override float ArmFollow => 0.35f;           // how fast the arm turns to its target pose (0..1 per tick)
        public override int SyncEveryTicks => 6;            // the owner re-sends the yoyo's position this often while it is out
        public override bool UseSlopes => true;             // false: only plain block collision (try it if a rolling trick behaves oddly on slopes)

        // ---- collision, hits and string wrap (patch 8) ----
        public override float CollisionStepPx => 6f;        // the yoyo moves in pieces of at most this many pixels, each checked against blocks, so a fast yoyo cannot clip a corner or jump a block (smaller = safer, a bit slower)
        public override bool SweepHits => true;             // the damage hitbox is the whole path flown since last tick, so a fast yoyo (or a pull back through the ground) cannot skip an enemy
        public override bool RopeWraps => true;             // while out, the string bends around block corners (Around the World passes through blocks, so it switches this off for itself: YoyoTrick.WrapsString)
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
        public override float HitShake => 0.5f;             // very subtle camera punch per enemy hit (0 = none)
        public override bool Afterimages => true;           // afterimages while the yoyo is fast and out
        public override int AfterimageTicks => 8;           // how long one afterimage lives
        public override float AfterimageMinScale => 0.7f;   // it shrinks to this fraction of the yoyo's size while it fades
        public override float AfterimageMinSpeed => 3f;     // none are laid while the yoyo is slower than this (px per tick)
        // The color the sprite is multiplied by: Color.White = the same sprite (lit like the yoyo), Color.White * 0.5f = half faded,
        // Color.Cyan * 0.6f = a faded cyan tint, and so on.
        public override Color AfterimageColor => Color.White * 0.25f;

        // ---- Pop the Clutch (left click) ----
        public int PopWindupTicks => 60;                          // the wind-up: the yoyo dangles behind the back and spins up
        public float PopDangleBackPx => 12f;                      // the dangling yoyo hangs this far behind the hand ...
        public float PopDangleDownPx => 30f;                      // ... and this far below it
        public float PopDangleSwayPx => 2f;                       // a little side sway
        public int PopDangleDropTicks => 80;                       // it drops from the player to the dangle spot over this many ticks
        public float PopWindupArmDeg => -125f;                    // the arm's angle while winding up (0 = forward, -90 = straight up, below -90 = up and back)
        public float PopWindupSpinStart => 0.15f;                 // the sprite's spin (radians per tick) at the start of the wind-up ...
        public float PopWindupSpinMax => 1.2f;                    // ... and at its end
        public float PopWindupSpinCurve => 2f;                    // 1 = a steady speed-up, 2 = slow at first and faster and faster
        public int PopPasses => 2;                                // swings in a row: the 1st bulges up, the 2nd down
        public int PopPassTicks => 16;                            // one pass (out along the ellipse and back to the player)
        public int PopThrowBlendTicks => 8;                       // the first pass leaves the spot behind the back and swings through the player over this many ticks
        public float PopRecallStartSpeed => 12f;                  // a pass that hits a block is pulled straight back: speed at the start ...
        public float PopRecallAccel => 1.6f;                      // ... speeding up by this much per tick ...
        public float PopRecallMaxSpeed => 26f;                    // ... up to this speed
        public float PopRecallArrivePx => 14f;                    // back at the player when this close (the next pass starts there)
        public bool PopResetHitsPerPass => true;                  // every pass starts with fresh hit cooldowns
        public bool PopHoldToWindup => true;                      // the button must be held through the wind-up: letting go before it ends cancels it (the yoyo goes back to the hand)

        // ---- the ellipse of every pass (IForwardPassTuning; ForwardPassTicks is not used by Pop the Clutch, PopPassTicks is) ----
        public int ForwardPassTicks => 36;                        // (unused here)
        public float ForwardPassWidthRatio => 0.42f;              // the ellipse's half width as a fraction of its half length
        public float ForwardPassReachMargin => 30f;               // the far end is the string's full length minus this
        public float ForwardPassEase => 0.35f;                    // 0 = constant speed, 1 = slow at both ends
        public bool ForwardPassThroughPlatforms => true;          // platforms and other top-solid blocks do not stop it
        public bool ForwardPassBlockedReturnHurts => true;        // a block in the last pass does not make the way back harmless: it keeps hurting while it is pulled to the hand

        // ---- Around the World (right click) ----
        public int AroundSpins => 3;                              // whole turns round the player; the last one slows down and spirals in to the hand (the Hive-Five makes 2)
        public float AroundReachMargin => 36f;                    // the circle's radius is the string's full length minus this
        public float AroundStartRadiusPx => 18f;                  // the yoyo leaves the hand this far away ...
        public int AroundFlingTicks => 24;                        // ... and is flung out to the full radius over this many ticks
        public float AroundStartSpeed => 0.12f;                   // radians per tick at the throw
        public float AroundTopSpeed => 0.34f;                     // radians per tick at full speed (0.34 = one turn in about 18 ticks)
        public int AroundAccelTicks => 18;                        // speeding up from the start speed to the top speed
        public float AroundEndSpeed => 0.1f;                      // radians per tick at the very end of the last spin
        public float AroundCatchRadiusPx => 20f;                  // where the last spin ends: this close to the hand
        public float AroundArmBaseDeg => 5f;                      // the arm's angle below the horizontal at the sides of the circle
        public float AroundArmUpDeg => 38f;                       // how far the arm tilts up while the yoyo is on the upper half
        public float AroundArmDownDeg => 24f;                     // how far it tilts down while it is on the lower half
        public float AroundSpriteSpin => 0.8f;                    // how fast the yoyo's own sprite turns (radians per tick at full speed)
        public bool AroundThroughBlocks => true;                  // the yoyo passes through blocks
        public int AroundWhooshPerTurn => 2;                      // whooshes per turn
    }
}