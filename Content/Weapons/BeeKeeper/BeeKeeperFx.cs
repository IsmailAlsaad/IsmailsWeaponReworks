using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// Every dust and sound of the Bee Keeper rework (vanilla dust and sounds only). Every machine calls these for the same event; all of
    /// it is visual, nothing here is synced.
    /// </summary>
    public static class BeeKeeperFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position, bool up)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = up ? 0.1f : -0.1f, PitchVariance = 0.1f }, position);
        }

        public static void PlayHit(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.45f, Pitch = 0.5f, PitchVariance = 0.2f }, position);
        }

        public static void PlayBeePop(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.2f, Pitch = 0.7f, PitchVariance = 0.2f }, position);
        }

        public static void PlayBeeHurt(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.3f, Pitch = 0.9f, PitchVariance = 0.2f }, position);
        }

        public static void PlayCloud(Vector2 position, bool big)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCDeath1 with { Volume = big ? 0.5f : 0.35f, Pitch = big ? 0.2f : 0.5f, PitchVariance = 0.15f }, position);
        }

        // ---- dust ----

        static void Spark(Vector2 position, Vector2 velocity, float scale, int type)
        {
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 0, default, scale);
            dust.noGravity = true;
        }

        // Amber flecks off the blade tip while it strikes.
        public static void SlashDust(Vector2 ownerCenter, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Spark(tip, Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.NextFloat(0.9f, 1.4f), Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Torch);
        }

        public static void HitSpark(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Spark(position, Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(1f, 1.5f), i % 2 == 0 ? DustID.GoldFlame : DustID.Torch);
        }

        // A new bee appears over the player's head.
        public static void BeeSpawn(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 5; i++)
                Spark(position, Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextFloat(0.8f, 1.2f), DustID.GoldFlame);
            PlayBeePop(position);
        }

        public static void BeeHurt(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 4; i++)
                Spark(position, Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(0.8f, 1.2f), DustID.Torch);
            PlayBeeHurt(position);
        }

        // ---- the swarm attack (every machine calls these when the bee's state changes or its timers fire; visual only) ----

        // A bee leaves the swarm for an enemy.
        public static void Launch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 3; i++)
                Spark(position, Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextFloat(0.8f, 1.2f), DustID.GoldFlame);
            if (Main.rand.NextFloat() < BeeKeeperTuning.StingSoundChance)
                PlayBeePop(position);
        }

        // A faint amber streak behind a bee on its way.
        public static void TravelTrail(Vector2 position, Vector2 velocity)
        {
            if (Main.dedServ)
                return;
            Spark(position - velocity * 0.5f, -velocity * 0.1f, Main.rand.NextFloat(0.6f, 0.9f), DustID.GoldFlame);
        }

        // A latched bee stings: a green fleck, an amber one and now and then a small sound.
        public static void Sting(Vector2 position, bool big)
        {
            if (Main.dedServ)
                return;
            Dust poison = Dust.NewDustPerfect(position, DustID.Poisoned, Main.rand.NextVector2Circular(1.5f, 1.5f), 120, Color.White, Main.rand.NextFloat(0.8f, 1.2f));
            poison.noGravity = true;
            Spark(position, Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextFloat(0.7f, 1f), DustID.GoldFlame);
            if (Main.rand.NextFloat() < BeeKeeperTuning.StingSoundChance)
                SoundEngine.PlaySound(SoundID.Item17 with { Volume = big ? 0.3f : 0.2f, Pitch = big ? 0.2f : 0.6f, PitchVariance = 0.2f }, position);
        }

        // A bee lets go of its enemy after the final sting: a small burst of green and amber.
        public static void FinalSting(Vector2 position, bool big)
        {
            if (Main.dedServ)
                return;
            int count = big ? 8 : 5;
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                {
                    Dust poison = Dust.NewDustPerfect(position, DustID.Poisoned, Main.rand.NextVector2Circular(3f, 3f), 120, Color.White, Main.rand.NextFloat(0.9f, 1.4f));
                    poison.noGravity = true;
                }
                else
                {
                    Spark(position, Main.rand.NextVector2Circular(3f, 3f), Main.rand.NextFloat(0.9f, 1.3f), DustID.GoldFlame);
                }
            }
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = big ? 0.45f : 0.3f, Pitch = big ? 0f : 0.4f, PitchVariance = 0.2f }, position);
        }

        // ---- the poison cloud ----

        // 'radius' relative to a big bee's cloud: the puffs of a small cloud are a little smaller.
        static float SizeScale(float radius)
        {
            return System.Math.Max(0.6f, radius / BeeKeeperTuning.CloudRadius);
        }

        // The first tick of a cloud: a ring of puffs thrown outward.
        public static void CloudBurst(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            MetaballLayer layer = MetaballLayers.Of<BeeKeeperCloudStyle>();
            int count = BeeKeeperTuning.CloudBurstPuffs;
            float scale = SizeScale(radius);
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.25f, 0.25f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(1.5f, 3.5f) * scale;
                layer.Spawn(center + velocity * 2f, velocity, Main.rand.Next(40, 65), Main.rand.NextFloat(3f, 12f) * scale);
            }
            for (int i = 0; i < 2; i++)
                Smoke(center + Main.rand.NextVector2Circular(radius * 0.8f, radius * 0.8f), Main.rand.NextVector2Circular(10f, 10f));
        }

        // Every tick of a living cloud: new puffs inside it so it keeps rolling.
        public static void CloudPuffs(Vector2 center, float radius, int ticks)
        {
            if (Main.dedServ)
                return;
            MetaballLayer layer = MetaballLayers.Of<BeeKeeperCloudStyle>();
            float scale = SizeScale(radius);
            for (int i = 0; i < BeeKeeperTuning.CloudPuffsPerTick; i++)
            {
                Vector2 at = center + Main.rand.NextVector2Circular(radius * 0.7f, radius * 0.7f);
                layer.Spawn(at, Main.rand.NextVector2Circular(0.4f, 0.4f), Main.rand.Next(45, 75), Main.rand.NextFloat(6f, 11f) * scale);
            }
            if (ticks % 3 == 0)
                Smoke(center + Main.rand.NextVector2Circular(radius * 0.8f, radius * 0.8f), Main.rand.NextVector2Circular(0.5f, 0.5f));
        }

        static void Smoke(Vector2 position, Vector2 velocity)
        {
            Dust dust = Dust.NewDustPerfect(position, DustID.Poisoned, velocity, 120, Color.White, Main.rand.NextFloat(1f, 1.5f));
            dust.noGravity = true;
        }
    }
}