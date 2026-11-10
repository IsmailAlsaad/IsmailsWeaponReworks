using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// A yoyo trick: what the yoyo does while it is out. The YoyoProjectile owns the state machine (idle, out, return), the rope, the arm
    /// and the net sync; a trick only moves the yoyo (through yoyo.Step or yoyo.Move) and reacts to hits. Every machine runs Tick from the
    /// synced start state; the owner's client alone decides hits and when to retract.
    /// A trick is a PATTERN any yoyo can use: WalkTheDogTrick and ForwardPassTrick live in this folder. A yoyo uses one as it is or
    /// makes its own subclass in its folder (WoodenYoyoTrick, Code1Trick) that overrides Begin, Tick, OnHit or any virtual (calling the
    /// base to keep the pattern) to add custom behavior on top. The pattern's numbers come from the yoyo's tuning (see IWalkTheDogTuning).
    /// </summary>
    public abstract class YoyoTrick
    {
        protected readonly YoyoTuning T;

        protected YoyoTrick(YoyoTuning tuning)
        {
            T = tuning;
        }

        public abstract string Name { get; }

        // Enemy hits before the yoyo retracts.
        public abstract int MaxHits { get; }

        // True: while the yoyo is out the owner's cursor side keeps deciding the yoyo's side (the trick can be turned around mid-way).
        public virtual bool FollowsCursorSide => false;

        // How far past the player's center the cursor must be before the side switches (no flicker over the player).
        public virtual float CursorSideDeadzonePx => 10f;

        // False: letting go of the button does not cut the trick short (it runs to its end by itself).
        public virtual bool RetractsOnRelease => true;

        // True while the string should be pulled taut and straight even though the yoyo is still out (Forward Pass: its way back). Visual only.
        public virtual bool RopeTaut(YoyoProjectile yoyo)
        {
            return false;
        }

        // A trick that draws the string by hand (the Artery's loop: the straight line plus the noose) fills 'points' with a dense polyline
        // and returns true; the rope is then drawn along it instead of the simulated string. Visual only; never called on the server.
        public virtual bool StringPath(YoyoProjectile yoyo, List<Vector2> points)
        {
            return false;
        }

        // True: the yoyo keeps its damage hitbox while it is pulled back to the hand (the Return state), until it is caught. Read by
        // YoyoProjectile.CanDamage on the owner's client; the Forward Pass answers true after a block stopped it on its way back.
        public virtual bool HurtsWhileReturning(YoyoProjectile yoyo)
        {
            return false;
        }

        // The throw starts. authoritative = the owner's client, which sets the launch velocity; the others get it from the sync.
        public abstract void Begin(YoyoProjectile yoyo, bool authoritative);

        // Once per tick while the yoyo is out, on every machine.
        public abstract void Tick(YoyoProjectile yoyo);

        // The owner's client only: the yoyo just hit an enemy.
        public virtual void OnHit(YoyoProjectile yoyo, NPC target)
        {
        }

        // How much the yoyo's sprite turns this tick.
        public virtual float SpinStep(YoyoProjectile yoyo)
        {
            return yoyo.Vel.X * T.SpinPerPixel;
        }
    }
}