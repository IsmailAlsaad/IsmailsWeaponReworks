using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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
        // The handle: your art (VolcanoHandle.png) when it exists, else the vanilla sprite cropped to its hilt corner. Shared by the held and
        // the thrown sword. 'hand' is where the hilt is (the stand-in is drawn from it), 'joint' where the lava blade starts (your art is
        // centred on it), 'rotation' the blade's angle; 'heat' and 'flash' (0..1) make the handle glow.
        public static void DrawHandle(Asset<Texture2D> art, Texture2D standIn, Vector2 hand, Vector2 joint, float rotation, int spriteDirection, float scale, Color lightColor, float heat, float flash)
        {
            if (Main.dedServ)
                return;
            Color body = Color.Lerp(lightColor, Color.White, 0.35f + 0.4f * heat);
            float glow = heat * 0.6f + flash * 0.4f;

            if (art?.IsLoaded == true)
            {
                Texture2D texture = art.Value;
                HeldSwordPose.Sprite(texture, spriteDirection, out _, out float offset, out SpriteEffects effects);
                Vector2 origin = new Vector2(texture.Width, texture.Height) * 0.5f;
                Vector2 position = joint - Main.screenPosition;
                float angle = rotation + offset;
                Main.EntitySpriteDraw(texture, position, null, body, angle, origin, scale, effects, 0);
                if (glow > 0.05f)
                    Main.EntitySpriteDraw(texture, position, null, VolcanoTuning.HandleGlowColor * glow, angle, origin, scale * 1.06f, effects, 0);
                return;
            }

            int width = Math.Max(1, (int)(standIn.Width * VolcanoTuning.HandleCrop));
            int height = Math.Max(1, (int)(standIn.Height * VolcanoTuning.HandleCrop));
            Rectangle source = new Rectangle(0, standIn.Height - height, width, height);
            HeldSwordPose.Sprite(standIn, spriteDirection, out _, out float rotationOffset, out SpriteEffects flip);
            Vector2 hiltOrigin = spriteDirection > 0 ? new Vector2(0f, height) : new Vector2(width, height);
            Vector2 handPosition = hand - Main.screenPosition;
            float standInAngle = rotation + rotationOffset;
            Main.EntitySpriteDraw(standIn, handPosition, source, body, standInAngle, hiltOrigin, scale, flip, 0);
            if (glow > 0.05f)
                Main.EntitySpriteDraw(standIn, handPosition, source, VolcanoTuning.HandleGlowColor * glow, standInAngle, hiltOrigin, scale * 1.06f, flip, 0);
        }

        // A stable pseudo-random number 0..1 from x: the same input gives the same answer on every machine and every tick.
        static float Hash(float x)
        {
            double v = Math.Sin(x * 12.9898) * 43758.5453;
            return (float)(v - Math.Floor(v));
        }

        static float Chance(float rate)
        {
            int count = (int)rate;
            if (Main.rand.NextFloat() < rate - count)
                count++;
            return count;
        }

        // 'length' = joint to tip in px (already scaled), 'heat' 0..1, 'time' in seconds, 'seed' something every machine knows,
        // 'motion' = how far the tip moved this tick (the flames and drips inherit a part of it).
        // 'swell' multiplies the blob radius (the thrown sword's lava swelling), 'boost' is added to the color heat (it brightens).
        public static void Feed(Vector2 joint, Vector2 direction, float length, float scale, float heat, float time, float seed, Vector2 motion, float swell = 1f, float boost = 0f)
        {
            if (Main.dedServ)
                return;

            MetaballLayer body = MetaballLayers.Of<VolcanoBlobStyle>();
            MetaballLayer drips = MetaballLayers.Of<VolcanoDripStyle>();
            float h = MathHelper.Clamp(heat, 0f, 1f);
            Vector2 normal = new Vector2(-direction.Y, direction.X);

            float radius = MathHelper.Lerp(VolcanoTuning.EdgeRadiusLow, VolcanoTuning.EdgeRadiusHigh, h) * scale * swell;
            float turbulence = MathHelper.Lerp(VolcanoTuning.WobbleLow, VolcanoTuning.WobbleHigh, h) * scale;
            float speed = MathHelper.Lerp(VolcanoTuning.WobbleSpeedLow, VolcanoTuning.WobbleSpeedHigh, h);
            float baseHeat = MathHelper.Lerp(VolcanoTuning.HeatLow, VolcanoTuning.HeatHigh, h) + boost;

            // ---- the body: round blobs scattered inside the blade ----
            // BodyEndLow/BodyEndHigh (by heat) cut the thin end of the blade off: that is where the blobs are too small and too far apart to merge and
            // leave lone pixels. Every blob sits at a stable random place inside its slot of the blade and a random distance from the middle line (no
            // straight rows); the 'seed' keeps them from changing every tick.
            float bodyEnd = MathHelper.Lerp(VolcanoTuning.BodyEndLow, VolcanoTuning.BodyEndHigh, h);
            float bodySpan = Math.Max(0.05f, Math.Abs(bodyEnd - VolcanoTuning.BodyStart));
            int count = Math.Max(5, (int)(length * bodySpan / Math.Max(1f, VolcanoTuning.BlobSpacing * scale)));
            for (int i = 0; i < count; i++)
            {
                float s = (i + 0.2f + 0.6f * Hash(i * 1.37f + seed * 7.1f)) / count; // 0 at the joint, 1 at the cut
                float profile = (0.6f + 0.4f * (float)Math.Sin(Math.PI * s)) * (1f - 0.55f * s * s * s); // fat in the middle, a point at the tip
                float phase = time * speed + i * 1.9f + seed * 3.1f;
                float sideways = (float)Math.Sin(phase) * turbulence * (0.3f + 0.7f * s);
                float scatter = (Hash(i * 2.91f + seed * 3.3f + 17f) * 2f - 1f) * radius * profile * VolcanoTuning.BodyScatter;
                float along = 0.5f * (float)Math.Sin(time * speed * 0.7f + i * 2.7f + seed * 2f) * turbulence;
                Vector2 position = joint + direction * (length * (VolcanoTuning.BodyStart + (bodyEnd - VolcanoTuning.BodyStart) * s) + along) + normal * (sideways + scatter);
                float r = radius * profile * (1f + 0.18f * h * (float)Math.Sin(time * speed * 1.3f + i * 1.1f));
                float blobHeat = baseHeat + VolcanoTuning.HeatFlicker * h * (float)Math.Sin(phase * 0.8f) + 0.12f * s * h;
                body.AddBlob(position, r, MathHelper.Clamp(blobHeat, 0f, 1f));
            }

            // white-hot spots inside the lava (the color bands show them as bright patches): random places inside the blade, never a line
            if (h > VolcanoTuning.CoreFrom)
            {
                float core = (h - VolcanoTuning.CoreFrom) / (1f - VolcanoTuning.CoreFrom);
                int coreCount = Math.Max(3, count / 2);
                for (int i = 0; i < coreCount; i++)
                {
                    float s = (i + Hash(i * 5.17f + seed * 1.9f + 40f)) / coreCount;
                    float profile = (0.6f + 0.4f * (float)Math.Sin(Math.PI * s)) * (1f - 0.55f * s * s * s);
                    float across = (Hash(i * 3.71f + seed * 4.3f + 90f) * 2f - 1f) * radius * profile * VolcanoTuning.CoreScatter
                        + (float)Math.Sin(time * speed * 0.6f + i * 2.3f + seed) * radius * 0.15f * profile;
                    Vector2 position = joint + direction * (length * (VolcanoTuning.BodyStart + (bodyEnd - VolcanoTuning.BodyStart) * s)) + normal * across;
                    float size = radius * 0.5f * core * profile * (0.6f + 0.8f * Hash(i * 7.7f + seed * 2.2f + 3f));
                    body.AddBlob(position, size, VolcanoTuning.CoreHeat);
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