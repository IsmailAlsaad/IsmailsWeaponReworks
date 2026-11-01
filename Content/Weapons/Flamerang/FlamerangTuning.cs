using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Flamerang
{
    /// <summary>
    /// Every tunable number of the Flamerang rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. The throw numbers deliberately match the Ice Boomerang's.
    /// </summary>
    public static class FlamerangTuning
    {
        // ---- throw (same as the Ice Boomerang) ----
        public static float ThrowSpeed => 16f;
        public static float MinThrowRange => 20f * 16f;
        public static float MaxThrowRange => MinThrowRange;
        public static int SettleTicks => 18;               // spin-down before the explosion at the end of the range
        public static float BrakeDistance => 160f;
        public static float BrakeMinSpeed => 2f;
        public static int FlightMaxTicks => 1800;          // safety net

        // ---- the fire explosion (end of range, and on an enemy or tile) ----
        public static float BlastRadius => 96f;
        public static float BlastDamageMultiplier => 1f;   // of the boomerang's damage
        public static int BlastLifeTicks => 8;

        // ---- molten boomerangs ----
        public static float MoltenDamageMultiplier => 1f;
        public static float MoltenArcSpeed => 13f;         // the two sideways ones
        public static float MoltenArcTurnRadians => 0.085f; // how much their velocity bends toward the player every tick
        public static int MoltenArcTicks => 32;            // then they home straight back
        public static float ReturnStartSpeed => 8f;
        public static float ReturnAcceleration => 1f;
        public static float ReturnMaxSpeed => 20f;
        public static float CatchDistance => 28f;
        public static int MoltenHitCooldownTicks => 12;    // infinite pierce: the same enemy can be hit again after this long
        public static float SpinSpeed => 0.5f;

        // ---- the fiery trail ----
        public static int TrailLifeTicks => 40;
        public static float TrailRadius => 18f;
        public static int BurnCheckInterval => 3;
        public static int BurnTicks => 240;                // 4 seconds of burning
        public static int BurnRefreshBelowTicks => 90;
        public static float TrailWidth => 12f;             // pixel line width of the outer glow (the hot core is half of it)
        public static float TrailJitter => 2.4f;           // ragged edge, in pixels
        public static float TrailBurnFade => 0.6f;         // the oldest part of the trail burns this much less far than the newest

        // ---- obsidian shards (contact explosion only) ----
        public static int ShardCount => 6;
        public static float ShardDamageMultiplier => 0.6f;
        public static float ShardMinSpeed => 4f;
        public static float ShardMaxSpeed => 9f;
        public static float ShardSpreadDeg => 70f;         // around straight up
        public static float ShardGravity => 0.3f;
        public static float ShardMaxFallSpeed => 12f;
        public static int ShardLingerTicks => 480;         // how long a shard lasts in total (8 seconds)
        public static int ShardFadeTicks => 40;
        public static int ShardCoolTicks => 70;            // glows like lava, then cools to dark obsidian
        public static int ShardHitCooldownTicks => 20;

        // ---- look ----
        public static float PixelSize => 2f;
        public static Color HotColor => new Color(255, 235, 160, 0);
        public static Color FireColor => new Color(255, 140, 40, 0);
        public static Color EmberColor => new Color(190, 50, 20, 0);
        public static Color ObsidianColor => new Color(55, 38, 85);
    }

    /// <summary>Sets enemies on fire without re-applying it every tick.</summary>
    public static class FlamerangBurn
    {
        public static void Apply(NPC npc)
        {
            int index = npc.FindBuffIndex(BuffID.OnFire);
            if (index >= 0 && npc.buffTime[index] > FlamerangTuning.BurnRefreshBelowTicks)
                return;
            npc.AddBuff(BuffID.OnFire, FlamerangTuning.BurnTicks); // vanilla syncs this to the server unless quiet
        }
    }
}