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
        public static int ChargeTicks(int tier) { return Pick(tier, 30, 54, 54); }      // hold this long to complete the tier's charge

        // ---- the elliptical throw (tiers 1 and 2) ----
        public static float MinReach => 7f * 16f;         // distance hand to apex (the cursor), clamped to this range
        public static float MaxReach => 40f * 16f;
        public static float EllipseWidthRatio => 0.42f;   // half-width of the ellipse divided by half its length
        public static float ThrowSpeed(int tier)
        {
            return Pick(tier, 26, 32, 40);
            //return 32f;
        }  // px per tick along the path; this sets the retract window
        public static int ReleaseBlendTicks => 5;         // the head glides from the spin position onto the ellipse in this many ticks
        public static int MinHalfTicks => 10;

        // ---- damage (locked in the moment the tier's charge completes) ----
        public static float TierDamageMultiplier(int tier) { return Pick(tier, 1f, 1.7f, 2.6f); }
        public static int HitCooldownTicks => 10;
        public static float SpinDamageMultiplier(int tier) { return Pick(tier, 0.5f, 0.5f, 0.5f); }  // of that tier's damage, for the head hurting enemies while it spins near you (0 = harmless spin)
        public static float HeadSize => 30f;
        public static int BurnTicks => 180;               // On Fire from a head hit

        // ---- the spinning head while charging ----
        public static float SpinRadiusMin => 18f;
        public static float SpinRadiusMax => 52f;
        public static float SpinRadiusSmoothing => 0.25f;
        public static float SpinOmegaStart(int tier) { return Pick(tier, 0.18f, 0.18f, 0.18f); }  // radians per tick when the charge begins (per tier)
        public static float SpinOmegaEnd(int tier) { return Pick(tier, 0.55f, 0.65f, 0.75f); }     // radians per tick when the charge is complete (per tier)

        // ---- tier 3 special: straight throw, sun explosion, retract ----
        public static float SpecialSpeed => 50f;
        public static float SpecialMinReach => 5f * 16f;
        public static float SpecialMaxReach => 40f * 16f;
        public static bool SpecialStopsOnTiles => false;   // the head detonates where it hits a block instead of passing through
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
        public static float TrailJitter => 0f;
        public static int AfterimageTicks => 12;
        public static int FlashTicks => 12;
        public static Color GlowColor(int tier)
        {
            //return Pick(tier, new Color(255, 110, 30, 0), new Color(255, 160, 50, 0), new Color(255, 215, 110, 0));
            return new Color(255, 110, 30, 0);
        }
        public static Color HotColor(int tier) 
        {
            //return Pick(tier, new Color(255, 200, 90, 0), new Color(255, 230, 140, 0), new Color(255, 250, 210, 0)); 
            return new Color(255, 200, 90, 0);
        }
        public static string ChainName => "Chain6";

        // ---- fire metaballs (the shared metaball tool, the Volcano's look) ----
        public static float FireMerge => 2.6f;            // how far a droplet's field reaches (bigger = they bridge and merge more)
        public static float FireGlow => 0.1f;
        public static float FireGravity => 0.03f;
        public static float FireDrag => 0.9f;             // velocity kept per tick: low = thrown out and stopped quickly
        public static float FireHeat => 0.95f;            // color heat at birth, cools to about a quarter of it
        public static Color FireOutline => new Color(0, 0, 0, 0);

        // The fire fill by heat: dark ember, deep red, orange, amber, white-yellow (the five hard bands of the metaball layer).
        public static Color FireFill(float heat)
        {
            Color[] stops =
            {
                new Color(70, 14, 8), new Color(170, 35, 10), new Color(240, 95, 15), new Color(255, 165, 35), new Color(255, 240, 160)
            };
            float x = MathHelper.Clamp(heat, 0f, 1f) * (stops.Length - 1);
            int i = System.Math.Min((int)x, stops.Length - 2);
            return Color.Lerp(stops[i], stops[i + 1], x - i);
        }

        // charge-complete ring (per tier): a glob at the head and droplets flung outward
        public static int CompleteDroplets(int tier) { return Pick(tier, 12, 16, 22); }
        public static float CompleteSpeed(int tier) { return Pick(tier, 3f, 3.8f, 4.8f); }       // px per tick at birth
        public static float CompleteDropSize(int tier) { return Pick(tier, 2.6f, 3f, 3.6f); }    // droplet radius
        public static float CompleteCoreSize(int tier) { return Pick(tier, 5f, 6.5f, 8f); }      // the glob in the middle

        // the sun explosion: big slow globs that merge in the middle and droplets thrown out and braked by FireDrag
        public static int SunCores => 8;
        public static float SunCoreSizeMin => 14f;
        public static float SunCoreSizeMax => 24f;
        public static float SunCoreSpeed => 2.4f;
        public static int SunDroplets => 90;
        public static float SunSpeedMin => 6f;
        public static float SunSpeedMax => 22f;
        public static float SunDropMin => 3f;
        public static float SunDropMax => 7f;

        static T Pick<T>(int tier, T one, T two, T three)
        {
            return tier <= 1 ? one : tier == 2 ? two : three;
        }
    }
}