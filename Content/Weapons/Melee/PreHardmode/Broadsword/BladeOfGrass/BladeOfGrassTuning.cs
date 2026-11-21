using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// Every tunable number of the Blade of Grass rework in one place (ticks: 60 = 1 second, lengths in pixels: 16 = 1 tile).
    /// Properties instead of fields, so hot reload can tweak them.
    /// </summary>
    public static class BladeOfGrassTuning
    {
        // ---- sword and pose ----
        public static float BladeLength => 70f;        // hand to tip, in pixels at scale 1 (adjust to the real sprite)
        public static float SwordScale => 1f;
        public static float GripOffset => 4f;          // where the hand holds the sword: pixels up from the end of the handle, at scale 1 (visual only)

        // ---- the swing: the shared charged blade (Common/ChargedBlade) reads everything below through BladeOfGrassBladeTune. A chop centered on the cursor,
        // alternating up and down, flowing into each other while the button stays down; the same numbers as the Light's Bane. Arm angles are for a
        // player facing right and RELATIVE TO THE AIM (0 = at the cursor, negative = above it, positive = below it); the ready pose is relative to
        // the player (0 = forward, positive = down). The blade angle is arm + tilt. The charge is the length of the vines: ----
        public static float ReadyArmDeg => 30f;
        public static float ReadyTiltDeg => -60f;
        public static float SwingLowDeg => 70f;  // bottom of the arc
        public static float SwingHighDeg => -70f;  // top of the arc
        public static float TiltLowDeg => 14f;  // ... and at the bottom
        public static float TiltHighDeg => -14f;  // blade tilt relative to the arm at the top (wrist) ...
        public static int ChainAnticipateTicks => 9;  // a swing that FLOWS out of the previous one: the previous strike carries on through this wind-up ...
        public static int AnticipateTicks => 5;  // a swing that starts from the ready pose: the pull-back ...
        public static float ChainAnticipateDeg => 24f;  // ... to this far past the end of the arc. Keep it above OvershootDeg, so the arm keeps drifting the way it was going
        public static float AnticipateDeg => 18f;  // ... this far past the start of the arc
        public static int SwingTicks => 10;  // the strike (the only part that damages)
        public static int RecoverTicks => 7;  // only when the button is let go: the arm settles back to the end of the arc over this long
        public static float SwingEasePower => 2f;  // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 8f;  // the strike carries this far past the end of the arc
        public static float AnticipateTiltDeg => 10f;  // the wrist cocks back this much before every strike
        public static float ChargedAt => 2f;  // above 1: the blade never counts as fully charged (no outline, no stab)
        public static float ChargeSyncStep => 0.04f;  // the owner tells the other machines when the charge moved this much
        public static float ChargeShowEase => 0.15f;  // how fast the DRAWN charge follows the real one (per tick, 0..1)
        public static float OutlineFade => 0.25f;  // how fast the outline fades in and out (per tick, 0..1)
        public static int IdleBobTicks => 90;
        public static float IdleBobDeg => 2f;
        public static float IdleEase => 0.15f;  // how fast the arm settles into the ready pose after a swing (per tick, 0..1)
        public static float StabBlendOut => 0.2f;  // after the stab the sword slides back out of the chest into the hand by this much per tick (0..1)
        public static float SparkleChance => 0.25f;  // per tick, while fully charged: a mote somewhere along the blade
        public static int FlashTicks => 12;  // the white flash when a strike starts and when the sword fills
        public static int StabBufferTicks => 10;  // a right click during a strike is remembered this long and fires as soon as the strike is over
        public static int StabTurnTicks => 12;  // the arm goes to StabArmDeg, the blade turns to point at the chest and slides back through the fist
        public static int StabAimTicks => 5;  // a short held aim: the blade trembles and sparkles
        public static int StabPlungeTicks => 3;  // the plunge (fast)
        public static float StabArmDeg => -10f;  // the arm while stabbing (facing-local: 0 = forward, negative = up)
        public static float StabChestOffsetY => -2f;  // the chest point the blade aims at, relative to the player's center (px, negative = up)
        public static float StabAimDistance => 10f;  // while aiming, the tip stands this far in front of the chest (px)
        public static float StabDepth => 6f;  // when plunged, the tip is this far PAST the center of the chest (px)
        public static float StabShake => 1.2f;  // tremble of the blade while aiming and buried (px; half as much while aiming)
        public static int StabHoldTicks => 7;  // the blade stays buried while the burst plays
        public static float StabHitShake => 2.5f;  // camera punch of the impact
        public static float MaxSpeedScale => 1f;  // no speed-up from long vines
        public static float SpeedCurvePower => 1f;  // 1 = the speed rises evenly with the charge; above 1 it rises late
        public static float AttackSpeedInfluence => 1f;  // 0 = ignore the player's melee attack speed, 1 = fully applied
        public static float MinSpeed => 0.3f;  // the final speed (charge x melee speed) is clamped to this range
        public static float MaxSpeed => 6f;
        public static int TrailSamplesPerTick => 8;  // trail points laid per tick (the strike is fast, so many keep the fan smooth)
        public static float TrailLagTicks => 0.4f;  // how many ticks the trail runs BEHIND the sprite (0 = exactly on it)
        public static float TrailOuter => 1.25f;  // the outer edge of the fan, as a fraction of the blade length (1 = the tip)
        public static float TrailInnerLow => 0.8f;  // fully expanded, the fan reaches down the blade to this fraction of its length: with no charge ...
        public static float TrailInnerHigh => 0.3f;  // ... and with a full charge
        public static float TrailExpandBy => 0.8f;  // the fan has fully expanded this far into the strike (0..1)
        public static int SweepHitSteps => 6;  // the hit test is checked at this many poses between two ticks (a fast strike needs more)
        public static float HitLengthScale => 1.3f;  // how far the sword HITS along the blade, as a fraction of BladeLength (1.5 = 50% further). Does not change what you see
        public static float HitWidth => 18f;  // how thick the hit line is (pixels at scale 1; the line is that wide around the blade). Does not change what you see
        public static float DamageMultiplier => 1f;  // of the item's damage (the speed is the reward for charging, so 1)
        public static float KnockbackMultiplier => 1f;
        public static float HitShake => 0.6f;  // camera punch on a hit (grows with the charge)
        public static float ChargePerHit => LengthPerHitPx / System.Math.Max(1f, MaxLength - MinLength);  // the first hit of a swing adds LengthPerHitPx to every vine (as a fraction of the span MinLength..MaxLength)
        public static float GlowMaxAlpha => 0.45f;  // opacity of the soft green glow when the vines are nearly at MaxLength
        public static Color OutlineColor => new Color(120, 255, 90, 0);
        public static float GlowScale => 1.18f;  // the glow copy is this much bigger than the sword, grown from the MIDDLE of the sprite
        public static int OutlinePulseTicks => 40;  // one full pulse
        public static float OutlineMin => 0.5f;  // the pulse runs between these two strengths
        public static float OutlineMax => 1f;
        public static float OutlineWidth => 2f;  // screen pixels
        public static Color OutlineCoreColor => new Color(215, 255, 190, 0);
        public static Color CoreColor => new Color(225, 255, 205, 0);
        public static bool ShowBladeDebug => false;  // true: a magenta square on the tip the hit test uses

        // ---- swing trail (a SweepTrail fan) ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 16;
        public static float TailLifeScale => 0.45f;     // the first point of a swing's trail lives this fraction as long as the last one
        public static int TrailRails => 7;              // parallel pixel lines that fill the fan
        public static float TrailOpacityLow => 0.3f;    // how strong the fan is with short vines ...
        public static float TrailOpacityHigh => 0.75f;  // ... and with the longest
        public static Color TrailCoreColor => new Color(215, 255, 175, 0);
        public static Color TrailGreenColor => new Color(90, 220, 70, 0);
        public static Color TrailDeepColor => new Color(25, 110, 40, 0);
        public static float ChargeLeafChance => 0.06f;  // per tick at full length: a leaf off the blade (less with shorter vines)

        // ---- the vines ----
        public static int VineCount => 3;
        public static int PointsPerVine => 18;         // points of each rope (more = smoother and more lively)
        public static float RootStart => 0.45f;        // where along the blade the first vine grows from (fraction of BladeLength)
        public static float RootEnd => 0.9f;           // and the last
        public static float MinLength => 70f;          // length every vine starts with (and cools back to); a vine shorter than MinWrapPixels cannot attack
        public static float MaxLength => 460f;         // the longest a vine gets (about 29 tiles)
        public static float LengthPerHitPx => 40f;     // a swing that hits something (the first enemy it touches) makes EVERY vine this much longer
        public static int DecayHoldTicks => 480;       // the vines do not shorten for this long after the last counted hit (8 s) ...
        public static float DecayPerTick => 0.15f;     // ... then they cool toward MinLength by this many pixels per tick (about 9 px per second)
        public static float FreeSlackPx => 16f;        // a free vine shows this much more than the straight distance to its goal (room for the wave); it only shows what it needs to put its tip on the cursor
        public static float VineVariation => 0.1f;    // how different the three vines are (wave, stiffness, speed, growth, length); 0 = identical copies

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
        public static float TipMaxSpeed => 50f;        // top speed of a vine tip homing on the cursor (pixels per tick)
        public static float TipAccel => 1.7f;          // how fast the tip can change its velocity per tick (lower = heavier, more inertia and overshoot)
        public static float TipArrive => 0.3f;         // slows the tip down near the cursor (fraction of the distance per tick; lower = longer, floatier glide in)

        // ---- targeting and searching ----
        public static float ReachSlack => 10f;         // an enemy may be this much farther than the vine's current length and still count as in reach
        public static int RetargetInterval => 5;       // ticks between target searches
        public static float SamePenalty => 100000f;    // added to an enemy's score per vine already on it, so vines spread out; the same enemy is only reused when nothing else is in reach
        public static float SearchOrbitSpeed => 0.006f; // radians per tick the search direction turns around the player (a full turn in about 9 s); every other vine turns the other way
        public static float SearchSweepDeg => 40f;     // on top of that a vine sweeps this far to each side ...
        public static float SearchSweepSpeed => 0.03f; // ... at this speed (radians per tick of the sweep's own sine)
        public static float SearchReachMin => 0.55f;   // the tip floats between these two fractions of the vine's length from the player ...
        public static float SearchReachMax => 0.95f;
        public static float SearchBreathSpeed => 0.025f; // ... breathing in and out at this speed

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
        public static int PoisonTicks => 30;           // Poisoned is topped up to this on the damage ticks of a latched coil, so it lasts as long as a coil is on the enemy and about this long after the last one
        public static float SegmentDamageScale => 0.3f; // damage of one tick, as a fraction of the weapon's damage
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
        public static float ChainStartTrimPx => 12f;       // the HELD vines' chain starts this many pixels out from the root (the first bit of each vine is not drawn)
        public static float PlantedChainStartTrimPx => 0f; // the same for the planted set

        // ---- the planted sword (right click, patch 8) ----
        public static float PlantMinReachPx => 24f;        // the sword is planted at least this far in front of the player (toward the cursor's side) ...
        public static float PlantMaxReachPx => 160f;       // ... and at most this far; in between it follows the cursor's x
        public static float PlantMaxDropPx => 480f;        // no ground within this far below the hand: nothing is planted
        public static float PlantDropSpeed => 26f;         // the sword's fall, pixels per tick
        public static float PlantSinkPx => 14f;            // how far the tip sinks into the tile (the tile hides that part when PlantedDrawBehind is on)
        public static int PlantRegrowTicks => 30;          // after landing the hand stays empty this long, then the sword grows back in it
        public static float PlantShake => 3f;              // camera punch when the sword lands
        public static int PlantedLifeTicks => 3600;        // how long the planted set lives after landing (1 minute)
        public static float PlantedRootDownPx => 8f;       // the vines grow from this far below the top (hilt) of the planted sword
        public static float PlantedGrowPerTick => 0.05f;   // every planted vine grows this many pixels per tick (3 px per second) ...
        public static float PlantedMaxLength => MaxLength; // ... up to this length (break-offs are never regrown by anything else)
        public static int PlantedSyncTicks => 30;          // while growing, the owner sends the lengths this often
        public static int WitherTicks => 30;               // a planted set that is replaced fades out over this long
        public static bool PlantedSearchAbove => true;     // the planted vines search only the half-space above the sword (not into the ground)
        public static bool PlantedDrawBehind => true;      // true: drawn behind tiles and enemies (the ground hides the sunk tip, the vines are behind enemies); false: in front of enemies, the tip is not hidden
        public static float PlantedLeafChance => 0.15f;    // per tick, a leaf drifts off the planted sword

        // ---- netcode ----
        // The vines are sent (target and length of each) only when a target or a length changes; nothing to tune.

        // ---- colors (alpha 0 = additive) ----
        public static Color GlowColor => new Color(120, 255, 90, 0);
    }
}