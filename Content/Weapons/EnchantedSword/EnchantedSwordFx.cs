using IsmailsWeaponReworks.Content.Weapons.EnchantedBoomerang;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// Every dust and sound of the Enchanted Sword rework (vanilla dust and sounds only), in the Enchanted Boomerang's stardust style: gold,
    /// white, blue and pink sparkles that drift without gravity. Every machine calls these for the same event; all of it is visual, nothing
    /// here is synced. 'stage' is 0..2 (the drawn outline stage).
    /// </summary>
    public static class EnchantedSwordFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position, bool thrust)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = thrust ? 0.35f : 0f, PitchVariance = 0.1f }, position);
        }

        public static void PlayHit(Vector2 position, float stage)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.4f + 0.1f * stage, Pitch = 0.2f + 0.2f * stage, PitchVariance = 0.15f }, position);
        }

        // ---- dust ----

        static int StardustType()
        {
            switch (Main.rand.Next(4))
            {
                case 0: return DustID.Enchanted_Gold;
                case 1: return DustID.MagicMirror;
                case 2: return DustID.BlueTorch;
                default: return DustID.PinkFairy;
            }
        }

        static void Mote(Vector2 position, Vector2 velocity, float scale, int alpha = 80)
        {
            Dust dust = Dust.NewDustPerfect(position, StardustType(), velocity, alpha, default, scale);
            dust.noGravity = true;
        }

        // Stardust flung from the tip while the blade strikes (more of it at the higher stages).
        public static void SwingDust(Vector2 tip, Vector2 motion, float stage)
        {
            if (Main.dedServ)
                return;
            int count = 1 + (int)(stage * 1.2f);
            for (int i = 0; i < count; i++)
                Mote(tip + Main.rand.NextVector2Circular(5f, 5f), motion * Main.rand.NextFloat(0.1f, 0.3f) + Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(0.9f, 1.4f));
        }

        // A true melee hit: a burst of stardust and a hit sound.
        public static void HitSpark(Vector2 position, float stage)
        {
            if (Main.dedServ)
                return;
            PlayHit(position, stage);
            int count = 6 + (int)(stage * 3f);
            for (int i = 0; i < count; i++)
                Mote(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(2f, 4.5f + stage), Main.rand.NextFloat(1.1f, 1.7f), 60);
        }

        // The outline just got stronger: a ring of stardust around the middle of the blade, a spray along it and a chime that rises with the stage.
        public static void StageUp(Vector2 hand, Vector2 tip, int stage)
        {
            if (Main.dedServ)
                return;
            Vector2 middle = Vector2.Lerp(hand, tip, 0.5f);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.1f + 0.15f * stage }, middle);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.3f, Pitch = 0.1f + 0.15f * stage }, middle);
            int ring = 10 + 4 * stage;
            for (int i = 0; i < ring; i++)
                Mote(middle, (MathHelper.TwoPi * i / ring).ToRotationVector2() * (3.5f + stage), 1.3f, 60);
            for (int i = 0; i < 8 + 4 * stage; i++)
                Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 3f), Main.rand.NextFloat(1f, 1.6f));
        }

        // The 3rd hit: the Enchanted Boomerang's stardust burst where the enemy is, and a flash along the blade.
        public static void Discharge(Vector2 center, Vector2 hand, Vector2 tip, float radius)
        {
            if (Main.dedServ)
                return;
            EnchantedBoomerangFx.Burst(center, radius);
            for (int i = 0; i < 20; i++)
                Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f), Main.rand.NextFloat(1.2f, 1.9f), 60);
        }

        // A mote somewhere along an outlined blade.
        public static void Sparkle(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.3f), 0.9f, 100);
        }
    }
}