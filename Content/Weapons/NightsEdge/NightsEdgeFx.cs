using Microsoft.Xna.Framework;
using Terraria;
using IsmailsWeaponReworks.Common;
using System;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// Every dust and sound of the Night's Edge rework (vanilla dust and sounds only), so a later juice pass edits one file. Every machine
    /// calls these for the same event; all of it is visual, nothing here is synced. 'charge' is 0..1. Patch 3 adds the dark flame look.
    /// </summary>
    public static class NightsEdgeFx
    {
        // ---- sounds ----

        // The swing whoosh gets higher the more the sword is charged (a little deeper than the Light's Bane's).
        public static void PlaySwing(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.85f, Pitch = -0.25f + 0.6f * charge, PitchVariance = 0.1f }, position);
        }

        public static void PlayHit(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.45f + 0.15f * charge, Pitch = 0f + 0.4f * charge, PitchVariance = 0.15f }, position);
        }

        // The sword just filled up: a deep rising chime.
        public static void PlayCharged(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = 0f }, position);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.45f, Pitch = 0.2f }, position);
        }

        // ---- dust ----

        static void Mote(Vector2 position, Vector2 velocity, float scale, int type)
        {
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 100, default, scale);
            dust.noGravity = true;
        }

        // Dark flame flecks flung from the tip while the blade strikes (more of them with a fuller charge).
        public static void SwingDust(Vector2 tip, Vector2 motion, float charge)
        {
            if (Main.dedServ)
                return;
            int count = 1 + (int)(charge * 2.5f);
            for (int i = 0; i < count; i++)
                Mote(tip + Main.rand.NextVector2Circular(5f, 5f), motion * Main.rand.NextFloat(0.1f, 0.3f) + Main.rand.NextVector2Circular(1.2f, 1.2f), Main.rand.NextFloat(0.9f, 1.5f),
                    Main.rand.NextBool(2) ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // A true melee hit: a burst of dark sparks and a hit sound, bigger with the charge.
        public static void HitSpark(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;
            PlayHit(position, charge);
            int count = 6 + (int)(charge * 8f);
            for (int i = 0; i < count; i++)
                Mote(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(2f, 5f + 3f * charge), Main.rand.NextFloat(1.1f, 1.8f), i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // The hit that counted toward the charge: a few motes shoot off the tip.
        public static void ChargeHit(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Mote(tip, Main.rand.NextVector2Circular(3f, 3f), 1.2f, DustID.Shadowflame);
        }

        // The sword just filled up: a ring around the middle of the blade, a spray along it and a chime.
        public static void Charged(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Vector2 middle = Vector2.Lerp(hand, tip, 0.5f);
            PlayCharged(middle);
            for (int i = 0; i < 18; i++)
                Mote(middle, (MathHelper.TwoPi * i / 18f).ToRotationVector2() * 5.5f, 1.5f, i % 2 == 0 ? DustID.PurpleTorch : DustID.Shadowflame);
            for (int i = 0; i < 20; i++)
                Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4.5f), Main.rand.NextFloat(1.2f, 1.9f), DustID.Shadowflame);
        }

        // A mote somewhere along a fully charged blade.
        public static void Sparkle(Vector2 hand, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Mote(Vector2.Lerp(hand, tip, Main.rand.NextFloat()), Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.3f), 0.9f, Main.rand.NextBool(2) ? DustID.PurpleTorch : DustID.Shadowflame);
        }

        // ---- the dark flame look (patch 3) ----

        // A vanilla dust mote that leaves the edge of the blade.
        public static void Ember(Vector2 position, Vector2 motion)
        {
            if (Main.dedServ)
                return;
            Mote(position, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.4f, 1.4f)) + motion * 0.2f, Main.rand.NextFloat(0.9f, 1.5f), Main.rand.NextBool(2) ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // The held blade draws the three metaball layers from its PreDraw, in front of the enemies (an explosion's droplets, the flames, the edge).
        public static void DrawLayers()
        {
            if (Main.dedServ)
                return;
            MetaballLayers.Of<NightsEdgeBurstStyle>().DrawInBatch();
            MetaballLayers.Of<NightsEdgeFlameStyle>().DrawInBatch();
            MetaballLayers.Of<NightsEdgeEdgeStyle>().DrawInBatch();
        }

        // ---- the flame wave (patch 4, back in patch 6a) ----

        // The wave leaves the blade: a low whoosh with a dark flame crackle.
        public static void PlayWave(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.6f, Pitch = -0.35f, PitchVariance = 0.1f }, position);
        }

        // The wave hit a block and dissolves: a puff of dark flame dust.
        public static void WaveBreak(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.35f, Pitch = -0.1f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < 14; i++)
                Mote(position, direction * Main.rand.NextFloat(-1f, 0.5f) + Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(1f, 1.7f), i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // One dust mote and one metaball flame shed off a point of the crescent.
        public static void WaveShed(Vector2 position, Vector2 velocity, float fade, bool dust, bool flame)
        {
            if (Main.dedServ)
                return;
            if (dust)
                Mote(position, velocity, Main.rand.NextFloat(0.9f, 1.5f), Main.rand.NextBool(2) ? DustID.Shadowflame : DustID.PurpleTorch);
            if (flame)
                MetaballLayers.Of<NightsEdgeFlameStyle>().Spawn(position, velocity * 0.4f + new Vector2(0f, -0.3f), Main.rand.Next(NightsEdgeTuning.FlameLifeMin, NightsEdgeTuning.FlameLifeMax + 1),
                    NightsEdgeTuning.WaveFlameSize * Main.rand.NextFloat(0.7f, 1.15f) * (0.5f + 0.5f * fade));
        }

        // ---- Shadow Armor (patch 5) ----

        // Every machine, every tick while the buff is on: dark flames rise off the whole body, a few dust motes, a purple light. 'fade' is 1 and eases to 0 over the buff's last ticks.
        public static void ArmorLook(Player player, float fade)
        {
            if (Main.dedServ)
                return;
            float rate = NightsEdgeTuning.ArmorFlameRate * fade;
            int count = (int)rate + (Main.rand.NextFloat() < rate - (int)rate ? 1 : 0);
            MetaballLayer flames = MetaballLayers.Of<NightsEdgeFlameStyle>();
            for (int i = 0; i < count; i++)
            {
                Vector2 at = player.Center + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f) * player.width, Main.rand.NextFloat(-0.5f, 0.5f) * player.height);
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.5f, 1.5f)) + player.velocity * 0.25f;
                flames.Spawn(at, velocity, Main.rand.Next(NightsEdgeTuning.FlameLifeMin, NightsEdgeTuning.FlameLifeMax + 1), NightsEdgeTuning.ArmorFlameSize * Main.rand.NextFloat(0.7f, 1.2f));
            }
            if (Main.rand.NextFloat() < NightsEdgeTuning.ArmorDustChance * fade)
                Mote(player.Center + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f) * player.width, Main.rand.NextFloat(-0.5f, 0.5f) * player.height), new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.4f, 1.2f)),
                    Main.rand.NextFloat(0.9f, 1.5f), Main.rand.NextBool(2) ? DustID.Shadowflame : DustID.PurpleTorch);
            float light = NightsEdgeTuning.ArmorLightStrength * fade;
            Lighting.AddLight(player.Center, 0.55f * light, 0.2f * light, 1f * light);
        }

        // The armor just switched on (every machine sees it on every player): a burst of big dark droplets from the body and a deep whoosh.
        public static void ArmorIgnite(Player player)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = -0.4f }, player.Center);
            MetaballLayer burst = MetaballLayers.Of<NightsEdgeBurstStyle>();
            for (int i = 0; i < NightsEdgeTuning.ArmorIgniteDroplets; i++)
                burst.Spawn(player.Center + Main.rand.NextVector2Circular(6f, 8f), Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f), Main.rand.Next(20, 36), Main.rand.NextFloat(NightsEdgeTuning.BlastDropMin, NightsEdgeTuning.BlastDropMax));
            for (int i = 0; i < 16; i++)
                Mote(player.Center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5f), Main.rand.NextFloat(1.2f, 1.8f), i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // A dark droplet lands: a small splash of dust.
        public static void DropLand(Vector2 position, float size)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 4; i++)
                Mote(position, new Vector2(Main.rand.NextFloat(-1.6f, 1.6f), -Main.rand.NextFloat(0.6f, 2f)), Main.rand.NextFloat(0.9f, 1.4f) * size, Main.rand.NextBool(2) ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // A droplet that falls in water (or is told to go): a puff of smoke-like dust.
        public static void DropHiss(Vector2 position, bool sound)
        {
            if (Main.dedServ)
                return;
            if (sound)
                SoundEngine.PlaySound(SoundID.LiquidsWaterLava with { Volume = 0.25f, Pitch = 0.2f }, position);
            for (int i = 0; i < 5; i++)
                Mote(position, new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), -Main.rand.NextFloat(0.3f, 1.2f)), Main.rand.NextFloat(0.9f, 1.4f), DustID.Shadowflame);
        }

        // The dark flame explosion of a charged hit (radius in px): a deep bang, a few big slow blobs in the middle that merge and shrink away,
        // droplets thrown out and braked by drag, and some vanilla dust.
        public static void Blast(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = -0.25f, PitchVariance = 0.1f }, center);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.45f, Pitch = -0.3f, PitchVariance = 0.15f }, center);
            float scale = radius / (NightsEdgeTuning.BlastSize * 0.5f);
            float dropScale = (float)Math.Sqrt(Math.Max(1f, scale));
            MetaballLayer burst = MetaballLayers.Of<NightsEdgeBurstStyle>();
            for (int i = 0; i < NightsEdgeTuning.BlastCores; i++)
            {
                Vector2 position = center + Main.rand.NextVector2Circular(4f, 4f) * scale;
                Vector2 velocity = Main.rand.NextVector2Circular(1f, 1f) * NightsEdgeTuning.BlastCoreSpeed * scale;
                burst.Spawn(position, velocity, Main.rand.Next(14, 23), Main.rand.NextFloat(NightsEdgeTuning.BlastCoreSizeMin, NightsEdgeTuning.BlastCoreSizeMax) * scale);
            }
            for (int i = 0; i < NightsEdgeTuning.BlastDroplets; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(NightsEdgeTuning.BlastSpeedMin, NightsEdgeTuning.BlastSpeedMax) * scale;
                burst.Spawn(center, velocity, Main.rand.Next(20, 39), Main.rand.NextFloat(NightsEdgeTuning.BlastDropMin, NightsEdgeTuning.BlastDropMax) * dropScale);
            }
            for (int i = 0; i < 14; i++)
                Mote(center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f) * scale, Main.rand.NextFloat(1.2f, 1.9f), i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // ---- the impale (patch 6a; the shared blade calls these) ----

        // The arm drops and the sword is drawn back: a low breath of a whoosh.
        public static void ImpaleWindup(Vector2 hand)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, Pitch = -0.6f }, hand);
        }

        // The point goes in (caught = the enemy is impaled on it): a heavy thud, a spray of dark sparks along the blade.
        public static void ImpalePierce(Vector2 point, Vector2 direction, bool caught)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.9f, Pitch = -0.6f }, point);
            if (caught)
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = -0.2f }, point);
            int count = caught ? 20 : 10;
            for (int i = 0; i < count; i++)
                Mote(point, direction * Main.rand.NextFloat(1f, 6f) + Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(1.1f, 1.8f), i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // The end of a beat: a ring of dark motes and a chime that climbs with every beat (patch 6b adds the energy streams and the big flashes).
        public static void ImpaleBeat(Vector2 point, int beat)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.2f + 0.25f * beat }, point);
            // THE FLASH: a bright core of metaballs at the enemy that is bigger with every beat, and a burst of embers.
            float flashSize = NightsEdgeTuning.ImpaleFlashSize * (float)Math.Pow(1f + Math.Max(0f, NightsEdgeTuning.ImpaleFlashGrowth), beat);
            MetaballLayer flash = MetaballLayers.Of<NightsEdgeBurstStyle>();
            for (int i = 0; i < 3 + beat; i++)
                flash.Spawn(point + Main.rand.NextVector2Circular(3f, 3f), Main.rand.NextVector2Circular(0.6f, 0.6f), Main.rand.Next(8, 13), flashSize * Main.rand.NextFloat(0.7f, 1.1f));
            Lighting.AddLight(point, 1.2f + 0.4f * beat, 0.4f + 0.1f * beat, 2f);
            int count = 12 + 6 * beat;
            for (int i = 0; i < count; i++)
                Mote(point, (MathHelper.TwoPi * i / count).ToRotationVector2() * (4f + beat), 1.4f + 0.2f * beat, i % 2 == 0 ? DustID.PurpleTorch : DustID.Shadowflame);
        }

        // Every tick of the hold: dark energy flows from the enemy into the blade, more of it as the hold goes on (and every beat).
        public static void ImpaleStream(Vector2 enemy, Vector2 bladeFrom, Vector2 bladeTo, float progress, int beat)
        {
            if (Main.dedServ)
                return;
            float rate = MathHelper.Lerp(NightsEdgeTuning.ImpaleStreamRateMin, NightsEdgeTuning.ImpaleStreamRateMax, progress);
            int count = (int)rate;
            if (Main.rand.NextFloat() < rate - count)
                count++;
            float ticks = Math.Max(2f, NightsEdgeTuning.ImpaleStreamTicks);
            for (int i = 0; i < count; i++)
            {
                Vector2 start = enemy + Main.rand.NextVector2Circular(14f, 14f);
                Vector2 target = Vector2.Lerp(bladeFrom, bladeTo, Main.rand.NextFloat());
                Mote(start, (target - start) / ticks, Main.rand.NextFloat(1f, 1.4f + 0.5f * progress), Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch);
            }
            Lighting.AddLight(Vector2.Lerp(bladeFrom, bladeTo, 0.5f), 0.4f + 0.5f * progress, 0.15f + 0.1f * progress, 0.8f + 0.7f * progress);
        }

        // The enemy is let go and the explosion goes off (the blast projectile plays its own burst, bigger than a charged hit's): a deep sound, two rings and a few motes.
        public static void ImpaleFinish(Vector2 point)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.5f, Pitch = -0.3f }, point);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.5f }, point);
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < 28; i++)
                    Mote(point, (MathHelper.TwoPi * i / 28f).ToRotationVector2() * (7f + 5f * ring), 1.8f + 0.4f * ring, i % 2 == 0 ? DustID.PurpleTorch : DustID.Shadowflame);
            for (int i = 0; i < 10; i++)
                Mote(point, Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f), Main.rand.NextFloat(1.2f, 1.8f), i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch);
        }

        // ---- the self-stab (patch 5; the shared blade calls these two) ----

        public static void PlayStabWindup(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.55f, Pitch = 0.3f }, position);
        }

        public static void StabImpact(Vector2 chest)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.9f, Pitch = -0.5f }, chest);
            for (int i = 0; i < 24; i++)
                Mote(chest, (MathHelper.TwoPi * i / 24f).ToRotationVector2() * 6.5f, 1.7f, i % 2 == 0 ? DustID.PurpleTorch : DustID.Shadowflame);
        }
    }
}