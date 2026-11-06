using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Where a moving thing first touches a box. Extracted from the Thorn Chakram's latch when the Volcano's thrown sword needed the same
    /// contact point (the point it sticks to on an enemy). Pure math, no state.
    /// </summary>
    public static class Contact
    {
        // Where the line from 'from' to 'to' first enters the box (the point of contact); if it never does, the closest point of the box.
        public static Vector2 EntryPoint(Vector2 from, Vector2 to, Rectangle box)
        {
            Vector2 closest = new Vector2(MathHelper.Clamp(to.X, box.Left, box.Right), MathHelper.Clamp(to.Y, box.Top, box.Bottom));
            if (from == Vector2.Zero)
                return closest;
            Vector2 delta = to - from;
            float tMin = 0f;
            float tMax = 1f;
            if (Math.Abs(delta.X) < 0.0001f)
            {
                if (from.X < box.Left || from.X > box.Right)
                    return closest;
            }
            else
            {
                float a = (box.Left - from.X) / delta.X;
                float b = (box.Right - from.X) / delta.X;
                tMin = Math.Max(tMin, Math.Min(a, b));
                tMax = Math.Min(tMax, Math.Max(a, b));
            }
            if (Math.Abs(delta.Y) < 0.0001f)
            {
                if (from.Y < box.Top || from.Y > box.Bottom)
                    return closest;
            }
            else
            {
                float a = (box.Top - from.Y) / delta.Y;
                float b = (box.Bottom - from.Y) / delta.Y;
                tMin = Math.Max(tMin, Math.Min(a, b));
                tMax = Math.Min(tMax, Math.Max(a, b));
            }
            return tMin > tMax ? closest : from + delta * tMin;
        }
    }
}