using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.BladeOfGrass
{
    /// <summary>
    /// Every tunable number of the Blade of Grass rework in one place (ticks: 60 = 1 second, lengths in pixels: 16 = 1 tile).
    /// Properties instead of fields, so hot reload can tweak them.
    /// </summary>
    public static class BladeOfGrassTuning
    {
        // ---- sword and pose ----
        public static float BladeLength => 46f;        // hand to tip, in pixels at scale 1 (adjust to the real sprite)
        public static float SwordScale => 1f;
        public static float GripOffset => 4f;          // where the hand holds the sword: pixels up from the end of the handle, at scale 1 (visual only)

        // ---- the charge: holding makes the blade shake more and more ----
        public static int ChargeTicks => 120;          // how long the shake takes to reach its maximum
        public static float ShakeCurve => 2f;          // 1 = linear, 2 = the shake stays small for a while and then rises fast
        public static float ShakeArmDeg => 1f;         // arm jitter at full charge
        public static float ShakeHandPx => 0.5f;       // hand jitter at full charge
        public static int ChargeDustInterval => 6;     // a leaf off the blade this often at full charge (rarer when less charged)

        // ---- the vines ----
        public static int VineCount => 3;
        public static int PointsPerVine => 18;         // points of each rope (more = smoother and more lively)
        public static float RootStart => 0.45f;        // where along the blade the first vine grows from (fraction of BladeLength)
        public static float RootEnd => 0.9f;           // and the last
        public static int StartTicks => 18;            // the first vine starts growing after this many ticks of holding
        public static int StaggerTicks => 10;          // each next vine starts this much later
        public static int GrowTicks => 360;            // ticks of holding (after its start) for a vine to reach MaxLength
        public static float MinLength => 70f;          // length a vine starts with
        public static float MaxLength => 460f;         // the longest a vine gets (about 29 tiles)
        public static float GrowSpeed => 1.5f;           // how fast a vine gets longer, pixels per tick (also how fast it regrows after a cut)
        public static float SpreadDeg => 9f;           // the three vines fan out by this much around the cursor direction
        public static float FreeSlackPx => 16f;        // a free vine shows this much more than the straight distance to its goal (room for the wave); it only shows what it needs to put its tip on the cursor

        // ---- the rope: how lively the vines are ----
        public static float Damping => 0.93f;          // velocity kept per tick
        public static float Stiffness => 0.16f;        // how hard each point is pulled toward where the vine wants to be
        public static float Gravity => 0.08f;
        public static float WrigglePx => 8f;          // sideways wave of a free vine (pixels)
        public static float WriggleSpeed => 0.22f;     // radians per tick
        public static float WriggleWaves => 4f;        // radians of wave along the whole vine
        public static float WriggleTargetFactor => 0.4f; // the wave shrinks to this fraction while a vine has a target
        public static float WriggleChargeBoost => 1.5f;  // extra wave at full charge (the vines get wilder)
        public static float MaxPointSpeed => 40f;      // a point never moves faster than this per tick (keeps a quick player move from flinging the rope)
        public static int ConstraintPasses => 2;
        public static float RootInfluenceFloor => 0.1f; // how strongly the point next to the root follows the vine's world goal (the rest follows the root through the rope)
        public static float TipInfluencePower => 1f; // the pull toward the goal grows with this power toward the tip (higher = the tip is freer)
        public static float TargetStiffnessBoost => 2f; // the whole vine is pulled harder toward its goal while it reaches for an enemy

        // ---- targeting ----
        public static float TargetRadius => 200f;      // an enemy this close to the cursor (pixels) can be targeted
        public static float ReachSlack => 40f;         // an enemy may be this much farther than the vine's current length and still count as in reach
        public static float LoseTargetFactor => 1.5f;  // a target is dropped when the cursor moves farther than TargetRadius times this from it
        public static int RetargetInterval => 5;       // ticks between target searches
        public static float SamePenalty => 100000f;    // added to an enemy's score per vine already on it, so vines spread out; the same enemy is only reused when nothing else is in reach

        // ---- looping and breaking off ----
        public static int LoopTicks => 36;             // how long a vine circles an enemy before the section breaks off
        public static float LoopSpeed => 0.22f;        // radians per tick the tip circles
        public static float WrapRadiusScale => 0.55f;  // circle radius = the enemy's larger side times this ...
        public static float WrapRadiusPad => 10f;      // ... plus this
        public static float WrapTighten => 0.35f;      // the circle shrinks by this fraction over the loop
        public static float WrapTurns => 1.25f;        // how many turns of the circle the end of the vine lays around the enemy
        public static int WrapSpanTicks => 14;         // the coil winds on over this many ticks after the vine arrives
        public static float WrapSpiral => 0.15f;       // the first turn is this much wider than the tip's turn (the coil tapers)
        public static float WrapSlackPx => 14f;        // extra vine on show beyond the exact distance plus coil length
        public static float ShownEase => 0.25f;        // how fast the vine on show shortens/lengthens while it reaches for an enemy
        public static float CoilStiffness => 0.45f;    // how hard the coiled end is pulled onto the circle
        public static int WrapCooldownTicks => 25;     // a vine waits this long after breaking off before it targets again
        public static float MaxWrapFraction => 0.85f;  // enemy at the player: this fraction of the vine breaks off
        public static float MinWrapFraction => 0.3f;   // enemy at MaxLength: this fraction
        public static float MinWrapPixels => 60f;
        public static float MinRemainPixels => 30f;    // the vine never breaks off shorter than this

        // ---- the spiny section stuck on the enemy ----
        public static int SegmentLifeTicks => 360;
        public static int SegmentFadeTicks => 24;      // the coil unwinds over the last ticks
        public static int HitIntervalTicks => 12;      // damage tick of one section
        public static float SegmentDamageScale => 0.2f; // damage of one tick, as a fraction of the weapon's damage
        public static float SegmentMinLengthFactor => 0.7f; // a short section deals this times the scale ...
        public static float SegmentMaxLengthFactor => 1.5f; // ... and a section as long as MaxLength this times
        public static int MaxSegments => 12;           // per player; the oldest is removed first
        public static float EnemyCapFraction => 1f;    // all sections on ONE enemy together are at most this fraction of MaxLength (1 = one full vine); a new section is cut to the room that is left
        public static float MinEnemyRoomPx => 40f;     // an enemy with less room than this left under the cap is left alone: no vine targets it until some sections expire
        public static int CoilGrowTicks => 14;         // the coil winds around the enemy over this time
        public static int SpineGrowTicks => 18;        // the smooth vine turns into the spiny one over this time
        public static float CoilRadiusScale => 0.55f;  // coil half-size relative to the enemy's half-size
        public static float CoilRadiusPad => 6f;
        public static float CoilDrift => 0.5f;         // how far the coil climbs up the enemy (fraction of its height)
        public static float MaxLoops => 3.5f;
        public static int CoilSamples => 48;

        // ---- vanilla textures ----
        public static string VineChainName => "Chain5";    // Terraria/Images/<this>: the Vine Hook (Ivy Whip) chain. If the look is wrong try another Chain1..Chain43
        public static string SpineChainName => "Chains_1";   // Terraria/Images/<this>: the Thorn Hook chain, used for the spiny coil
        public static float HeadForwardPx => 2f;           // the head sticks out this far beyond the end of the vine (its back overlaps the chain, so there is no gap)
        public static float VineScale => 1f;
        public static float VineTipScale => 0.7f;          // the vine gets this thick at its tip (fraction of VineScale)
        public static float SpineScale => 1f;
        public static float HeadScale => 1f;
        public static float ChainSpacing => 0.8f;          // distance between sprites along a vine, as a fraction of the sprite's height
        public static float ChainEndTrimPx => 4f;          // the chain stops this many pixels before each end (the head covers the rest), raise it if a link pokes out of a head
        public static float RetractSpeed => 6f;            // pixels per tick the vines shrink after the button is released or the weapon is swapped

        // ---- netcode ----
        public static int SyncIntervalTicks => 4;      // how often the owner sends the aim, the targets and the lengths

        // ---- colors (alpha 0 = additive) ----
        public static Color GlowColor => new Color(120, 255, 90, 0);
    }
}