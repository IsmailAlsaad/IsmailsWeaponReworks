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
        public static float Reach => 24f;                 // pixels the fork is pushed out from the hand along its direction
        public static int ArmSwitchesPerTurn => 2;        // the arm flips between raised and lowered this many times per full turn of the fork
        public static float ArmUpAngle => -1.4f;          // the raised arm (radians in the player's own frame: 0 = forward, negative = up)
        public static float ArmDownAngle => 1.0f;         // the lowered arm (positive = down)

        // ---- the javelin (right click) ----
        public static float JavelinSpeed => 16f;            // pixels per tick (42 ticks = about 42 tiles)
        public static int JavelinLifeTicks => 42;           // the flight ends (in an explosion) after this long if nothing stops it
        public static float JavelinDamageMultiplier => 2f;  // the pierce hit, times the item's damage
        public static float JavelinHitWidth => 20f;         // thickness of the hit line (pixels at scale 1)
        public static int JavelinCooldownTicks => 10;       // after the explosion, before the fork can be used again
        public static int MaxImpaled => 3;                  // enemies carried at once
        public static float MaxCarrySize => 72f;            // an enemy bigger than this (width or height) is pierced, not impaled
        public static float CarryFollow => 0.55f;           // how fast an impaled enemy slides into its slot (0 to 1 per tick)
        public static float CarryFrontGap => 8f;            // from the tip to the front edge of the first impaled enemy
        public static float CarryGap => 4f;                 // between two impaled enemies
        public static float BlastRadius => 60f;            // the explosion at the tip hits every enemy this close
        public static float BlastDamageMultiplier => 0.75f; // of the javelin hit's damage
        public static float CarriedBlastMultiplier => 1.5f; // extra damage for each impaled enemy
        public static float BlastKnockback => 7f;
        public static float CarriedKnockback => 16f;        // speed (pixels per tick) an impaled enemy is flung at along the flight direction, times its knockback resistance factor
        public static float BlastShake => 3f;               // camera punch of the explosion (nearby players)

        // ---- look ----
        public static int TrailLifeTicks => 12;
        public static int TrailRails => 6;
        public static float TrailOpacity => 0.8f;
        public static float TrailInnerFraction => 0.6f;  // how far down the shaft (from the tip toward the butt) the fan reaches
        public static float PixelSize => 2f;
        public static float JavelinTrailTaper => 0.7f;   // the oldest part of the javelin streak is a point that widens to full width this far (0 to 1) along it
        public static ThrustPalette Palette => new ThrustPalette(new Color(255, 200, 185), new Color(205, 50, 60), new Color(110, 15, 35), DustID.Blood, DustID.RedTorch);
    }
}