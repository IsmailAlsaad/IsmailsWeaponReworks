using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Katana
{
    /// <summary>All dust and sound for the Katana (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class KatanaFx
    {
        static void Spark(Vector2 position, Vector2 velocity, float scale, bool bright)
        {
            Dust dust = Dust.NewDustPerfect(position, bright ? DustID.IceTorch : DustID.Smoke, velocity, bright ? 0 : 100, default, scale);
            dust.noGravity = true;
        }

        // The sword clicks into the sheathed pose.
        public static void Click(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit52 with { Volume = 0.7f, Pitch = 0.5f }, hand);
            int count = KatanaTuning.ClickDustCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / count).ToRotationVector2();
                Spark(hand + direction * 4f, direction * Main.rand.NextFloat(1f, 3f), 1.1f, true);
            }
            for (int i = 0; i < 3; i++)
                Spark(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(1f, 1f), 1f, true);
        }

        // The blade leaves the hand.
        public static void Throw(Vector2 origin, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = 0.5f }, origin);
            SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.35f, Pitch = 0.8f }, origin);
            for (int i = 0; i < 8; i++)
                Spark(origin, direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 6f), 1.1f, true);
        }

        // Sparks laid along the stretch the blade flew this tick.
        public static void FlightDust(Vector2 from, Vector2 to)
        {
            if (Main.dedServ)
                return;
            int count = 1 + (int)(Vector2.Distance(from, to) / 16f);
            for (int i = 0; i < count; i++)
                Spark(Vector2.Lerp(from, to, Main.rand.NextFloat()), Main.rand.NextVector2Circular(0.6f, 0.6f), 0.9f, true);
        }

        // The blade bites into a tile.
        public static void Embed(Vector2 head, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.9f, Pitch = 0.2f }, head);
            for (int i = 0; i < 8; i++)
                Spark(head, (-direction).RotatedByRandom(1.0f) * Main.rand.NextFloat(1f, 4f), 1.1f, true);
            for (int i = 0; i < 6; i++)
                Spark(head, (-direction).RotatedByRandom(1.3f) * Main.rand.NextFloat(0.5f, 2f), 1.4f, false);
        }

        // The player crosses the line in a blink: a hard whoosh and a streak of speed dust left along it (no burst at either end).
        public static void Dash(Vector2 start, Vector2 end)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.1f }, start);
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.8f, Pitch = 0.4f }, end);
            Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
            int count = 3 + (int)(Vector2.Distance(start, end) / 10f);
            for (int i = 0; i < count; i++)
                Spark(Vector2.Lerp(start, end, i / (float)count) + Main.rand.NextVector2Circular(8f, 8f), -direction * Main.rand.NextFloat(1f, 3f), 1f, i % 3 == 0);
        }

        public static void HitSpark(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Spark(position, Main.rand.NextVector2Circular(3f, 3f), 1.2f, i % 2 == 0);
        }
    }
}