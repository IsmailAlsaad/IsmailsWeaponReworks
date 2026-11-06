using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// The procedural lava blade: what the held sword hands to the two metaball layers every tick. The body is a line of round blobs from the
    /// joint to the tip (thin burning edge at heat 0, a fat wobbling slab of lava with a white-hot core at full heat); flames rise off it,
    /// and from a few heat onward lava drips fall from it and, when it is hot, spits fly off. Visual only: nothing is synced, every machine
    /// feeds its own layers from the heat it knows. Client only.
    /// </summary>
    public static class VolcanoBlade
    {
        static float Chance(float rate)
        {
            int count = (int)rate;
            if (Main.rand.NextFloat() < rate - count)
                count++;
            return count;
        }

        // 'length' = joint to tip in px (already scaled), 'heat' 0..1, 'time' in seconds, 'seed' something every machine knows,
        // 'motion' = how far the tip moved this tick (the flames and drips inherit a part of it).
        public static void Feed(Vector2 joint, Vector2 direction, float length, float scale, float heat, float time, float seed, Vector2 motion)
        {
            if (Main.dedServ)
                return;

            MetaballLayer body = MetaballLayers.Of<VolcanoBlobStyle>();
            MetaballLayer drips = MetaballLayers.Of<VolcanoDripStyle>();
            float h = MathHelper.Clamp(heat, 0f, 1f);
            Vector2 normal = new Vector2(-direction.Y, direction.X);

            float radius = MathHelper.Lerp(VolcanoTuning.EdgeRadiusLow, VolcanoTuning.EdgeRadiusHigh, h) * scale;
            float turbulence = MathHelper.Lerp(VolcanoTuning.WobbleLow, VolcanoTuning.WobbleHigh, h) * scale;
            float speed = MathHelper.Lerp(VolcanoTuning.WobbleSpeedLow, VolcanoTuning.WobbleSpeedHigh, h);
            float baseHeat = MathHelper.Lerp(VolcanoTuning.HeatLow, VolcanoTuning.HeatHigh, h);

            // ---- the body: round blobs along the blade ----
            int count = Math.Max(5, (int)(length * Math.Max(0.1f, Math.Abs(VolcanoTuning.BodyEnd - VolcanoTuning.BodyStart)) / Math.Max(1f, VolcanoTuning.BlobSpacing * scale)));
            for (int i = 0; i < count; i++)
            {
                float s = (i + 0.5f) / count; // 0 at the joint, 1 at the tip
                float profile = (0.6f + 0.4f * (float)Math.Sin(Math.PI * s)) * (1f - 0.55f * s * s * s); // fat in the middle, a point at the tip
                float phase = time * speed + i * 1.9f + seed * 3.1f;
                float sideways = (float)Math.Sin(phase) * turbulence * (0.3f + 0.7f * s);
                float along = 0.5f * (float)Math.Sin(time * speed * 0.7f + i * 2.7f + seed * 2f) * turbulence;
                Vector2 position = joint + direction * (length * (VolcanoTuning.BodyStart + (VolcanoTuning.BodyEnd - VolcanoTuning.BodyStart) * s) + along) + normal * sideways;
                float r = radius * profile * (1f + 0.18f * h * (float)Math.Sin(time * speed * 1.3f + i * 1.1f));
                float blobHeat = baseHeat + VolcanoTuning.HeatFlicker * h * (float)Math.Sin(phase * 0.8f) + 0.12f * s * h;
                body.AddBlob(position, r, MathHelper.Clamp(blobHeat, 0f, 1f));
            }

            // a white-hot core down the middle of the lava (the color bands show it as a bright line)
            if (h > VolcanoTuning.CoreFrom)
            {
                float core = (h - VolcanoTuning.CoreFrom) / (1f - VolcanoTuning.CoreFrom);
                for (int i = 0; i < count; i += 2)
                {
                    float s = (i + 0.5f) / count;
                    float profile = (0.6f + 0.4f * (float)Math.Sin(Math.PI * s)) * (1f - 0.55f * s * s * s);
                    Vector2 position = joint + direction * (length * (VolcanoTuning.BodyStart + (VolcanoTuning.BodyEnd - VolcanoTuning.BodyStart) * s));
                    body.AddBlob(position, radius * 0.5f * core * profile, VolcanoTuning.CoreHeat);
                }
            }

            // ---- flames rising off the edge ----
            float flames = Chance(MathHelper.Lerp(VolcanoTuning.FlameRateLow, VolcanoTuning.FlameRateHigh, h));
            for (int i = 0; i < flames; i++)
            {
                float s = Main.rand.NextFloat(VolcanoTuning.FlameStart, VolcanoTuning.FlameEnd);
                Vector2 position = joint + direction * (length * s) + normal * Main.rand.NextFloat(-radius, radius);
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.5f, 1.3f) * (0.7f + h)) + motion * 0.25f;
                int life = (int)(Main.rand.Next(VolcanoTuning.FlameLifeMin, VolcanoTuning.FlameLifeMax + 1) * (0.8f + 0.6f * h));
                float size = (VolcanoTuning.FlameSize + h * VolcanoTuning.FlameSizeHeat) * scale * Main.rand.NextFloat(0.7f, 1.15f);
                body.Spawn(position, velocity, life, size);
            }

            // ---- lava drips falling off ----
            if (h > VolcanoTuning.DripFrom)
            {
                float hot = (h - VolcanoTuning.DripFrom) / (1f - VolcanoTuning.DripFrom);
                float count2 = Chance(VolcanoTuning.DripRateHigh * hot);
                for (int i = 0; i < count2; i++)
                {
                    float s = Main.rand.NextFloat(VolcanoTuning.DripStart, VolcanoTuning.DripEnd);
                    Vector2 position = joint + direction * (length * s) + normal * Main.rand.NextFloat(-radius, radius) * 0.6f;
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(0f, 0.6f)) + motion * 0.2f;
                    drips.Spawn(position, velocity, Main.rand.Next(40, 71), Main.rand.NextFloat(VolcanoTuning.DripSizeMin, VolcanoTuning.DripSizeMax) * scale);
                }
            }

            // ---- lava spits flung out of the blade (violent) ----
            if (h > VolcanoTuning.SpitFrom)
            {
                float hot = (h - VolcanoTuning.SpitFrom) / (1f - VolcanoTuning.SpitFrom);
                float count3 = Chance(VolcanoTuning.SpitRateHigh * hot);
                for (int i = 0; i < count3; i++)
                {
                    float s = Main.rand.NextFloat(VolcanoTuning.SpitStart, VolcanoTuning.SpitEnd);
                    Vector2 position = joint + direction * (length * s);
                    Vector2 outward = normal * (Main.rand.NextBool() ? 1f : -1f);
                    Vector2 velocity = (outward + direction * Main.rand.NextFloat(-0.4f, 0.4f) + new Vector2(0f, -0.4f)).SafeNormalize(Vector2.UnitY)
                        * Main.rand.NextFloat(VolcanoTuning.SpitSpeedMin, VolcanoTuning.SpitSpeedMax) + motion * 0.3f;
                    drips.Spawn(position, velocity, Main.rand.Next(30, 55), Main.rand.NextFloat(1.8f, 3f) * scale);
                }
            }

            // ---- vanilla fire dust sparks and the light ----
            if (h > VolcanoTuning.EmberDustFrom && Main.rand.NextFloat() < VolcanoTuning.EmberDustChance * h)
                VolcanoFx.Ember(joint + direction * (length * Main.rand.NextFloat(0.2f, 1f)), motion);
            Lighting.AddLight(joint + direction * (length * 0.5f), 1f * (0.3f + 0.9f * h), 0.45f * (0.3f + 0.9f * h), 0.1f * (0.3f + 0.9f * h));
        }
    }
}