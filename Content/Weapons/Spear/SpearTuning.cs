using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Spear
{
    /// <summary>
    /// Every tunable number of the Spear rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload can
    /// tweak them. One burst = Thrusts thrusts of (Windup + Thrust + Hold + Recover) ticks each, then a cooldown.
    /// </summary>
    public static class SpearTuning
    {
        // ---- the burst ----
        public static int Thrusts => 3;
        public static int WindupTicks => 4;      // the spear is pulled back a little
        public static int ThrustTicks => 3;      // it shoots out
        public static int HoldTicks => 2;        // it stays out (the thrust and the hold are the only damaging ticks)
        public static int RecoverTicks => 5;     // it eases back to the hand
        public static float PullBack => 0.25f;   // the windup's pull-back, as a fraction of the reach
        public static float EasePower => 2.2f;   // higher = a harder start and a longer slow-down
        public static int GapTicks => 0;         // extra pause between two thrusts
        public static float SpreadDeg => 12f;    // each thrust points up to this far off the cursor, leaning the other way each time
        public static int CooldownTicks => 30;   // after the burst
        public static float DamageMultiplier => 0.8f; // per thrust (three thrusts per burst, so the burst stays near vanilla damage per second)

        public static ThrustChain Chain => new ThrustChain(
            new ThrustTimeline(WindupTicks, ThrustTicks, HoldTicks, RecoverTicks, PullBack, EasePower),
            Thrusts, GapTicks, SpreadDeg, CooldownTicks);

        // ---- the spear ----
        public static float Reach => 36f;        // pixels the spear is pushed forward at full extension
        public static float SpearScale => 1f;
        public static float GripFraction => 0.35f; // how much of the shaft sticks out behind the hand at rest
        public static float HitWidth => 22f;

        // ---- look ----
        public static int TrailLifeTicks => 12;
        public static int TrailRails => 5;
        public static float TrailOpacity => 0.7f;
        public static float PixelSize => 2f;
        public static ThrustPalette Palette => new ThrustPalette(Color.White, new Color(190, 215, 235), new Color(120, 150, 190), DustID.MagicMirror, DustID.Smoke);
    }
}