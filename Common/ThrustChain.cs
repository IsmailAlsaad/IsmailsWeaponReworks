using System;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// A burst of thrusts: how many (Count 0 = endless, used by the Swordfish's rage), the timeline of each, the gap between two thrusts,
    /// the angle variance and the cooldown after the burst. Like the timeline it is a pure function of the projectile's age, so the
    /// owner and every other machine know which thrust is running without any sync. For an endless chain the owner decides (one thrust
    /// ahead of time) when to stop and writes the end age into the projectile (see HeldSpearProjectile).
    /// </summary>
    public readonly struct ThrustChain
    {
        public readonly ThrustTimeline Timeline;
        public readonly int Count;
        public readonly int GapTicks;
        public readonly float SpreadDeg;
        public readonly int CooldownTicks;

        public ThrustChain(ThrustTimeline timeline, int count, int gapTicks, float spreadDeg, int cooldownTicks)
        {
            Timeline = timeline;
            Count = count;
            GapTicks = Math.Max(0, gapTicks);
            SpreadDeg = spreadDeg;
            CooldownTicks = Math.Max(0, cooldownTicks);
        }

        public bool Endless => Count <= 0;
        public int PeriodTicks => Math.Max(1, Timeline.TotalTicks + GapTicks);

        // The age at which a finite burst is over (the last thrust's recovery ends; the gap after it is not counted).
        public int FiniteTotalTicks => Endless ? int.MaxValue : Count * PeriodTicks - GapTicks;

        public bool IsFinished(int age)
        {
            return !Endless && age >= FiniteTotalTicks;
        }

        // Which thrust the age is in and how many ticks into it. Past the timeline's end (in the gap) the extension is simply 0.
        public void Locate(int age, out int index, out int tickInThrust)
        {
            int period = PeriodTicks;
            index = Math.Max(0, age) / period;
            if (!Endless && index >= Count)
                index = Count - 1;
            tickInThrust = age - index * period;
        }

        // The age at which thrust 'index' is fully recovered.
        public int EndOfThrust(int index)
        {
            return index * PeriodTicks + Timeline.TotalTicks;
        }
    }
}