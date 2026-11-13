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
        //public override float ThrowSpeed(int tier) { return Pick(tier, 34f, 40f, 44f); }   // px per tick along the arc; the shared default is 26 / 32 / 40
        public override float TrailWidth(int tier) { return Pick(tier, 6f, 9f, 12f); }
        public override float SpinOmegaStart(int tier) { return Pick(tier, 0.18f, 0.18f, 0.18f); }  // radians per tick when the charge begins
        public override float SpinOmegaEnd(int tier) { return Pick(tier, 0.34f, 0.48f, 0.64f); }     // radians per tick when the charge is complete

        public override float MinReach => 5f * 16f;          // regular throws: distance hand to apex (the cursor), clamped to this range
        public override float MaxReach => 18f * 16f;         // the regular range: 12 tiles
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

        // ---- the shrapnel: the vanilla head fractured into a grid of pieces, thrown out and pulled down by gravity (as the Rally's shell shards) ----
        public int ShardColumns => 3;                        // the head sprite is cut into this many columns ...
        public int ShardRows => 3;                           // ... and rows; one piece each (3 x 3 = 9 pieces)
        public float ShardLaunchSpeedMin => 4f;              // how fast the pieces fly outward (a ring around the hit)
        public float ShardLaunchSpeedMax => 9f;
        public float ShardLaunchUp => 3f;                    // extra kick away from the feet side, so they arc before falling
        public float ShardGravity => 0.3f;
        public float ShardMaxFallSpeed => 12f;
        public float ShardAirDrag => 0.995f;                 // horizontal speed kept per tick in the air
        public float ShardBounceDamp => 0.38f;               // speed kept on a bounce (low = subtle)
        public float ShardRestSpeed => 1.2f;                 // a landing slower than this settles instead of bouncing
        public float ShardGroundFriction => 0.8f;            // horizontal speed kept per landing tick while it settles
        public float ShardSpin => 0.14f;                     // radians per tick, plus a little per pixel of speed
        public int ShardPierce => 3;                         // enemies one piece goes through before it breaks
        public int ShardLifeTicks => 150;                    // a piece that did not break fades away after this long
        public int ShardGraceTicks => 8;                     // the enemy the head exploded on cannot be hit by the pieces for this long (they fly out of it first)
        public float ShardBoxPx => 10f;
        public float ShardTrailWidth => 4f;                  // the thin steel streak behind a fast piece
        public int ShardTrailLifeTicks => 10;
        public float ShardTrailMinSpeed => 3f;               // no streak below this speed (settled pieces)

        // ---- looks ----
        public override Color GlowColor(int tier) { return new Color(150, 170, 205, 0); }
        public override Color HotColor(int tier) { return new Color(240, 245, 255, 0); }
        public override string ChainName => "Chain";   // unverified: the vanilla Mace chain texture under Terraria/Images; try ChainN names (Chain3, Chain4...) if it is not the right one
    }
}