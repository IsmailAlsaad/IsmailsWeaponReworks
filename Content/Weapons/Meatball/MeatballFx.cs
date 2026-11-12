using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Meatball
{
    /// <summary>
    /// All dust, sound and metaball effects of the Meatball (vanilla dust and sounds only): blood spray, wet thumps, gore globs. The first
    /// part is the shared flail loop's FlailFx; the second part is the detached ball (tear-off, splat, heartbeat, drips, the blood explosion).
    /// Every method runs on every machine for the same event and returns at once on a dedicated server. Nothing here is synced.
    /// </summary>
    public class MeatballFx : FlailFx
    {
        public static MeatballFx Instance { get; } = new MeatballFx();

        static MeatballTuning Tune => MeatballTuning.Instance;

        static Dust Blood(Vector2 position, Vector2 velocity, float scale, bool noGravity = true)
        {
            int type = Main.rand.NextBool(5) ? DustID.RedTorch : DustID.Blood;
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 0, default, scale);
            dust.noGravity = noGravity;
            return dust;
        }

        static MetaballLayer Layer => MetaballLayers.Of<MeatballBloodStyle>();

        // ---- the shared flail loop ----

        public override void ChargeGather(Vector2 center, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 4 - tier)))
                return;
            Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * (30f + 16f * (1f - progress));
            Blood(center + offset, -offset * 0.08f, 1f + 0.4f * tier);
        }

        public override void SpinEmbers(Vector2 head, Vector2 tangent, int tier, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(Math.Max(1, 3 - (int)(progress * 2f))))
                return;
            Blood(head + Main.rand.NextVector2Circular(8f, 8f), tangent * 0.35f + Main.rand.NextVector2Circular(1f, 1f), 0.9f + 0.3f * tier, Main.rand.NextBool(2));
        }

        public override void TierComplete(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            int count = 12 + 5 * tier;
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            MetaballLayer blood = Layer;
            blood.Spawn(head, Vector2.Zero, 16, 4f + 1.5f * tier);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / count).ToRotationVector2();
                Blood(head + direction * 8f, direction * (3f + 0.6f * tier), 1.3f);
                if (i % 2 == 0)
                    blood.Spawn(head + direction * 8f, direction * (2.6f + 0.5f * tier) * Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(18, 28), Main.rand.NextFloat(2.2f, 3.4f));
            }
            SoundEngine.PlaySound(SoundID.NPCDeath21 with { Volume = 0.5f, Pitch = -0.6f + 0.3f * tier }, head);
        }

        public override void ThrowWhoosh(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Blood(head, Main.rand.NextVector2Circular(3f, 3f), 1.1f, Main.rand.NextBool(2));
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.4f + 0.15f * tier }, head);
        }

        public override void ApexJingle(Vector2 head, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Blood(head, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f);
        }

        public override void Caught(Vector2 hand, int nextTier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Blood(hand + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(2.5f, 2.5f), 1.1f, Main.rand.NextBool(2));

            SoundStyle sound = Main.rand.Next() == 0 ? SoundID.Zombie24 : SoundID.Zombie25;
            SoundEngine.PlaySound(sound with { Volume = 0.5f, Pitch = -0.3f + 0.25f * nextTier }, hand);
        }

        public override void Fizzle(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Blood(head, Main.rand.NextVector2Circular(1.5f, 1.5f), 0.9f, false);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.5f, Pitch = -0.5f }, head);
        }

        public override void LockedChain(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 12; i++)
                Blood(head, Main.rand.NextVector2Circular(4f, 4f), 1.2f, false);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.9f, Pitch = -0.5f }, head);
        }

        public override void Mistimed(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Blood(head, Main.rand.NextVector2Circular(2f, 2f), 1.1f, false);
            SoundEngine.PlaySound(SoundID.Item13 with { Volume = 0.6f, Pitch = -0.6f }, head);
        }

        public override void CatchHead(Vector2 hand)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Blood(hand, Main.rand.NextVector2Circular(2.5f, 2.5f), 1f, Main.rand.NextBool(2));
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.4f, Pitch = 0.1f }, hand);
        }

        public override void FlightDust(Vector2 position, Vector2 velocity, int tier)
        {
            if (Main.dedServ)
                return;
            int count = tier >= 3 ? 2 : 1;
            for (int i = 0; i < count; i++)
                if (Main.rand.NextBool(2))
                    Blood(position + Main.rand.NextVector2Circular(10f, 10f), -velocity * 0.06f + Main.rand.NextVector2Circular(0.8f, 0.8f), 1f + 0.3f * tier, Main.rand.NextBool(3));
        }

        public override void SpecialLaunch(Vector2 head)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 16; i++)
                Blood(head, Main.rand.NextVector2Circular(5f, 5f), 1.4f, Main.rand.NextBool(2));
            SoundEngine.PlaySound(SoundID.NPCDeath23 with { Volume = 0.7f, Pitch = -0.5f }, head);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.4f }, head);
        }

        public override void HitSparks(Vector2 position, int tier)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6 + 2 * tier; i++)
                Blood(position + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextVector2Circular(4f, 4f), 1.1f, Main.rand.NextBool(2));
            if (tier >= 3)
            {
                MetaballLayer blood = Layer;
                for (int i = 0; i < 4; i++)
                    blood.Spawn(position, Main.rand.NextVector2Circular(3f, 3f), Main.rand.Next(14, 22), Main.rand.NextFloat(1.8f, 3f));
            }
        }

        // The blood metaballs, drawn from a Meatball projectile's PreDraw (once per frame, in front of enemies). If no Meatball projectile is
        // alive the shared system draws the layer behind enemies instead, so the blood never just vanishes.
        public override void DrawLayers()
        {
            if (Main.dedServ)
                return;
            Layer.DrawInBatch();
        }

        // ---- the detached ball ----

        // The ball tears loose at the apex: the chain snaps with a spray of blood in the direction of the throw.
        public void Detach(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            Vector2 forward = direction.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 28; i++)
                Blood(position, forward.RotatedByRandom(0.7f) * Main.rand.NextFloat(2f, 9f), Main.rand.NextFloat(1.1f, 1.9f), Main.rand.NextBool(3));
            MetaballLayer blood = Layer;
            blood.Spawn(position, Vector2.Zero, 16, 6f);
            for (int i = 0; i < 12; i++)
                blood.Spawn(position, forward.RotatedByRandom(0.6f) * Main.rand.NextFloat(3f, 9f), Main.rand.Next(16, 28), Main.rand.NextFloat(1.8f, 3.4f));
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.2f }, position);
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.9f, Pitch = -0.35f }, position);
            CameraShake.Punch(position, 2.5f, 10, "MeatballDetach");
        }

        // One tick of the ball in flight: a trail of drops (the trail line itself belongs to the projectile).
        public void BallFlight(Vector2 position, Vector2 velocity)
        {
            if (Main.dedServ)
                return;
            if (Main.rand.NextBool(2))
                Blood(position + Main.rand.NextVector2Circular(8f, 8f), -velocity * 0.08f + Main.rand.NextVector2Circular(0.8f, 0.8f), 1.3f, Main.rand.NextBool(2));
            if (Main.rand.NextBool(3))
                Layer.Spawn(position, -velocity * 0.05f + Main.rand.NextVector2Circular(1f, 1f), Main.rand.Next(14, 24), Main.rand.NextFloat(1.8f, 3.2f));
        }

        // The ball sticks to an enemy or a tile: a wet splat.
        public void Attach(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 20; i++)
                Blood(position, Main.rand.NextVector2Circular(5f, 5f), 1.5f, Main.rand.NextBool(2));
            MetaballLayer blood = Layer;
            for (int i = 0; i < 12; i++)
                blood.Spawn(position, Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f), Main.rand.Next(16, 26), Main.rand.NextFloat(2f, 4.2f));
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 1f, Pitch = -0.45f }, position);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, Pitch = -0.6f }, position);
            CameraShake.Punch(position, 2f, 8, "MeatballAttach");
        }

        // One heartbeat of the fuse. t runs from 0 (just stuck) to 1 (about to go off): the thump gets louder and higher, more blood is
        // pumped out, and near the end the screen shakes a little.
        public void Beat(Vector2 center, float t)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.3f + 0.6f * t, Pitch = -0.8f + 0.7f * t, PitchVariance = 0.05f }, center);
            int count = 4 + (int)(8f * t);
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            MetaballLayer blood = Layer;
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / count).ToRotationVector2();
                blood.Spawn(center + direction * 6f, direction * (1.5f + 2.5f * t) * Main.rand.NextFloat(0.8f, 1.2f), Main.rand.Next(14, 22), Main.rand.NextFloat(1.8f, 3f));
                if (i % 2 == 0)
                    Blood(center + direction * 8f, direction * (1.5f + 2f * t), 1.1f, Main.rand.NextBool(2));
            }
            if (t > 0.65f)
                CameraShake.Punch(center, 1.5f * t, 8, "MeatballBeat");
        }

        // A drop falling off the swelling ball.
        public void Drip(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Layer.Spawn(position + Main.rand.NextVector2Circular(8f, 8f), new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), 0.5f), Main.rand.Next(24, 40), Main.rand.NextFloat(1.6f, 2.8f));
        }

        // The blood explosion: a ring of blood that flies out to the radius, heavy clots that fall back, red smoke, gore globs that merge into
        // one blob and droplets that spray out, a thud, a wet burst and a camera punch.
        public void Explode(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.55f }, center);
            SoundEngine.PlaySound(SoundID.Zombie21 with { Volume = 1f, Pitch = -0.2f }, center);

            int ring = 44;
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < ring; i++)
            {
                Vector2 direction = (offset + MathHelper.TwoPi * i / ring).ToRotationVector2();
                Blood(center + direction * radius * 0.12f, direction * radius * Main.rand.NextFloat(0.07f, 0.11f), 1.8f);
            }
            for (int i = 0; i < 40; i++)
                Blood(center + Main.rand.NextVector2Circular(radius * 0.1f, radius * 0.1f), Main.rand.NextVector2Circular(radius * 0.08f, radius * 0.08f) + new Vector2(0f, -2.5f), Main.rand.NextFloat(1.6f, 2.6f), false);
            for (int i = 0; i < 24; i++)
            {
                Dust smoke = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(radius * 0.4f, radius * 0.4f), DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f) + new Vector2(0f, -1.5f), 150, new Color(90, 8, 14), 2f);
                smoke.noGravity = true;
            }

            MetaballLayer blood = Layer;
            for (int i = 0; i < Tune.ExplosionCores; i++)
                blood.Spawn(center + Main.rand.NextVector2Circular(radius * 0.05f, radius * 0.05f), Main.rand.NextVector2Circular(1f, 1f) * Tune.ExplosionCoreSpeed,
                    Main.rand.Next(18, 29), Main.rand.NextFloat(Tune.ExplosionCoreSizeMin, Tune.ExplosionCoreSizeMax));
            for (int i = 0; i < Tune.ExplosionDroplets; i++)
                blood.Spawn(center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(Tune.ExplosionSpeedMin, Tune.ExplosionSpeedMax),
                    Main.rand.Next(24, 45), Main.rand.NextFloat(Tune.ExplosionDropMin, Tune.ExplosionDropMax));

            CameraShake.Punch(center, Tune.ExplosionShake, 22, "MeatballBlast");
        }
    }
}