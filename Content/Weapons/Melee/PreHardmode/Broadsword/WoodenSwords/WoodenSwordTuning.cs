using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords
{
    /// <summary>
    /// Every tunable number of the wooden sword reworks in one place (ticks: 60 = 1 second, lengths in pixels: 16 = 1 tile). It covers all
    /// eight swords (Wooden, Boreal, Palm, Rich Mahogany, Shadewood, Ebonwood, Ashwood, Cactus); the per-wood differences (splinter cap, lifetime,
    /// trail colors) are in WoodenSwordWoods.Spec at the bottom. Properties instead of fields, so hot reload can tweak them.
    /// </summary>
    public static class WoodenSwordTuning
    {
        // ---- blade length: measured from the item sprite (hilt in the bottom-left corner, so the diagonal), times this ----
        public static float BladeLengthFactor => 1f;
        public static float BladeLengthFallback => 45f; // used for the one frame before the item sprite is loaded

        // ---- sword and pose ----
        public static float SwordScale => 1f;
        public static float GripOffset => 0f;          // where the hand holds the sword: pixels up from the end of the handle, at scale 1 (visual only)

        // ---- the swing: the shared charged blade (Common/ChargedBlade) reads everything below through BladeOfGrassBladeTune. A chop centered on the cursor,
        // alternating up and down, flowing into each other while the button stays down; the same numbers as the Light's Bane. Arm angles are for a
        // player facing right and RELATIVE TO THE AIM (0 = at the cursor, negative = above it, positive = below it); the ready pose is relative to
        // the player (0 = forward, positive = down). The blade angle is arm + tilt. There is no charge in this weapon, it is the plain swing: ----
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
        public static float MaxSpeedScale => 1f;  // no speed-up (plain swing)
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
        public static float HitLengthScale => 1f;  // how far the sword HITS along the blade, as a fraction of BladeLength (1.5 = 50% further). Does not change what you see
        public static float HitWidth => 18f;  // how thick the hit line is (pixels at scale 1; the line is that wide around the blade). Does not change what you see
        public static float DamageMultiplier => 1f;  // of the item's damage (the speed is the reward for charging, so 1)
        public static float KnockbackMultiplier => 1f;
        public static float HitShake => 0f;  // camera punch on a hit (grows with the charge); 0 = none
        public static float ChargePerHit => 0f;  // no charge in this weapon
        public static float GlowMaxAlpha => 0f;  // no glow (no charge)
        public static Color OutlineColor => new Color(120, 255, 90, 0);
        public static float GlowScale => 1.18f;  // the glow copy is this much bigger than the sword, grown from the MIDDLE of the sprite
        public static int OutlinePulseTicks => 40;  // one full pulse
        public static float OutlineMin => 0.5f;  // the pulse runs between these two strengths
        public static float OutlineMax => 1f;
        public static float OutlineWidth => 2f;  // screen pixels
        public static Color OutlineCoreColor => new Color(215, 255, 190, 0);
        public static Color CoreColor => new Color(225, 255, 205, 0);
        public static bool ShowBladeDebug => false;  // true: a magenta square on the tip the hit test uses

        // ---- swing trail (a SweepTrail fan, colors per wood in WoodenSwordWoods) ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 14;
        public static float TailLifeScale => 0.45f;     // the first point of a swing's trail lives this fraction as long as the last one
        public static int TrailRails => 6;              // parallel pixel lines that fill the fan
        public static float TrailOpacity => 0.55f;

        // ---- splinters ----
        public static int HitIntervalTicks => 60;       // every splinter hurts its enemy once per this many ticks (60 = once a second)
        public static int DamagePerHit => 1;            // fixed damage of one splinter tick (defense still applies, a hit is never below 1)
        public static int MaxSplinters => 3;            // per enemy, per player: one more pushes the oldest out
        public static int LifeTicks => 300;             // how long a splinter stays in (5 s)
        public static int CactusMaxSplinters => 5;
        public static int CactusLifeTicks => 420;       // 7 s
        public static int CellSize => 12;               // Splinters.png: every cell is this big, the splinter centered in it
        public static int StyleCount => 4;              // splinter styles stacked vertically per wood
        public static float SplinterScale => 1f;
        public static float StickShake => 0f;           // camera punch when a splinter lodges; 0 = none
        public static int StickDustCount => 3;          // wood chips when a splinter lodges
        public static int TickDustCount => 0;           // dust at a splinter every time it hurts (0 = none)
        public static int BreakDustCount => 5;          // a splinter breaks into this many bits when its life ends (or its enemy dies, or it is pushed out)
        public static float BreakDustSpeed => 2.5f;
        public static int WaitForNpcTicks => 30;        // a client that gets the splinter before the enemy waits this long for it
    }
}