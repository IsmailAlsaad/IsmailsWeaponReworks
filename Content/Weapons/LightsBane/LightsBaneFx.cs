using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// Every dust and sound of the Light's Bane rework (vanilla dust and sounds only), so a later juice pass edits one file. Every machine
    /// calls these for the same event; all of it is visual, nothing here is synced. 'charge' is 0..1.
    /// </summary>
    public static class LightsBaneFx
    {
        // ---- sounds ----

        // The swing whoosh gets higher the more the sword is charged.
        public static void PlaySwing(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.1f + 0.6f * charge, PitchVariance = 0.1f }, position);
        }

        public static void PlayHit(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.4f + 0.15f * charge, Pitch = 0.2f + 0.4f * charge, PitchVariance = 0.15f }, position);
        }

        // The sword just filled up: a rising chime.
        public static void PlayCharged(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = 0.3f }, position);
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.4f, Pitch = 0.5f }, position);
        }

        // ---- dust ----

        static void Mote(Vector2 position, Vector2 velocity, float scale, int type)
        {
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 100, default, scale);
            dust.noGravity = true;
        }

        // Purple flecks flung from the tip while the blade strikes (more of them with a fuller charge).
        public static void SwingDust(Vector2 tip, Vector2 motion, float charge)
        {
            if (Main.dedServ)
                return;
            int count = 1 + (int)(charge * 2.5f);
            for (int i = 0; i < count; i++)
                Mote(tip + Main.rand.NextVector2Circular(5f, 5f), motion * Main.rand.NextFloat(0.1f, 0.3f) + Main.rand.NextVector2Circular(1.2f, 1.2f), Main.rand.NextFloat(0.9f, 1.5f),
                    Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // A true melee hit: a burst of purple sparks and a hit sound, bigger with the charge.
        public static void HitSpark(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;
            PlayHit(position, charge);
            int count = 6 + (int)(charge * 8f);
            for (int i = 0; i < count; i++)
                Mote(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(2f, 5f + 3f * charge), Main.rand.NextFloat(1.1f, 1.8f), i % 3 == 0 ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // The hit that counted toward the charge: a few motes shoot off the tip.
        public static void ChargeHit(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Mote(tip, Main.rand.NextVector2Circular(3f, 3f), 1.2f, DustID.PurpleTorch);
        }

        // The sword just filled up: a ring around the middle of the blade, a spray along it and a chime.
        public static void Charged(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            PlayCharged(Vector2.Lerp(hand, tip, 0.5f));
            Vector2 middle = Vector2.Lerp(hand, tip, 0.5f);
            for (int i = 0; i < 18; i++)
                Mote(middle, (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 5.5f, 1.5f, i % 2 == 0 ? DustID.PurpleTorch : DustID.Shadowflame);
            for (int i = 0; i < 20; i++)
                Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4.5f), Main.rand.NextFloat(1.2f, 1.9f), DustID.PurpleTorch);
        }

        // A mote somewhere along a fully charged blade.
        public static void Sparkle(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.3f), 0.9f, DustID.PurpleTorch);
        }
    }
}