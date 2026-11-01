using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.IceBlade
{
    /// <summary>
    /// Every tunable number of the Ice Blade rework in one place (ticks: 60 = 1 second). Properties instead of fields, so hot
    /// reload can tweak them. Per-tier values are picked with Pick(tier, tier1, tier2, tier3).
    /// </summary>
    public static class IceBladeTuning
    {
        // ---- charge rhythm: fixed ticks since the press, NOT scaled by melee speed ----
        public static int Click1Ticks => 18;
        public static int Click2Ticks => 36;
        public static int Click3Ticks => 54;
        public static int FreezeClickTicks => 68;     // the 4th click; the sweet spot is the gap between click 3 and this one
        public static int ReleaseGraceTicks => 0;     // extra ticks after the 4th click in which holding still does not freeze yet

        public static int ClickTicks(int click)
        {
            return click <= 1 ? Click1Ticks : click == 2 ? Click2Ticks : click == 3 ? Click3Ticks : FreezeClickTicks;
        }

        // ---- the freeze punishment (vanilla Frozen buff: roots the player and blocks every item) ----
        public static int FreezeDebuffTicks => 140;
        public static int FrozenAnimTicks => 14;      // how long the shaking, frosted blade is shown before it vanishes

        // ---- charge pose (angles are for a player facing right, 0 = forward, positive = down) ----
        public static float ArmStartDeg => 25f;
        public static float ArmEndDeg => -105f;       // fully lifted: up and a little behind the head
        public static int LiftTicks => 54;            // cubic ease-out, ends exactly on click 3
        public static float BladeTiltDeg => -15f;      // the blade stays tilted this far forward from the arm
        public static float BladeLength => 58f;       // hand to tip, in pixels at scale 1
        public static float SwordScale => 1.15f;
        public static int ShakeStartTicks => 16;      // the blade starts to tremble just before the freeze click
        public static float ShakeDeg => 3f;
        public static int ClickFlashTicks => 12;

        // ---- tap swing: a plain melee chop ----
        public static int TapWindupTicks => 3;
        public static int TapSwingTicks => 9;
        public static int TapRecoverTicks => 8;
        public static float TapStartDeg => -80f;
        public static float TapEndDeg => 60f;

        // ---- release slash ----
        public static int SlashTicks => 9;
        public static int SlashWaveTick => 4;         // the wave leaves the blade on this tick of the slash
        public static int SlashRecoverTicks => 8;
        public static float SlashMaxAimDeg => 65f;    // the visual slash follows the cursor up to this angle
        public static int TrailLifeTicks => 26;
        public static int TrailMaxSamples => 80;
        public static float TrailStartFraction => 0.25f; // the fan trail covers the outer part of the blade
        public static float TrailThickness(int tier) { return Pick(tier, 3f, 5f, 8f); }

        // ---- the wave (tier 1, 2, 3) ----
        public static float WaveDamageMultiplier(int tier) { return Pick(tier, 0.55f, 1f, 1.8f); }
        public static float WaveKnockbackMultiplier(int tier) { return Pick(tier, 0.8f, 1f, 1.4f); }
        public static float WaveSpeed(int tier) { return Pick(tier, 11f, 13f, 16f); }
        public static int WaveLifeTicks(int tier) { return Pick(tier, 34, 40, 46); }
        public static float WaveRadius(int tier) { return Pick(tier, 30f, 40f, 54f); }      // radius of the crescent's circle, at scale 1
        public static float WaveThickness(int tier) { return Pick(tier, 7f, 10f, 14f); }    // thickness in the middle of the crescent
        public static float WaveArcHalfDeg => 58f;
        public static float WaveDrag => 0.965f;       // velocity multiplier per tick: it slows down while it expands
        public static float WaveGrowth => 0.7f;       // the crescent ends up this much bigger (1 + growth) at the end of its life

        // ---- pixel look ----
        public static float PixelSize => 2f;
        public static int AfterimageCount => 7;       // at most 7 (the projectile's trail cache holds 8)
        public static Color HaloColor => new Color(60, 140, 255, 0);
        public static Color MidColor => new Color(110, 200, 255, 0);
        public static Color CoreColor => new Color(235, 250, 255, 0);

        static T Pick<T>(int tier, T one, T two, T three)
        {
            return tier <= 1 ? one : tier == 2 ? two : three;
        }
    }
}