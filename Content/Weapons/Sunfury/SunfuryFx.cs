using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>All dust, sound, camera shake and shared drawing for the Sunfury (vanilla dust and sounds only).</summary>
    public static class SunfuryFx
    {
        static int FireDust()
        {
            int roll = Main.rand.Next(4);
            return roll == 0 ? DustID.GoldFlame : roll == 1 ? DustID.InfernoFork : DustID.Torch;
        }

        static Dust Fire(Vector2 position, Vector2 velocity, float scale)
        {
            Dust dust = Dust.NewDustPerfect(position, FireDust(), velocity, 80, default, scale);
            dust.noGravity = true;
            return dust;
        }

        // ---- charging ----

        // Heat gathering toward the hand, denser as the charge fills. 'progress' runs 0 to 1.
        public static void ChargeGather(Vector2 center, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 4 - tier)))
                return;
            Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * (30f + 16f * (1f - progress));
            Fire(center + offset, -offset * 0.08f, 1f + 0.4f * tier);
        }

        // Embers flung off the spinning head.
        public static void SpinEmbers(Vector2 head, Vector2 tangent, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 3 - (int)(progress * 2f))))
                return;
            Fire(head + Main.rand.NextVector2Circular(8f, 8f), tangent * 0.35f + Main.rand.NextVector2Circular(1f, 1f), 0.9f + 0.3f * tier);
        }

        // The tier's charge is complete: a ring of fire, a flash and a chime. Tier 3 is the big one.
        public static void TierComplete(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            MetaballLayer fire = MetaballLayers.Of<SunfuryBurstStyle>();
            fire.Spawn(head, Vector2.Zero, 16, SunfuryTuning.CompleteCoreSize(tier));
            int count = SunfuryTuning.CompleteDroplets(tier);
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / count).ToRotationVector2();
                fire.Spawn(head + direction * 8f, direction * SunfuryTuning.CompleteSpeed(tier) * Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(18, 30), SunfuryTuning.CompleteDropSize(tier) * Main.rand.NextFloat(0.8f, 1.2f));
            }
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.7f, Pitch = -0.2f + 0.2f * tier }, head);
            //if (tier >= 3)
                //SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.6f, Pitch = 0.5f }, head);
        }

        // ---- throwing and the window ----

        public static void ThrowWhoosh(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Fire(head, Main.rand.NextVector2Circular(3f, 3f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.3f + 0.15f * tier }, head);
        }

        // The head reached the apex: the retract window opens now.
        public static void ApexJingle(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.Item35 with { Volume = 0.9f, Pitch = 0.1f + 0.1f * tier }, head);
            for (int i = 0; i < 10; i++)
                Fire(head, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
        }

        // The press landed inside the window.
        public static void Caught(Vector2 hand, int nextTier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Fire(hand + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(2.5f, 2.5f), 1.1f);
            //SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0.2f * nextTier }, hand);
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.8f, Pitch = -0.7f + 0.4f * nextTier }, hand);

        }

        // ---- mistakes ----

        // Released before the charge was complete: nothing throws.
        public static void Fizzle(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
            {
                Dust smoke = Dust.NewDustPerfect(head, DustID.Smoke, Main.rand.NextVector2Circular(1.5f, 1.5f), 120, default, 1f);
                smoke.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.5f, Pitch = -0.4f }, head);
        }

        // Pressed too early: the chain locks up.
        public static void LockedChain(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 12; i++)
                Fire(head, Main.rand.NextVector2Circular(4f, 4f), 1.2f);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.9f, Pitch = -0.5f }, head);
        }

        // Too late, or let go before the charge was complete after a good catch.
        public static void Mistimed(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
            {
                Dust smoke = Dust.NewDustPerfect(head, DustID.Smoke, Main.rand.NextVector2Circular(2f, 2f), 120, default, 1.2f);
                smoke.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.6f, Pitch = -0.6f }, head);
        }

        public static void CatchHead(Vector2 hand)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Fire(hand, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.35f, Pitch = -0.1f }, hand);
        }

        // ---- flight ----

        public static void FlightDust(Vector2 position, Vector2 velocity, int tier)
        {
            if (Main.dedServ)
                return;
            int count = tier >= 3 ? 2 : 1;
            for (int i = 0; i < count; i++)
                if (Main.rand.NextBool(2))
                    Fire(position + Main.rand.NextVector2Circular(10f, 10f), -velocity * 0.06f + Main.rand.NextVector2Circular(0.8f, 0.8f), 1f + 0.3f * tier);
        }

        public static void SpecialLaunch(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 16; i++)
                Fire(head, Main.rand.NextVector2Circular(5f, 5f), 1.5f);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.2f }, head);
        }

        public static void HitSparks(Vector2 position, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6 + 2 * tier; i++)
                Fire(position + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextVector2Circular(4f, 4f), 1.1f);
        }

        // ---- the sun explosion ----

        public static void SunBlast(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;

            // Three rings of fire plus a spray of streaks and a column of smoke.
            //for (int ring = 0; ring < 3; ring++)
            //{
            //    int count = 36 + 14 * ring;
            //    float speed = 7f + 5f * ring;
            //    for (int i = 0; i < count; i++)
            //    {
            //        Vector2 direction = (MathHelper.TwoPi * i / count + ring * 0.2f).ToRotationVector2();
            //        Fire(center + direction * 12f, direction * speed, 2f - 0.3f * ring);
            //    }
            //}
            for (int i = 0; i < 70; i++)
                Fire(center, Main.rand.NextVector2Circular(radius * 0.045f, radius * 0.045f), Main.rand.NextFloat(1.5f, 2.6f));
            for (int i = 0; i < 24; i++)
            {
                Dust smoke = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(radius * 0.4f, radius * 0.4f), DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f) + new Vector2(0f, -1.5f), 140, default, 2.4f);
                smoke.noGravity = true;
            }

            // The fire metaballs: big slow globs in the middle that merge into one, and droplets thrown out and braked by the style's drag.
            MetaballLayer fire = MetaballLayers.Of<SunfuryBurstStyle>();
            for (int i = 0; i < SunfuryTuning.SunCores; i++)
                fire.Spawn(center + Main.rand.NextVector2Circular(radius * 0.05f, radius * 0.05f), Main.rand.NextVector2Circular(1f, 1f) * SunfuryTuning.SunCoreSpeed,
                    Main.rand.Next(18, 29), Main.rand.NextFloat(SunfuryTuning.SunCoreSizeMin, SunfuryTuning.SunCoreSizeMax));
            for (int i = 0; i < SunfuryTuning.SunDroplets; i++)
                fire.Spawn(center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(SunfuryTuning.SunSpeedMin, SunfuryTuning.SunSpeedMax),
                    Main.rand.Next(24, 45), Main.rand.NextFloat(SunfuryTuning.SunDropMin, SunfuryTuning.SunDropMax));

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.3f }, center);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.4f }, center);
            CameraShake.Punch(center, SunfuryTuning.ShakeStrength, 22, "SunfuryBlast");
        }

        // ---- drawing helpers ----

        // The fire metaballs, drawn from a Sunfury projectile's PreDraw (once per frame, in front of enemies). If no Sunfury projectile is
        // alive the layer is drawn behind them by the shared system instead, so the fire never just vanishes.
        public static void DrawLayers()
        {
            if (Main.dedServ)
                return;
            MetaballLayers.Of<SunfuryBurstStyle>().DrawInBatch();
        }

        // A filled circle from horizontal pixel rows on the world pixel grid (client only, spriteBatch already running in world space).
        public static void DrawDisc(Vector2 center, float radius, Color color, float grid)
        {
            if (radius < grid)
                return;
            grid = Math.Max(1f, grid);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int rows = (int)(radius / grid);
            for (int row = -rows; row <= rows; row++)
            {
                float y = row * grid;
                float half = (float)Math.Sqrt(Math.Max(0f, radius * radius - y * y));
                float left = (float)Math.Floor((center.X - half) / grid) * grid;
                float width = (float)Math.Ceiling(half * 2f / grid) * grid;
                float top = (float)Math.Floor((center.Y + y) / grid) * grid;
                Main.EntitySpriteDraw(pixel, new Vector2(left, top) - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, 0f, Vector2.Zero, new Vector2(width, grid), SpriteEffects.None, 0);
            }
        }
    }
}