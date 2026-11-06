using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>All vanilla dust and sound for the Volcano, so a later juice pass edits one file.</summary>
    public static class VolcanoFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position, float heat)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.1f - 0.3f * heat, PitchVariance = 0.1f }, position);
            if (heat > 0.4f)
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.25f + 0.4f * heat, Pitch = -0.3f, PitchVariance = 0.1f }, position);
        }

        public static void PlayHit(Vector2 position, float heat)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.35f + 0.35f * heat, Pitch = -0.2f + 0.2f * heat, PitchVariance = 0.15f }, position);
        }

        public static void PlayCharged(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.1f }, position);
        }

        // ---- dust ----

        // One vanilla fire spark drifting up off the blade.
        public static void Ember(Vector2 position, Vector2 motion)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(position, DustID.Torch, new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(0.4f, 1.6f)) + motion * 0.2f, 100, default, Main.rand.NextFloat(0.9f, 1.5f));
            dust.noGravity = true;
        }

        // Sparks flung from the tip while the blade strikes.
        public static void SwingEmbers(Vector2 tip, Vector2 motion, float heat)
        {
            if (Main.dedServ)
                return;
            int count = 1 + (int)(heat * 2.5f);
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(6f, 6f), DustID.Torch, motion * Main.rand.NextFloat(0.1f, 0.35f) + Main.rand.NextVector2Circular(1.2f, 1.2f), 100, default, Main.rand.NextFloat(1f, 1.8f));
                dust.noGravity = Main.rand.NextBool(3) ? false : true;
            }
        }

        // A hit: a burst of sparks and a few lava dust that fall.
        public static void HitSpark(Vector2 position, float heat)
        {
            if (Main.dedServ)
                return;
            PlayHit(position, heat);
            int count = 6 + (int)(heat * 8f);
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.Torch, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(2f, 5f + 3f * heat), 100, default, Main.rand.NextFloat(1.2f, 2f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 2 + (int)(heat * 3f); i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.Lava, Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0f, -1.5f), 0, default, Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = false;
            }
        }

        // ---- patch 2: throw, stick, pop and the charged blast ----

        public static void PlayThrow(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f, PitchVariance = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.1f }, position);
        }

        // The sword leaves the hand: a fan of sparks along the throw.
        public static void Throw(Vector2 hand, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            PlayThrow(hand);
            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.5) * Main.rand.NextFloat(3f, 9f);
                Dust dust = Dust.NewDustPerfect(hand + direction * Main.rand.NextFloat(0f, 40f), DustID.Torch, velocity, 100, default, Main.rand.NextFloat(1.3f, 2.2f));
                dust.noGravity = true;
            }
        }

        // It sticks (a tile) or latches (an enemy): sparks and lava fly back out of the contact point, a clank and a hiss, a camera punch.
        public static void Stick(Vector2 point, Vector2 direction, bool onEnemy)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(onEnemy ? SoundID.NPCHit1 : SoundID.Tink, point);
            //SoundEngine.PlaySound(SoundID.NPCHit1, point);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, Pitch = -0.4f }, point);
            Vector2 back = -direction;
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(point, DustID.Torch, back.RotatedByRandom(1.0) * Main.rand.NextFloat(2f, 7f), 100, default, Main.rand.NextFloat(1.2f, 2f));
                dust.noGravity = true;
            }
            MetaballLayer drips = MetaballLayers.Of<VolcanoDripStyle>();
            for (int i = 0; i < 8; i++)
                drips.Spawn(point, back.RotatedByRandom(1.1) * Main.rand.NextFloat(1.5f, 4f), Main.rand.Next(30, 60), Main.rand.NextFloat(VolcanoTuning.DripSizeMin, VolcanoTuning.DripSizeMax));
            CameraShake.Punch(point, VolcanoTuning.StickShake, 8, "VolcanoStick");
        }

        // The hiss of lava being pumped into something, louder and higher as the pressure builds ('s' 0..1).
        public static void PlaySizzle(Vector2 position, float s)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f + 0.3f * s, Pitch = -0.2f + 0.5f * s, PitchVariance = 0.15f }, position);
        }

        // The three lava layers of the Volcano, drawn from a Volcano projectile's PreDraw AFTER its swing trail, so the trail is always
        // behind the lava: drips and spits first, then the explosion droplets, then the blade, the flames and the pools on top. Once per frame.
        public static void DrawLayers()
        {
            if (Main.dedServ)
                return;
            MetaballLayers.Of<VolcanoDripStyle>().DrawInBatch();
            MetaballLayers.Of<VolcanoBurstStyle>().DrawInBatch();
            MetaballLayers.Of<VolcanoBlobStyle>().DrawInBatch();
        }

        // A lava explosion made only of merging metaball lava: a few big slow blobs in the middle (they merge into one glob that shrinks away)
        // and droplets thrown outward that the style's drag brakes quickly. 'scale' 1 = the charged swing's blast (radius about 56 px).
        static void LavaBurst(Vector2 center, float scale, int droplets, int cores)
        {
            MetaballLayer burst = MetaballLayers.Of<VolcanoBurstStyle>();
            float dropScale = (float)System.Math.Sqrt(System.Math.Max(1f, scale));
            for (int i = 0; i < cores; i++)
            {
                Vector2 position = center + Main.rand.NextVector2Circular(4f, 4f) * scale;
                Vector2 velocity = Main.rand.NextVector2Circular(1f, 1f) * VolcanoTuning.BlastCoreSpeed * scale;
                burst.Spawn(position, velocity, Main.rand.Next(14, 23), Main.rand.NextFloat(VolcanoTuning.BlastCoreSizeMin, VolcanoTuning.BlastCoreSizeMax) * scale);
            }
            for (int i = 0; i < droplets; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(VolcanoTuning.BlastSpeedMin, VolcanoTuning.BlastSpeedMax) * scale;
                burst.Spawn(center, velocity, Main.rand.Next(20, 39), Main.rand.NextFloat(VolcanoTuning.BlastDropMin, VolcanoTuning.BlastDropMax) * dropScale);
            }
        }

        // The end of the fuse: the stored lava erupts. A shockwave of merging droplets around a big glob, a deep bang, a few embers, a camera
        // punch. (The lava drops that rain down are real projectiles, VolcanoLavaDropProjectile, and draw themselves.)
        public static void Eruption(Vector2 point)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.45f }, point);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.3f }, point);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, Pitch = -0.5f }, point);
            LavaBurst(point, VolcanoTuning.EruptionSize / (float)VolcanoTuning.BlastSize, VolcanoTuning.EruptionDroplets, VolcanoTuning.EruptionCores);
            for (int i = 0; i < 8; i++)
                Ember(point + Main.rand.NextVector2Circular(12f, 12f), Main.rand.NextVector2Circular(3f, 3f));
            CameraShake.Punch(point, VolcanoTuning.EruptionShake, 24, "VolcanoEruption");
            Lighting.AddLight(point, 2.4f, 1.1f, 0.25f);
        }

        // The handle reaches the player's hand: a clank, a few sparks and lava drops.
        public static void Catch(Vector2 hand)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.9f, Pitch = 0.1f }, hand);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.35f, Pitch = 0.4f }, hand);
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(hand, DustID.Torch, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(1.5f, 4f), 100, default, Main.rand.NextFloat(1.1f, 1.8f));
                dust.noGravity = true;
            }
            MetaballLayer drips = MetaballLayers.Of<VolcanoDripStyle>();
            for (int i = 0; i < 4; i++)
                drips.Spawn(hand, Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0f, -1f), Main.rand.Next(20, 40), Main.rand.NextFloat(VolcanoTuning.DripSizeMin, VolcanoTuning.DripSizeMax));
        }

        // A lava drop meets water (or is told to make room): a little smoke and, when 'sound', a hiss.
        public static void Hiss(Vector2 point, bool sound)
        {
            if (Main.dedServ)
                return;
            if (sound)
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.4f, PitchVariance = 0.2f }, point);
            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(point, DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(0.5f, 1.5f)), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
            }
        }

        // A lava drop lands: a little splash of lava and now and then a soft sizzle.
        public static void DropLand(Vector2 point, float size)
        {
            if (Main.dedServ)
                return;
            if (Main.rand.NextBool(4))
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = 0.2f, PitchVariance = 0.25f }, point);
            MetaballLayer drips = MetaballLayers.Of<VolcanoDripStyle>();
            for (int i = 0; i < 2; i++)
                drips.Spawn(point, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -Main.rand.NextFloat(0.8f, 2.2f)), Main.rand.Next(14, 28), Main.rand.NextFloat(1.6f, 2.6f) * size);
        }

        // The small explosion of a charged hit (radius in px): a sound and merging lava droplets, no dust.
        public static void Blast(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.2f, PitchVariance = 0.1f }, center);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.4f, Pitch = -0.1f, PitchVariance = 0.15f }, center);
            LavaBurst(center, radius / (VolcanoTuning.BlastSize * 0.5f), VolcanoTuning.BlastDroplets, VolcanoTuning.BlastCores);
        }

        // The sword reaches full heat: a ring of sparks around the blade and a roar.
        public static void Charged(Vector2 joint, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            PlayCharged(joint);
            for (int i = 0; i < 24; i++)
            {
                Vector2 point = Vector2.Lerp(joint, tip, Main.rand.NextFloat());
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust dust = Dust.NewDustPerfect(point, DustID.Torch, direction * Main.rand.NextFloat(2f, 5f), 100, default, Main.rand.NextFloat(1.4f, 2.2f));
                dust.noGravity = true;
            }
        }
    }
}