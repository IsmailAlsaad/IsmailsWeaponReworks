using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The numbers of the Around the World pattern, with defaults (see IWalkTheDogTuning for how a yoyo uses them). Properties, so hot reload can tweak them.
    /// </summary>
    public interface IAroundTheWorldTuning
    {
        int AroundSpins => 2;                 // whole turns round the player; the last one slows down and spirals in to the hand (2 = the base)
        float AroundReachMargin => 36f;       // the circle's radius is the string's full length minus this (keep it above the hand's reach, about 16 px)
        float AroundStartRadiusPx => 18f;     // the yoyo leaves the hand this far away ...
        int AroundFlingTicks => 12;           // ... and is flung out to the full radius over this many ticks
        float AroundStartSpeed => 0.12f;      // radians per tick at the throw
        float AroundTopSpeed => 0.34f;        // radians per tick at full speed (0.34 = one turn in about 18 ticks)
        int AroundAccelTicks => 18;           // speeding up from the start speed to the top speed
        float AroundEndSpeed => 0.1f;         // radians per tick at the very end of the last spin
        float AroundCatchRadiusPx => 20f;     // where the last spin ends: this close to the hand
        float AroundArmBaseDeg => 5f;         // the arm's angle below the horizontal at the sides of the circle (0 = straight forward, 90 = straight down)
        float AroundArmUpDeg => 38f;          // how far the arm tilts up while the yoyo is on the upper half of the circle
        float AroundArmDownDeg => 24f;        // how far the arm tilts down while it is on the lower half
        float AroundSpriteSpin => 0.8f;       // how fast the yoyo's own sprite turns (radians per tick at full speed)
        bool AroundThroughBlocks => true;     // the yoyo passes through blocks (false: a block ends the spin and the yoyo is pulled back)
        int AroundWhooshPerTurn => 2;         // whooshes per turn
    }

    class AroundTheWorldDefaults : IAroundTheWorldTuning
    {
    }

    /// <summary>
    /// Around the World (pattern; the Hive-Five's left click): the yoyo is flung forward from the hand on the cursor's side and swings in a big
    /// vertical circle round the hand, forward, down, behind and over the top (clockwise on screen when facing right), for AroundSpins
    /// turns. It speeds up for the first turn(s); the last turn slows down and spirals in until the yoyo is back at the hand and is caught.
    /// The arm does NOT turn with the yoyo like a real hand: it only tilts up while the yoyo is on the upper half and down while it is on the
    /// lower half (YoyoTrick.ArmPose). The string stays taut. The yoyo hurts everything on its path (the swept hitbox of the base), the
    /// player is not tied to the enemies it hits (nothing is caught). Letting go of the button does not cut it short; a held button throws
    /// the next one after the catch.
    /// The motion is a closed-form function of the ticks since the throw (Sample), so every machine computes the same place.
    /// MULTIPLAYER: the owner decides the click, the side (the cursor's side at the throw) and the hits; the base already syncs the state, the side and the
    /// throw serial. Every machine places the yoyo from OutTicks, the side and its own copy of the player's hand; the arm, the string and the
    /// fx are never synced. The owner's client alone ends the spin (BeginReturn).
    /// </summary>
    public class AroundTheWorldTrick : YoyoTrick
    {
        protected readonly IAroundTheWorldTuning A;
        protected float angle;      // where the yoyo is on the circle: 0 = straight ahead, growing over the top (radians)
        protected float radius;     // its distance from the hand
        protected float omega;      // radians per tick right now
        protected float speed01;    // omega as a fraction of the top speed (0..1)
        protected bool finished;    // the last spin has ended (the owner pulls the yoyo back)
        protected bool stopped;     // a block ended the spin on this machine (only when AroundThroughBlocks is false)
        int lastWhoosh;
        Vector2 pivotPrev;          // the centre of the circle (the hand) at the last tick and at this one: a trail blends between them
        Vector2 pivotNow;

        public AroundTheWorldTrick(YoyoTuning tuning) : base(tuning)
        {
            A = tuning as IAroundTheWorldTuning ?? new AroundTheWorldDefaults();
        }

        public override string Name => "Around the World";
        public override int MaxHits => int.MaxValue;
        public override bool RetractsOnRelease => false;
        public override bool WrapsString => !A.AroundThroughBlocks;   // the string follows the yoyo through blocks too (a yoyo whose tuning has RopeWraps on keeps wrapping for its other tricks)

        public float Angle => angle;
        public float Radius => radius;
        public float Speed01 => speed01;

        // How many whole turns this throw makes (a yoyo whose throw can be charged overrides it).
        protected virtual int Spins => A.AroundSpins;

        // The circle's full radius: the string's length minus the margin.
        protected float FullRadius()
        {
            return Math.Max(A.AroundCatchRadiusPx + 24f, T.RopeLength() - A.AroundReachMargin);
        }

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            angle = 0f;
            radius = A.AroundStartRadiusPx;
            omega = A.AroundStartSpeed;
            speed01 = 0f;
            finished = false;
            stopped = false;
            lastWhoosh = 0;
            pivotNow = yoyo.HandFollowed;
            pivotPrev = pivotNow;
            yoyo.Vel = Vector2.Zero;
        }

        // Where the yoyo is 'ticks' ticks after the throw: the angle, the radius and the speed. Speeding up (linearly), then full speed until the last
        // whole turn starts, then that turn slows down (linearly) while the radius shrinks to the catch radius.
        void Sample(float ticks, out float theta, out float r, out float w, out bool done)
        {
            int spins = Math.Max(1, Spins);
            float top = Math.Max(0.05f, A.AroundTopSpeed);
            float start = MathHelper.Clamp(A.AroundStartSpeed, 0.02f, top);
            float end = MathHelper.Clamp(A.AroundEndSpeed, 0.02f, top);
            float accel = Math.Max(1, A.AroundAccelTicks);
            float full = FullRadius();
            float catchRadius = Math.Min(A.AroundCatchRadiusPx, full);
            float t = ticks;

            float accelAngle = start * accel + (top - start) * accel * 0.5f;
            float decelStartAngle = Math.Max(MathHelper.TwoPi * (spins - 1), accelAngle);
            float cruise = (decelStartAngle - accelAngle) / top;
            float decel = 2f * MathHelper.TwoPi / (top + end);   // the last whole turn: (top + end) / 2 * decel = 2 pi

            float fling = MathHelper.Clamp(t / Math.Max(1, A.AroundFlingTicks), 0f, 1f);
            float flingRadius = MathHelper.Lerp(A.AroundStartRadiusPx, full, 1f - (1f - fling) * (1f - fling) * (1f - fling));
            done = false;
            if (t < accel)
            {
                theta = start * t + (top - start) * t * t / (2f * accel);
                w = start + (top - start) * t / accel;
                r = flingRadius;
                return;
            }
            t -= accel;
            if (t < cruise)
            {
                theta = accelAngle + top * t;
                w = top;
                r = flingRadius;
                return;
            }
            t -= cruise;
            float u = Math.Min(t, decel);
            float v = u / decel;
            theta = decelStartAngle + top * u - (top - end) * u * u / (2f * decel);
            w = top - (top - end) * v;
            r = MathHelper.Lerp(flingRadius, catchRadius, v * v * (3f - 2f * v));
            done = t >= decel;
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            if (stopped)
            {
                yoyo.Vel = Vector2.Zero;
                return;
            }

            Sample(yoyo.OutTicks, out angle, out radius, out omega, out finished);
            speed01 = MathHelper.Clamp(omega / Math.Max(0.05f, A.AroundTopSpeed), 0f, 1f);

            pivotPrev = pivotNow;
            pivotNow = yoyo.HandFollowed;   // the hand as it is now, so a running player does not leave the circle a tick behind
            Vector2 target = pivotNow + Offset(yoyo, angle, radius);
            Vector2 delta = target - yoyo.Projectile.Center;
            if (A.AroundThroughBlocks)
            {
                yoyo.Projectile.Center = target;
                yoyo.Vel = delta;
            }
            else
            {
                YoyoContact contact = yoyo.Move(delta, true);
                if (contact.Blocked)
                {
                    stopped = true;
                    yoyo.Fx.Impact(yoyo.Projectile.Center, contact.Normal, contact.Impact);
                    if (yoyo.IsOwner)
                        yoyo.BeginReturn();
                    return;
                }
            }

            Effects(yoyo);
            if (finished && yoyo.IsOwner)
                yoyo.BeginReturn();   // the last spin has ended at the hand: a short pull and the catch
        }

        // The whooshes (one every half turn, higher with speed) and the trail dust.
        void Effects(YoyoProjectile yoyo)
        {
            int step = (int)Math.Floor(angle / (MathHelper.TwoPi / Math.Max(1, A.AroundWhooshPerTurn)));
            if (step != lastWhoosh)
            {
                lastWhoosh = step;
                yoyo.Fx.SpinWhoosh(yoyo.Projectile.Center, speed01);
            }
            yoyo.Fx.SpinTrail(yoyo.Projectile.Center, yoyo.Vel, speed01);
        }

        // The offset from the hand of the yoyo at angle 'theta' and distance 'r': the circle is in the vertical plane, forward on the throwing side,
        // then DOWN, behind and over the top (clockwise on screen when facing right; mirrored when facing left, and for flipped gravity).
        Vector2 Offset(YoyoProjectile yoyo, float theta, float r)
        {
            return new Vector2(yoyo.Side * (float)Math.Cos(theta), yoyo.GravDir * (float)Math.Sin(theta)) * r;
        }

        // A point of the yoyo's path inside this tick: fraction 0 = where it was at the start of the tick, 1 = where it is now. The circle's
        // centre moves with the player, so the centre is blended between the two ticks too: the points of a trail then follow the real path
        // (no zigzag while the player runs) and the last one is exactly the yoyo.
        public Vector2 PathPoint(YoyoProjectile yoyo, float fraction)
        {
            Sample(Math.Max(0f, yoyo.OutTicks - 1 + fraction), out float theta, out float r, out _, out _);
            return Vector2.Lerp(pivotPrev, pivotNow, fraction) + Offset(yoyo, theta, r);
        }

        // The arm does not follow the yoyo round: it tilts up on the upper half of the circle and down on the lower half, and is only
        // raised while the yoyo is far out (it eases back to the catch pose as the yoyo spirals in).
        public override bool ArmPose(YoyoProjectile yoyo, out float localArmRadians)
        {
            float low = Math.Min(A.AroundStartRadiusPx, A.AroundCatchRadiusPx);
            float amount = MathHelper.Clamp((radius - low) / Math.Max(1f, FullRadius() - low), 0f, 1f);
            amount = amount * amount * (3f - 2f * amount);
            float s = (float)Math.Sin(angle);
            float tilt = (s < 0f ? A.AroundArmUpDeg : A.AroundArmDownDeg) * s;   // the yoyo is on the upper half while sin < 0 (the circle runs clockwise): up (negative) there, down (positive) on the lower half
            float pose = MathHelper.Lerp(T.CatchArmDeg, A.AroundArmBaseDeg + tilt, amount);
            localArmRadians = MathHelper.ToRadians(pose);
            return true;
        }

        // Rope: straight and tight all the way round.
        public override bool RopeTaut(YoyoProjectile yoyo)
        {
            return true;
        }

        public override float SpinStep(YoyoProjectile yoyo)
        {
            return yoyo.Side * A.AroundSpriteSpin * (0.4f + 0.6f * speed01);   // clockwise (positive) when facing right, like the circle
        }
    }
}
