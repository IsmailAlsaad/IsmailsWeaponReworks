using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.FlowerOfFire
{
    /// <summary>
    /// Every tunable number of the Flower of Fire shader fireball test (ticks: 60 = 1 second, distances in pixels: 16 = 1 tile).
    /// Properties instead of fields, so hot reload can tweak them.
    /// </summary>
    public static class FlowerOfFireTuning
    {
        // ---- flight ----
        public static float Speed => 11f;
        public static int FlightTicks => 150;      // after this long without a hit it detonates in mid-air
        public static float HitSize => 20f;
        public static int BurnTicks => 180;        // On Fire from a hit
        public static int BurstTicks => 18;        // how long the detonation shader plays

        // ---- the shader look ----
        public static float PixelSize => 2f;       // world px per shader pixel (the project's pixel grid)
        public static float UnitScale => 64f;      // world px per shader unit; the head radius is about 0.26 units
        public static int QuadSize => 192;         // side of the square the shader is drawn on, in world px (must cover the tail and the burst)
        public static float FlameSpeed => 1f;      // flame time per real second
        public static int ColorBands => 6;
        public static float Dither => 1f;          // 0 = hard bands, 1 = Bayer dither
        public static Vector3 LightColor => new Vector3(1.0f, 0.55f, 0.15f);
    }
}