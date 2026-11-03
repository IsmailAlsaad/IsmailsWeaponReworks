using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The shape of ONE thrust, as a pure function of the tick inside it: WINDUP (the spear is pulled back a little), THRUST (it shoots
    /// forward, fastest at the start and slowing at the end), HOLD (it stays out) and RECOVER (it eases back to the hand). Extension runs
    /// from slightly below 0 (pulled back) through 1 (fully out) back to 0. Only the THRUST and HOLD ticks are 'striking' (they damage).
    /// Being a pure function of the tick is what lets every machine mirror a thrust without any sync.
    /// </summary>
    public readonly struct ThrustTimeline
    {
        public readonly int WindupTicks;
        public readonly int ThrustTicks;
        public readonly int HoldTicks;
        public readonly int RecoverTicks;
        public readonly float PullBack;   // how far the windup pulls the spear back, as a fraction of the reach
        public readonly float EasePower;  // 1 = even speed; higher = faster start and a longer deceleration

        public ThrustTimeline(int windupTicks, int thrustTicks, int holdTicks, int recoverTicks, float pullBack, float easePower)
        {
            WindupTicks = Math.Max(0, windupTicks);
            ThrustTicks = Math.Max(1, thrustTicks);
            HoldTicks = Math.Max(0, holdTicks);
            RecoverTicks = Math.Max(0, recoverTicks);
            PullBack = pullBack;
            EasePower = Math.Max(1f, easePower);
        }

        public int TotalTicks => WindupTicks + ThrustTicks + HoldTicks + RecoverTicks;
        public int StrikeStart => WindupTicks;
        public int StrikeEnd => WindupTicks + ThrustTicks + HoldTicks;

        // Damaging ticks: the thrust itself and the hold.
        public bool IsStriking(int tick)
        {
            return tick >= StrikeStart && tick < StrikeEnd;
        }

        public float Extension(int tick)
        {
            if (tick < 0)
                return 0f;
            if (tick < WindupTicks)
                return -PullBack * MathHelper.SmoothStep(0f, 1f, (tick + 1f) / WindupTicks);

            int t = tick - WindupTicks;
            if (t < ThrustTicks)
            {
                float u = (t + 1f) / ThrustTicks;
                float eased = 1f - (float)Math.Pow(1f - u, EasePower);
                return MathHelper.Lerp(-PullBack, 1f, eased);
            }

            t -= ThrustTicks;
            if (t < HoldTicks)
                return 1f;

            t -= HoldTicks;
            if (t < RecoverTicks)
                return 1f - MathHelper.SmoothStep(0f, 1f, (t + 1f) / RecoverTicks);
            return 0f;
        }
    }
}