using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The numbers of the Forward Pass pattern, with defaults (see IWalkTheDogTuning for how a yoyo uses them).
    /// </summary>
    public interface IForwardPassTuning
    {
        int ForwardPassTicks => 36;               // one whole pass (out along the ellipse and back to the hand)
        float ForwardPassWidthRatio => 0.42f;     // the ellipse's half width as a fraction of its half length (0.42 = a slim ellipse)
        float ForwardPassReachMargin => 30f;      // the far end is the string's full length minus this
        float ForwardPassEase => 0.35f;           // 0 = constant speed along the ellipse, 1 = slow at both ends
        bool ForwardPassThroughPlatforms => true; // platforms and other top-solid blocks do not stop it
        bool ForwardPassBlockedReturnHurts => true; // a block that stops it (in either half) does not make the way back harmless: it keeps hurting while it is pulled to the hand
    }

    class ForwardPassDefaults : IForwardPassTuning
    {
    }

    /// <summary>
    /// Forward Pass (pattern; Code 1's left click): the yoyo is thrown along an ellipse toward the cursor and comes back along the other
    /// half of the same ellipse, one whole loop that starts and ends at the player. It always goes the full distance (the string's length
    /// minus a margin) in the direction of the cursor, wherever the cursor is. Gravity does not touch it. The outward half bulges to the
    /// upper side. It moves by following the player, so running does not break the loop. Solid blocks stop it (it does not pass through
    /// them: impact dust, then it is pulled back), but platforms and other top-solid blocks do not. It hurts everything on the way (an
    /// enemy can be hit again after the hit cooldown), and it is not cut short by letting go of the button. Holding the button throws
    /// the next pass after the catch. Another yoyo uses it as it is, or subclasses it.
    /// MULTIPLAYER: the owner decides the aim (the vector to the far end, YoyoProjectile.Aim, travels in the existing extra sync), every
    /// machine then moves the yoyo along the same ellipse and stops it at blocks itself; the owner's client alone decides when it returns.
    /// </summary>
    public class ForwardPassTrick : YoyoTrick
    {
        protected readonly IForwardPassTuning P;
        protected Vector2 axis;       // unit vector from the player toward the far end
        protected Vector2 bulge;      // unit vector across the axis: the side the outward half swings to
        protected float halfReach;    // half the length of the ellipse
        protected bool stopped;       // a block stopped it on this machine; it waits for the owner's return
        protected bool comingBack;    // the second half of the ellipse: the string is pulled taut
        protected bool blockedComingBack; // a block stopped it on the way back: it keeps its damage hitbox while it is pulled to the hand

        public ForwardPassTrick(YoyoTuning tuning) : base(tuning)
        {
            P = tuning as IForwardPassTuning ?? new ForwardPassDefaults();
        }

        public override string Name => "Forward Pass";
        public override int MaxHits => int.MaxValue;
        public override bool RetractsOnRelease => false;

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            stopped = false;
            comingBack = false;
            blockedComingBack = false;
            if (authoritative)
            {
                Player owner = yoyo.Owner;
                Vector2 toCursor = Main.MouseWorld - owner.MountedCenter;
                float reach = System.Math.Max(48f, T.RopeLength() - P.ForwardPassReachMargin);   // always the full distance
                Vector2 direction = toCursor.LengthSquared() > 1f ? Vector2.Normalize(toCursor) : new Vector2(yoyo.Side, 0f);
                yoyo.Aim = direction * reach;
            }
            PrepareEllipse(yoyo);
            yoyo.Vel = Vector2.Zero;
        }

        // Works out the ellipse (axis, half length, the side it bulges to) from yoyo.Aim. A pattern that throws more than one pass (Pop the Clutch) calls it itself.
        protected void PrepareEllipse(YoyoProjectile yoyo)
        {
            float length = System.Math.Max(1f, yoyo.Aim.Length());
            axis = yoyo.Aim / length;
            halfReach = length * 0.5f;
            bulge = new Vector2(-axis.Y, axis.X);
            float lean = Vector2.Dot(bulge, new Vector2(0f, -yoyo.GravDir));  // toward "up" for this player's gravity
            if (System.Math.Abs(lean) < 0.15f)
                lean = Vector2.Dot(bulge, new Vector2(yoyo.Side, 0f));        // aiming straight up or down: bulge toward the throwing side
            if (lean < 0f)
                bulge = -bulge;
        }

        // The yoyo's offset from the player's centre at 'eased' (0..1, one whole ellipse) along the prepared ellipse. bulgeSign 1 = the usual
        // (upper) side, -1 = the mirrored (lower) side.
        protected Vector2 EllipseOffset(float eased, float bulgeSign)
        {
            float theta = eased * MathHelper.TwoPi;
            return axis * (halfReach * (1f - (float)System.Math.Cos(theta)))
                 + bulge * (bulgeSign * halfReach * P.ForwardPassWidthRatio * (float)System.Math.Sin(theta));
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            if (stopped)
            {
                yoyo.Vel = Vector2.Zero;
                return;
            }

            float t = MathHelper.Clamp(yoyo.OutTicks / (float)System.Math.Max(1, P.ForwardPassTicks), 0f, 1f);
            float eased = MathHelper.Lerp(t, t * t * (3f - 2f * t), P.ForwardPassEase);
            comingBack = eased > 0.5f;
            Vector2 target = yoyo.Owner.MountedCenter + EllipseOffset(eased, 1f);

            YoyoContact contact = yoyo.Move(target - yoyo.Projectile.Center, P.ForwardPassThroughPlatforms);
            if (contact.Blocked)
            {
                // A solid block in the way: it does not pass through. Impact dust, then the owner pulls it back.
                stopped = true;
                blockedComingBack = P.ForwardPassBlockedReturnHurts;   // a block in either half: the pull back to the hand still hurts
                yoyo.Fx.Impact(yoyo.Projectile.Center, contact.Normal, contact.Impact);
                if (yoyo.IsOwner)
                    yoyo.BeginReturn();
                return;
            }
            if (t >= 1f && yoyo.IsOwner)
                yoyo.BeginReturn();   // the loop is done: a short pull to the hand and the catch
        }

        public override bool RopeTaut(YoyoProjectile yoyo)
        {
            return comingBack;   // the way back along the ellipse: straight and tight, like the return
        }

        public override bool HurtsWhileReturning(YoyoProjectile yoyo)
        {
            return blockedComingBack;   // stopped by a block on the way back: still dangerous until it is caught
        }

        public override float SpinStep(YoyoProjectile yoyo)
        {
            return yoyo.Side * yoyo.Vel.Length() * T.SpinPerPixel;
        }
    }
}