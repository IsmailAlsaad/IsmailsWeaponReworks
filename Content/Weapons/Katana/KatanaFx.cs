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
            scale *= 1.5f;

            Dust dust = Dust.NewDustPerfect(position, bright ? DustID.WhiteTorch : DustID.Smoke, velocity, bright ? 0 : 100, default, scale);
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
            SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = 1f, Pitch = 0.2f }, head);
            for (int i = 0; i < 8; i++)
                Spark(head, (-direction).RotatedByRandom(1.0f) * Main.rand.NextFloat(1f, 4f), 1.1f, true);
            for (int i = 0; i < 6; i++)
                Spark(head, (-direction).RotatedByRandom(1.3f) * Main.rand.NextFloat(0.5f, 2f), 1.4f, false);
        }

        // The dash starts: a hard whoosh and a burst of speed dust behind the player.
        public static void DashStart(Vector2 start, Vector2 offset)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.1f }, start);
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.8f, Pitch = 0.4f }, start);
            Vector2 direction = offset.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 6; i++)
                Spark(start, -direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 5f), 1.1f, i % 2 == 0);
        }

        // Every tick of the dash: speed dust thrown backward from the player (everyone sees it, from the synced position).
        public static void DashTrail(Vector2 center, Vector2 offset)
        {
            if (Main.dedServ)
                return;
            Vector2 direction = offset.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 3; i++)
                Spark(center + Main.rand.NextVector2Circular(10f, 14f), -direction * Main.rand.NextFloat(1f, 3f), 1f, i == 0);
        }

        // The cooldown is over: a soft chime and white motes along the blade.
        public static void Ready(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.5f, Pitch = 0.3f }, hand);
            for (int i = 0; i < 7; i++)
                Spark(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(1.2f, 1.2f), 1f, true);
        }

        // A mote drifting off the glowing sword while it is ready.
        public static void ReadySparkle(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Spark(position, new Vector2(0f, -0.3f) + Main.rand.NextVector2Circular(0.3f, 0.3f), 0.8f, true);
        }

        // The dash is over and the stuck blade fades away: a few sparks, no sound.
        public static void Unstick(Vector2 head, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Spark(head, (-direction).RotatedByRandom(1.2f) * Main.rand.NextFloat(0.5f, 2f), 1f, i % 2 == 0);
        }

        // The tile around the stuck blade is gone: it pops loose.
        public static void Drop(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.6f, Pitch = 0.5f }, position);
            for (int i = 0; i < 6; i++)
                Spark(position, (-direction).RotatedByRandom(1.2f) * Main.rand.NextFloat(0.5f, 2.5f), 1.2f, i % 2 == 0);
        }

        // The dropped blade hits something solid.
        public static void DropLand(Vector2 position, float strength)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.Tink with { Volume = MathHelper.Clamp(0.25f + 0.08f * strength, 0.25f, 0.8f), Pitch = 0.1f }, position);
            for (int i = 0; i < 4; i++)
                Spark(position, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -Main.rand.NextFloat(0.5f, 2f)), 1f, i % 2 == 0);
        }

        // A hit mark crosses the enemy: a metallic ring and sparks flying out along both ends of the main mark.
        public static void HitMark(Vector2 center, float angle)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit52 with { Volume = 0.5f, Pitch = 0.8f }, center);
            Vector2 axis = angle.ToRotationVector2();
            for (int i = 0; i < 10; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Spark(center, (axis * side).RotatedByRandom(0.15f) * Main.rand.NextFloat(3f, 8f), 1.1f, i % 3 != 0);
            }
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