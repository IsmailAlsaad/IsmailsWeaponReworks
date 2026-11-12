using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.BlueMoon
{
    /// <summary>All dust and sound for the Blue Moon (vanilla dust and sounds only): water droplets, blue sparks and splashes.</summary>
    public class BlueMoonFx : FlailFx
    {
        public static BlueMoonFx Instance { get; } = new BlueMoonFx();

        static Dust Water(Vector2 position, Vector2 velocity, float scale)
        {
            int roll = Main.rand.Next(4);
            int type = roll == 0 ? DustID.BlueTorch : roll == 1 ? DustID.MagicMirror : DustID.Water;
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 80, default, scale);
            dust.noGravity = true;
            return dust;
        }

        public override void ChargeGather(Vector2 center, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 4 - tier)))
                return;
            Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * (30f + 16f * (1f - progress));
            Water(center + offset, -offset * 0.08f, 1f + 0.4f * tier);
        }

        public override void SpinEmbers(Vector2 head, Vector2 tangent, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 3 - (int)(progress * 2f))))
                return;
            Water(head + Main.rand.NextVector2Circular(8f, 8f), tangent * 0.35f + Main.rand.NextVector2Circular(1f, 1f), 0.9f + 0.3f * tier);
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
                Water(head + direction * 8f, direction * (3f + 0.6f * tier), 1.3f);
            }
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.7f, Pitch = -0.1f + 0.2f * tier }, head);
        }

        public override void ThrowWhoosh(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Water(head, Main.rand.NextVector2Circular(3f, 3f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.1f + 0.15f * tier }, head);
        }

        public override void ApexJingle(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Water(head, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
        }

        public override void Caught(Vector2 hand, int nextTier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Water(hand + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(2.5f, 2.5f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.6f, Pitch = -0.2f + 0.3f * nextTier }, hand);
        }

        public override void Fizzle(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Water(head, Main.rand.NextVector2Circular(1.5f, 1.5f), 0.9f);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.5f, Pitch = -0.4f }, head);
        }

        public override void LockedChain(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 12; i++)
                Water(head, Main.rand.NextVector2Circular(4f, 4f), 1.2f);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.9f, Pitch = -0.5f }, head);
        }

        public override void Mistimed(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Water(head, Main.rand.NextVector2Circular(2f, 2f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.6f, Pitch = -0.6f }, head);
        }

        public override void CatchHead(Vector2 hand)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Water(hand, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
            SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.4f, Pitch = 0.2f }, hand);
        }

        public override void FlightDust(Vector2 position, Vector2 velocity, int tier)
        {
            if (Main.dedServ)
                return;
            int count = tier >= 3 ? 2 : 1;
            for (int i = 0; i < count; i++)
                if (Main.rand.NextBool(2))
                    Water(position + Main.rand.NextVector2Circular(10f, 10f), -velocity * 0.06f + Main.rand.NextVector2Circular(0.8f, 0.8f), 1f + 0.3f * tier);
        }

        public override void SpecialLaunch(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 16; i++)
                Water(head, Main.rand.NextVector2Circular(5f, 5f), 1.4f);
            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.6f, Pitch = 0.2f }, head);
        }

        public override void HitSparks(Vector2 position, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6 + 2 * tier; i++)
                Water(position + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextVector2Circular(4f, 4f), 1.1f);
        }

        public override void DrawLayers() { }

        // The water magic explosion on a hit enemy: a ring of droplets thrown out, a burst of splash dust and a splash sound.
        public void WaterBlast(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            int ring = 22;
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < ring; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / ring).ToRotationVector2();
                Water(center + direction * radius * 0.3f, direction * Main.rand.NextFloat(4f, 8f), 1.6f);
            }
            for (int i = 0; i < 26; i++)
                Water(center + Main.rand.NextVector2Circular(radius * 0.5f, radius * 0.5f), Main.rand.NextVector2Circular(5f, 5f), 1.3f);
            SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.7f, Pitch = 0.1f }, center);
            SoundEngine.PlaySound(SoundID.Item85 with { Volume = 0.4f, Pitch = 0.3f }, center);
        }
    }
}