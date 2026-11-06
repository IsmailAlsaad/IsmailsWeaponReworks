using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// The Bee Keeper's state on the owner's machine (nothing here is synced: the owner decides and the bee projectiles carry what the
    /// other machines need). It remembers how the last swing ended (so the next one continues from there and alternates) and it runs the
    /// passive swarm: while the sword is held, and for LingerTicks after it is put away, it adds one bee every SpawnIntervalTicks until
    /// there are MaxBig big and MaxSmall small ones. When the linger time runs out, the bees see it (Linger is 0) and leave by themselves.
    /// It also counts the true melee hits on each enemy and, on every hit, sends bees from the idle pool at it (RecruitBees, owner's client).
    /// Bees that come home with honey hand it over through Deliver: the honey of bees arriving within DeliveryBatchTicks of each other is
    /// collected, then given as ONE heal and ONE Honey buff top-up.
    /// </summary>
    public class BeeKeeperPlayer : ModPlayer
    {
        public int LastPhase = -1;       // BeeKeeperSwingProjectile.Phase of the previous swing, -1 = none yet
        public float LastEndArm;         // where the arm was when the previous swing ended (relative to its aim)
        public uint LastEndTick;         // Main.GameUpdateCount when it ended

        public int Linger;               // ticks the swarm stays after the sword was last held (owner only)
        int spawnTimer;

        // Honey handed over by returning bees, waiting to be given (owner only).
        float pendingHealth;
        float pendingHoney;
        int deliveryTimer;

        // How often each enemy was hit by the sword's swing (owner only; the key is the NPC's slot, the type tells a reused slot apart).
        struct HitRecord
        {
            public int Type;
            public uint LastTick;
            public int Count;
        }
        Dictionary<int, HitRecord> hits;

        public override void OnRespawn()
        {
            Linger = 0;
            hits?.Clear();
            pendingHealth = 0f;
            pendingHoney = 0f;
            deliveryTimer = 0;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            TickDelivery();

            if (Player.dead)
                Linger = 0;
            else if (Player.HeldItem.type == ItemID.BeeKeeper)
                Linger = BeeKeeperTuning.LingerTicks;
            else if (Linger > 0)
                Linger--;

            if (Linger <= 0)
            {
                spawnTimer = 0;
                return;
            }

            spawnTimer++;
            if (spawnTimer < BeeKeeperTuning.SpawnIntervalTicks || Player.HeldItem.type != ItemID.BeeKeeper)
                return;
            spawnTimer = 0;
            SpawnBee();
        }

        // Owner's client only: a true melee hit of the swing landed on 'target'. Counts the hit and sends bees at the enemy: BeesPerHit on
        // the first hit, growing evenly to every idle bee on the HitsForFullSwarm-th, picked at random from the idle pool only (big or small;
        // a bee that is travelling, latched on another enemy or leaving is never taken). 'swordDamage' is the swing's damage.
        public void RecruitBees(NPC target, int swordDamage)
        {
            if (Player.whoAmI != Main.myPlayer || Player.dead || !target.active)
                return;

            hits ??= new Dictionary<int, HitRecord>();
            uint now = Main.GameUpdateCount;
            if (!hits.TryGetValue(target.whoAmI, out HitRecord record) || record.Type != target.type || now - record.LastTick > (uint)BeeKeeperTuning.HitMemoryTicks)
                record = new HitRecord { Type = target.type };
            record.Count++;
            record.LastTick = now;
            hits[target.whoAmI] = record;

            List<BeeKeeperBeeProjectile> pool = new List<BeeKeeperBeeProjectile>();
            int onEnemy = 0; // bees of this player already flying to or sitting on this enemy
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.ModProjectile is BeeKeeperBeeProjectile bee)
                {
                    if (bee.IsIdle)
                        pool.Add(bee);
                    else if (bee.IsSentAt(target.whoAmI))
                        onEnemy++;
                }
            }
            int room = BeeKeeperTuning.MaxBeesPerEnemy - onEnemy;
            if (pool.Count == 0 || room <= 0)
                return; // nobody idle, or this enemy already has its full share of bees

            int full = Math.Max(1, BeeKeeperTuning.HitsForFullSwarm);
            float progress = full <= 1 ? 1f : (Math.Min(record.Count, full) - 1) / (float)(full - 1);
            int wanted = (int)Math.Ceiling(MathHelper.Lerp(BeeKeeperTuning.BeesPerHit, BeeKeeperTuning.MaxBig + BeeKeeperTuning.MaxSmall, progress));
            wanted = Math.Min(Math.Min(wanted, pool.Count), room);
            for (int i = 0; i < wanted; i++)
            {
                int pick = Main.rand.Next(i, pool.Count); // a random bee of the ones not chosen yet
                BeeKeeperBeeProjectile chosen = pool[pick];
                pool[pick] = pool[i];
                pool[i] = chosen;
                chosen.Recruit(target, swordDamage);
            }
        }

        // Owner's client only: a returning bee has reached the player carrying 'honey' (0..1, 1 = a full load). The honey is collected for
        // DeliveryBatchTicks (the bees of one swarm arrive within a moment of each other) and then given at once: HealPerHoneyBee health and
        // HoneyBuffTicksPerBee ticks of the Honey buff per full load, scaled by what each bee carried.
        public void Deliver(float honey)
        {
            if (Player.whoAmI != Main.myPlayer || Player.dead)
                return;
            pendingHealth += BeeKeeperTuning.HealPerHoneyBee * honey;
            pendingHoney += honey;
            deliveryTimer = Math.Max(1, BeeKeeperTuning.DeliveryBatchTicks);
        }

        // Owner only, every tick: when the batch has been quiet for DeliveryBatchTicks, give it. Player.Heal syncs the new health itself and
        // AddBuff is the owner's own buff; the buff is only ever raised (never shortened below what the player already has).
        void TickDelivery()
        {
            if (Player.dead)
            {
                pendingHealth = 0f;
                pendingHoney = 0f;
                deliveryTimer = 0;
                return;
            }
            if (deliveryTimer <= 0)
                return;
            deliveryTimer--;
            if (deliveryTimer > 0)
                return;

            int heal = Math.Min(BeeKeeperTuning.HealMaxPerBatch, (int)Math.Round(pendingHealth));
            heal = Math.Min(heal, Player.statLifeMax2 - Player.statLife);
            if (heal > 0)
                Player.Heal(heal);

            int add = (int)(pendingHoney * BeeKeeperTuning.HoneyBuffTicksPerBee);
            if (add > 0)
            {
                int index = Player.FindBuffIndex(BuffID.Honey);
                int current = index >= 0 ? Player.buffTime[index] : 0;
                Player.AddBuff(BuffID.Honey, Math.Min(BeeKeeperTuning.HoneyBuffMaxTicks, current + add));
            }

            BeeKeeperFx.HoneyHeal(Player.Center, pendingHoney);
            pendingHealth = 0f;
            pendingHoney = 0f;
        }

        // Owner only: one more bee of whichever kind has room, picked at random in proportion to how many of each are missing.
        void SpawnBee()
        {
            int bigType = ModContent.ProjectileType<BeeKeeperBigBeeProjectile>();
            int smallType = ModContent.ProjectileType<BeeKeeperSmallBeeProjectile>();
            int bigMissing = BeeKeeperTuning.MaxBig - Player.ownedProjectileCounts[bigType];
            int smallMissing = BeeKeeperTuning.MaxSmall - Player.ownedProjectileCounts[smallType];
            if (bigMissing <= 0 && smallMissing <= 0)
                return;

            bool big;
            if (bigMissing <= 0)
                big = false;
            else if (smallMissing <= 0)
                big = true;
            else
                big = Main.rand.Next(bigMissing + smallMissing) < bigMissing;

            Vector2 position = Player.Top + new Vector2(Main.rand.NextFloat(-20f, 20f), -10f);
            Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
            // ai[0] = state (0 = idle), ai[1] = hurt counter, ai[2] = the bee's own random seed (its wander pattern).
            Projectile.NewProjectile(Player.GetSource_FromThis(), position, velocity, big ? bigType : smallType, 0, 0f, Player.whoAmI, 0f, 0f, Main.rand.NextFloat(1f, 1000f));
        }
    }
}