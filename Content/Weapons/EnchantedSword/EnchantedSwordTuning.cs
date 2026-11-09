using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// Every tunable number of the Enchanted Sword rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Arm angles are for a player facing right and RELATIVE TO THE AIM (the direction to the cursor when the swing
    /// starts): 0 = at the cursor, negative = above it, positive = below it; the ready pose is relative to the player instead (0 = forward,
    /// positive = down). The blade angle is arm + tilt. The sword swings UP and DOWN (kinds 0 and 1) and does a THRUST (kind 2) only after a down swing and an up swing have both hit an enemy, one after the other. When the thrusts have made the sword FULL, a right click does a THROW (kind 3, the thrust's pose without a hit) that sends the ethereal beam out of the tip.
    /// </summary>
    public static class EnchantedSwordTuning
    {
        // ---- sword ----
        public static float BladeLength => 52f;       // hand to tip, px at scale 1: a first guess, tune it with ShowBladeDebug until the magenta square sits on the sprite's tip
        public static float SwordScale => 1.1f;
        public static float GripOffset => 0f;         // draw only: moves the sprite this far (px at scale 1) back along the blade, so the hand holds it higher or lower
        public static bool ShowBladeDebug => false;   // true: a magenta square on the tip the hit test uses

        // ---- damage: everything that decides what a swing or a thrust does to an enemy ----
        public static float DamageMultiplier => 1f;           // of the item's damage, for the up and down swings
        public static float KnockbackMultiplier => 1f;
        public static float ThrustDamageMultiplier => 1.15f;  // the thrust on top of DamageMultiplier
        public static float ThrustKnockbackMultiplier => 1.4f;
        public static float HitLengthScale => 1.3f;           // how far the sword HITS along the blade, as a fraction of BladeLength (1.5 = 50% further). Does not change what you see
        public static float HitWidth => 18f;                  // how thick the hit line is (pixels at scale 1; the line is that wide around the blade). Does not change what you see
        public static int SweepHitSteps => 6;                 // the hit test is checked at this many poses between two ticks (a fast strike needs more)

        // ---- the hits: thrust hits give the blade a stronger outline; at HitsToDischarge (3) the sword is FULL (outline stage 3) and a right click throws the beam ----
        // NOTE: HitsToDischarge keeps its old name and now means "hits to FULL"; hits at full add nothing, a partial count is lost HitHoldTicks after the last
        // hit, a FULL sword never decays (it keeps the charge until the throw, a swap of items or death). DischargeShake and DischargeBurstRadius are no longer used.
        public static int HitsToDischarge => 3;         // ONLY THE THRUST charges the sword: the first enemy each thrust touches counts as one hit (up and down hits only count toward the thrust, see SwingHitsToThrust)
        public static int HitHoldTicks => 300;          // the count is lost this long after the last counted hit
        public static float StageShowEase => 0.2f;      // how fast the DRAWN outline follows the real count (per tick, 0..1)
        public static float HitShake => 0.6f;           // camera punch on a hit (grows with the stage)
        public static float DischargeShake => 2f;       // camera punch of the discharge
        public static float DischargeBurstRadius => 72f; // size of the stardust burst (the Enchanted Boomerang's burst, 72 = its default)

        // ---- the throw (right click at full charge: the thrust's pose, nothing is hit, the ethereal beam leaves the tip and the charge is spent) ----
        public static int ThrowBufferTicks => 12;       // a right click while a strike is still going is remembered until that strike ends plus this many ticks, and starts the throw the moment the strike ends
        public static int ThrowAnticipateTicks => 7;    // the pull-back before the throw, from the ready pose ...
        public static int ThrowChainAnticipateTicks => 7; // ... and when it flows out of a swing
        public static int ThrowTicks => 6;              // the forward drive (the thrust's pull-back, reach and ease are used)
        public static float ThrowReleaseFraction => 0.15f; // the beam leaves the tip this far into the drive (0 = at the start, 1 = at the end)
        public static float ThrowShake => 1.2f;         // camera punch when the beam leaves (owner)

        // ---- the thrown beam (the Enchanted Beam sprite, stretched to GhostBladeLength; it stops at the first enemy and the ghost takes over there) ----
        public static float BeamSpeed => 24f;           // px per tick
        public static float BeamRangeTiles => 36f;      // it fizzles after flying this far (a miss is NOT refunded)
        public static float BeamHitWidth => 22f;        // how thick the hit line is around the tip's path
        public static float BeamDamageMultiplier => 0.5f;    // of the sword's damage, for the beam's own hit (the ghost's strikes come on top)
        public static float BeamKnockbackMultiplier => 0.5f;
        public static int BeamTrailLifeTicks => 14;
        public static float BeamTrailWidth => 10f;      // px at the start of the trail
        public static int BeamTrailSamples => 4;        // trail points laid per tick
        public static float BeamDustChance => 0.7f;     // per tick: a stardust mote off the tip

        // ---- the cycle: up, down, thrust, up, down, thrust ... ----
        public static int StartKind => 1;               // the first swing after the sword was at rest: 0 = up, 1 = down (the swings alternate from there; see SwingHitsToThrust for when a thrust comes)
        public static int CycleResetTicks => 10;        // the cycle starts over (at StartKind) when the sword rested this long
        public static int SwingHitsToThrust => 2;       // the thrust comes after this many up and down swings IN A ROW that each hit an enemy (2 = a down swing and an up swing; a swing that hits nothing starts the count over)
        public static float AttackSpeedInfluence => 1f; // 0 = ignore the player's melee attack speed, 1 = fully applied
        public static float MinSpeed => 0.3f;           // the final speed (melee speed) is clamped to this range
        public static float MaxSpeed => 6f;

        // ---- the up and down swings (a chop centered on the cursor) ----
        public static int AnticipateTicks => 5;         // a swing that starts from the ready pose: the pull-back ...
        public static float AnticipateDeg => 18f;       // ... this far past the start of the arc
        public static int ChainAnticipateTicks => 9;    // a swing that FLOWS out of the previous one: the previous strike carries on through this wind-up ...
        public static float ChainAnticipateDeg => 24f;  // ... to this far past the end of the arc. Keep it above OvershootDeg, so the arm keeps drifting the way it was going
        public static float AnticipateTiltDeg => 10f;   // the wrist cocks back this much before every strike
        public static int SwingTicks => 9;              // the strike (the only part that damages)
        public static float SwingEasePower => 2f;       // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 8f;         // the strike carries this far past the end of the arc
        public static int RecoverTicks => 7;            // only when the button is let go: the arm settles back to the end of the arc over this long
        public static float SwingHighDeg => -70f;       // top of the arc
        public static float SwingLowDeg => 70f;         // bottom of the arc
        public static float TiltHighDeg => -14f;        // blade tilt relative to the arm at the top (wrist) ...
        public static float TiltLowDeg => 14f;          // ... and at the bottom

        // ---- the thrust (a jab at the cursor: the arm points at it, the sword is pulled back and then driven forward through the fist) ----
        public static float ThrustWindArmDeg => 22f;    // the arm while the sword is pulled back (positive = below the aim); the blade stays parallel to the aim
        public static int ThrustAnticipateTicks => 12;   // a thrust that starts from the ready pose: the pull-back
        public static int ThrustChainAnticipateTicks => 12; // a thrust that flows out of the previous swing
        public static int ThrustTicks => 8;             // the strike (the only part that damages)
        public static float ThrustEasePower => 6f;      // higher = a harder, faster jab that eases out at the end
        public static float ThrustPullPx => 16f;        // before the jab the sword slides back through the fist this far (px at scale 1): the fist slides up the blade, the hilt trails behind it
        public static float ThrustReachPx => 0f;        // at the end of the jab the sword is this far forward of its normal grip (the hilt leaves the fist by this much: keep it small or the sword looks detached)
        public static float ThrustSettle => 0.5f;       // after the jab the sword rests at this fraction of ThrustReachPx until the next swing takes over

        // ---- the ready pose (between swings): the sword held up at the shoulder, bobbing a little ----
        public static float ReadyArmDeg => 30f;
        public static float ReadyTiltDeg => -60f;
        public static float IdleBobDeg => 2f;
        public static int IdleBobTicks => 90;
        public static float IdleEase => 0.15f;          // how fast the arm settles into the ready pose after a swing (per tick, 0..1)

        // ---- swing trail (a SweepTrail fan, drawn by the shared pixel line renderer; it grows with the stage) ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 16;
        public static float TailLifeScale => 0.45f;     // the first point of a swing's trail lives this fraction as long as the last one
        public static int TrailRails => 7;              // parallel pixel lines that fill the fan
        public static float TrailExpandBy => 0.8f;      // the fan has fully expanded this far into the strike (0..1)
        public static float TrailInnerLow => 0.8f;      // fully expanded, the fan reaches down the blade to this fraction of its length: with no hits ...
        public static float TrailInnerHigh => 0.4f;     // ... and at the highest stage
        public static float TrailInnerThrust => 0.5f;   // the thrust's streak
        public static float TrailOuter => 1.25f;        // the outer edge of the fan, as a fraction of the blade length (1 = the tip)
        public static float TrailLagTicks => 0.4f;      // how many ticks the trail runs BEHIND the sprite (0 = exactly on it)
        public static int TrailSamplesPerTick => 8;     // trail points laid per tick (the strike is fast, so many keep the fan smooth)
        public static float TrailOpacityLow => 0.3f;    // how strong the fan is with no hits ...
        public static float TrailOpacityHigh => 0.8f;   // ... and at the highest stage

        // ---- the outline: stage 0 = none, stage 1 = after the first hit, stage 2 = after the second, stage 3 = FULL (the third) ----
        public static float Stage3Strength => 1f;
        public static float Stage3Width => 3.2f;
        public static int Stage3PulseTicks => 18;       // full: the fastest pulse
        public static float FullSparkleChance => 0.7f;  // per tick while the sword is FULL: a mote somewhere along the blade
        public static int FullPulseTicks => 36;         // while FULL, a small ring of stardust at the tip this often (the ready cue)
        public static float Stage1Strength => 0.4f;
        public static float Stage2Strength => 0.85f;
        public static float Stage1Width => 1.5f;        // screen pixels
        public static float Stage2Width => 2.5f;
        public static int Stage1PulseTicks => 54;       // one full pulse
        public static int Stage2PulseTicks => 28;       // faster at the higher stage
        public static float OutlineMin => 0.55f;        // the pulse runs between these two strengths
        public static float OutlineMax => 1f;
        public static float GlowScale => 1.18f;         // the glow copy is this much bigger than the sword, grown from the MIDDLE of the sprite
        public static float SparkleChance => 0.3f;      // per tick, at the highest stage: a mote somewhere along the blade (less at stage 1)
        public static int FlashTicks => 12;             // the white flash when a strike starts and when the sword discharges

        // ---- the ghost sword (the thrown beam conjures it where it hits an enemy: it floats around the enemy, slashes 4 times, backs away and drives itself in) ----
        public static int GhostSlashes => 4;                // quick slashes before the stab
        public static int GhostSlashTicks => 8;             // one slash (quick). The ghost's blade fans across the enemy one way, then back the other way
        public static int GhostFadeInTicks => 10;           // the ghost starts where the beam hit (the blade pointing at the enemy) and eases onto its orbit this long (it cannot hit yet)
        public static int GhostBackoffTicks => 14;          // the 5th swing: it backs away from the enemy this long (trembling, stardust drawn into the tip) ...
        public static float GhostBackoffPx => 70f;          // ... this far
        public static int GhostHoldTicks => 6;              // a held beat before the drive
        public static int GhostDriveTicks => 5;             // the drive into the enemy
        public static float GhostDriveEasePower => 2.5f;    // 1 = even speed; higher = it accelerates harder into the enemy
        public static float GhostEmbedFraction => 0.6f;     // where the blade explodes: the hilt stops this fraction of the blade length from the enemy's middle (0.6 = the tip is past the middle)
        public static int GhostFadeOutTicks => 10;
        public static float GhostBladeLength => 64f;        // hilt to tip in px (the beam sprite is stretched to it; the thrown beam is drawn at this size too)
        public static float GhostHoldDistance => 56f;       // the hilt keeps this far from the enemy's middle while it slashes ...
        public static float GhostHoldPerSize => 0.8f;       // ... plus this fraction of the enemy's half size (so a big enemy is still reached)
        public static float GhostSlashArcDeg => 65f;        // each slash fans the blade this far to each side of the enemy
        public static float GhostOrbitDegPerTick => 2.5f;   // the ghost drifts around the enemy this fast while it slashes (the direction is random)
        public static float GhostTremblePx => 1.5f;
        public static int MaxGhosts => 3;                   // per player; a 4th makes the oldest leave
        public static float GhostRetargetRange => 400f;     // px: if its enemy dies first the ghost goes for the nearest enemy within this range, or leaves
        public static float GhostSlashDamageMultiplier => 0.6f; // of the sword's damage, for each slash
        public static float GhostStabDamageMultiplier => 2f;    // the drive
        public static float GhostStabKnockbackMultiplier => 1.5f;
        public static float GhostStabShake => 2.5f;         // camera punch when the blade lands
        public static float GhostSpriteOffsetDeg => 45f;    // the beam sprite points UP: it is drawn turned this much from the blade's direction (tune it if the sprite looks sideways or backward)
        public static float GhostSpritePivotX => 0f;      // where the sprite is pinned to the hilt and turns around, as a fraction of its width (0 = left edge, 1 = right edge): raise it to move the drawn sword LEFT of the blade line, lower it to move it RIGHT (the sprite is turned by GhostSpriteOffsetDeg first, so left and right are the sprite's own)
        public static float GhostSpritePivotY => 1f;        // the same for the height (0 = top edge, 1 = bottom edge): lower it to push the sword further OUT along the blade, raise it above 1 to pull it back toward the hilt
        public static float GhostExplosionDamageMultiplier => 1f; // of the sword's damage: the stardust burst the blade explodes into the moment it lands (the Enchanted Boomerang's burst)
        public static float GhostOpacity => 0.9f;
        public static float GhostGlowScale => 1.25f;        // the glow copy is this much bigger, grown from the MIDDLE of the sprite
        public static float GhostTrailInner => 0.45f;       // the slash's fan reaches down the blade to this fraction of its length
        public static int GhostAfterimageTicks => 9;

        // ---- colors (alpha 0 = additive): the Enchanted Boomerang's stardust, gold and white into blue and pink ----
        public static Color OutlineColor => new Color(110, 190, 255, 0);
        public static Color OutlineCoreColor => new Color(255, 240, 200, 0);
        public static Color CoreColor => new Color(255, 245, 215, 0);
        public static Color BlueColor => new Color(120, 190, 255, 0);
        public static Color PinkColor => new Color(235, 140, 220, 0);

        // A value that runs through the four stages (0 = none, 1, 2, 3 = full) and is eased between them while the drawn stage changes.
        static float Through(float stage, float at0, float at1, float at2, float at3)
        {
            stage = Math.Max(0f, stage);
            if (stage <= 1f)
                return MathHelper.Lerp(at0, at1, stage);
            if (stage <= 2f)
                return MathHelper.Lerp(at1, at2, stage - 1f);
            return MathHelper.Lerp(at2, at3, Math.Min(1f, stage - 2f));
        }

        // The outline's strength, width and pulse length for a (smoothly changing) stage between 0 and 3.
        public static float OutlineStrengthAt(float stage)
        {
            return Through(stage, 0f, Stage1Strength, Stage2Strength, Stage3Strength);
        }

        public static float OutlineWidthAt(float stage)
        {
            return Through(stage, 0f, Stage1Width, Stage2Width, Stage3Width);
        }

        public static int PulseTicksAt(float stage)
        {
            return (int)Through(stage, Stage1PulseTicks, Stage1PulseTicks, Stage2PulseTicks, Stage3PulseTicks);
        }

        // The stage as 0..1 (0 = none, 1 = full): drives the trail and the glow.
        public static float Stage01(float stage)
        {
            return MathHelper.Clamp(stage / Math.Max(1f, HitsToDischarge), 0f, 1f);
        }
    }
}