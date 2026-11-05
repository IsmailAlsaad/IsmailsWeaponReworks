using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>All dust and sound for the Muramasa (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class MuramasaFx
    {
        // ---- sounds ----

        // A quiet metal scrape while the sword slides home.
        public static void PlayScrape(Vector2 position)
        {
            if (Main.dedServ)
                return;

            // Make it a continous sound
            //SoundEngine.PlaySound(SoundID.Item7 with { Volume = 1.3f, Pitch = 0f, MaxInstances = 0}, position);
        }

        // The click of the sword seating in the scabbard.
        public static void PlayClick(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit52 with { Volume = 0.9f, Pitch = 0.3f }, position);
            //SoundEngine.PlaySound(SoundID.Item35 with { Volume = 0.25f, Pitch = 0.5f }, position);
        }

        public static void PlaySwing(Vector2 position, bool up)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = up ? 0.3f : 0.1f, PitchVariance = 0.1f }, position);
        }

        // ---- dust ----

        static void Spark(Vector2 position, Vector2 velocity, float scale, bool bright)
        {
            Dust dust = Dust.NewDustPerfect(position, bright ? DustID.IceTorch : DustID.BlueTorch, velocity, 0, default, scale);
            dust.noGravity = true;
        }

        // The sword clicks shut: a ring of blue sparks around the hand.
        public static void Click(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            PlayClick(hand);
            int count = MuramasaTuning.ClickDustCount;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / count).ToRotationVector2();
                Spark(hand + direction * 4f, direction * Main.rand.NextFloat(1.5f, 3.5f), 1.2f, i % 2 == 0);
            }
            for (int i = 0; i < 4; i++)
                Spark(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(1.5f, 1.5f), 1f, true);
        }

        // A spark somewhere along the sheathed blade (the haze around the glowing sword).
        public static void BladeSparkle(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Spark(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(0.5f, 0.5f) + new Vector2(0f, -0.3f), 0.9f, Main.rand.NextBool());
        }

        // The draw window opens (owner's client).
        public static void DrawStart(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.5f, Pitch = 0.5f }, position);
            for (int i = 0; i < 8; i++)
                Spark(position, (MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(1f, 2.5f), 1f, i % 2 == 0);
        }

        // A point of the drawn path was laid under the cursor.
        public static void DrawDust(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 2; i++)
                Spark(position, Main.rand.NextVector2Circular(0.8f, 0.8f), 0.9f, Main.rand.NextBool());
        }

        // The draw window closed with a path (patch 2 shows the simplified strokes; patch 3 turns them into slashes).
        public static void DrawEnd(System.Collections.Generic.IList<Vector2> points)
        {
            if (Main.dedServ || points == null || points.Count == 0)
                return;
            SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f, Pitch = 0.4f }, points[points.Count - 1]);
            for (int i = 0; i < points.Count; i++)
                for (int k = 0; k < 4; k++)
                    Spark(points[i], Main.rand.NextVector2Circular(2.5f, 2.5f), 1.1f, k % 2 == 0);
        }

        public static void SlashDust(Vector2 from, Vector2 to)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 3; i++)
                Spark(Vector2.Lerp(from, to, Main.rand.NextFloat(0.5f, 1f)), Main.rand.NextVector2Circular(1.2f, 1.2f), 1.1f, Main.rand.NextBool());
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