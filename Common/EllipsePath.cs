using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The pure math of the "Forward Pass" ellipse: one whole loop that starts and ends at the thrower, goes out to a far end (the aim
    /// vector) along one side and comes back along the other half. Used by the yoyos' ForwardPassTrick and by the Combat Wrench. No
    /// state beyond the prepared shape, nothing synced: every machine prepares it from the same aim vector.
    /// </summary>
    public class EllipsePath
    {
        public Vector2 Axis;       // unit vector from the thrower toward the far end
        public Vector2 Bulge;      // unit vector across the axis: the side the outward half swings to
        public float HalfReach;    // half the length of the ellipse

        // Works out the ellipse (axis, half length, the side it bulges to) from 'aim' (the vector from the thrower to the far end).
        // gravDir 1 = normal gravity, -1 = flipped: the outward half bulges toward "up" for that gravity; aiming straight up or down it
        // bulges toward the throwing side (side 1 = right, -1 = left).
        public void Prepare(Vector2 aim, int gravDir, int side)
        {
            float length = Math.Max(1f, aim.Length());
            Axis = aim / length;
            HalfReach = length * 0.5f;
            Bulge = new Vector2(-Axis.Y, Axis.X);
            float lean = Vector2.Dot(Bulge, new Vector2(0f, -gravDir));
            if (Math.Abs(lean) < 0.15f)
                lean = Vector2.Dot(Bulge, new Vector2(side, 0f));
            if (lean < 0f)
                Bulge = -Bulge;
        }

        // The offset from the thrower at 'eased' (0..1, one whole ellipse). bulgeSign 1 = the usual (upper) side, -1 = the mirrored
        // (lower) side. widthRatio = the ellipse's half width as a fraction of its half length (read every call, so it can be tuned live).
        public Vector2 Offset(float eased, float bulgeSign, float widthRatio)
        {
            float theta = eased * MathHelper.TwoPi;
            return Axis * (HalfReach * (1f - (float)Math.Cos(theta)))
                 + Bulge * (bulgeSign * HalfReach * widthRatio * (float)Math.Sin(theta));
        }
    }
}