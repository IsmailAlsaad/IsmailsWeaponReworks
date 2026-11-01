using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.IceBlade
{
    /// <summary>All dust and sound for the Ice Blade (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class IceBladeFx
    {
        // ---- sounds ----

        // Clicks 1 to 3: a crack that rises in pitch.
        public static void PlayClick(Vector2 position, int click)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.55f, Pitch = -0.5f + 0.375f * (click - 1) }, position);
        }

        // Click 4: low and harsh, the warning that the hand is about to freeze.
        public static void PlayFreezeClick(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.9f, Pitch = -0.75f }, position);
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f, Pitch = -0.9f }, position);
        }

        public static void PlayFreeze(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.9f, Pitch = -0.3f }, position);
        }

        public static void PlayTapSwing(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, position);
        }

        public static void PlaySlash(Vector2 position, int tier)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.2f + 0.2f * tier }, position);
            SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.5f + 0.1f * tier, Pitch = 0.1f + 0.2f * tier }, position);
        }

        public static void PlayWaveShatter(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, PitchVariance = 0.25f }, position);
        }

        // ---- charge ----

        public static void ClickBurst(Vector2 tip, int click)
        {
            IceFx.Burst(tip, 6 + 5 * click, 2.5f + click, 1.1f);
            IceFx.Ring(tip, 8 + 4 * click, 5f + 2f * click, 2f + click);
        }

        public static void FreezeClickBurst(Vector2 tip)
        {
            IceFx.Burst(tip, 18, 5f, 1.4f);
        }

        public static void ChargeGather(Vector2 tip, int tier)
        {
            IceFx.Gather(tip, 18f + 8f * tier);
        }

        // A spark somewhere along the blade.
        public static void BladeSparkle(Vector2 hand, Vector2 tip, int tier)
        {
            if (Main.dedServ)
                return;
            Vector2 spot = Vector2.Lerp(hand, tip, Main.rand.NextFloat());
            Dust dust = Dust.NewDustPerfect(spot, DustID.IceTorch, Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.3f), 100, default, 0.8f + 0.2f * tier);
            dust.noGravity = true;
        }

        // Frost creeping over the hand between click 3 and click 4 (amount 0 to 1).
        public static void FrostOnHand(Vector2 hand, float amount)
        {
            if (Main.dedServ || Main.rand.NextFloat() > 0.25f + 0.6f * amount)
                return;
            Dust dust = Dust.NewDustPerfect(hand + Main.rand.NextVector2Circular(9f, 9f), DustID.Ice, Main.rand.NextVector2Circular(0.5f, 0.5f), 120, default, 1f + amount);
            dust.noGravity = true;
        }

        public static void FreezeShatter(Vector2 hand)
        {
            IceFx.Burst(hand, 34, 6.5f, 1.7f);
            IceFx.Ring(hand, 22, 10f, 4.5f);
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(hand + Main.rand.NextVector2Circular(8f, 8f), DustID.Ice, Main.rand.NextVector2Circular(2f, 2f), 150, default, 2.4f);
                dust.noGravity = true;
            }
        }

        // ---- slash ----

        // Dust along the path the blade tip took this tick.
        public static void SlashDust(Vector2 from, Vector2 to, int tier)
        {
            if (Main.dedServ)
                return;
            int count = 2 + 2 * tier;
            for (int i = 0; i < count; i++)
            {
                Vector2 spot = Vector2.Lerp(from, to, Main.rand.NextFloat());
                int dustType = Main.rand.NextBool(3) ? DustID.IceTorch : DustID.Ice;
                Dust dust = Dust.NewDustPerfect(spot, dustType, Main.rand.NextVector2Circular(1f + 0.6f * tier, 1f + 0.6f * tier), 100, default, 1.2f + 0.2f * tier);
                dust.noGravity = true;
            }
        }

        // ---- wave ----

        public static void WaveBorn(Vector2 center, Vector2 direction, int tier)
        {
            IceFx.Burst(center, 10 + 6 * tier, 4f + tier, 1.3f);
            IceFx.Ring(center + direction * 12f, 12 + 4 * tier, 6f, 3f + tier);
        }

        // One puff of blue fog (large, see-through Ice dust).
        public static void WaveFog(Vector2 position, Vector2 velocity, float scale)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(position, DustID.Ice, velocity, 150, default, scale);
            dust.noGravity = true;
        }

        public static void WaveSpark(Vector2 position, Vector2 velocity, float scale)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(position, DustID.IceTorch, velocity, 80, default, scale);
            dust.noGravity = true;
        }
    }
}