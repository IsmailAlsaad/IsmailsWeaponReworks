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
    /// </summary>
    public class BeeKeeperPlayer : ModPlayer
    {
        public int LastPhase = -1;       // BeeKeeperSwingProjectile.Phase of the previous swing, -1 = none yet
        public float LastEndArm;         // where the arm was when the previous swing ended (relative to its aim)
        public uint LastEndTick;         // Main.GameUpdateCount when it ended

        public int Linger;               // ticks the swarm stays after the sword was last held (owner only)
        int spawnTimer;

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
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

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
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.ModProjectile is BeeKeeperBeeProjectile bee && bee.IsIdle)
                    pool.Add(bee);
            }
            if (pool.Count == 0)
                return;

            int full = Math.Max(1, BeeKeeperTuning.HitsForFullSwarm);
            float progress = full <= 1 ? 1f : (Math.Min(record.Count, full) - 1) / (float)(full - 1);
            int wanted = (int)Math.Ceiling(MathHelper.Lerp(BeeKeeperTuning.BeesPerHit, BeeKeeperTuning.MaxBig + BeeKeeperTuning.MaxSmall, progress));
            wanted = Math.Min(wanted, pool.Count);
            for (int i = 0; i < wanted; i++)
            {
                int pick = Main.rand.Next(i, pool.Count); // a random bee of the ones not chosen yet
                BeeKeeperBeeProjectile chosen = pool[pick];
                pool[pick] = pool[i];
                pool[i] = chosen;
                chosen.Recruit(target, swordDamage);
            }
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