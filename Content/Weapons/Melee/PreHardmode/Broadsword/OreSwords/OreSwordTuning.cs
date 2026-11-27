namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.OreSwords
{
    /// <summary>
    /// The tunable numbers that belong to the ore broadswords alone (ticks: 60 = 1 second, lengths in pixels). The SWING itself (the arm, the
    /// chained chops, the idle pose, the hit line) is exactly the wooden swords': OreSwordBladeTune forwards it to WoodenSwordTuning, so a
    /// change there changes both. Only the trail's geometry and look and the hit flare are separate. The per-metal colors are in OreSwordMetals.
    /// Properties instead of fields, so hot reload can tweak them.
    /// </summary>
    public static class OreSwordTuning
    {
        // ---- swing trail geometry (a SweepTrail fan: the outer edge past the tip, the inner edge slides down the blade as the strike builds up) ----
        public static int TrailSamplesPerTick => 8;     // trail points laid per tick
        public static float TrailLagTicks => 0.4f;      // how many ticks the trail runs BEHIND the sprite (0 = exactly on it)
        public static float TrailOuter => 0.9f;        // the outer edge of the fan, as a fraction of the blade length (1 = the tip)
        public static float TrailInnerLow => 0.7f;      // fully expanded, the fan reaches down the blade to this fraction of its length (no charge: always this one) ...
        public static float TrailInnerHigh => 0.2f;     // ... (only a charged blade uses this one; the ore swords have no charge)
        public static float TrailExpandBy => 0.8f;      // the fan has fully expanded this far into the strike (0..1)

        // ---- swing trail look ----
        public static float PixelSize => 2f;
        public static int TrailLifeTicks => 12;
        public static float TailLifeScale => 0.45f;     // the first point of a swing's trail lives this fraction as long as the last one
        public static int TrailRails => 6;              // parallel pixel lines that fill the fan
        public static float TrailOpacity => 0.28f;      // faint

        // ---- hit flare (Assets/Effects/SmallFlare.png) ----
        public static int FlareLifeTicks => 18;
        public static float FlarePeakScale => 0.9f;     // 1 = the PNG's own size (33 px)
        public static float FlareGrowFraction => 0.3f;  // the first this much of its life it grows, then it shrinks to 0
        public static float FlareSpinMin => 0.02f;      // radians per tick, constant over the life, random direction
        public static float FlareSpinMax => 0.06f;
        public static float FlareTint => 1f;          // 0 = white, 1 = fully the metal's color
        public static float FlareOpacity => 1f;
        public static float FlareLight => 0.3f;
        public static int FlareMax => 24;               // flares alive at once per player (one more removes the oldest)

        // ---- hit sound (Assets/Sounds/MetalSword/MetalSword1..N) ----
        public static int HitSoundCount => 3;           // how many MetalSword files there are (MetalSword1 to MetalSwordN)
        public static float HitSoundVolume => 0.8f;
        public static float HitSoundPitch => 0f;
        public static float HitSoundPitchVariance => 0.1f;
        public static int HitSoundMaxInstances => 1;    // hits that can sound at the same time
    }
}