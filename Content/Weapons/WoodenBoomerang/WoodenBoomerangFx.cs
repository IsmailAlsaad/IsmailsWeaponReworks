using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.WoodenBoomerang
{
    /// <summary>All dust and sound for the Wooden Boomerang (vanilla dust and sounds only). No camera shake.</summary>
    public static class WoodenBoomerangFx
    {
        public static void Splinters(Vector2 center, int count, float speed, float scale)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, WoodenBoomerangTuning.WoodDust, Main.rand.NextVector2Circular(speed, speed), 0, default, scale * Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = Main.rand.NextBool(3);
            }
        }

        public static void HitSparks(Vector2 position)
        {
            Splinters(position, 1, 3f, 0.9f);
        }

        // A single chip left behind a moving thing.
        public static void Trail(Vector2 center, Vector2 velocity, float scale)
        {
            if (Main.dedServ)
                return;
            //Dust dust = Dust.NewDustPerfect(center, WoodenBoomerangTuning.WoodDust, velocity * -0.1f + Main.rand.NextVector2Circular(1f, 1f), 0, default, scale);
            //dust.noGravity = true;
        }

        public static void Catch(Vector2 position)
        {
            //Splinters(position, 4, 2.5f, 0.9f);
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.4f, Pitch = 0.4f }, position);
        }

        // A shard breaking (after its hits, on a block, or when its time is up): a small crack, quieter than the boomerang's.
        public static void PlayShardBreak(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.35f, Pitch = -0.2f, PitchVariance = 0.2f }, position);
        }

        public static void PlayBreak(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.25f, Pitch = 0.9f, PitchVariance = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = -0.4f }, position);
        }
    }
}