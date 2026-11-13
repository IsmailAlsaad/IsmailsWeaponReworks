using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Mace
{
    /// <summary>All dust and sound for the Mace (vanilla dust and sounds only): iron and silver flecks, hot sparks, smoke and metal clangs.</summary>
    public class MaceFx : FlailFx
    {
        public static MaceFx Instance { get; } = new MaceFx();

        // A fleck of metal; now and then a hot spark.
        static Dust Metal(Vector2 position, Vector2 velocity, float scale)
        {
            int roll = Main.rand.Next(6);
            int type = roll == 0 ? DustID.Torch : roll <= 2 ? DustID.Silver : DustID.Iron;
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 0, default, scale);
            dust.noGravity = true;
            return dust;
        }

        static Dust Smoke(Vector2 position, Vector2 velocity, float scale)
        {
            Dust dust = Dust.NewDustPerfect(position, DustID.Smoke, velocity, 120, default, scale);
            dust.noGravity = true;
            return dust;
        }

        public override void ChargeGather(Vector2 center, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 5 - tier)))
                return;
            Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * (28f + 14f * (1f - progress));
            Metal(center + offset, -offset * 0.08f, 0.9f + 0.3f * tier);
        }

        public override void SpinEmbers(Vector2 head, Vector2 tangent, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 3 - (int)(progress * 2f))))
                return;
            Metal(head + Main.rand.NextVector2Circular(8f, 8f), tangent * 0.35f + Main.rand.NextVector2Circular(1f, 1f), 0.9f + 0.3f * tier);
        }

        public override void TierComplete(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            int count = 10 + 4 * tier;
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / count).ToRotationVector2();
                Metal(head + direction * 8f, direction * (3f + 0.6f * tier), 1.2f);
            }
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.6f, Pitch = -0.2f + 0.2f * tier }, head);
        }

        public override void ThrowWhoosh(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Metal(head, Main.rand.NextVector2Circular(3f, 3f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.3f + 0.1f * tier }, head);
        }

        public override void ApexJingle(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Metal(head, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
            SoundEngine.PlaySound(SoundID.Item35 with { Volume = 0.35f, Pitch = -0.4f }, head);
        }

        public override void Caught(Vector2 hand, int nextTier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Metal(hand + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(2.5f, 2.5f), 1.1f);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.45f, Pitch = -0.1f + 0.2f * nextTier }, hand);
        }

        public override void Fizzle(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 5; i++)
                Smoke(head, Main.rand.NextVector2Circular(1.5f, 1.5f), 1f);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.45f, Pitch = -0.5f }, head);
        }

        public override void LockedChain(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Metal(head, Main.rand.NextVector2Circular(4f, 4f), 1.2f);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.9f, Pitch = -0.5f }, head);
        }

        public override void Mistimed(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Smoke(head, Main.rand.NextVector2Circular(2f, 2f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.55f, Pitch = -0.6f }, head);
        }

        public override void CatchHead(Vector2 hand)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Metal(hand, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.35f, Pitch = 0.1f }, hand);
        }

        public override void FlightDust(Vector2 position, Vector2 velocity, int tier)
        {
            if (Main.dedServ)
                return;
            if (Main.rand.NextBool(2))
                Metal(position + Main.rand.NextVector2Circular(10f, 10f), -velocity * 0.05f + Main.rand.NextVector2Circular(0.8f, 0.8f), 1f + 0.2f * tier);
            if (tier >= 3 && Main.rand.NextBool(3))
                Smoke(position, -velocity * 0.03f, 0.9f);
        }

        public override void SpecialLaunch(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 14; i++)
                Metal(head, Main.rand.NextVector2Circular(5f, 5f), 1.4f);
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.7f, Pitch = 0.1f }, head);
        }

        public override void HitSparks(Vector2 position, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 7 + 2 * tier; i++)
                Metal(position + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextVector2Circular(4.5f, 4.5f), 1.1f);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.5f, Pitch = -0.2f + 0.1f * tier, MaxInstances = 3 }, position);
        }

        public override void DrawLayers() { }

        // ---- the Mace's own: the head explodes into shrapnel ----

        // The head blows apart: a ring of metal flecks thrown out fast, hot sparks, a puff of smoke, a clang and a bang, and a short shake.
        public void Explosion(Vector2 center)
        {
            if (Main.dedServ)
                return;
            int ring = 24;
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < ring; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / ring).ToRotationVector2();
                Metal(center + direction * 6f, direction * Main.rand.NextFloat(5f, 10f), 1.5f);
            }
            for (int i = 0; i < 18; i++)
            {
                Dust spark = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 8f), DustID.Torch, Main.rand.NextVector2Circular(6f, 6f), 0, default, 1.6f);
                spark.noGravity = true;
            }
            for (int i = 0; i < 10; i++)
                Smoke(center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(2f, 2f), 1.5f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = 0.3f }, center);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.9f, Pitch = -0.3f }, center);
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.7f, Pitch = 0.2f }, center);
            CameraShake.Punch(center, 3f, 10, "MaceExplosion");
        }

        // Dust behind a flying shard (the pixel trail is the main streak).
        public void ShardFlight(Vector2 position, Vector2 velocity)
        {
            if (Main.dedServ || !Main.rand.NextBool(4))
                return;
            Metal(position, -velocity * 0.08f + Main.rand.NextVector2Circular(0.6f, 0.6f), 0.8f);
        }

        public void ShardHit(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 5; i++)
                Metal(position + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextVector2Circular(3.5f, 3.5f), 1f);
            SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.35f, Pitch = 0.4f, MaxInstances = 4 }, position);
        }

        // The shard breaks (after its last enemy, on a tile, or at the end of its life).
        public void ShardBreak(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Metal(position, Main.rand.NextVector2Circular(3f, 3f), 1f);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f, Pitch = 0.5f, MaxInstances = 4 }, position);
        }
    }
}