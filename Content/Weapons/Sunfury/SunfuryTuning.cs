using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>
    /// Every tunable number of the Sunfury rework (and of the flail rhythm loop it is the first user of) in one place
    /// (ticks: 60 = 1 second, distances in pixels: 16 = 1 tile). Properties instead of fields, so hot reload can tweak them.
    /// Per-tier values are picked with Pick(tier, tier1, tier2, tier3).
    /// </summary>
    public static class SunfuryTuning
    {
        // ---- the rhythm loop ----
        public static int ChargeTicks(int tier) { return Pick(tier, 30, 36, 42); }      // hold this long to complete the tier's charge
        public static int WindowEarlyGraceTicks => 0;     // a press this many ticks before the apex still counts as inside the window
        public static int WindowLateGraceTicks => 0;      // a press this many ticks after the head reached the hand still counts

        // ---- the elliptical throw (tiers 1 and 2) ----
        public static float MinReach => 7f * 16f;         // distance hand to apex (the cursor), clamped to this range
        public static float MaxReach => 22f * 16f;
        public static float EllipseWidthRatio => 0.42f;   // half-width of the ellipse divided by half its length
        public static float ThrowSpeed(int tier) { return Pick(tier, 12f, 17f, 17f); }  // px per tick along the path; this sets the retract window
        public static int ReleaseBlendTicks => 5;         // the head glides from the spin position onto the ellipse in this many ticks
        public static int MinHalfTicks => 10;

        // ---- damage (locked in the moment the tier's charge completes) ----
        public static float TierDamageMultiplier(int tier) { return Pick(tier, 1f, 1.7f, 2.6f); }
        public static int HitCooldownTicks => 10;
        public static float HeadSize => 30f;
        public static int BurnTicks => 180;               // On Fire from a head hit

        // ---- the spinning head while charging ----
        public static float SpinRadiusMin => 18f;
        public static float SpinRadiusMax => 52f;
        public static float SpinRadiusSmoothing => 0.25f;
        public static float SpinOmegaStart => 0.18f;      // radians per tick
        public static float SpinOmegaEnd => 0.75f;

        // ---- tier 3 special: straight throw, sun explosion, retract ----
        public static float SpecialSpeed => 30f;
        public static float SpecialMinReach => 5f * 16f;
        public static float SpecialMaxReach => 40f * 16f;
        public static bool SpecialStopsOnTiles => true;   // the head detonates where it hits a block instead of passing through
        public static float SpecialPathDamageMultiplier => 0.6f;  // of the tier 3 damage, for the head itself on its way out
        public static int SpecialHangTicks => 14;         // the head is gone (it IS the sun) this long before it comes back
        public static float RetractSpeed => 34f;
        public static float CatchDistance => 30f;
        public static float FailRetractSpeed => 38f;

        // ---- the sun explosion ----
        public static float BlastRadius => 230f;
        public static float BlastDamageMultiplier => 2.2f; // of the tier 3 damage
        public static float BlastKnockbackMultiplier => 2f;
        public static int BlastLifeTicks => 26;
        public static int BlastBurnTicks => 300;
        public static float ShakeStrength => 14f;

        // ---- looks ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 22;
        public static float TrailWidth(int tier) { return Pick(tier, 10f, 14f, 22f); }
        public static float TrailJitter => 1.8f;
        public static int AfterimageTicks => 12;
        public static int FlashTicks => 12;
        public static Color GlowColor(int tier) { return Pick(tier, new Color(255, 110, 30, 0), new Color(255, 160, 50, 0), new Color(255, 215, 110, 0)); }
        public static Color HotColor(int tier) { return Pick(tier, new Color(255, 200, 90, 0), new Color(255, 230, 140, 0), new Color(255, 250, 210, 0)); }
        public static Color ChainColor => new Color(86, 74, 68);
        public static Color ChainLightColor => new Color(150, 128, 112);

        static T Pick<T>(int tier, T one, T two, T three)
        {
            return tier <= 1 ? one : tier == 2 ? two : three;
        }
    }
}