using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.RottedFork
{
    /// <summary>
    /// Every tunable number of the Rotted Fork rework in one place (ticks: 60 = 1 second, speeds in radians per tick, 16 pixels = 1 tile).
    /// Properties instead of fields, so hot reload can tweak them. Left click (hold) spins the fork around the player continuously;
    /// the right click javelin comes in patch 5.
    /// </summary>
    public static class RottedForkTuning
    {
        // ---- the spin ----
        public static float SpinSpeed => 0.3f;           // radians per tick at full speed (0.3 = about 3 turns a second)
        public static int RampTicks => 10;               // from the start speed up to full speed
        public static float StartSpeedFraction => 0.3f;  // the spin starts at this fraction of full speed
        public static int WindDownTicks => 10;           // after the button is released the spin slows to a stop over this long
        public static int MinSpinTicks => 14;            // a tap still spins at least this long before it may end
        public static int CooldownTicks => 12;           // after the spin ends, before the next one may start

        // ---- damage ----
        public static float DamageMultiplier => 0.8f;    // per hit, times the item's damage (balance by damage per second)
        public static int HitCooldownTicks => 14;        // the same enemy can be hit again after this long
        public static float MinHitSpeed => 0.35f;        // the fork only hurts while it is at least this fast (fraction of full speed)
        public static float HitWidth => 22f;             // thickness of the hit line (pixels at scale 1)

        // ---- the fork ----
        public static float SpearScale => 1.15f;
        public static float GripFraction => 0.45f;       // how much of the shaft sticks out behind the hand
        public static float Reach => 8f;                 // pixels the fork is pushed out from the hand along its direction

        // ---- look ----
        public static int TrailLifeTicks => 12;
        public static int TrailRails => 6;
        public static float TrailOpacity => 0.8f;
        public static float TrailInnerFraction => 0.6f;  // how far down the shaft (from the tip toward the butt) the fan reaches
        public static float PixelSize => 2f;
        public static ThrustPalette Palette => new ThrustPalette(new Color(255, 200, 185), new Color(205, 50, 60), new Color(110, 15, 35), DustID.Blood, DustID.RedTorch);
    }
}