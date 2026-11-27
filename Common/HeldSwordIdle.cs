using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The shared "sword held out, waiting" pose of a held-projectile sword (the charged blades and every IdleSword projectile use it): the
    /// arm eases into a ready pose and bobs a little, and the HAND follows the walking bob of the player's body (vanilla draws the body 2 px
    /// higher on some frames of the walk cycle and the arm goes with it, but the hand position it reports does not, so without
    /// WalkShift a sword looks nailed to the air while the player walks). Pure pose math: no state of its own, the caller keeps the angles.
    /// </summary>
    public static class HeldSwordIdle
    {
        // How many pixels the body rises on the raised frames of the walk cycle, and how much of that the sword copies (1 = all of it).
        public static float WalkBobPixels => 2f;
        public static float WalkBobScale => 1f;
        // How fast the sword follows the body (per tick, 0..1): below 1 it smooths the 2 px steps into a soft bob.
        public static float WalkBobEase => 0.4f;

        // The frames of the walk cycle (bodyFrame row) in which the body is drawn higher: 7 to 9 and 14 to 16.
        public static bool IsRaisedFrame(int frame)
        {
            return (frame >= 7 && frame <= 9) || (frame >= 14 && frame <= 16);
        }

        // The hand's offset this tick (world pixels, already flipped for reversed gravity). 'smoothed' is the caller's own float that remembers
        // the previous tick's value; set it to 0 when the idle pose is not shown, so it starts calm.
        public static Vector2 WalkShift(Player owner, ref float smoothed)
        {
            float target = 0f;
            int height = owner.bodyFrame.Height;
            if (height > 0 && IsRaisedFrame(owner.bodyFrame.Y / height))
                target = -WalkBobPixels * WalkBobScale;
            smoothed += (target - smoothed) * MathHelper.Clamp(WalkBobEase, 0.01f, 1f);
            return new Vector2(0f, smoothed * owner.gravDir);
        }

        // One tick of the ready pose: the (facing-local) arm and blade tilt ease toward the ready angles, the arm also swaying slowly up and
        // down by bobRad over bobTicks. The angles are the caller's own fields; they may start anywhere (right after a swing) and settle in.
        // Above this speed (pixels per tick) the player counts as moving: the slow breathing sway is off then (the walking bob already moves the sword).
        public static float MovingSpeed => 0.1f;
        // How fast the sway fades out when the player starts moving and back in when they stop (per tick, 0..1).
        public static float SwayEase => 0.1f;

        // 'sway' is the caller's own float (start it at 1): 1 = the breathing sway is fully on (standing still), 0 = off (moving).
        public static void Step(Player owner, int ticks, float readyArmRad, float readyTiltRad, int bobTicks, float bobRad, float ease, ref float armLocal, ref float tiltRad, ref float sway)
        {
            bool moving = owner.velocity.LengthSquared() > MovingSpeed * MovingSpeed;
            sway += ((moving ? 0f : 1f) - sway) * MathHelper.Clamp(SwayEase, 0.01f, 1f);
            float bob = (float)System.Math.Sin(MathHelper.TwoPi * ticks / System.Math.Max(1, bobTicks)) * bobRad * sway;
            float e = MathHelper.Clamp(ease, 0.01f, 1f);
            armLocal += MathHelper.WrapAngle(readyArmRad + bob - armLocal) * e;
            tiltRad += (readyTiltRad - tiltRad) * e;
        }
    }
}