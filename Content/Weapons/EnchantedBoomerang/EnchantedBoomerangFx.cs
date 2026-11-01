using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedBoomerang
{
    /// <summary>All dust and sound for the Enchanted Boomerang (vanilla dust and sounds only).</summary>
    public static class EnchantedBoomerangFx
    {
        static int SparkleType(bool ethereal)
        {
            return ethereal ? DustID.MagicMirror : DustID.Enchanted_Gold;
        }

        // A sparkle shed while flying.
        public static void FlightDust(Vector2 position, Vector2 velocity, bool ethereal)
        {
            if (Main.dedServ || Main.rand.NextBool(2))
                return;
            Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), SparkleType(ethereal), -velocity * 0.05f + Main.rand.NextVector2Circular(0.5f, 0.5f), ethereal ? 120 : 80, default, ethereal ? 1.1f : 0.9f);
            dust.noGravity = true;
        }

        public static void HitSparks(Vector2 position, bool ethereal)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, SparkleType(ethereal), Main.rand.NextVector2Circular(3.5f, 3.5f), 80, default, 1.1f);
                dust.noGravity = true;
            }
        }

        public static void Catch(Vector2 position, bool ethereal)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, SparkleType(ethereal), Main.rand.NextVector2Circular(2.5f, 2.5f), 80, default, 1f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.45f, Pitch = ethereal ? 0.5f : 0.2f }, position);
        }

        // Each boomerang, when it starts to bounce off the other one.
        public static void BounceSparks(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, Main.rand.NextBool() ? DustID.Enchanted_Gold : DustID.MagicMirror, Main.rand.NextVector2Circular(4f, 4f), 60, default, 1.2f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = 0.3f }, position);
        }

        // The stardust explosion where the two boomerangs meet.
        public static void Burst(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            int[] types = { DustID.Enchanted_Gold, DustID.MagicMirror, DustID.BlueTorch, DustID.PinkFairy };
            float reach = radius / 72f;
            for (int i = 0; i < 46; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 9f) * reach;
                Dust dust = Dust.NewDustPerfect(center, types[Main.rand.Next(types.Length)], velocity, 60, default, Main.rand.NextFloat(1.1f, 2f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 12; i++) // slow, large, see-through puffs
            {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(radius * 0.4f, radius * 0.4f), DustID.MagicMirror, Main.rand.NextVector2Circular(1.2f, 1.2f), 150, default, 2.2f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item105 with { Volume = 0.6f }, center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.25f, Pitch = 0.2f }, center);
        }
    }
}