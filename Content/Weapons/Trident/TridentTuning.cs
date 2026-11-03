using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Trident
{
    /// <summary>
    /// Every tunable number of the Trident rework in one place (ticks: 60 = 1 second, speeds in pixels per tick, 16 pixels = 1 tile).
    /// Properties instead of fields, so hot reload can tweak them. The Trident is ONE strong thrust that dashes the player toward the
    /// cursor and bounces them off the first enemy or tile; in liquid the dash lasts longer.
    /// </summary>
    public static class TridentTuning
    {
        // ---- the dash ----
        public static float DashSpeed => 18f;                  // pixels per tick (10 ticks = about 11 tiles)
        public static int DashTicks => 10;
        public static float LiquidDashMultiplier => 1.6f;      // the dash lasts this much longer while any liquid touches the player
        public static int LiquidDashTicks => Math.Max(1, (int)Math.Round(DashTicks * LiquidDashMultiplier));
        public static float BounceSpeed => 0.6f;               // the bounce keeps this much of the dash speed
        public static int HitStopTicks => 3;                   // the player freezes this long on impact
        public static int BounceTicks => 8;                    // how long the bounce flight is controlled before normal physics return
        public static float BounceDrag => 0.88f;               // the bounce slows by this factor every tick
        public static float GrazeCos => 0.3f;                  // a hit shallower than this (cosine to the surface normal) slides instead of bouncing
        public static float BounceShake => 5f;                 // camera punch on impact (owner only)
        public static float DamageMultiplier => 2.2f;          // the one thrust is worth this many vanilla hits (balance this first)

        // ---- the thrust (its strike phase is the dash) ----
        public static int WindupTicks => 4;
        public static int HoldTicks => 2;
        public static int RecoverTicks => 10;
        public static float PullBack => 0.3f;
        public static float EasePower => 2f;
        public static int CooldownTicks => 40;

        public static ThrustChain Chain => BuildChain(DashTicks);
        public static ThrustChain LiquidChain => BuildChain(LiquidDashTicks);

        static ThrustChain BuildChain(int thrustTicks)
        {
            return new ThrustChain(new ThrustTimeline(WindupTicks, thrustTicks, HoldTicks, RecoverTicks, PullBack, EasePower), 1, 0, 0f, CooldownTicks);
        }

        public static DashSettings Dash => new DashSettings(DashSpeed, DashTicks, true, BounceSpeed, HitStopTicks, BounceTicks, 0, GrazeCos, BounceDrag);
        public static DashSettings LiquidDash => new DashSettings(DashSpeed, LiquidDashTicks, true, BounceSpeed, HitStopTicks, BounceTicks, 0, GrazeCos, BounceDrag);

        // ---- gills and flipper (refreshed every tick while the head is under water and the Trident is held) ----
        public static int BuffRefreshTicks => 6;

        // ---- the spear ----
        public static float Reach => 30f;
        public static float SpearScale => 1f;
        public static float GripFraction => 0.5f;
        public static float HitWidth => 26f;

        // ---- look ----
        public static int TrailLifeTicks => 14;
        public static int TrailRails => 6;
        public static float TrailOpacity => 0.7f;
        public static float PixelSize => 2f;
        public static ThrustPalette Palette => new ThrustPalette(Color.White, new Color(90, 200, 255), new Color(30, 90, 200), DustID.BlueTorch, DustID.Water);
    }
}