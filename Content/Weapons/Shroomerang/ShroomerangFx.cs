using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Shroomerang
{
    /// <summary>All dust and sound for the Shroomerang (vanilla dust and sounds only). No camera shake.</summary>
    public static class ShroomerangFx
    {
        static int SporeType()
        {
            return Main.rand.NextBool(3) ? DustID.MagicMirror : DustID.BlueTorch;
        }

        // A spore shed while flying.
        public static void FlightDust(Vector2 position, Vector2 velocity)
        {
            if (Main.dedServ || Main.rand.NextBool(2))
                return;
            Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), SporeType(), -velocity * 0.08f + Main.rand.NextVector2Circular(0.8f, 0.8f), 100, default, 1.1f);
            dust.noGravity = true;
        }

        public static void SporePuff(Vector2 center, int count, float speed, float scale)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, SporeType(), Main.rand.NextVector2Circular(speed, speed), 100, default, scale * Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }
        }

        public static void HitSparks(Vector2 position)
        {
            SporePuff(position, 6, 3.5f, 1.1f);
        }

        public static void SporeRing(Vector2 center, int count, float radius, float outwardSpeed)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / count).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(center + direction * radius, SporeType(), direction * outwardSpeed, 100, default, 1.3f);
                dust.noGravity = true;
            }
        }

        // Slow spores hanging in the air after a burst (called a few ticks in a row).
        public static void SporeCloud(Vector2 center, float radius, int count)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(radius, radius), SporeType(), new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-0.8f, -0.1f)), 120, default, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
        }

        // A single spore left behind a moving thing.
        public static void Trail(Vector2 center, Vector2 velocity, float scale)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(center, SporeType(), velocity * -0.1f + Main.rand.NextVector2Circular(1f, 1f), 100, default, scale);
            dust.noGravity = true;
        }

        public static void Catch(Vector2 position)
        {
            SporePuff(position, 8, 3f, 1f);
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = 0.2f }, position);
        }

        public static void PlayPop(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.25f, Pitch = 0.5f, PitchVariance = 0.3f }, position);
        }

        public static void PlayBurst(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.5f, PitchVariance = 0.2f }, position);
        }
    }
}