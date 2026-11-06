using Microsoft.Xna.Framework;
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
    /// </summary>
    public class BeeKeeperPlayer : ModPlayer
    {
        public int LastPhase = -1;       // BeeKeeperSwingProjectile.Phase of the previous swing, -1 = none yet
        public float LastEndArm;         // where the arm was when the previous swing ended (relative to its aim)
        public uint LastEndTick;         // Main.GameUpdateCount when it ended

        public int Linger;               // ticks the swarm stays after the sword was last held (owner only)
        int spawnTimer;

        public override void OnRespawn()
        {
            Linger = 0;
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