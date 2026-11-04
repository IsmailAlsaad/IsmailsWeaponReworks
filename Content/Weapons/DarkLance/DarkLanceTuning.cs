using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// Every tunable number of the Dark Lance rework in one place (ticks: 60 = 1 second, 16 pixels = 1 tile).
    /// Properties instead of fields, so hot reload can tweak them. Patch 6: right click (hold) charges dark flame orbs that orbit the
    /// player; the left click sweep and thrust come in patch 7.
    /// </summary>
    public static class DarkLanceTuning
    {
        // ---- the charge (right click, hold) ----
        public static int ChargeIntervalTicks => 20;     // one orb every this many ticks while the button is held
        public static int MaxOrbs => 8;                  // orbs alive at once
        public static int ChargeWindDownTicks => 8;      // after the button is released the lance stays up this long
        public static int ChargeCooldownTicks => 10;     // before the lance can be used again
        public static float LanceScale => 1.15f;
        public static float LanceTilt => 0.1f;          // radians the raised lance leans toward the side the player faces
        public static float ArmRaise => 1f;              // itemRotation magnitude: above 0.75 vanilla picks the raised arm frame
        public static float HandOffsetX => 4f;           // pixels from the middle of the player to the butt of the raised lance
        public static float HandOffsetY => -4f;
        public static float TremblePerOrb => 0.3f;       // pixels the lance shakes per orb

        // ---- left click: a sweep, then a thrust, then a sweep ... (hold the button to keep cycling) ----
        public static int SweepWindupTicks => 5;         // the lance is pulled back
        public static int SweepTicks => 20;              // one fast 360 degree turn (smooth start and end)
        public static int SweepRecoverTicks => 6;        // it stays at the start aim before the next click may begin
        public static float SweepPullBack => 0.6f;       // radians the windup pulls back against the turn
        public static int SweepCooldownTicks => 4;
        public static float SweepDamageMultiplier => 0.8f;   // times the item's damage; every enemy once per sweep
        public static float SweepScale => 1.15f;
        public static float SweepGrip => 0.25f;          // how much of the shaft sticks out behind the hand
        public static float SweepHitWidth => 34f;
        public static float SweepMinHitSpeed => 0.25f;   // 0 to 1 of the fastest tick: slower than this neither hits nor collects
        public static float CollectSize => 30f;          // the box around an orb the lance has to touch to gather it
        public static float CollectWidth => 36f;         // thickness of the lance line when it gathers
        public static float CollectMinSpeed => 0.2f;
        public static int CollectTicks => 10;            // a gathered orb flies into the lance tip over this long
        public static int SweepTrailLifeTicks => 14;
        public static int SweepTrailRails => 5;
        public static float SweepTrailOpacity => 0.8f;
        public static float SweepTrailInner => 0.6f;     // how far down the lance the fan reaches
        public static float SweepEmberChance => 0.7f;    // chance per tick (times the speed) that the tip sheds an ember

        // ---- the thrust (every orb the sweep gathered makes it bigger and stronger) ----
        public static int ThrustWindupTicks => 9;
        public static int ThrustTicks => 4;
        public static int ThrustHoldTicks => 3;
        public static int ThrustRecoverTicks => 14;
        public static float ThrustPullBack => 0.3f;
        public static float ThrustEasePower => 2f;
        public static int ThrustCooldownTicks => 24;
        public static float ThrustReach => 70f;
        public static float ThrustScale => 1.15f;
        public static float ThrustGrip => 0.35f;
        public static float ThrustHitWidth => 26f;
        public static float ThrustDamageMultiplier => 1.7f;  // times the item's damage, before the orbs
        public static float SizePerOrb => 0.12f;         // +12% size (scale, reach and hit width) per gathered orb
        public static float DamagePerOrb => 0.15f;       // +15% damage per gathered orb
        public static int ThrustTrailLifeTicks => 12;
        public static int ThrustTrailRails => 5;
        public static float ThrustTrailOpacity => 0.8f;
        public static ThrustChain ThrustBurst => new ThrustChain(
            new ThrustTimeline(ThrustWindupTicks, ThrustTicks, ThrustHoldTicks, ThrustRecoverTicks, ThrustPullBack, ThrustEasePower),
            1, 0, 0f, ThrustCooldownTicks);

        // ---- the explosion (a thrust with all 8 orbs, on the first enemy it hits) ----
        public static float BlastRadius => 150f;
        public static float BlastDamageMultiplier => 1.5f;   // times the thrust's damage, every enemy in the circle once
        public static int BlastLifeTicks => 8;
        public static float BlastShake => 4f;
        public static int BlastBuffTicks => 360;         // Shadowflame on everything it reaches

        // ---- the orbs ----
        public static float OrbRadius => 64f;            // pixels from the middle of the player
        public static float OrbBob => 2.5f;                // the radius breathes by this much
        public static float OrbSpin => 0.015f;           // radians per tick
        public static float OrbEase => 0.1f;             // how fast an orb glides to its new slot when one is added or popped
        public static int OrbSpawnTicks => 14;           // an orb flies from the lance tip out to its orbit over this long
        public static int OrbLifeTicks => 1800;          // an orb that is never collected fizzles after 30 s
        public static float OrbHitSize => 26f;           // the pop zone: a box this wide around the orb
        public static int OrbPopGraceTicks => 20;        // a new orb cannot be popped for this long
        public static float PopRadius => 110f;           // the Shadowflame cloud when an enemy pops an orb
        public static int PopBuffTicks => 300;           // Shadowflame duration (5 s)
        public static float PopShake => 1f;            // camera punch of a pop (nearby players)

        // ---- look ----
        public static int TrailLifeTicks => 10;          // the orb's trail (Common PixelTrail) fades out over this long
        public static float TrailWidth => 12f;           // px at its newest end; it shrinks as it ages
        public static float PixelSize => 2f;             // world px per shader pixel (the orb's shader and the afterimages)
        public static int ColorBands => 5;               // the orb shader's hard color bands
        // (the orb body is drawn by the metaball layer; its halo is DarkLanceFx.DrawGlow, which reads HaloReach, OrbUnitPx and HaloStrength)
        public static float OrbUnitPx => 28f;            // world px per shader unit (the orb's body radius is half of it)
        public static float HaloReach => 1.5f;           // the halo fades to nothing this many units beyond the ball (smooth falloff)
        public static float HaloStrength => 0.8f;        // brightness of the halo right at the ball (0 to 1)
        // embers: world particles drawn as merging blobs (DarkLanceEmbers), not anchored to the orb after they are born
        public static int EmberSpawnTicks => 3;          // each orb sheds one ember every this many ticks
        public static int EmberLifeTicks => 50;          // average life; each ember gets 0.7 to 1.3 times this
        public static float EmberSize => 5f;             // px: radius at birth (random 0.6 to 1.4 times this); it shrinks to nothing
        public static float EmberMerge => 2.2f;          // how far an ember's field reaches, in radii: bigger = blobs join from further away and bridge more
        public static float EmberOutline => 0f;        // thickness of the merged outline (field units; 0.1 = thin, 0.4 = thick)
        public static float EmberRise => 0.03f;          // upward push per tick (px per tick squared)
        public static float EmberDrag => 0.97f;          // velocity kept per tick
        public static float EmberKick => 1.1f;           // px per tick: the speed an ember leaves the rim with (upward, random sideways)
        public static float EmberInherit => 0.35f;       // how much of the orb's own velocity an ember gets at birth (after that it is on its own)
        public static float EmberWobble => 0.05f;        // sideways wander (px per tick squared)
        public static float EmberGlow => 0.3f;           // brightness of the soft glow around the blobs (0 = none)
        public static int EmberMax => 400;               // all machines together; the oldest are dropped first
        public static float EmberHeat => 0.7f;          // temperature of an ember at birth (0 cold, 0.5 = the mid color, 1 = the bright core color); it cools to 0
        public static float BallRadius => 10f;           // px: the orb's body (a lone blob is this big; merged blobs and tongues make it a little bigger)
        public static float BallBodyHeat => 0.5f;        // the body's temperature (the mid purple)
        public static float BallCoreHeat => 1f;          // the small hot core in the middle (the bright color)
        public static float TipGlowMinRadius => 16f;     // px: the glow at the lance tip with no charge
        public static float TipGlowMaxRadius => 46f;     // px: fully charged
        public static float TipGlowFalloff => 1.6f;      // 1 = soft cone, 2 = tight bright core
        public static float TipGlowStrength => 0.9f;
        public static ThrustPalette Palette => new ThrustPalette(new Color(240, 215, 255), new Color(160, 60, 235), new Color(70, 20, 130), DustID.Shadowflame, DustID.PurpleTorch);
    }
}