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

        // The button is released: the blade is cut loose (everywhere, from the synced state).
        public static void Release(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Spark(head, (MathHelper.TwoPi * i / 10f).ToRotationVector2() * Main.rand.NextFloat(1.5f, 4f), 1.2f, i % 2 == 0);
        }

        // The player is gone from 'start' and arrives at 'end'.
        public static void Teleport(Vector2 start, Vector2 end)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = 0.7f }, end);
            SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.5f, Pitch = 0.8f }, end);
            Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 12; i++)
                Spark(start, Main.rand.NextVector2Circular(2f, 2f), 1.2f, i % 2 == 0);
            for (int i = 0; i < 14; i++)
                Spark(end, direction.RotatedByRandom(0.8f) * Main.rand.NextFloat(1f, 5f), 1.3f, i % 2 == 0);
        }

        // Sparks laid all along the slash line.
        public static void SlashLine(Vector2 start, Vector2 end)
        {
            if (Main.dedServ)
                return;
            int count = 2 + (int)(Vector2.Distance(start, end) / 12f);
            for (int i = 0; i < count; i++)
                Spark(Vector2.Lerp(start, end, Main.rand.NextFloat()), Main.rand.NextVector2Circular(1.6f, 1.6f), 1.1f, Main.rand.NextBool());
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