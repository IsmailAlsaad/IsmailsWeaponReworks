using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>All dust and sound for the Night's Edge (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class NightsEdgeFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position, bool up, bool charged)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = (up ? 0.1f : -0.1f) - (charged ? 0.2f : 0f), PitchVariance = 0.1f }, position);
            if (charged)
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.35f, Pitch = -0.5f, PitchVariance = 0.15f }, position);
        }

        public static void PlayChargeUp(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.8f, Pitch = -0.4f }, position);
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.6f, Pitch = -0.2f }, position);
        }

        public static void PlayHit(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = -0.4f, PitchVariance = 0.15f }, position);
        }

        // ---- dust ----

        static void Spark(Vector2 position, Vector2 velocity, float scale, int type)
        {
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 0, default, scale);
            dust.noGravity = true;
        }

        // Dark motes shed by the tip during the strike (more of them, and flames among them, when the sword is charged).
        public static void SlashDust(Vector2 tip, bool charged)
        {
            if (Main.dedServ)
                return;
            Spark(tip + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextVector2Circular(1.2f, 1.2f), Main.rand.NextFloat(0.9f, 1.4f), DustID.PurpleTorch);
            if (charged)
                Spark(tip + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextVector2Circular(1.8f, 1.8f) + new Vector2(0f, -0.8f), Main.rand.NextFloat(1f, 1.6f), DustID.Shadowflame);
        }

        public static void HitSpark(Vector2 center)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Spark(center, Main.rand.NextVector2Circular(4.5f, 4.5f), Main.rand.NextFloat(1f, 1.6f), i % 2 == 0 ? DustID.PurpleTorch : DustID.Shadowflame);
        }

        // The moment the sword becomes fully charged: a ring of dark flame leaves the blade.
        public static void ChargeUp(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 18; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / 18f).ToRotationVector2();
                Spark(tip + direction * 4f, direction * Main.rand.NextFloat(2.5f, 5f), Main.rand.NextFloat(1.2f, 1.9f), i % 3 == 0 ? DustID.Shadowflame : DustID.PurpleTorch);
            }
        }

        // A mote on the blade while the sword carries charge.
        public static void ChargeSparkle(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Vector2 position = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.2f, 1f)) + Main.rand.NextVector2Circular(4f, 4f);
            Spark(position, new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.2f, 1f)), Main.rand.NextFloat(0.8f, 1.3f), DustID.PurpleTorch);
        }
    }
}