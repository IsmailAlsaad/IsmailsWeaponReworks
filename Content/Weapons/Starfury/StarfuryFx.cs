using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>All dust and sound for the Starfury (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class StarfuryFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position, bool up)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = up ? 0.15f : -0.05f, PitchVariance = 0.1f }, position);
        }

        public static void PlayCall(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.8f, Pitch = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.5f, Pitch = 0.4f }, position);
        }

        public static void PlayStarImpact(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.7f, Pitch = 0.3f, PitchVariance = 0.15f }, position);
        }

        // The pitch climbs with every bat.
        public static void PlayBat(Vector2 position, int generation)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.8f, Pitch = 0.1f + 0.2f * generation }, position);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = -0.3f }, position);
        }

        public static void PlayBounce(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.5f, Pitch = 0.3f, PitchVariance = 0.2f }, position);
        }

        // ---- dust ----

        static void Spark(Vector2 position, Vector2 velocity, float scale, bool pink)
        {
            Dust dust = Dust.NewDustPerfect(position, pink ? DustID.Enchanted_Pink : DustID.Enchanted_Gold, velocity, 0, default, scale);
            dust.noGravity = true;
        }

        // Motes drifting in toward the raised tip while the arm lifts.
        public static void CallGather(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(22f, 34f);
            Spark(tip + offset, -offset * 0.08f, 1f, Main.rand.NextBool());
        }

        // The star is called: a column of sparks shoots up from the tip and a ring spreads out.
        public static void CallBurst(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 16; i++)
                Spark(tip, new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), -Main.rand.NextFloat(3f, 9f)), 1.3f, i % 3 == 0);
            for (int i = 0; i < 14; i++)
                Spark(tip, (MathHelper.TwoPi * i / 14f).ToRotationVector2() * 4.5f, 1.2f, i % 2 == 0);
        }

        // Dust along the path the blade tip took this tick.
        public static void SlashDust(Vector2 from, Vector2 to)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 3; i++)
                Spark(Vector2.Lerp(from, to, Main.rand.NextFloat(0.5f,1f)), Main.rand.NextVector2Circular(1.2f, 1.2f), 1.1f, Main.rand.NextBool());
        }

        // A spark somewhere along the blade.
        public static void BladeSparkle(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Spark(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.3f), 0.9f, Main.rand.NextBool());
        }

        public static void HitSpark(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Spark(position, Main.rand.NextVector2Circular(3f, 3f), 1.2f, i % 2 == 0);
        }

        // The dust of each generation: gold, ice, pink, fire.
        static int DustFor(int generation)
        {
            return generation <= 0 ? DustID.Enchanted_Gold : generation == 1 ? DustID.IceTorch : generation == 2 ? DustID.Enchanted_Pink : DustID.Torch;
        }

        static void SparkGen(Vector2 position, Vector2 velocity, float scale, int generation)
        {
            Dust dust = Dust.NewDustPerfect(position, DustFor(generation), velocity, 0, default, scale);
            dust.noGravity = true;
        }

        // A few motes left behind a flying star, in the color of its generation.
        public static void StarTrail(Vector2 center, Vector2 velocity, int generation)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;
            SparkGen(center + Main.rand.NextVector2Circular(6f, 6f), -velocity * 0.05f + Main.rand.NextVector2Circular(0.6f, 0.6f), 0.9f, generation);
        }

        // The star ends (hit an enemy, hit a block, or ran out of time): sparks and a ring.
        public static void StarImpact(Vector2 position, int generation)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 16; i++)
                SparkGen(position, Main.rand.NextVector2Circular(5f, 5f), 1.3f, generation);
            for (int i = 0; i < 12; i++)
                SparkGen(position, (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 3.5f, 1.1f, generation);
        }

        // A bat: a bright ring and a spray of the new generation's color.
        public static void BatBurst(Vector2 position, int generation)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 18; i++)
                SparkGen(position, (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 6.5f, 1.5f, generation);
            for (int i = 0; i < 14; i++)
                SparkGen(position, Main.rand.NextVector2Circular(8f, 8f), 1.2f, generation);
        }

        // A launched star bounced off an enemy: a small burst of the old generation's color.
        public static void BounceBurst(Vector2 position, int generation)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                SparkGen(position, Main.rand.NextVector2Circular(4f, 4f), 1.2f, generation);
        }

        // ---- charge (the color is the generation of the star the sword will toss: gold, ice, pink) ----

        // A blade hit charged the sword: a few motes of the current tier's color shoot off the tip.
        public static void ChargeHit(Vector2 tip, int level)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                SparkGen(tip, Main.rand.NextVector2Circular(3f, 3f), 1f, level);
        }

        // The sword reached the next level: a ring and a column of sparks in the new color.
        public static void ChargeUp(Vector2 tip, int level)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 18; i++)
                SparkGen(tip, (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 5.5f, 1.4f, level);
            for (int i = 0; i < 12; i++)
                SparkGen(tip, new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(2f, 7f)), 1.3f, level);
        }

        public static void PlayChargeUp(Vector2 position, int level)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = 0.1f + 0.25f * level }, position);
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.4f, Pitch = 0.2f + 0.25f * level }, position);
        }

        // The charge is spent on a tossed star: the stored energy sprays off the tip.
        public static void ChargeRelease(Vector2 tip, int level)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 22; i++)
                SparkGen(tip, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 8f), 1.4f, level);
        }

        // A mote somewhere along a charged blade.
        public static void ChargeSparkle(Vector2 hand, Vector2 tip, int level)
        {
            if (Main.dedServ)
                return;
            SparkGen(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.3f), 0.9f, level);
        }

        // The finisher: three rings, a spray and some gold, all in the hot color of the last generation.
        public static void BlastBurst(Vector2 position)
        {
            if (Main.dedServ)
                return;

            List<int> generations = new List<int>();
            while(generations.Count < 3)
            {
                int randInt = Main.rand.Next(0, 3);
                if (generations.Contains(randInt))
                {
                    continue;
                }

                generations.Add(randInt);
            }

            for (int ring = 0; ring < 3; ring++)
            {
                for (int i = 0; i < 24; i++)
                    SparkGen(position, (MathHelper.TwoPi * i / 24f).ToRotationVector2() * (4f + 4f * ring), 2.8f - 0.8f * ring, generations[0]);
            }
            for (int i = 0; i < 30; i++)
                SparkGen(position, Main.rand.NextVector2Circular(14f, 14f), 1.6f, generations[1]);
            for (int i = 0; i < 16; i++)
                SparkGen(position, Main.rand.NextVector2Circular(9f, 9f), 0.8f, generations[2]);
        }

        public static void PlayBlast(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f, Pitch = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.7f, Pitch = 0.6f }, position);
        }
    }
}