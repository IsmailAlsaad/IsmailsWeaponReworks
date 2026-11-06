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

        // A latched bee stings: an amber fleck and now and then a small sound.
        public static void Sting(Vector2 position, bool big)
        {
            if (Main.dedServ)
                return;
            Spark(position, Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextFloat(0.7f, 1f), DustID.GoldFlame);
            if (Main.rand.NextFloat() < BeeKeeperTuning.StingSoundChance)
                SoundEngine.PlaySound(SoundID.Item17 with { Volume = big ? 0.3f : 0.2f, Pitch = big ? 0.2f : 0.6f, PitchVariance = 0.2f }, position);
        }

        // A bee lets go of its enemy after the final sting: a small burst of amber.
        public static void FinalSting(Vector2 position, bool big)
        {
            if (Main.dedServ)
                return;
            int count = big ? 8 : 5;
            for (int i = 0; i < count; i++)
            {
                Spark(position, Main.rand.NextVector2Circular(3f, 3f), Main.rand.NextFloat(0.9f, 1.3f), DustID.GoldFlame);
            }
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = big ? 0.45f : 0.3f, Pitch = big ? 0f : 0.4f, PitchVariance = 0.2f }, position);
        }

        // ---- honey (every machine calls these from its own copy of the bee; visual only) ----

        // A drop of honey leaves the enemy (or a bee carrying a load): a metaball particle that falls, merges with its neighbours and is gone
        // when it lands.
        public static void HoneyDrip(Vector2 position)
        {
            if (Main.dedServ)
                return;
            MetaballLayers.Of<BeeKeeperHoneyStyle>().Spawn(
                position + new Vector2(Main.rand.NextFloat(-5f, 5f), 0f),
                new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(0f, 0.6f)),
                Main.rand.Next(BeeKeeperTuning.HoneyDripLifeMin, BeeKeeperTuning.HoneyDripLifeMax + 1),
                Main.rand.NextFloat(BeeKeeperTuning.HoneyDripSizeMin, BeeKeeperTuning.HoneyDripSizeMax));
        }

        // The honey a returning bee carries: a metaball body for THIS tick, hanging under the bee (call it every tick).
        public static void HoneyBlob(Vector2 position, float carry, bool big)
        {
            if (Main.dedServ)
                return;
            carry = MathHelper.Clamp(carry, 0f, 1f);
            float kind = big ? 1f : BeeKeeperTuning.HoneyBlobSmallScale;
            float scale = MathHelper.Lerp(BeeKeeperTuning.HoneyBlobMinScale, 1f, carry) * kind;
            MetaballLayers.Of<BeeKeeperHoneyStyle>().AddBlob(position + new Vector2(0f, BeeKeeperTuning.HoneyBlobOffset * kind), BeeKeeperTuning.HoneyBlobRadius * scale, MathHelper.Clamp(BeeKeeperTuning.HoneyBlobHeat + 0.3f * carry, 0f, 1f));
        }

        // A returning bee reaches the owner and hands its honey over: honey flecks and now and then a soft pop.
        public static void Deliver(Vector2 position, float carry, bool big)
        {
            if (Main.dedServ)
                return;
            int count = 2 + (int)(MathHelper.Clamp(carry, 0f, 1f) * (big ? 5f : 3f));
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.Honey, Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
            if (Main.rand.NextFloat() < BeeKeeperTuning.DeliverSoundChance)
                SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.25f, Pitch = 0.9f, PitchVariance = 0.15f }, position);
        }

        // The owner gets the honey of the bees that just arrived (one heal and one buff top-up): a ring of honey flecks and a soft sound.
        public static void HoneyHeal(Vector2 center, float honey)
        {
            if (Main.dedServ)
                return;
            int count = 6 + (int)System.Math.Min(14f, honey * 3f);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust dust = Dust.NewDustPerfect(center + direction * 14f, DustID.Honey, direction * Main.rand.NextFloat(0.5f, 2f), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item3 with { Volume = 0.5f, Pitch = 0.4f, PitchVariance = 0.15f }, center);
        }
    }
}