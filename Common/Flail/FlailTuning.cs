using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common.Flail
{
    /// <summary>
    /// Every number the shared flail loop (FlailProjectile) reads, in one place per flail (ticks: 60 = 1 second, distances in pixels:
    /// 16 = 1 tile). A flail derives from this and overrides only what differs; the defaults are the Sunfury's feel. Properties instead of
    /// fields, so hot reload can tweak them. Per-tier values are picked with Pick(tier, tier1, tier2, tier3).
    /// </summary>
    public abstract class FlailTuning
    {
        // ---- the rhythm loop ----
        public virtual int ChargeTicks(int tier) { return Pick(tier, 30, 54, 54); }      // hold this long to complete the tier's charge

        // ---- the elliptical throw (tiers 1 and 2) ----
        public virtual float MinReach => 7f * 16f;         // distance hand to apex (the cursor), clamped to this range
        public virtual float MaxReach => 40f * 16f;
        public virtual float EllipseWidthRatio => 0.42f;   // half-width of the ellipse divided by half its length
        public virtual float ThrowSpeed(int tier) { return Pick(tier, 26f, 32f, 40f); }  // px per tick along the path; this sets the retract window
        public virtual int ReleaseBlendTicks => 5;         // the head glides from the spin position onto the ellipse in this many ticks
        public virtual int MinHalfTicks => 10;

        // ---- damage (locked in the moment the tier's charge completes) ----
        public virtual float TierDamageMultiplier(int tier) { return Pick(tier, 1f, 1.7f, 2.6f); }
        public virtual int HitCooldownTicks => 10;
        public virtual float SpinWhooshAngle => MathHelper.TwoPi;  // the spin sound plays once per this much rotation (radians; a full circle = once per lap)
        public virtual float SpinHitPeriodFraction => 0.85f;  // an enemy can be hit again after this fraction of one full circle (below 1 so the next pass never arrives early)
        public virtual float SpinDamageMultiplier(int tier) { return Pick(tier, 0.5f, 0.5f, 0.5f); }  // of that tier's damage, for the head hurting enemies while it spins near you (0 = harmless spin)
        public virtual float HeadSize => 30f;

        // ---- the spinning head while charging ----
        public virtual float SpinRadiusMin => 18f;
        public virtual float SpinRadiusMax => 52f;
        public virtual float SpinRadiusSmoothing => 0.25f;
        public virtual float SpinOmegaStart(int tier) { return Pick(tier, 0.18f, 0.18f, 0.18f); }  // radians per tick when the charge begins
        public virtual float SpinOmegaEnd(int tier) { return Pick(tier, 0.55f, 0.65f, 0.75f); }     // radians per tick when the charge is complete

        // ---- tier 3 special (the flail's own code decides what it does; these are the shared parts) ----
        public virtual float SpecialMinReach => 5f * 16f;   // the cursor distance is clamped to this range (ai[2] for the special)
        public virtual float SpecialMaxReach => 40f * 16f;
        public virtual float SpecialPathDamageMultiplier => 0.6f;  // of the tier 3 damage, for the head itself during the special
        public virtual int SpecialHangTicks => 14;          // after the special the head stays hidden this long before it comes back (0 = none)
        public virtual float RetractSpeed => 34f;
        public virtual float CatchDistance => 30f;
        public virtual float FailRetractSpeed => 38f;

        // ---- looks ----
        public virtual float PixelSize => 2f;
        public virtual int TrailLifeTicks => 22;
        public virtual float TrailWidth(int tier) { return Pick(tier, 10f, 14f, 22f); }
        public virtual float TrailJitter => 0f;
        public virtual int AfterimageTicks => 12;
        public virtual int FlashTicks => 12;
        public abstract Color GlowColor(int tier);
        public abstract Color HotColor(int tier);
        public abstract string ChainName { get; }           // the vanilla chain texture under Terraria/Images (for example "Chain6")

        protected static T Pick<T>(int tier, T one, T two, T three)
        {
            return tier <= 1 ? one : tier == 2 ? two : three;
        }
    }
}