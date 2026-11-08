using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// Every tunable number of the Enchanted Sword rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Angles are in degrees. The ghost's swing angles are relative to the line from the ghost's hand to the enemy's
    /// center: 0 = the blade points at the enemy, positive = the tip is raised (an "up" swing goes from -SwingArcDeg to +SwingArcDeg).
    /// </summary>
    public static class EnchantedSwordTuning
    {
        // ---- charge: true melee hits fill the sword, the last one discharges ----
        public static int HitsToDischarge => 3;         // the FIRST enemy each swing touches counts as one hit (a swing through a crowd = 1)
        public static int MaxStage => Math.Max(0, HitsToDischarge - 1); // outline stages shown on the blade (stage = hits so far)
        public static int HitHoldTicks => 300;          // the charge is lost this long after the last counted hit (the sword never cools while you keep hitting)

        // ---- damage: everything that decides what the ethereal sword does to an enemy ----
        public static float GhostDamageMultiplier => 0.6f;     // of the sword's damage, for EACH swing of the ghost (5 swings)
        public static float GhostKnockbackMultiplier => 0.5f;
        public static float HitLengthScale => 1.2f;     // how far the ghost's blade HITS along the blade, as a fraction of BladeLength (1.5 = 50% further). Does not change what you see
        public static float HitWidth => 16f;            // how thick the hit line is (pixels at scale 1; the line is that wide around the blade). Does not change what you see
        public static int SweepHitSteps => 4;           // the hit test is checked at this many poses between two ticks (a fast strike needs more)

        // ---- ghost: timeline (one continuous motion, no separate pieces) ----
        public static int GhostSwings => 5;             // up, down, thrust, up, down
        public static int SpawnTicks => 12;             // it fades in and glides in from further out
        public static int SwingTicks => 9;              // one whole swing, wind-up and settle included
        public static int FadeOutTicks => 14;           // after the last swing it drifts away and fades
        public static float AnticipateEnd => 0.3f;      // up and down: the wind-up ends here (fraction of one swing)
        public static float StrikeEnd => 0.85f;         // up and down: the strike (the only part that damages) ends here
        public static float ThrustWindEnd => 0.3f;      // thrust: the pull-back ends here
        public static float ThrustStrikeEnd => 0.6f;    // thrust: the jab (the only part that damages) ends here
        public static float StrikeEasePower => 2.2f;    // 1 = even speed; higher = faster start and a longer deceleration

        // ---- ghost: pose ----
        public static float SwingArcDeg => 65f;         // the blade sweeps from -this to +this
        public static float WindDeg => 18f;             // the wind-up pulls this far past the start of the arc
        public static float OvershootDeg => 10f;        // the strike carries this far past the end of the arc
        public static float SweepLungePx => 10f;        // a sweep pushes the hand this far toward the enemy at its fastest
        public static float ThrustPullPx => 20f;        // the thrust first pulls the hand back this far ...
        public static float ThrustReachPx => 34f;       // ... then jabs this far past the resting distance
        public static float HoverGap => 34f;            // resting distance of the hand from the edge of the enemy
        public static float SpawnRisePx => 60f;         // it glides in from (and drifts away to) this much further out
        public static float DriftDegPerSwing => 12f;    // the ghost circles the enemy by this much per swing
        public static float SideTiltDeg => 18f;         // the side it stands on is tilted up or down by up to this much (random)
        public static float BobPx => 3f;
        public static int BobTicks => 40;
        public static float FollowEase => 0.35f;       // how fast the ghost follows a moving enemy (per tick, 0..1)
        public static float RetargetRange => 480f;     // when its enemy dies it jumps to the nearest enemy within this range, else it leaves

        // ---- ghost: look ----
        public static float BladeLength => 64f;         // hand to tip, px: the beam sprite is scaled to this; tune it until the tip matches
        public static float GripFraction => 0.1f;       // the hand holds the sprite this fraction of its height up from the bottom
        public static float GhostAlpha => 0.85f;
        public static float GlowScale => 1.3f;
        public static float GlowAlpha => 0.45f;
        public static float GhostOutlineWidth => 1.5f;
        public static float ArmLength => 36f;           // the faint ghostly arm trailing from the hand
        public static float ArmWidth => 5f;
        public static float ArmAlpha => 0.35f;
        public static float PixelSize => 2f;

        // ---- ghost: trail (a SweepTrail fan along the blade, one per strike) ----
        public static int TrailLifeTicks => 14;
        public static int TrailRails => 7;
        public static float TrailOpacity => 0.85f;
        public static float TrailInner => 0.35f;        // the fan reaches down the blade to this fraction of its length
        public static float TrailOuter => 1f;           // and starts at this fraction (1 = the tip)
        public static float TrailLagTicks => 0.5f;      // the trail is laid this many ticks behind the blade
        public static int TrailSamplesPerTick => 3;

        // ---- the player's blade: the outline each counted hit adds ----
        public static float OutlineStrengthAt(int stage) => stage >= 2 ? 1f : 0.55f;
        public static float OutlineWidthAt(int stage) => stage >= 2 ? 2f : 1f;
        public static int OutlinePulseTicks => 36;
        public static float OutlinePulseMin => 0.7f;    // the pulse swings between this and 1 times the strength

        // ---- feel ----
        public static float ThrustShake => 2.5f;       // camera punch when the thrust connects
        public static float DischargeShake => 3f;

        // ---- colors (alpha 0 = additive) ----
        public static Color CoreColor => new Color(220, 245, 255, 0);
        public static Color BlueColor => new Color(110, 190, 255, 0);
        public static Color DeepColor => new Color(60, 100, 235, 0);
    }
}