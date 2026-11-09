using IsmailsWeaponReworks.Content.Weapons.EnchantedBoomerang;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// Every dust and sound of the Enchanted Sword rework (vanilla dust and sounds only), in the Enchanted Boomerang's stardust style: gold,
    /// white, blue and pink sparkles that drift without gravity. Every machine calls these for the same event; all of it is visual, nothing
    /// here is synced. 'stage' is 0..3 (the drawn outline stage, 3 = full).
    /// </summary>
    public static class EnchantedSwordFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position, bool thrust)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = thrust ? 0.35f : 0f, PitchVariance = 0.1f }, position);
        }

        public static void PlayHit(Vector2 position, float stage)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.4f + 0.1f * stage, Pitch = 0.2f + 0.2f * stage, PitchVariance = 0.15f }, position);
        }

        // ---- dust ----

        static int StardustType()
        {
            switch (Main.rand.Next(4))
            {
                case 0: return DustID.Enchanted_Gold;
                case 1: return DustID.MagicMirror;
                case 2: return DustID.BlueTorch;
                default: return DustID.PinkFairy;
            }
        }

        static void Mote(Vector2 position, Vector2 velocity, float scale, int alpha = 80)
        {
            Dust dust = Dust.NewDustPerfect(position, StardustType(), velocity, alpha, default, scale);
            dust.noGravity = true;
        }

        // Stardust flung from the tip while the blade strikes (more of it at the higher stages).
        public static void SwingDust(Vector2 tip, Vector2 motion, float stage)
        {
            if (Main.dedServ)
                return;
            int count = 1 + (int)(stage * 1.2f);
            for (int i = 0; i < count; i++)
                Mote(tip + Main.rand.NextVector2Circular(5f, 5f), motion * Main.rand.NextFloat(0.1f, 0.3f) + Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(0.9f, 1.4f));
        }

        // A true melee hit: a burst of stardust and a hit sound.
        public static void HitSpark(Vector2 position, float stage)
        {
            if (Main.dedServ)
                return;
            PlayHit(position, stage);
            int count = 6 + (int)(stage * 3f);
            for (int i = 0; i < count; i++)
                Mote(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(2f, 4.5f + stage), Main.rand.NextFloat(1.1f, 1.7f), 60);
        }

        // The outline just got stronger: a ring of stardust around the middle of the blade, a spray along it and a chime that rises with the stage.
        public static void StageUp(Vector2 hand, Vector2 tip, int stage)
        {
            if (Main.dedServ)
                return;
            Vector2 middle = Vector2.Lerp(hand, tip, 0.5f);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.1f + 0.15f * stage }, middle);
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.3f, Pitch = 0.1f + 0.15f * stage }, middle);
            int ring = 10 + 4 * stage;
            for (int i = 0; i < ring; i++)
                Mote(middle, (MathHelper.TwoPi * i / ring).ToRotationVector2() * (3.5f + stage), 1.3f, 60);
            for (int i = 0; i < 8 + 4 * stage; i++)
                Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 3f), Main.rand.NextFloat(1f, 1.6f));
        }

        // ---- full charge, the throw and the beam ----

        // The sword just became FULL: stardust along the blade, a ring round its middle and a bright rising chime.
        public static void Full(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Vector2 middle = Vector2.Lerp(hand, tip, 0.5f);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = 0.45f }, middle);
            SoundEngine.PlaySound(SoundID.Item105 with { Volume = 0.45f, Pitch = 0.35f }, middle);
            for (int i = 0; i < 20; i++)
                Mote(middle, (MathHelper.TwoPi * i / 20f).ToRotationVector2() * 4.5f, 1.5f, 50);
            for (int i = 0; i < 18; i++)
                Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4.5f), Main.rand.NextFloat(1.2f, 1.8f), 60);
        }

        // While the sword stays full: a small ring of stardust at the tip now and then, so it is plain that it is ready.
        public static void FullPulse(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Mote(tip, (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 1.8f, 1.1f, 80);
        }

        // Right click without a full charge: a dull little chime and a few motes. Nothing is spent. The owner's machine only.
        public static void NotReady(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.25f, Pitch = -0.6f }, tip);
            for (int i = 0; i < 4; i++)
                Mote(tip, Main.rand.NextVector2Circular(1.2f, 1.2f), 0.9f, 110);
        }

        // The throw: a swoosh, a chime and stardust sprayed forward from the tip.
        public static void Throw(Vector2 tip, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = 0.5f }, tip);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = 0.6f }, tip);
            for (int i = 0; i < 16; i++)
                Mote(tip + Main.rand.NextVector2Circular(6f, 6f), direction * Main.rand.NextFloat(3f, 9f) + Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.NextFloat(1.1f, 1.7f), 60);
        }

        // Stardust the thrown beam sheds as it flies.
        public static void BeamDust(Vector2 position, Vector2 velocity)
        {
            if (Main.dedServ)
                return;
            Mote(position + Main.rand.NextVector2Circular(6f, 6f), -velocity * Main.rand.NextFloat(0.05f, 0.15f) + Main.rand.NextVector2Circular(0.8f, 0.8f), Main.rand.NextFloat(1f, 1.4f));
        }

        // The beam ends: a small puff of stardust (an enemy, a wall or the end of its range). 'sound' is the soft fizzle of a miss.
        public static void BeamEnd(Vector2 position, bool sound)
        {
            if (Main.dedServ)
                return;
            if (sound)
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.35f, Pitch = -0.4f }, position);
            for (int i = 0; i < 10; i++)
                Mote(position, Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 3.5f), Main.rand.NextFloat(1f, 1.5f), 70);
        }

        // A mote somewhere along an outlined blade.
        public static void Sparkle(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.3f), 0.9f, 100);
        }

        // ---- the ghost sword ----

        // The ghost fades in: a ring of stardust and a high chime.
        public static void GhostAppear(Vector2 center)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = 0.5f }, center);
            for (int i = 0; i < 16; i++)
                Mote(center, (MathHelper.TwoPi * i / 16f).ToRotationVector2() * Main.rand.NextFloat(2f, 4f), Main.rand.NextFloat(1.1f, 1.6f), 60);
        }

        // Stardust the ghost's blade sheds while it slashes and drives.
        public static void GhostDust(Vector2 tip, Vector2 motion)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 2; i++)
                Mote(tip + Main.rand.NextVector2Circular(6f, 6f), motion * Main.rand.NextFloat(0.1f, 0.25f) + Main.rand.NextVector2Circular(1f, 1f), Main.rand.NextFloat(1f, 1.5f));
        }

        // The back-off begins: a rising chime.
        public static void StabChargeStart(Vector2 hilt)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.2f }, hilt);
        }

        // While it backs away and holds: stardust drawn into the tip from all around ('t' runs 0..1 over the charge).
        public static void StabCharge(Vector2 tip, float t)
        {
            if (Main.dedServ)
                return;
            int count = 1 + (int)(t * 2.5f);
            for (int i = 0; i < count; i++)
            {
                Vector2 start = tip + Main.rand.NextVector2Unit() * Main.rand.NextFloat(28f, 60f);
                Mote(start, (tip - start) * 0.12f, Main.rand.NextFloat(1f, 1.5f), 80);
            }
        }

        // The blade lands: a hard ring of stardust and a sound.
        public static void StabImpact(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item105 with { Volume = 0.8f, Pitch = 0.1f }, tip);
            for (int i = 0; i < 22; i++)
                Mote(tip, (MathHelper.TwoPi * i / 22f).ToRotationVector2() * Main.rand.NextFloat(3f, 7f), Main.rand.NextFloat(1.2f, 1.9f), 60);
        }

        // The ghost bursts into stardust at the end.
        public static void GhostVanish(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            EnchantedBoomerangFx.Burst(center, radius);
        }
    }
}