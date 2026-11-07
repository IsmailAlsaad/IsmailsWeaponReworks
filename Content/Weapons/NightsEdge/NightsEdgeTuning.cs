using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// Every tunable number of the Night's Edge rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot reload
    /// can tweak them. Swing arm angles are for a player facing right and RELATIVE TO THE AIM (the direction to the cursor when the swing
    /// starts): 0 = at the cursor, negative = above it, positive = below it. The swing's timeline is in "timeline ticks": the combo and
    /// melee attack speed make a timeline tick shorter than a real tick (see ComboSpeedAt and AttackSpeedInfluence).
    /// </summary>
    public static class NightsEdgeTuning
    {
        // ---- damage (everything that decides how hard it hits, in one place) ----
        public static float SwingDamageMultiplier => 1f;      // of the item's damage, per swing
        public static float SwingKnockbackMultiplier => 1f;

        // ---- sword ----
        public static float BladeLength => 56f;       // hand to tip, in pixels at scale 1 (the vanilla sprite's diagonal; adjust if the sword looks too long or short)
        public static float SwordScale => 1.1f;
        public static float GripOffset => 0f;         // moves the sprite back along the blade (DRAW only) so the hand holds the handle higher up (pixels at scale 1)

        // ---- swing (the Starfury's: alternates down and up, centered on the cursor) ----
        public static int AnticipateTicks => 9;
        public static float AnticipateDeg => 22f;     // how far past the start of the arc the arm pulls back
        public static float AnticipateTiltDeg => 10f; // and the wrist (the blade tilt) the same way
        public static int SwingTicks => 8;            // the strike (the only part that damages)
        public static float SwingEasePower => 2.5f;   // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 4f;       // the strike carries this far past the end of the arc ...
        public static int RecoverTicks => 6;          // ... and settles back to the end of the arc over this long
        public static int SweepHitSteps => 4;         // the blade's hit test is checked at this many poses between two ticks
        public static float RestArmDeg => 10f;        // where a fresh combo starts (just before the high end of the arc)
        public static float SwingHighDeg => -75f;     // top of the arc
        public static float SwingLowDeg => 75f;       // bottom of the arc
        public static float TiltHighDeg => -12f;      // blade tilt relative to the arm at the top (wrist) ...
        public static float TiltLowDeg => 12f;        // ... and at the bottom
        public static int ComboWindowTicks => 30;     // a swing started within this long after the last one ended is consecutive (alternates and charges)
        public static float HitLengthScale => 1.3f;   // how far the sword HITS along the blade, as a fraction of BladeLength (1.5 = 50% further). Does not change what you see
        public static float HitWidth => 18f;          // how thick the hit line is (pixels at scale 1; the line is that wide around the blade). Does not change what you see
        public static float HitShake => 0.25f;         // camera punch on a hit (pixels)

        // ---- the combo: every consecutive swing is faster and charges the sword ----
        public static int ChargeSwings => 6;          // this many consecutive swings fully charge the sword (the swing after them is the first charged one)
        public static float ComboSpeedMin => 1f;      // the timeline speed of a fresh combo's first swing ...
        public static float ComboSpeedMax => 2.2f;    // ... growing evenly to this at full charge (before melee attack speed)
        public static int ChargedLingerTicks => 180;  // a fully charged sword stays charged this long after the last swing ended, then it is lost
        public static float AttackSpeedInfluence => 1f; // 1 = the timeline also runs at your melee attack speed, 0 = ignores it
        public static float SpeedScaleMin => 0.6f;
        public static float SpeedScaleMax => 3.5f;    // keep it below the shortest damaging window (SwingTicks), or a tick could step over it

        // The timeline speed of a swing that is the (level + 1)th of its combo (level 0 = the first swing, ChargeSwings = charged).
        public static float ComboSpeedAt(int level)
        {
            float fraction = MathHelper.Clamp(level / (float)Math.Max(1, ChargeSwings), 0f, 1f);
            return MathHelper.Lerp(ComboSpeedMin, ComboSpeedMax, fraction);
        }

        // ---- swing trail (a SweepTrail fan, drawn by the shared pixel line renderer) ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 22;
        public static float TrailOpacityLow => 0.35f; // a fresh combo's trail ...
        public static float TrailOpacityHigh => 0.8f; // ... grows to this at full charge
        public static float TailLifeScale => 0.45f;   // the first point of a swing's trail lives this fraction as long as the last one
        public static int TrailRails => 7;            // parallel pixel lines that fill the fan
        public static float TrailExpandBy => 0.9f;    // the fan has fully expanded this far into the swing (0..1)
        public static float TrailInner => 0.5f;       // fully expanded, the fan reaches down the blade to this fraction of its length
        public static float TrailOuter => 1.3f;       // the outer edge of the fan, as a fraction of the blade length (1 = the tip)
        public static float TrailLagTicks => 0.4f;    // how many ticks the trail runs BEHIND the sprite (0 = exactly on it)
        public static int TrailSamplesPerTick => 8;   // trail points laid per timeline tick (the strike is fast, so many keep the fan smooth)
        public static Color CoreColor => new Color(235, 205, 255, 0);
        public static Color MidColor => new Color(160, 70, 235, 0);
        public static Color DeepColor => new Color(75, 25, 150, 0);

        // ---- the charge: a soft purple glow that grows with every swing, and at full charge the pulsing outline (the Dark Lance's) ----
        public static float ChargeGlowAlpha => 0.45f;   // strength of the glow copies behind the sword at full charge
        public static float ChargeGlowFollow => 0.25f;  // how fast the glow follows the charge (0..1 per tick)
        public static Color ChargeGlowColor => new Color(140, 50, 225, 0);
        public static float OutlineWidth => 2f;         // screen pixels
        public static int OutlinePulseTicks => 48;      // one full pulse
        public static float OutlineMin => 0.45f;        // the pulse runs between these two strengths
        public static float OutlineMax => 1f;
        public static float OutlineFade => 0.3f;        // how fast the outline fades in and out (per tick, 0..1)
        public static float OutlineGlowScale => 1.18f;  // the soft glow copy behind the outline (grows from the middle of the sprite)
        public static Color OutlineColor => new Color(170, 70, 255, 0);     // alpha 0 = additive
        public static Color OutlineCoreColor => new Color(235, 200, 255, 0);
        public static float ChargeUpPunch => 0.8f;      // camera punch when the sword becomes fully charged
        public static int FlashTicks => 14;             // how long the charge-up flash on the blade lasts

        // ---- the edge flames (patch 2): metaballs along the blade edge, clipped to Ismail's mask, and free flames that leave it ----
        public static int FlameGrowSteps => 2;          // the flames grow in over the last this-many combo steps before full charge
        public static int EdgeAnchors => 14;            // blobs along the edge (the edge is cut into this many slices from hilt to tip)
        public static float EdgeRadius => 5f;           // radius of one edge blob at full strength (px at scale 1)
        public static float EdgeRadiusPulse => 0.3f;    // how much the blobs breathe (0.3 = 30%)
        public static float EdgeJitter => 1.5f;         // how far the blobs wander around their spot (px at scale 1)
        public static float EdgeHeatBase => 0.55f;      // the heat of the edge, with a wave running up the blade around it
        public static float EdgeHeatWave => 0.45f;
        public static float EdgeWaveSpeed => 0.16f;     // radians per tick: how fast the heat wave runs along the blade
        public static float EdgeWaveLength => 1.6f;     // waves on the blade at once
        public static float FlameSpawnChance => 0.6f;   // free flames per tick at full strength (0.6 = about 3 in 5 ticks)
        public static float FlameSize => 3.5f;          // radius of a free flame at birth (px at scale 1)
        public static int FlameLifeTicks => 34;
        public static float FlameKick => 0.5f;          // random push when a free flame starts
        public static float FlameInherit => 0.3f;       // how much of the blade's motion a free flame takes with it
        public static float FlameInheritMax => 6f;      // ... at most this fast (px per tick)
        public static float FlameHeat => 0.9f;          // free flames start this hot and cool down
        public static float FlameRise => 0.06f;         // upward push per tick
        public static float FlameDrag => 0.95f;
        public static float FlameWobble => 0.05f;
        public static int FlameMaxParticles => 260;
        public static float FlamePixelSize => 2f;       // world px per flame pixel
        public static float FlameMerge => 2.2f;         // how far a blob's field reaches (bigger = merges from further away)
        public static float FlameOutlineWidth => 0.2f;
        public static int FlameBands => 5;              // hard color bands
        public static float FlameGlow => 0.25f;         // faint glow around the blobs (0 = none)
        public static float FlameLight => 0.9f;         // light the flames cast (0 = none)
        public static Color FlameColdColor => new Color(55, 14, 105);
        public static Color FlameMidColor => new Color(150, 60, 230);
        public static Color FlameHotColor => new Color(238, 205, 255);
        public static Color FlameOutlineColor => new Color(34, 8, 70);
    }
}