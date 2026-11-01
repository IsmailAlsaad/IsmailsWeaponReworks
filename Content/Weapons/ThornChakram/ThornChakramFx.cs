using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.ThornChakram
{
    /// <summary>All dust and sound for the Thorn Chakram (vanilla dust and sounds only). No camera shake.</summary>
    public static class ThornChakramFx
    {
        static int GreenType()
        {
            return Main.rand.NextBool(2) ? ThornChakramTuning.LeafDust : ThornChakramTuning.PoisonDust;
        }

        public static void Puff(Vector2 center, int count, float speed, float scale)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, GreenType(), Main.rand.NextVector2Circular(speed, speed), 0, default, scale * Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }
        }

        public static void HitSparks(Vector2 position)
        {
            Puff(position, 5, 3f, 1f);
        }

        public static void Ring(Vector2 center, int count, float radius, float outwardSpeed)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / count).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(center + direction * radius, GreenType(), direction * outwardSpeed, 0, default, 1.2f);
                dust.noGravity = true;
            }
        }

        // A single leaf left behind a moving thing.
        public static void Trail(Vector2 center, Vector2 velocity, float scale)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(center, GreenType(), velocity * -0.1f + Main.rand.NextVector2Circular(1f, 1f), 0, default, scale);
            dust.noGravity = true;
        }

        // The shredding: leaves and blood flying off the enemy.
        public static void Shred(Vector2 center, int count)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                int type = Main.rand.NextBool(2) ? ThornChakramTuning.BloodDust : GreenType();
                Dust dust = Dust.NewDustPerfect(center, type, Main.rand.NextVector2Circular(5f, 5f), 0, default, Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = Main.rand.NextBool(2);
            }
        }

        public static void Catch(Vector2 position)
        {
            Puff(position, 6, 2.5f, 1f);
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.4f, Pitch = -0.2f }, position);
        }

        public static void PlayBurst(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.5f, PitchVariance = 0.2f }, position);
        }

        public static void PlayLatch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item22 with { Volume = 0.7f, Pitch = 0.2f }, position);
        }

        // spinT: 0 at the start of the latch, 1 at the end; the shred sound climbs with the spin.
        public static void PlayShred(Vector2 position, float spinT)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item22 with { Volume = 0.4f, Pitch = 0.1f + 0.6f * spinT, PitchVariance = 0.1f }, position);
        }

        public static void PlayExplosion(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = 0.2f, PitchVariance = 0.2f }, position);
        }
    }
}