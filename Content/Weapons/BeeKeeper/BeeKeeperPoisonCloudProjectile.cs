using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// The poison cloud a dying bee leaves: a round area that lingers CloudTicks and keeps (re)applying the Poisoned buff to everything inside
    /// it every CloudPulseTicks: enemies AND players, the owner too. It deals no damage itself. ai[0] = 1 for a big bee's cloud, 0 for a small one's.
    /// MULTIPLAYER: the owner's client spawns it (so tModLoader syncs it) and poisons the enemies inside (NPC.AddBuff syncs). Every machine
    /// poisons its OWN local player when that player stands inside, because a player's buffs belong to that player's client. Every machine
    /// plays the same puffs and sound itself. No custom packets.
    /// </summary>
    public class BeeKeeperPoisonCloudProjectile : ModProjectile
    {
        // Never drawn (the gas is the metaball layer): any vanilla projectile texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Fireball;

        bool IsBig => Projectile.ai[0] > 0.5f;
        float Radius => BeeKeeperTuning.CloudRadius * (IsBig ? 1f : BeeKeeperTuning.CloudSmallScale);
        bool IsOwner => Projectile.owner == Main.myPlayer;

        int ticks;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = BeeKeeperTuning.CloudTicks;
        }

        public override void AI()
        {
            float radius = Radius;
            Vector2 center = Projectile.Center;

            if (ticks == 0)
            {
                BeeKeeperFx.CloudBurst(center, radius);
                BeeKeeperFx.PlayCloud(center, IsBig);
            }
            if (Projectile.timeLeft > 30)
                BeeKeeperFx.CloudPuffs(center, radius, ticks);
            Lighting.AddLight(center, 0.2f, 0.35f, 0.08f);

            if (ticks % System.Math.Max(1, BeeKeeperTuning.CloudPulseTicks) == 0)
            {
                if (IsOwner)
                    AreaEffect.ApplyBuff(center, radius, BuffID.Poisoned, BeeKeeperTuning.PoisonTicks);

                if (!Main.dedServ)
                {
                    Player local = Main.LocalPlayer;
                    if (local.active && !local.dead)
                    {
                        float reach = radius + System.Math.Max(local.width, local.height) * 0.5f;
                        if (Vector2.DistanceSquared(local.Center, center) <= reach * reach)
                            local.AddBuff(BuffID.Poisoned, BeeKeeperTuning.PoisonTicks);
                    }
                }
            }
            ticks++;
        }
    }
}