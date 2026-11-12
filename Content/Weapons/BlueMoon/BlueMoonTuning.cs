using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.BlueMoon
{
    /// <summary>
    /// Every tunable number of the Blue Moon rework in one place (ticks: 60 = 1 second, distances in pixels: 16 = 1 tile). It overrides only
    /// what differs from the shared flail defaults (FlailTuning), so every number not listed here is the shared one. Properties instead of
    /// fields, so hot reload can tweak them.
    /// </summary>
    public class BlueMoonTuning : FlailTuning
    {
        public static BlueMoonTuning Instance { get; } = new BlueMoonTuning();

        // ---- the shared loop, Blue Moon flavor ----
        public override float ThrowSpeed(int tier) { return Pick(tier, 28f, 40f, 40f); }
        public override float SpinOmegaEnd(int tier) { return Pick(tier, 0.5f, 0.6f, 0.7f); }
        public override float SpecialMinReach => 6f * 16f;
        public override float SpecialMaxReach => 30f * 16f;
        public override float SpecialPathDamageMultiplier => 0.7f;  // of the tier 3 damage, for every pass of the head during the special
        public override int SpecialHangTicks => 0;                  // the head is never hidden: it is the one doing the hitting
        public override float RetractSpeed => 40f;
        public override int TrailLifeTicks => 16;

        public override float MinReach => 7f * 16f;         // distance hand to apex (the cursor), clamped to this range
        public override float MaxReach => 30f * 16f;

        // ---- tier 3 special: six very fast throws of the regular arc, alternating the regular and the mirrored one ----
        public int SpecialThrows => 6;
        public int SpecialThrowTicks => 10;              // one whole arc (out and back)
        public bool SpecialRetargets => true;            // each arc after the first aims at the cursor's position at that moment (false = all six use the release aim)
        public int SpecialHitCooldownTicks => 8;         // an enemy can be hit again by the next throw

        // ---- the water explosion on every enemy the special hits ----
        public float BlastRadius => 85f;
        public float BlastDamageMultiplier => 0.6f;      // of the tier 3 damage
        public float BlastKnockbackMultiplier => 1f;
        public int BlastLifeTicks => 16;
        public int BlastWetTicks => 240;

        // ---- looks ----
        public override Color GlowColor(int tier) { return new Color(50, 110, 255, 0); }
        public override Color HotColor(int tier) { return new Color(170, 225, 255, 0); }
        public override string ChainName => "Chain3";   // unverified: the vanilla Blue Moon chain texture; try other ChainN names if it is not the right one
    }
}