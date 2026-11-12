using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.BallOHurt
{
    /// <summary>
    /// Every tunable number of the Ball O' Hurt rework in one place (ticks: 60 = 1 second, distances in pixels: 16 = 1 tile). It overrides
    /// only what differs from the shared flail defaults (FlailTuning). Properties instead of fields, so hot reload can tweak them.
    /// </summary>
    public class BallOHurtTuning : FlailTuning
    {
        public static BallOHurtTuning Instance { get; } = new BallOHurtTuning();

        // ---- the shared loop, Ball O' Hurt flavor ----
        public override float SpinOmegaEnd(int tier) { return Pick(tier, 0.5f, 0.6f, 0.7f); }
        public override float SpecialMinReach => 5f * 16f;
        public override float SpecialMaxReach => 30f * 16f;
        public override float SpecialPathDamageMultiplier => 0.6f;  // of the tier 3 damage, for the head on its straight throw
        public override int SpecialHangTicks => 0;
        public override float RetractSpeed => 48f;                  // the snapped chain whips back to the hand when the ball tears loose
        public override int AfterimageTicks => 18;                  // long afterimages: the loose ball is very fast
        public override int TrailLifeTicks => 18;
        public override float MinReach => 7f * 16f;         // distance hand to apex (the cursor), clamped to this range
        public override float MaxReach => 25f * 16f;

        // ---- tier 3 special: the straight throw ----
        public float SpecialSpeed => 34f;                // px per tick, the head flying straight at the cursor

        // ---- the loose ball ----
        public float BallSpeed => 40f;                   // px per tick, in every stage
        public float BallDamageMultiplier => 0.8f;       // of the tier 3 damage, for every hit of the loose ball
        public int BallHitCooldownTicks => 8;            // an enemy can be hit again by the ball after this many ticks
        public float BallBoxPx => 12f;                   // size of the box the ball bounces off tiles with
        public int FreeMaxTicks => 300;                  // no enemy hit this long after the tear-off: the ball goes home
        public int ResyncTicks => 20;                    // the owner re-syncs the ball's position this often
        public int MaxBalls => 2;                        // balls out at once per player: a new tear-off recalls the oldest one
        public float BallReturnSpeed => 40f;             // px per tick on the straight way home

        // ---- the hops between enemies ----
        public int MaxHops => 8;                         // enemies reached (the first one counts) before the ball goes home
        public float HopRangePx => 50f * 16f;            // a new target is picked among enemies this close to the ball
        public float ArriveRadiusPx => 20f;              // the ball has arrived when its path passes this close to the aim point
        public float ReboundPx => 8f * 16f;              // with one enemy only: the ball flies on through it this far, then comes back
        public int HopMaxTicks => 60;                    // a hop that takes this long counts as arrived (the target moved away)

        // ---- looks ----
        public override Color GlowColor(int tier) { return new Color(120, 45, 200, 0); }
        public override Color HotColor(int tier) { return new Color(185, 255, 110, 0); }
        public override string ChainName => "Chain2";
    }
}