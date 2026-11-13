using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.FlamingMace
{
    /// <summary>
    /// Every tunable number of the Flaming Mace rework in one place (ticks: 60 = 1 second, distances in pixels: 16 = 1 tile). It overrides
    /// only what differs from the shared flail defaults (FlailTuning), so every number not listed here is the shared one. Properties
    /// instead of fields, so hot reload can tweak them.
    /// </summary>
    public class FlamingMaceTuning : FlailTuning
    {
        public static FlamingMaceTuning Instance { get; } = new FlamingMaceTuning();

        // ---- the shared loop, Flaming Mace flavor ----
        public override float MinReach => 7f * 16f;         // tiers 1 and 2: distance hand to apex (the cursor), clamped to this range
        public override float MaxReach => 18f * 16f;
        public override float SpecialMinReach => 5f * 16f;  // the special's own range
        public override float SpecialMaxReach => 30f * 16f;
        public override float SpecialPathDamageMultiplier => 0.7f;  // of the tier 3 damage, for every pass of the head during the special
        public override int SpecialHangTicks => 0;                  // the head is never hidden: it is the one doing the hitting
        public override float RetractSpeed => 40f;
        public override int TrailLifeTicks => 10;
        public override float TrailWidth(int tier) { return Pick(tier, 6f, 9f, 12f); }

        public override float SpinOmegaStart(int tier) { return Pick(tier, 0.18f, 0.18f, 0.18f); }  // radians per tick when the charge begins
        public override float SpinOmegaEnd(int tier) { return Pick(tier, 0.34f, 0.48f, 0.64f); }     // radians per tick when the charge is complete

        public override int AfterimageTicks => 5;

        // ---- tier 3 special: the regular throw, much faster, piercing, a small fireball on every enemy it passes through ----
        public float SpecialThrowSpeed => 46f;            // px per tick along the whole ellipse (the regular tier 3 throw is 40 out and 40 back, but only half the ellipse per leg)
        public int SpecialMinTicks => 12;                 // a very near cursor still takes this long
        public float SpecialHitCooldownFraction => 0.5f;  // an enemy can be hit again after this fraction of the flight (so out and back can both hit it, a double pass at the apex cannot)
        public int SpecialTrailLifeTicks => 10;
        public float SpecialTrailJitter => 2f;

        // ---- the small fireball on every enemy the special passes through ----
        public float BlastRadius => 70f;
        public float BlastDamageMultiplier => 0.5f;       // of the tier 3 damage
        public float BlastKnockbackMultiplier => 1f;
        public int BlastLifeTicks => 14;
        public int BlastBurnTicks => 60;
        public float BlastShake => 1f;

        // ---- every hit ----
        public int BurnTicks => 180;                      // On Fire for every enemy the head hits

        // ---- looks ----
        public override Color GlowColor(int tier) { return new Color(255, 110, 20, 0); }
        public override Color HotColor(int tier) { return new Color(255, 215, 110, 0); }
        public Color EmberColor => new Color(190, 40, 10, 0);
        public override string ChainName => "Chain42";     // unverified: the vanilla Flaming Mace chain texture; try other ChainN names if it is not the right one
    }
}