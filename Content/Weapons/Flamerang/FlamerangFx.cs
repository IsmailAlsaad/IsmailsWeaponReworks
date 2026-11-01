using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Flamerang
{
    /// <summary>All dust and sound for the Flamerang (vanilla dust and sounds only).</summary>
    public static class FlamerangFx
    {
        static int FireDust()
        {
            int roll = Main.rand.Next(4);
            return roll == 0 ? DustID.GoldFlame : roll == 1 ? DustID.InfernoFork : DustID.Torch;
        }

        // Sparks and flame shed by the flying boomerang.
        public static void FlightDust(Vector2 position, Vector2 velocity, float strength)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;
            Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), FireDust(), -velocity * 0.08f + Main.rand.NextVector2Circular(0.8f, 0.8f), 80, default, 1f + 0.5f * strength);
            dust.noGravity = true;
        }

        // Heat gathering toward the boomerang while it winds down at the end of its range.
        public static void Gather(Vector2 center, float radius)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;
            Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
            Dust dust = Dust.NewDustPerfect(center + offset, FireDust(), -offset * 0.1f, 80, default, 1.2f);
            dust.noGravity = true;
        }

        public static void HitSparks(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(6f, 6f), FireDust(), Main.rand.NextVector2Circular(3.5f, 3.5f), 80, default, 1.1f);
                dust.noGravity = true;
            }
        }

        public static void Catch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, FireDust(), Main.rand.NextVector2Circular(2.5f, 2.5f), 80, default, 1f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.45f, Pitch = -0.1f }, position);
        }

        // The fire explosion: a ball of flame, flying sparks and smoke.
        public static void Blast(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            float reach = radius / 96f;
            for (int i = 0; i < 50; i++)
            {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(radius * 0.25f, radius * 0.25f), FireDust(), Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 9f) * reach, 60, default, Main.rand.NextFloat(1.2f, 2.2f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(radius * 0.5f, radius * 0.5f), DustID.Smoke, Main.rand.NextVector2Circular(1.5f, 1.5f) + new Vector2(0f, -0.8f), 120, default, 1.8f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f, Pitch = 0.1f }, center);
        }

        // A molten boomerang is born out of the explosion.
        public static void MoltenBorn(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, FireDust(), direction * Main.rand.NextFloat(1f, 6f) + Main.rand.NextVector2Circular(1.5f, 1.5f), 60, default, 1.3f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.45f, Pitch = 0.1f }, position);
        }

        // A lick of flame at a point of the trail.
        public static void TrailEmber(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), FireDust(), new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.4f, -0.2f)), 90, default, Main.rand.NextFloat(0.9f, 1.5f));
            dust.noGravity = true;
        }

        // A shard lands.
        public static void ShardLand(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, i % 2 == 0 ? DustID.Torch : DustID.Smoke, Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0f, -1f), 100, default, 1f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.35f, Pitch = -0.4f, PitchVariance = 0.2f }, position);
        }

        // A hot shard shedding a spark.
        public static void ShardGlow(Vector2 position, float heat)
        {
            if (Main.dedServ || Main.rand.NextFloat() > 0.35f * heat)
                return;
            Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(6f, 6f), DustID.Torch, new Vector2(0f, -0.6f) + Main.rand.NextVector2Circular(0.4f, 0.4f), 100, default, 0.9f);
            dust.noGravity = true;
        }
    }
}