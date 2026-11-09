using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// Every dust and sound of the yoyo rework (vanilla dust and sounds only), so a later juice pass edits one file. Every machine calls
    /// these for the same event; all of it is visual, nothing here is synced.
    /// </summary>
    public static class YoyoFx
    {
        static void Chip(Vector2 position, Vector2 velocity, float scale)
        {
            Dust.NewDustPerfect(position, YoyoTuning.WoodDust, velocity, 0, default, scale);
        }

        // The throw: a swish and a few chips shaken off.
        public static void Throw(Vector2 position, int side)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.25f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < 3; i++)
                Chip(position, new Vector2(side * Main.rand.NextFloat(0.5f, 2f), Main.rand.NextFloat(-1.5f, 0.5f)), Main.rand.NextFloat(0.7f, 1f));
        }

        // The yoyo hits the ground: a thump and a puff of chips and dust, more for a harder landing.
        public static void Land(Vector2 position, float impact, int gravDir)
        {
            if (Main.dedServ)
                return;
            float strength = MathHelper.Clamp(impact / 8f, 0.3f, 1.2f);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.3f + 0.3f * strength, Pitch = 0.3f, PitchVariance = 0.15f }, position);
            int count = 2 + (int)(5f * strength);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), -gravDir * Main.rand.NextFloat(0.5f, 2.5f) * strength);
                Chip(position, velocity, Main.rand.NextFloat(0.7f, 1.1f));
                if (i % 2 == 0)
                {
                    Dust dust = Dust.NewDustPerfect(position, DustID.Smoke, velocity * 0.5f, 120, default, 0.8f);
                    dust.noGravity = true;
                }
            }
        }

        // An enemy hit: a knock, a burst of chips, a rising pitch with every hit of the throw.
        public static void Hit(Vector2 position, int hitNumber)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = 0.1f + 0.2f * hitNumber, PitchVariance = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.45f, Pitch = 0.5f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < 8; i++)
                Chip(position, Main.rand.NextVector2Circular(3.5f, 3.5f), Main.rand.NextFloat(0.8f, 1.2f));
        }

        // The yoyo is let go of and starts back.
        public static void Retract(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.4f, Pitch = 0.4f, PitchVariance = 0.1f }, position);
        }

        // Caught: a small pop.
        public static void Catch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.5f, Pitch = 0.7f }, position);
            for (int i = 0; i < 4; i++)
                Chip(position, Main.rand.NextVector2Circular(1.5f, 1.5f), 0.8f);
        }
    }
}