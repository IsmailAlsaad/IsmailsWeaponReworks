using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>All vanilla dust and sound for the Volcano, so a later juice pass edits one file.</summary>
    public static class VolcanoFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position, float heat)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.1f - 0.3f * heat, PitchVariance = 0.1f }, position);
            if (heat > 0.4f)
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.25f + 0.4f * heat, Pitch = -0.3f, PitchVariance = 0.1f }, position);
        }

        public static void PlayHit(Vector2 position, float heat)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.35f + 0.35f * heat, Pitch = -0.2f + 0.2f * heat, PitchVariance = 0.15f }, position);
        }

        public static void PlayCharged(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.1f }, position);
        }

        // ---- dust ----

        // One vanilla fire spark drifting up off the blade.
        public static void Ember(Vector2 position, Vector2 motion)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(position, DustID.Torch, new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(0.4f, 1.6f)) + motion * 0.2f, 100, default, Main.rand.NextFloat(0.9f, 1.5f));
            dust.noGravity = true;
        }

        // Sparks flung from the tip while the blade strikes.
        public static void SwingEmbers(Vector2 tip, Vector2 motion, float heat)
        {
            if (Main.dedServ)
                return;
            int count = 1 + (int)(heat * 2.5f);
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(6f, 6f), DustID.Torch, motion * Main.rand.NextFloat(0.1f, 0.35f) + Main.rand.NextVector2Circular(1.2f, 1.2f), 100, default, Main.rand.NextFloat(1f, 1.8f));
                dust.noGravity = Main.rand.NextBool(3) ? false : true;
            }
        }

        // A hit: a burst of sparks and a few lava dust that fall.
        public static void HitSpark(Vector2 position, float heat)
        {
            if (Main.dedServ)
                return;
            PlayHit(position, heat);
            int count = 6 + (int)(heat * 8f);
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.Torch, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(2f, 5f + 3f * heat), 100, default, Main.rand.NextFloat(1.2f, 2f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 2 + (int)(heat * 3f); i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.Lava, Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0f, -1.5f), 0, default, Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = false;
            }
        }

        // The sword reaches full heat: a ring of sparks around the blade and a roar.
        public static void Charged(Vector2 joint, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            PlayCharged(joint);
            for (int i = 0; i < 24; i++)
            {
                Vector2 point = Vector2.Lerp(joint, tip, Main.rand.NextFloat());
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust dust = Dust.NewDustPerfect(point, DustID.Torch, direction * Main.rand.NextFloat(2f, 5f), 100, default, Main.rand.NextFloat(1.4f, 2.2f));
                dust.noGravity = true;
            }
        }
    }
}