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
        public override float RetractSpeed => 40f;                  // the ball flies straight home after its last hop
        public override int AfterimageTicks => 18;                  // long afterimages: the loose ball is very fast
        public override int TrailLifeTicks => 18;

        // ---- tier 3 special: the straight throw ----
        public float SpecialSpeed => 34f;                // px per tick, the head flying straight at the cursor

        // ---- the loose ball ----
        public float BallSpeed => 40f;                   // px per tick, in every stage
        public float BallDamageMultiplier => 0.8f;       // of the tier 3 damage, for every hit of the loose ball
        public int BallHitCooldownTicks => 8;            // an enemy can be hit again by the ball after this many ticks
        public float BallBoxPx => 12f;                   // size of the box the ball bounces off tiles with
        public int FreeMaxTicks => 300;                  // no enemy hit this long after the tear-off: the ball goes home
        public int ResyncTicks => 20;                    // the owner re-syncs the ball's position this often

        // ---- the hops between enemies ----
        public int MaxHops => 8;                         // enemies reached (the first one counts) before the ball goes home
        public float HopRangePx => 50f * 16f;            // a new target is picked among enemies this close to the ball
        public float HopTurnRate => 0.38f;               // radians per tick the ball can turn toward its target
        public float HopSnapDistancePx => 100f;          // closer than this (and past the cooldown) the ball aims straight at the target
        public int ArriveCooldownTicks => 8;             // after reaching an enemy the ball cannot 'arrive' for this long (it flies on through)
        public int HopMaxTicks => 90;                    // a hop that takes this long picks a new target

        // ---- looks ----
        public override Color GlowColor(int tier) { return new Color(120, 45, 200, 0); }
        public override Color HotColor(int tier) { return new Color(185, 255, 110, 0); }
        public override string ChainName => "Chain2";
    }
}