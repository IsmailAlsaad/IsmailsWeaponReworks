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

        // ---- the orbs ----
        public static float OrbRadius => 64f;            // pixels from the middle of the player
        public static float OrbBob => 5f;                // the radius breathes by this much
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