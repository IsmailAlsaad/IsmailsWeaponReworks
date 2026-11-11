using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// Every tunable number of the Hive-Five (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). The first part lists EVERY general
    /// parameter of YoyoTuning with this yoyo's value (the same as the general value unless it differs), so they are all in one place
    /// to read and change; the part after it holds the numbers of its trick (IAroundTheWorldTuning) and anything else only this yoyo has. A new yoyo
    /// copies this file and keeps the whole list.
    /// </summary>
    public class HiveFiveTuning : YoyoTuning, IAroundTheWorldTuning, ISleeperTuning
    {
        // ======== general parameters (see YoyoTuning) ========
        // His own art, when it exists (for example "IsmailsWeaponReworks/Content/Weapons/Yoyos/WoodenYoyo/WoodenYoyoProjectile"); null or
        // a missing file = the vanilla sprite.
        public override string TexturePath => null;

        // ---- rope length ----
        // The string's maximum length in pixels (16 px = 1 tile). It is also how far a throw can reach: Around the World's circle has this radius
        // minus its margin, and a yoyo that is out comes back when it is this far from the hand. Every yoyo lists its own value.
        public override float RopeLengthPx => 225f;         // the vanilla Hive-Five reaches 14.0625 tiles = 225 px (terraria.wiki.gg); verify ProjectileID.Sets.YoyosMaximumRange[ProjectileID.HiveFive] in the vanilla source

        // ---- rope (verlet, visual only: every machine simulates its own) ----
        public override int RopePoints => 16;               // points along the string (more = smoother and a bit slower)
        public override float RopeSlackFactor => 1.015f;    // while Out: the string is this much longer than the distance hand to yoyo (1.01 to 1.02 = a very slight bend)
        public override float RopeExtraSlack => 0f;         // plus this many pixels (0 = never slack)
        public override float RopeGravity => 0.08f;         // px per tick per tick: a small sag only
        public override float RopeTightenSpeed => 0.55f;    // pulled taut (Around the World: all the way round): how far toward the straight line the string goes every tick (0.55 = it trails a little behind the yoyo, 1 = at once)
        public override float RopeMinLength => 10f;
        public override float RopeDamping => 0.985f;        // velocity kept per tick
        public override int RopeIterations => 10;           // constraint passes per tick (more = stiffer string)
        public override bool RopeCollides => true;          // the string lies on the ground instead of going through it
        public override int RopeSubdivisions => 4;          // smoothing: curve pieces drawn between two rope points
        public override float RopeWidth => 1.8f;
        public override float RopeOutlineWidth => 3.2f;
        public override Color RopeColor => new Color(255, 214, 96);
        public override Color RopeAltColor => new Color(255, 238, 168); // the twist: every other piece of the string
        public override Color RopeOutlineColor => new Color(140, 84, 12);

        // ---- the yoyo body ----
        public override int HitboxSize => 32;               // the yoyo's hitbox (a square, in pixels) and the width of its swept hit line. The Hive-Five only hurts while it is out (the spin), so this is the size of the spinning yoyo's hit; the sprite is not scaled. Try 28 to 36. It is read when the yoyo appears: swap the item away and back (or reload) after a change
        public override int HitCooldownTicks => 14;         // the same enemy can be hit again after this long
        public override float ArmFollow => 0.4f;            // how fast the arm turns to its target pose (0..1 per tick)
        public override int SyncEveryTicks => 6;            // the owner re-sends the yoyo's position this often while it is out
        public override bool UseSlopes => true;             // false: only plain block collision (try it if a rolling trick behaves oddly on slopes)

        // ---- collision, hits and string wrap (patch 8) ----
        public override float CollisionStepPx => 6f;        // the yoyo moves in pieces of at most this many pixels, each checked against blocks, so a fast yoyo cannot clip a corner or jump a block (smaller = safer, a bit slower)
        public override bool SweepHits => true;             // the damage hitbox is the whole path flown since last tick, so a fast yoyo (or a pull back through the ground) cannot skip an enemy
        public override bool RopeWraps => false;            // off: Around the World passes through blocks, so the string does too (true = it bends around block corners)
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
        public override Color AfterimageColor => new Color(255, 200, 70) * 0.35f;

        // ---- Around the World (left click) ----
        public int AroundSpins => 2;                              // whole turns round the player; the last one slows down and spirals in to the hand
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
        public int ChargedSpins => 6;                             // whole turns of a CHARGED Around the World (a charge from the Sleeper hold)

        // ---- Sleeper hold (right click) ----
        public float SleeperDropPx => 34f;                        // how far below the hand the yoyo hangs while it is held
        public int SleeperDropTicks => 14;                        // letting it down
        public float SleeperSwayPx => 2f;                         // a little side sway
        public int SleeperChargeTicks => 90;                      // from the start of the hold until max speed (60 = 1 second)
        public float SleeperStartSpin => 0.1f;                    // the yoyo's sprite turns this fast (radians per tick) at the start ...
        public float SleeperMaxSpin => 1.3f;                      // ... and this fast at max speed
        public float SleeperSpinCurve => 2f;                      // 1 = a steady speed-up, 2 = slow at first and faster and faster
        public float SleeperArmDeg => 68f;                        // the arm's angle below the horizontal while holding

        // ---- the honey trail of a charged Around the World (a PixelTrail: it keeps fading after the yoyo is caught) ----
        public float HoneyTrailGrid => 2f;                        // pixel size of the trail
        public int HoneyTrailLifeTicks => 16;                     // how long a point of the trail lives (the trail's length: at top speed about 16 ticks is most of a turn)
        public float HoneyTrailWidth => 9f;                       // its widest, at the yoyo (it thins to a point at the tail)
        public int HoneyTrailSamplesPerTick => 4;                 // points laid per tick along the circle (more = a smoother ring)
        public float HoneyTrailJitter => 0.25f;                   // ragged edge in pixels
        public float HoneyTrailDripChance => 0.3f;                // per tick: a honey drop falls from a random point of the trail
        public Color HoneyTrailOuterColor => new Color(235, 140, 20, 0);   // the wide outer glow (alpha 0 = additive)
        public Color HoneyTrailCoreColor => new Color(255, 226, 120, 0);   // the bright core

        // ---- the bees of a charged Around the World (vanilla bees, sent out by the owner's client) ----
        public int BeeIntervalTicks => 6;                         // a bee leaves the yoyo this often while a charged spin flies
        public int BeeMaxPerSpin => 24;                           // at most this many bees per spin
        public float BeeSpeed => 5f;                              // how fast a bee leaves the yoyo (it flies out and away from the player, then hunts)
        public float BeeDamageFraction => 0.4f;                   // a bee's damage as a fraction of the yoyo's
        public float GiantBeeChance => 0.25f;                     // the chance that a bee is a giant bee ...
        public float GiantBeeDamageFraction => 0.7f;              // ... which hits harder

        // ---- the honey buff ----
        public int HoneyPerHitTicks => 60;                        // every enemy the yoyo hits during a charged spin adds this much Honey (60 = 1 second)
        public int HoneyBuffMaxTicks => 600;                     // the Honey buff is never topped up beyond this (the Bee Keeper's cap)

        // ---- the golden honey outline of a charged yoyo (also shown when the Sleeper hold has reached max speed) ----
        public float OutlineWidth => 1.5f;                        // pixels, at full strength
        public int OutlinePulseTicks => 40;                       // one pulse
        public float OutlineMin => 0.6f;                          // the pulse swings between these strengths
        public float OutlineMax => 1f;
        public float OutlineEase => 0.12f;                        // how fast it fades in and out (0..1 per tick)
        public float GlowScale => 1.6f;                           // the soft glow copy behind the outline is this much bigger than the yoyo
        public float GlowAlpha => 0.4f;                           // its opacity
        public Color OutlineColor => new Color(255, 176, 40, 0);       // alpha 0 = additive
        public Color OutlineCoreColor => new Color(255, 230, 130, 0);  // the brighter inner ring
    }
}