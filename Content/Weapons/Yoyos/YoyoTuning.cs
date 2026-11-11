using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The numbers every yoyo shares, with defaults (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). Every yoyo has its own
    /// subclass in its folder (WoodenYoyoTuning, Code1Tuning) that overrides what differs and adds the numbers of its trick (a trick
    /// pattern's numbers come as an interface with defaults, for example IWalkTheDogTuning: the yoyo's tuning implements it and lists its
    /// own values). Properties instead of fields, so hot reload can tweak them.
    /// </summary>
    public class YoyoTuning
    {
        // His own art, when it exists (for example "IsmailsWeaponReworks/Content/Weapons/Yoyos/WoodenYoyo/WoodenYoyoProjectile"); null or
        // a missing file = the vanilla sprite.
        public virtual string TexturePath => null;

        // ---- rope length ----
        // The string's maximum length in pixels (16 px = 1 tile). It is also how far a throw can reach: the Forward Pass goes this far
        // minus its margin, and a yoyo that is out comes back when it is this far from the hand. Every yoyo lists its own value.
        public virtual float RopeLengthPx => 130f;

        public float RopeLength()
        {
            return Math.Max(48f, RopeLengthPx);
        }

        // ---- rope (verlet, visual only: every machine simulates its own) ----
        public virtual int RopePoints => 16;               // points along the string (more = smoother and a bit slower)
        public virtual float RopeSlackFactor => 1.015f;    // while Out: the string is this much longer than the distance hand to yoyo (1.01 to 1.02 = a very slight bend)
        public virtual float RopeExtraSlack => 0f;         // plus this many pixels (0 = never slack)
        public virtual float RopeGravity => 0.08f;         // px per tick per tick: a small sag only
        public virtual float RopeTightenSpeed => 0.35f;    // pulled taut (the way back): how far toward the straight line the string goes every tick (0.35 = about 6 ticks, 1 = at once)
        public virtual float RopeMinLength => 10f;
        public virtual float RopeDamping => 0.985f;        // velocity kept per tick
        public virtual int RopeIterations => 10;           // constraint passes per tick (more = stiffer string)
        public virtual bool RopeCollides => true;          // the string lies on the ground instead of going through it
        public virtual int RopeSubdivisions => 4;          // smoothing: curve pieces drawn between two rope points
        public virtual float RopeWidth => 1.8f;
        public virtual float RopeOutlineWidth => 3.2f;
        public virtual Color RopeColor => new Color(238, 228, 205);
        public virtual Color RopeAltColor => new Color(214, 200, 172); // the twist: every other piece of the string
        public virtual Color RopeOutlineColor => new Color(92, 74, 54);

        // ---- the yoyo body ----
        public virtual int HitboxSize => 24;
        public virtual int HitCooldownTicks => 14;         // the same enemy can be hit again after this long
        public virtual float ArmFollow => 0.3f;            // how fast the arm turns to its target pose (0..1 per tick)
        public virtual int SyncEveryTicks => 6;            // the owner re-sends the yoyo's position this often while it is out
        public virtual bool UseSlopes => true;             // false: only plain block collision (try it if a rolling trick behaves oddly on slopes)

        // ---- collision, hits and string wrap (patch 8) ----
        public virtual float CollisionStepPx => 6f;        // the yoyo moves in pieces of at most this many pixels, each checked against blocks, so a fast yoyo cannot clip a corner or jump a block (smaller = safer, a bit slower)
        public virtual bool SweepHits => true;             // the damage hitbox is the whole path flown since last tick, so a fast yoyo (or a pull back through the ground) cannot skip an enemy
        public virtual bool RopeWraps => true;             // while out, the string bends around block corners instead of passing through them
        public virtual int RopeWrapMaxPivots => 6;         // how many corners one string can be wrapped around
        public virtual float RopeWrapClearancePx => 2f;    // the string goes this far outside a corner
        public virtual float RopeWrapSnapSpeed => 0.7f;    // when the wrap changes, how fast the string moves onto its new path (0..1 per tick)

        // ---- idle (held, nothing pressed) ----
        public virtual int IdleRestTicks => 8;             // the yoyo rests in the hand
        public virtual int IdleDropTicks => 22;            // it is let down gently
        public virtual int IdleSpinTicks => 38;            // it spins at the bottom
        public virtual int IdleRaiseTicks => 26;           // it is pulled back up
        public virtual float IdleDropPx => 24f;
        public virtual float IdleSwayPx => 2f;
        public virtual float IdleSpinRestSpeed => 0.1f;    // radians per tick at the hand
        public virtual float IdleSpinSpeed => 0.55f;       // radians per tick at the bottom
        public virtual float IdleArmRestDeg => 20f;        // arm angle below the horizontal, facing-local (90 = straight down)
        public virtual float IdleArmDropDeg => 60f;
        public virtual float ThrowArmStartDeg => 40f;      // the arm's angle when a throw starts ...
        public virtual int ThrowArmBlendTicks => 10;       // ... and it turns to point at the yoyo over this many ticks
        public virtual float CatchArmDeg => 10f;           // the arm's angle at the catch (50 = the idle rest pose)
        public virtual float CatchArmBlendDistance => 60f; // the arm eases from "pointing at the yoyo" to CatchArmDeg over the last this-many pixels

        // ---- throw rhythm and return ----
        public virtual int ReleaseGraceTicks => 10;        // a release this soon after the throw is ignored, so a tap still throws
        public virtual int RethrowDelayTicks => 8;         // after a catch, a held button throws again after this long
        public virtual float ReturnStartSpeed => 7f;
        public virtual float ReturnAccel => 1.1f;
        public virtual float ReturnMaxSpeed => 20f;
        public virtual float CatchDistance => 14f;
        public virtual float ReturnSpinSpeed => 0.6f;
        public virtual float SpinPerPixel => 0.12f;        // rolling: radians of spin per pixel of travel

        // ---- juice (off by default; the shared helpers in YoyoJuice.cs) ----
        public virtual float HitShake => 0f;               // very subtle camera punch per enemy hit (0 = none)
        public virtual bool Afterimages => false;          // afterimages while the yoyo is fast and out
        public virtual int AfterimageTicks => 10;          // how long one afterimage lives
        public virtual float AfterimageMinScale => 0.7f;   // it shrinks to this fraction of the yoyo's size while it fades
        public virtual float AfterimageMinSpeed => 3f;     // none are laid while the yoyo is slower than this (px per tick)
        // The color the sprite is multiplied by: Color.White = the same sprite (lit like the yoyo), Color.White * 0.5f = half faded,
        // Color.Cyan * 0.6f = a faded cyan tint, and so on.
        public virtual Color AfterimageColor => Color.White * 0.6f;
    }
}