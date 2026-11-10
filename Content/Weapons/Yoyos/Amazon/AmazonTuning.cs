using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// Every tunable number of the Amazon (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). The first part lists EVERY general
    /// parameter of YoyoTuning with this yoyo's value (the same as the general value unless it differs), so they are all in one place
    /// to read and change; the part after it holds the numbers of its trick (IForwardPassTuning) and anything else only this yoyo has. A new yoyo
    /// copies this file and keeps the whole list.
    /// </summary>
    public class AmazonTuning : YoyoTuning, IForwardPassTuning, IWalkTheDogTuning
    {
        // ======== general parameters (see YoyoTuning) ========
        // His own art, when it exists (for example "IsmailsWeaponReworks/Content/Weapons/Yoyos/WoodenYoyo/WoodenYoyoProjectile"); null or
        // a missing file = the vanilla sprite.
        public override string TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/Amazon/AmazonProjectile";

        // Amazon's own sprites (until they exist: the vanilla sprite with barbs drawn by code, same size as the vanilla one)
        public string Barbed1TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/Amazon/AmazonProjectileBarbed1";   // the yoyo after the first hit
        public string Barbed2TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/Amazon/AmazonProjectileBarbed2";   // after the second hit

        // ---- rope length ----
        // The string's maximum length in pixels (16 px = 1 tile). It is also how far a throw can reach: the Forward Pass goes this far
        // minus its margin, and a yoyo that is out comes back when it is this far from the hand. Every yoyo lists its own value.
        public override float RopeLengthPx => 260f;

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
        public override Color AfterimageColor => new Color(170, 255, 150) * 0.25f;

        // ---- Forward Pass (left click) ----
        public int ForwardPassTicks => 32;                        // one whole pass (out along the ellipse and back to the hand)
        public float ForwardPassWidthRatio => 0.42f;              // the ellipse's half width as a fraction of its half length
        public float ForwardPassReachMargin => 30f;               // the far end is the string's full length minus this
        public float ForwardPassEase => 0.35f;                    // 0 = constant speed, 1 = slow at both ends
        public bool ForwardPassThroughPlatforms => true;          // platforms and other top-solid blocks do not stop it
        public bool ForwardPassBlockedReturnHurts => true;        // a block that stops it (in either half) does not make the way back harmless: it keeps hurting while it is pulled to the hand

        // ---- the barbs ----
        public int BarbHitsToRelease => 3;                        // hits per cycle: the last one releases the barbs onto the enemy, the ones before make the yoyo more barbed
        public int MaxCountedHitsPerSwing => 2;                   // at most this many hits of ONE swing (pass) count toward the stage; later hits of the same swing still hurt but add nothing
        public float ReleaseShake => 0.8f;                        // the camera punch when the barbs are released (0 = none)

        // ---- the latched vines (the coil around an enemy, the Blade of Grass style) ----
        public int LatchVineCount => 2;                           // sections released onto the enemy by the last hit
        public float LatchSectionPx => 30f;                       // length of one section
        public float EnemyCapPx => 30f;                          // ALL latched sections of this player on ONE enemy together are at most this long
        public float MinEnemyRoomPx => 30f;                       // less room than this left on the enemy: nothing is released, the barbs wait for the next hit
        public int LatchLifeTicks => 300;                         // how long the vines stay latched
        public int LatchFadeTicks => 24;                          // they unwind over the last this-many ticks
        public int LatchPrickTicks => 20;                         // damage tick of one section
        public float LatchDamageFactor => 0.2f;                   // damage of one tick as a fraction of the yoyo's damage
        public int LatchPoisonTicks => 30;                        // Poisoned is topped up to this on the damage ticks, so it lasts as long as a vine is latched
        public int CoilGrowTicks => 14;                           // the coil winds around the enemy over this time
        public int SpineGrowTicks => 18;                          // the smooth vine turns into the spiny one over this time
        public float CoilRadiusScale => 0.55f;                    // coil half-size relative to the enemy's half-size
        public float CoilRadiusPad => 6f;
        public float CoilDrift => 0.5f;                           // how far the coil climbs up the enemy (fraction of its height)
        public float MaxLoops => 3.5f;
        public int CoilSamples => 48;
        public string SmoothChainName => "Chain5";                // Terraria/Images/<this>: the Ivy Whip chain
        public string SpineChainName => "Chains_1";               // Terraria/Images/<this>: the Thorn Hook chain
        public float SmoothScale => 1f;
        public float SpineScale => 1f;
        public float HeadScale => 1f;
        public float HeadForwardPx => 2f;                         // the head sticks out this far beyond the end of the vine
        public float ChainEndTrimPx => 4f;                        // the chain stops this far before each end (the head covers the rest)

        // ---- Walk the Dog (right click; the same numbers as the Wooden Yoyo's) ----
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

        // ---- the barbed ground vines (right click) ----
        public int VineIntervalTicks => 36;                       // while the yoyo rolls on the ground it plants a vine this often (never in the air)
        public float VineMinSpacingPx => 24f;                     // and never closer than this to the last one
        public int MaxVines => 5;                                 // vines of one player standing at once; a sixth makes the oldest dig away
        public int VineLifeTicks => 1800;                         // an untouched vine digs away after this long (30 s)
        public float VineLengthPx => 30f;                         // how tall a standing vine is
        public float VineHitWidthPx => 14f;                       // an enemy whose hitbox overlaps this column above the surface is snared
        public float VineRootSinkPx => 2f;                        // the foot of the vine is this far inside the surface
        public int VineGrowTicks => 12;                           // it grows out of the ground over this time
        public int DigTicks => 20;                                // it shrinks back into the ground over this time
        public float VineSwayPx => 2.5f;                          // how far the tip sways sideways
        public float VineSwaySpeed => 0.09f;                      // radians per tick
        public int VinePoints => 10;                              // points along the vine (the drawing is one smooth strip)
        public float VineBaseScale => 1f;                         // width of the chain at the root ...
        public float VineTipScale => 0.8f;                        // ... and at the tip
        public float VineHeadScale => 0.8f;                       // the Thorn Hook head on the tip
        public int SnareTicks => 500;                             // how long a snared enemy is held (5 s)
        public int ResnareCooldownTicks => 120;                   // after a release the same enemy cannot be snared again for this long
        public int WrapTicks => 14;                               // the vine grows into the hoop over the enemy over this time
        public float HoopFootPadPx => 6f;                         // the hoop comes down this far beside the enemy (half its width plus this), on both sides
        public float HoopHeightPadPx => 8f;                       // the top of the hoop is this far above the enemy's head
        public float HoopMaxHeightPx => 16f;                     // the enemy's height counted for the hoop is at most this
        public int HoopPoints => 24;                              // points along the hoop (the drawing is one smooth strip)
        public float AnchorSinkPx => 3f;                          // the tip digs this far into the ground
        public bool SnareStopsAI => true;                         // a held enemy runs no AI at all (no walking, jumping, flying, falling or attacks); false = only its movement is zeroed
        public int SnareDustTicks => 14;                          // a few leaves near the held enemy this often
        public float SnareShake => 0.5f;                          // a small camera punch when an enemy is snared (0 = none)
    }
}