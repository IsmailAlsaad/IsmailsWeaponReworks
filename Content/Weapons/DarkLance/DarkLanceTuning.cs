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
        public static float OrbSpin => 0.045f;           // radians per tick
        public static float OrbEase => 0.2f;             // how fast an orb glides to its new slot when one is added or popped
        public static int OrbSpawnTicks => 14;           // an orb flies from the lance tip out to its orbit over this long
        public static int OrbLifeTicks => 1800;          // an orb that is never collected fizzles after 30 s
        public static float OrbHitSize => 26f;           // the pop zone: a box this wide around the orb
        public static int OrbPopGraceTicks => 20;        // a new orb cannot be popped for this long
        public static float PopRadius => 110f;           // the Shadowflame cloud when an enemy pops an orb
        public static int PopBuffTicks => 300;           // Shadowflame duration (5 s)
        public static float PopShake => 1.5f;            // camera punch of a pop (nearby players)

        // ---- look ----
        public static int AfterimageTicks => 10;
        public static float PixelSize => 2f;             // world px per shader pixel (the orb's shader and the afterimages)
        public static int ColorBands => 5;               // the orb shader's hard color bands
        public static int OrbQuadSize => 80;             // world px: the square the orb shader is drawn over (must cover the glow)
        public static float OrbUnitPx => 28f;            // world px per shader unit (the orb's body radius is half of it)
        public static ThrustPalette Palette => new ThrustPalette(new Color(240, 215, 255), new Color(160, 60, 235), new Color(70, 20, 130), DustID.Shadowflame, DustID.PurpleTorch);
    }
}