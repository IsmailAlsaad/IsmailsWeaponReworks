using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.Meatball
{
    /// <summary>
    /// Every tunable number of the Meatball rework in one place (ticks: 60 = 1 second, distances in pixels: 16 = 1 tile). It overrides only
    /// what differs from the shared flail defaults (FlailTuning), so every number not listed here is the shared one. Properties instead of
    /// fields, so hot reload can tweak them.
    /// </summary>
    public class MeatballTuning : FlailTuning
    {
        public static MeatballTuning Instance { get; } = new MeatballTuning();

        // ---- the shared loop, Meatball flavor ----
        public override float SpinOmegaEnd(int tier) { return Pick(tier, 0.5f, 0.6f, 0.7f); }
        public override float SpecialMinReach => 5f * 16f;
        public override float SpecialMaxReach => 30f * 16f;
        public override float SpecialPathDamageMultiplier => 0.6f;  // of the tier 3 damage, for the head itself on its way to the apex
        public override int SpecialHangTicks => 0;                  // the retract starts at once (the head is hidden for all of it, see MeatballFlailProjectile)
        public override float RetractSpeed => 48f;                  // the snapped chain whips back
        public override int TrailLifeTicks => 20;
        public override float MinReach => 7f * 16f;         // distance hand to apex (the cursor), clamped to this range
        public override float MaxReach => 25f * 16f;

        // ---- tier 3 special: the straight throw ----
        public float SpecialSpeed => 30f;                // px per tick, the head flying straight at the cursor
        public int RegrowTicks => 0;                     // the hand stays empty this long after the ball tears loose (0 = only while the chain is whipping back)

        // ---- the detached ball ----
        public float BallSpeed => 22f;                   // px per tick, straight on in the throw's direction
        public float BallRangePx => 30f * 16f;           // straight flight past the apex; after that the ball falls
        public float BallGravity => 0.45f;
        public float BallMaxFall => 16f;
        public int BallMaxLifeTicks => 600;              // still flying after this long: it goes off where it is
        public float BallHitRadius => 12f;               // half the size of the box that touches enemies
        public int MaxBalls => 3;                        // attached balls per player; a new one makes the oldest go off
        public float ImpactDamageMultiplier => 1f;       // of the tier 3 damage, dealt to the enemy the ball sticks to
        public float ImpactKnockbackMultiplier => 0.4f;

        // ---- the fuse: it swells and pulses faster and faster ----
        public int FuseTicks => 180;
        public float PulsePeriodStart => 42f;            // ticks per beat when the ball sticks
        public float PulsePeriodEnd => 2f;               // ticks per beat just before it goes off
        public float PulseCurve => 2f;                   // above 1: the beats stay slow for longer and speed up near the end
        public float SwellMax => 0.55f;                  // scale gained over the whole fuse
        public float PulseSwell => 0.35f;                // extra scale at the peak of a beat (grows with the fuse)
        public float ShakePx => 2.5f;                    // draw shake at the end of the fuse
        public int DripChanceStart => 14;                // one drip about every this many ticks at the start ...
        public int DripChanceEnd => 2;                   // ... and at the end

        // ---- the blood explosion ----
        public float BlastRadius => 140f;
        public float BlastDamageMultiplier => 2.4f;      // of the tier 3 damage, once for every enemy in the circle
        public float BlastKnockback => 6f;
        public int BleedTicks => 480;                    // 8 s of Bleeding
        public float ExplosionShake => 8f;
        public int SpentLingerTicks => 4;                // the ball stays this long after it went off, then every machine removes its copy
        public int ExplosionCores => 6;                  // big slow globs in the middle that merge
        public float ExplosionCoreSizeMin => 9f;
        public float ExplosionCoreSizeMax => 15f;
        public float ExplosionCoreSpeed => 2f;
        public int ExplosionDroplets => 70;              // droplets thrown out and braked by BloodDrag
        public float ExplosionSpeedMin => 4f;
        public float ExplosionSpeedMax => 11f;
        public float ExplosionDropMin => 2.5f;
        public float ExplosionDropMax => 5.5f;

        // ---- blood metaballs (the shared metaball tool, the Sunfury's and the Volcano's way) ----
        public float BloodMerge => 2.6f;                 // how far a droplet's field reaches (bigger = they bridge and merge more)
        public float BloodGravity => 0.12f;
        public float BloodDrag => 0.92f;                 // velocity kept per tick: low = thrown out and stopped quickly
        public float BloodHeat => 0.9f;                  // color heat at birth, cools to about 40 percent of it
        public float BloodGlow => 0.05f;

        // The blood fill by heat: near black, dark red, red, bright red, a wet highlight (the five hard bands of the metaball layer).
        public Color BloodFill(float heat)
        {
            Color[] stops =
            {
                new Color(40, 2, 8), new Color(105, 8, 16), new Color(170, 16, 26), new Color(215, 38, 44), new Color(245, 96, 92)
            };
            float x = MathHelper.Clamp(heat, 0f, 1f) * (stops.Length - 1);
            int i = Math.Min((int)x, stops.Length - 2);
            return Color.Lerp(stops[i], stops[i + 1], x - i);
        }

        // ---- looks ----
        public override Color GlowColor(int tier) { return new Color(200, 24, 34, 0); }
        public override Color HotColor(int tier) { return new Color(255, 120, 110, 0); }
        public override string ChainName => "Chain13";   // the vanilla Meatball chain texture (Chain13 per the wiki's chain id list)
    }
}