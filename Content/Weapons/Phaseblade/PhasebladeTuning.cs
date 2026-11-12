using Microsoft.Xna.Framework;
using System;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Phaseblade
{
    /// <summary>
    /// Every tunable number of the Phaseblade rework (all seven vanilla colors share it) in one place (ticks: 60 = 1 second). Properties
    /// instead of fields, so hot reload can tweak them. Arm angles are for a player facing right: 0 = forward, positive = down, negative = up,
    /// and relative to the aim (the direction to the cursor when the swing starts).
    /// </summary>
    public static class PhasebladeTuning
    {
        // ---- damage (everything that changes how hard a swing hits; the base damage is the vanilla item's) ----
        public static float SwingDownDamageMultiplier => 1f;     // swing 1: the plain chop
        public static float SpinDamageMultiplier => 1.25f;       // swings 2 and 3 (the spinning ones)
        public static float SwingDownKnockbackMultiplier => 1f;
        public static float SpinKnockbackMultiplier => 1.2f;
        public static float PlantedDamageMultiplier => 0.5f;     // each burn hit of the planted blade (times the item's damage)
        public static float PlantedKnockbackMultiplier => 0.01f;
        public static int PlantedHitCooldownTicks => 12;         // an enemy touching the planted blade is hit again after this many ticks
        public static float ReturnDamageFactor => 2f;            // the blade flying back to the hand hits this much harder than the planted blade (on top of PlantedDamageMultiplier: 0.5 x 2 = the item's damage)
        public static int SpinHitCooldownTicks => 6;             // an enemy can be hit again by a spinning swing after this many ticks (strike 9 ticks: 5 = 2 hits, 3 to 4 = 3 hits)

        // ---- blade ----
        public static float BladeLength => 70;       // hand to tip, in pixels at scale 1
        public static float SwordScale => 1.1f;
        public static float HitWidth => 18f;          // width of the blade's hit line at scale 1

        // ---- the combo: swing 1 down, the arm loops over, swing 2 down with a spinning blade, swing 3 up with a spinning blade ----
        // Every swing is a WINDUP (swing 1 and 3: a short pull back; swing 2: the arm keeps rotating from where swing 1 ended all the way
        // over the head), a fast STRIKE that is quickest right after the windup and decelerates, and a short RECOVER.
        public static int AnticipateTicks => 7;       // the windup of swing 1 and swing 3
        public static float AnticipateDeg => 20f;     // how far past the start of the arc the arm pulls back
        public static float AnticipateTiltDeg => 10f; // and the wrist the same way
        public static int LoopTicks => 16;            // the windup of swing 2: the arm loops over the head
        public static float MinLoopDeg => 120f;       // the loop is at least this far round (so a short hop does not skip it)
        public static int SwingTicks => 7;            // the strike of swing 1
        public static int SpinSwingTicks => 14;        // the strike of swings 2 and 3
        public static float SwingEasePower => 2.5f;   // 1 = even speed; higher = faster start and a longer deceleration
        public static float OvershootDeg => 6f;       // the strike carries this far past the end of the arc ...
        public static int RecoverTicks => 5;          // ... and settles back to the end of the arc over this long
        public static int SpinTurns => 1;             // whole turns the blade spins about the hand during the strike of swings 2 and 3
        public static int SweepHitSteps => 8;         // the blade's hit test is checked at this many poses between two ticks (a spin is fast)
        public static float RestArmDeg => 10f;        // where a fresh combo starts
        public static float SwingHighDeg => -70f;     // top of the arc
        public static float SwingLowDeg => 70f;       // bottom of the arc
        public static float TiltHighDeg => -12f;      // blade tilt relative to the arm at the top ...
        public static float TiltLowDeg => 12f;        // ... and at the bottom
        public static int ComboTicks => 30;           // a swing started within this long after the last one continues the combo
        public static int ChainEarlyTicks => 0;       // the next click may start its swing this many ticks BEFORE the strike ends (the swing hands over to it with no gap: the arm is nearly at rest there)
        public static int RemoteLingerTicks => 6;     // other players' copy of a swing holds its last pose this long, waiting for the next swing to arrive over the network (so lag does not make the blade blink)
        public static int FlashTicks => 10;

        // ---- the plant (right click): the blade lifts up, then the arm drives it into the ground in front of the feet while it rotates point-down ----
        // Arm angles here are facing-local (0 = forward, negative = up), not relative to the aim.
        public static int PlantLiftTicks => 9;        // the arm lifts, blade up
        public static int PlantStabTicks => 8;        // the drive: the arm comes down accelerating while the blade rotates from point-up to point-down in the same motion; it lands on the last tick
        public static int PlantRecoverTicks => 9;     // the arm settles back after the blade is planted
        public static float PlantRestArmDeg => 25f;   // where a plant starts when no swing came right before it, and where the arm settles
        public static float PlantRaisedArmDeg => -80f;
        public static float PlantSinkPx => 14f;       // how far the blade's tip sinks into the tile below the ground surface (the tile hides that part: the planted blade is drawn behind tiles)
        public static float PlantReachPx => 20f;      // the planted blade stands this far in front of the player's centre; the arm's end pose is solved to reach it (the arm can only reach so far)
        public static float PlantGroundedDropPx => 24f; // ground within this far below the feet = a standing plant; farther (airborne) = the blade is shot straight down until it hits a tile (no distance limit)
        public static float PlantDropSpeed => 26f;    // the shot blade's speed straight down, in pixels per tick
        public static float PlantedMaxDistancePx => 2400f; // a planted blade returns by itself when its owner is farther away than this (or dies)
        public static float PlantedEmberChance => 0.35f;   // per tick
        public static float PlantShake => 4f;         // camera punch when the blade lands

        // ---- the pull back (any click while the blade is planted or falling): it leaves the ground, spins, flies to the hand, hits what it crosses ----
        public static float ReturnPopSpeed => 7f;     // it pops up out of the ground at this speed before the pull takes over
        public static float ReturnAcceleration => 1.6f; // pull toward the player's current position, per tick (BoomerangFlight.StepForceReturn)
        public static float ReturnDrag => 0.97f;      // velocity kept per tick; below 1 so the flight spirals in instead of orbiting
        public static float ReturnMaxSpeed => 24f;
        public static float ReturnCatchDistance => 28f;
        public static float ReturnSpinSpeed => 0.55f; // radians per tick
        public static int ReturnHitCooldownTicks => 12; // the same enemy can be hit again on the way back after this long
        public static int ReturnHitboxPx => 44;       // the flying blade's square hitbox
        public static int ReturnTimeoutTicks => 420;  // safety: a returning blade that was never caught is removed after this long

        // ---- pixel look ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 22;
        public static float TrailOpacity => 0.8f;
        public static float TailLifeScale => 0.45f;   // the first point of a swing's trail lives this fraction as long as the last one
        public static int TrailRails => 7;
        public static float TrailExpandBy => 0.9f;    // the fan has fully expanded this far into the swing (0..1)
        public static float TrailInner => 0.3f;       // fully expanded, the fan reaches down the blade to this fraction of its length
        public static float TrailOuter => 0.95f;      // the outer edge of the fan, as a fraction of the blade length (1 = the tip)
        public static float TrailLagTicks => 0f;    // how many ticks the trail runs BEHIND the sprite (0 = exactly on it)
        public static int TrailSamplesPerTick => 10;  // trail points laid per tick (a spin is fast, so many keep the fan smooth)

        // ---- colors: one per vanilla Phaseblade (alpha 0 = additive) ----
        public static bool IsPhaseblade(int itemType)
        {
            return itemType == ItemID.BluePhaseblade || itemType == ItemID.RedPhaseblade || itemType == ItemID.GreenPhaseblade
                || itemType == ItemID.PurplePhaseblade || itemType == ItemID.WhitePhaseblade || itemType == ItemID.YellowPhaseblade
                || itemType == ItemID.OrangePhaseblade;
        }

        public static Color ColorOf(int itemType)
        {
            if (itemType == ItemID.RedPhaseblade) return new Color(255, 70, 70, 0);
            if (itemType == ItemID.GreenPhaseblade) return new Color(80, 255, 110, 0);
            if (itemType == ItemID.PurplePhaseblade) return new Color(190, 90, 255, 0);
            if (itemType == ItemID.WhitePhaseblade) return new Color(235, 235, 255, 0);
            if (itemType == ItemID.YellowPhaseblade) return new Color(255, 235, 80, 0);
            if (itemType == ItemID.OrangePhaseblade) return new Color(255, 150, 50, 0);
            return new Color(80, 160, 255, 0); // blue (and the fallback)
        }

        // The hot core of a trail or halo: the blade's color pushed toward white.
        public static Color CoreOf(Color color)
        {
            Color core = Color.Lerp(color, Color.White, 0.75f);
            core.A = 0;
            return core;
        }
    }
}