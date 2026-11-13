using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.FlamingMace
{
    /// <summary>All dust and sound for the Flaming Mace (vanilla dust and sounds only): flames, embers, smoke and fire bursts.</summary>
    public class FlamingMaceFx : FlailFx
    {
        public static FlamingMaceFx Instance { get; } = new FlamingMaceFx();

        static Dust Fire(Vector2 position, Vector2 velocity, float scale)
        {
            int roll = Main.rand.Next(4);
            int type = roll == 0 ? DustID.InfernoFork : roll == 1 ? DustID.GoldFlame : DustID.Torch;
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 80, default, scale);
            dust.noGravity = true;
            return dust;
        }

        static void Smoke(Vector2 position, Vector2 velocity, float scale)
        {
            Dust smoke = Dust.NewDustPerfect(position, DustID.Smoke, velocity, 120, default, scale);
            smoke.noGravity = true;
        }

        public override void ChargeGather(Vector2 center, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 4 - tier)))
                return;
            Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * (30f + 16f * (1f - progress));
            Fire(center + offset, -offset * 0.08f, 1f + 0.4f * tier);
        }

        public override void SpinEmbers(Vector2 head, Vector2 tangent, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 3 - (int)(progress * 2f))))
                return;
            Fire(head + Main.rand.NextVector2Circular(8f, 8f), tangent * 0.35f + Main.rand.NextVector2Circular(1f, 1f), 0.9f + 0.3f * tier);
        }

        public override void TierComplete(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            int count = 12 + 5 * tier;
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / count).ToRotationVector2();
                Fire(head + direction * 8f, direction * (3f + 0.6f * tier), 1.3f);
            }
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.7f, Pitch = -0.2f + 0.2f * tier }, head);
        }

        public override void ThrowWhoosh(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Fire(head, Main.rand.NextVector2Circular(3f, 3f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.3f + 0.15f * tier }, head);
        }

        public override void ApexJingle(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Fire(head, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
        }

        public override void Caught(Vector2 hand, int nextTier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Fire(hand + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(2.5f, 2.5f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.7f, Pitch = -0.7f + 0.4f * nextTier }, hand);
        }

        public override void Fizzle(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Smoke(head, Main.rand.NextVector2Circular(1.5f, 1.5f), 1f);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.5f, Pitch = -0.4f }, head);
        }

        public override void LockedChain(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Smoke(head, Main.rand.NextVector2Circular(2f, 2f), 1.2f);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.9f, Pitch = -0.5f }, head);
        }

        public override void Mistimed(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Smoke(head, Main.rand.NextVector2Circular(2f, 2f), 1.2f);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.6f, Pitch = -0.6f }, head);
        }

        public override void CatchHead(Vector2 hand)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Fire(hand, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.35f, Pitch = -0.1f }, hand);
        }

        public override void FlightDust(Vector2 position, Vector2 velocity, int tier)
        {
            if (Main.dedServ)
                return;
            if (tier >= 3)
            {
                // The special's head leaves a burning streak: a lot of flames, drifting embers and some smoke.
                for (int i = 0; i < 3; i++)
                    Fire(position + Main.rand.NextVector2Circular(12f, 12f), -velocity * 0.08f + Main.rand.NextVector2Circular(1f, 1f), 1.4f);
                if (Main.rand.NextBool(2))
                    Fire(position + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextVector2Circular(2f, 2f) + new Vector2(0f, -1f), 0.9f);
                if (Main.rand.NextBool(4))
                    Smoke(position + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(1f, 1f) + new Vector2(0f, -0.8f), 1.1f);
                return;
            }
            if (Main.rand.NextBool(2))
                Fire(position + Main.rand.NextVector2Circular(10f, 10f), -velocity * 0.06f + Main.rand.NextVector2Circular(0.8f, 0.8f), 1f + 0.3f * tier);
        }

        public override void SpecialLaunch(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 18; i++)
                Fire(head, Main.rand.NextVector2Circular(5f, 5f), 1.5f);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.1f }, head);
        }

        public override void HitSparks(Vector2 position, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6 + 2 * tier; i++)
                Fire(position + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextVector2Circular(4f, 4f), 1.1f);
        }

        public override void DrawLayers() { }

        // The small fireball on an enemy the special passes through: a ring of flames thrown out, flames and smoke in the middle, a boom.
        public void FireBlast(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            int ring = 20;
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < ring; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / ring).ToRotationVector2();
                Fire(center + direction * radius * 0.3f, direction * Main.rand.NextFloat(3.5f, 7f), 1.6f);
            }
            for (int i = 0; i < 18; i++)
                Fire(center + Main.rand.NextVector2Circular(radius * 0.5f, radius * 0.5f), Main.rand.NextVector2Circular(4f, 4f), 1.3f);
            for (int i = 0; i < 5; i++)
                Smoke(center + Main.rand.NextVector2Circular(radius * 0.4f, radius * 0.4f), Main.rand.NextVector2Circular(2.5f, 2.5f) + new Vector2(0f, -1f), 1.6f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.3f, MaxInstances = 6 }, center);
            CameraShake.Punch(center, FlamingMaceTuning.Instance.BlastShake, 10, "FlamingMaceBlast");
        }
    }
}