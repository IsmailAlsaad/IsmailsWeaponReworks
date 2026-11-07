using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Katana
{
    /// <summary>
    /// The Katana's cooldown (owner's machine only: it only decides whether a new throw may start) and the upkeep of its stance: whenever
    /// the sword is held and neither a stance nor a flying blade exists, it starts a KatanaStanceProjectile (the sheathed pose).
    /// Nothing here is synced.
    /// </summary>
    public class KatanaPlayer : ModPlayer
    {
        public int CooldownTicks;

        public bool Ready => CooldownTicks <= 0;

        // The first active projectile of this type owned by this player, or null. (A manual scan instead of ownedProjectileCounts, which
        // lags a tick behind a projectile that was spawned or killed this tick.)
        public static Projectile Find(int owner, int type)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == owner && projectile.type == type)
                    return projectile;
            }
            return null;
        }

        // A thrown blade that has not been released yet (ai[2] stays negative until the release).
        public static bool BladeFlying(int owner)
        {
            int type = ModContent.ProjectileType<KatanaBladeProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == owner && projectile.type == type && projectile.ai[2] < 0f)
                    return true;
            }
            return false;
        }

        // A new throw ends the blades of the last one that are still stuck in a tile or falling (the owner's client calls this).
        public static void KillReleasedBlades(int owner)
        {
            int type = ModContent.ProjectileType<KatanaBladeProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == owner && projectile.type == type && projectile.ai[2] >= 0f)
                    projectile.Kill();
            }
        }



        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (Player.dead)
                CooldownTicks = 0;
            else if (CooldownTicks > 0)
                CooldownTicks--;

            if (Player.dead || Player.noItems || Player.CCed || Player.HeldItem.type != ItemID.Katana)
                return;
            if (Find(Player.whoAmI, ModContent.ProjectileType<KatanaStanceProjectile>()) != null)
                return;
            if (BladeFlying(Player.whoAmI))
                return;
            KatanaStanceProjectile.Spawn(Player.GetSource_ItemUse(Player.HeldItem), Player,
                MathHelper.ToRadians(KatanaTuning.ReadyArmDeg), MathHelper.ToRadians(KatanaTuning.ReadyTiltDeg), CooldownTicks);
        }
    }
}