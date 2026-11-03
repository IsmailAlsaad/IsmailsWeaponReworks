using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.StormSpear
{
    /// <summary>
    /// Every tunable number of the Storm Spear rework in one place (ticks: 60 = 1 second, speeds in pixels per tick). Properties instead
    /// of fields, so hot reload can tweak them. One strong thrust that dashes the player toward the cursor like the Trident's, but the tip
    /// pierces everything (no bounce), the player cannot be hurt for the whole dash, a tile just stops the dash, an electric trail stays
    /// behind and every enemy hit is Electrified.
    /// </summary>
    public static class StormSpearTuning
    {
        // ---- the dash ----
        public static float DashSpeed => 18f;
        public static int DashTicks => 10;
        public static float GrazeCos => 0.3f;
        public static int ImmuneTicks => DashTicks;      // immune for the whole dash
        public static int ImmuneTailTicks => 30;         // and for half a second after it ends (also when a tile stops it early)
        public static float DamageMultiplier => 1.8f;    // balance this first (it pierces, so it hits everything in the path)
        public static int ElectrifiedTicks => 240;

        // ---- the thrust ----
        public static int WindupTicks => 4;
        public static int HoldTicks => 2;
        public static int RecoverTicks => 10;
        public static float PullBack => 0.3f;
        public static float EasePower => 2f;
        public static int CooldownTicks => 40;

        public static ThrustChain Chain => new ThrustChain(
            new ThrustTimeline(WindupTicks, DashTicks, HoldTicks, RecoverTicks, PullBack, EasePower), 1, 0, 0f, CooldownTicks);

        public static DashSettings Dash => new DashSettings(DashSpeed, DashTicks, false, 0f, 0, 0, ImmuneTicks, GrazeCos, 0.88f, ImmuneTailTicks);

        // ---- the spear ----
        public static float Reach => 30f;
        public static float SpearScale => 1f;
        public static float GripFraction => 0.5f;
        public static float HitWidth => 28f;

        // ---- look ----
        public static int TrailLifeTicks => 14;
        public static int TrailRails => 6;
        public static float TrailOpacity => 0.7f;
        public static float PixelSize => 2f;
        public static int BoltLifeTicks => 16;           // the lightning left behind the player fades over this long
        public static float BoltWidth => 8f;
        public static float BoltJag => 10f;              // how far a bolt wanders off its straight line (pixels)
        public static Color BoltCore => new Color(255, 255, 230);
        public static Color BoltGlow => new Color(120, 170, 255);
        public static ThrustPalette Palette => new ThrustPalette(Color.White, new Color(255, 240, 130), new Color(70, 120, 255), DustID.Electric, DustID.MagicMirror);
    }
}