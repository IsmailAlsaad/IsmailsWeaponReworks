using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Swordfish
{
    /// <summary>
    /// Every tunable number of the Swordfish rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload can
    /// tweak them. Two chains: the normal burst (more and quicker thrusts than the Spear) and the ENRAGED chain used while the player is
    /// in liquid (endless: it thrusts for as long as the button is held, with no cooldown in between).
    /// </summary>
    public static class SwordfishTuning
    {
        // ---- the normal burst ----
        public static int Thrusts => 5;
        public static int WindupTicks => 3;
        public static int ThrustTicks => 2;
        public static int HoldTicks => 1;
        public static int RecoverTicks => 3;
        public static float PullBack => 0.2f;
        public static float EasePower => 2f;
        public static int GapTicks => 0;
        public static float SpreadDeg => 10f;
        public static int CooldownTicks => 30;
        public static float DamageMultiplier => 0.7f;

        public static ThrustChain Chain => new ThrustChain(
            new ThrustTimeline(WindupTicks, ThrustTicks, HoldTicks, RecoverTicks, PullBack, EasePower),
            Thrusts, GapTicks, SpreadDeg, CooldownTicks);

        // ---- enraged (in liquid): endless, a little faster, a little wilder ----
        public static int RageWindupTicks => 2;
        public static int RageThrustTicks => 2;
        public static int RageHoldTicks => 1;
        public static int RageRecoverTicks => 2;
        public static float RagePullBack => 0.15f;
        public static float RageSpreadDeg => 14f;
        public static int RageCooldownTicks => 20; // only after the chain ends (the button released or the liquid left)
        public static float RageDamageMultiplier => 0.6f;

        public static ThrustChain RageChain => new ThrustChain(
            new ThrustTimeline(RageWindupTicks, RageThrustTicks, RageHoldTicks, RageRecoverTicks, RagePullBack, EasePower),
            0, GapTicks, RageSpreadDeg, RageCooldownTicks);

        // ---- the spear ----
        public static float Reach => 40f;
        public static float SpearScale => 1f;
        public static float GripFraction => 0.3f;
        public static float HitWidth => 22f;

        // ---- look ----
        public static int TrailLifeTicks => 12;
        public static int TrailRails => 5;
        public static float TrailOpacity => 0.7f;
        public static float PixelSize => 2f;
        public static ThrustPalette Palette => new ThrustPalette(Color.White, new Color(140, 220, 240), new Color(60, 120, 200), DustID.BlueTorch, DustID.MagicMirror);
        public static ThrustPalette RagePalette => new ThrustPalette(new Color(255, 235, 210), new Color(255, 120, 70), new Color(190, 40, 40), DustID.Torch, DustID.Blood);
    }
}