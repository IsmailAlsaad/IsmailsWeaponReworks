using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Mace
{
    /// <summary>
    /// Every tunable number of the Mace rework in one place (ticks: 60 = 1 second, distances in pixels: 16 = 1 tile). It overrides only
    /// what differs from the shared flail defaults (FlailTuning), so every number not listed here is the shared one. Properties instead of
    /// fields, so hot reload can tweak them.
    /// </summary>
    public class MaceTuning : FlailTuning
    {
        public static MaceTuning Instance { get; } = new MaceTuning();

        // ---- the shared loop, Mace flavor: heavier and faster throws, short reach ----
        public override float ThrowSpeed(int tier) { return Pick(tier, 34f, 40f, 44f); }   // px per tick along the arc; the shared default is 26 / 32 / 40
        public override float SpinOmegaEnd(int tier) { return Pick(tier, 0.55f, 0.65f, 0.75f); }
        public override float MinReach => 5f * 16f;          // regular throws: distance hand to apex (the cursor), clamped to this range
        public override float MaxReach => 12f * 16f;         // the regular range: 12 tiles
        public override float RetractSpeed => 44f;           // the chain whipping home after the head exploded
        public override int TrailLifeTicks => 16;
        public override int AfterimageTicks => 14;

        // ---- damage ----
        public override float TierDamageMultiplier(int tier) { return Pick(tier, 1f, 1.7f, 2.6f); }
        public override float SpecialPathDamageMultiplier => 0.7f;  // of the tier 3 damage, for the head's own hit before it explodes
        public float ShardDamageMultiplier => 0.8f;                 // of the tier 3 damage, for every hit of every shard
        public float ShardKnockbackMultiplier => 0.6f;              // of the head's knockback

        // ---- tier 3 special: the regular arc, only faster and longer; the first enemy hit makes the head explode ----
        public override float SpecialMinReach => 8f * 16f;
        public override float SpecialMaxReach => 20f * 16f;  // the special range: 20 tiles
        public override int SpecialHangTicks => 0;
        public float SpecialSpeed => 54f;                    // px per tick along the whole arc (out and back); the regular tier 3 throw would be 44
        public int SpecialMinTicks => 9;                     // the arc never takes fewer ticks than this

        // ---- the shrapnel ----
        public int ShardCount => 9;                          // thrown out in a ring (a random start angle, a little jitter)
        public float ShardAngleJitter => 0.18f;              // radians, per shard
        public float ShardSpeedMin => 11f;                   // px per tick at the start
        public float ShardSpeedMax => 17f;
        public float ShardDrag => 0.985f;                    // speed kept per tick
        public int ShardPierce => 3;                         // enemies one shard goes through before it breaks (a tile or the end of its life breaks it too)
        public int ShardLifeTicks => 36;
        public int ShardGraceTicks => 8;                     // the enemy the head exploded on cannot be hit by the shards for this long (they fly out of it first)
        public float ShardBoxPx => 10f;
        public float ShardTrailWidth => 5f;
        public int ShardTrailLifeTicks => 12;

        // ---- looks ----
        public override Color GlowColor(int tier) { return new Color(150, 170, 205, 0); }
        public override Color HotColor(int tier) { return new Color(240, 245, 255, 0); }
        public override string ChainName => "Chain";   // unverified: the vanilla Mace chain texture under Terraria/Images; try ChainN names (Chain3, Chain4...) if it is not the right one
    }
}