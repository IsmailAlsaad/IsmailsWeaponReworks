using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.BallOHurt
{
    /// <summary>
    /// All dust and sound for the Ball O' Hurt (vanilla dust and sounds only): corruption purple with a sickly green-yellow core. The first
    /// part is the shared flail loop's FlailFx; the second part is the loose ball (tear-off, tile bounce, hop, recall).
    /// Every method runs on every machine for the same event and returns at once on a dedicated server. Nothing here is synced.
    /// </summary>
    public class BallOHurtFx : FlailFx
    {
        public static BallOHurtFx Instance { get; } = new BallOHurtFx();

        static Dust Rot(Vector2 position, Vector2 velocity, float scale)
        {
            int roll = Main.rand.Next(5);
            int type = roll == 0 ? DustID.CursedTorch : roll == 1 ? DustID.Shadowflame : roll == 2 ? DustID.PurpleTorch : DustID.Corruption;
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 60, default, scale);
            dust.noGravity = true;
            return dust;
        }

        public override void ChargeGather(Vector2 center, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 4 - tier)))
                return;
            Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * (30f + 16f * (1f - progress));
            Rot(center + offset, -offset * 0.08f, 1f + 0.4f * tier);
        }

        public override void SpinEmbers(Vector2 head, Vector2 tangent, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 3 - (int)(progress * 2f))))
                return;
            Rot(head + Main.rand.NextVector2Circular(8f, 8f), tangent * 0.35f + Main.rand.NextVector2Circular(1f, 1f), 0.9f + 0.3f * tier);
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
                Rot(head + direction * 8f, direction * (3f + 0.6f * tier), 1.3f);
            }
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.3f + 0.2f * tier }, head);
        }

        public override void ThrowWhoosh(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Rot(head, Main.rand.NextVector2Circular(3f, 3f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.2f + 0.15f * tier }, head);
        }

        public override void ApexJingle(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Rot(head, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
        }

        public override void Caught(Vector2 hand, int nextTier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Rot(hand + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(2.5f, 2.5f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = -0.3f + 0.25f * nextTier }, hand);
        }

        public override void Fizzle(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Rot(head, Main.rand.NextVector2Circular(1.5f, 1.5f), 0.9f);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.5f, Pitch = -0.5f }, head);
        }

        public override void LockedChain(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 12; i++)
                Rot(head, Main.rand.NextVector2Circular(4f, 4f), 1.2f);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.9f, Pitch = -0.5f }, head);
        }

        public override void Mistimed(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Rot(head, Main.rand.NextVector2Circular(2f, 2f), 1.1f);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.6f, Pitch = -0.6f }, head);
        }

        public override void CatchHead(Vector2 hand)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Rot(hand, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.45f, Pitch = 0.2f }, hand);
        }

        public override void FlightDust(Vector2 position, Vector2 velocity, int tier)
        {
            if (Main.dedServ)
                return;
            int count = tier >= 3 ? 2 : 1;
            for (int i = 0; i < count; i++)
                if (Main.rand.NextBool(2))
                    Rot(position + Main.rand.NextVector2Circular(10f, 10f), -velocity * 0.06f + Main.rand.NextVector2Circular(0.8f, 0.8f), 1f + 0.3f * tier);
        }

        public override void SpecialLaunch(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 16; i++)
                Rot(head, Main.rand.NextVector2Circular(5f, 5f), 1.4f);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.4f }, head);
        }

        public override void HitSparks(Vector2 position, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6 + 2 * tier; i++)
                Rot(position + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextVector2Circular(4f, 4f), 1.1f);
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = -0.3f, MaxInstances = 4 }, position);
        }

        public override void DrawLayers() { }

        // ---- the loose ball ----

        // The ball tears loose from its chain.
        public void Detach(Vector2 at, Vector2 velocity)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 20; i++)
                Rot(at, Main.rand.NextVector2Circular(5f, 5f) + velocity * 0.1f, 1.5f);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.8f, Pitch = 0.3f }, at);
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.6f, Pitch = -0.4f }, at);
        }

        // The ball bounced off a tile.
        public void TileBounce(Vector2 at)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Rot(at, Main.rand.NextVector2Circular(4f, 4f), 1.2f);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.45f, Pitch = 0.4f, MaxInstances = 4 }, at);
        }

        // The ball reached an enemy (hops = how many it has reached so far, the first one is 1).
        public void Hop(Vector2 at, int hops)
        {
            if (Main.dedServ)
                return;
            int ring = 16;
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < ring; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / ring).ToRotationVector2();
                Rot(at, direction * Main.rand.NextFloat(3f, 7f), 1.5f);
            }
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.45f, Pitch = 0.4f, MaxInstances = 4 }, at);

            //SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = Math.Min(0.8f, -0.3f + 0.1f * hops), MaxInstances = 4 }, at);
        }

        // The ball is out of hops and goes home.
        public void Recall(Vector2 at)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 12; i++)
                Rot(at, Main.rand.NextVector2Circular(3f, 3f), 1.2f);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.5f, Pitch = 0.1f }, at);
        }
    }
}