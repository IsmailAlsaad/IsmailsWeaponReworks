using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// Keeps the Muramasa's stance alive on the owner's machine: whenever the sword is held and neither a stance nor a swing exists,
    /// it starts a MuramasaStanceProjectile (the sheathing animation). The swing hands over to a new stance itself when it ends.
    /// Nothing here is synced: the stance is a normal spawned projectile.
    /// </summary>
    public class MuramasaPlayer : ModPlayer
    {
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

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (Player.dead || Player.noItems || Player.CCed || Player.HeldItem.type != ItemID.Muramasa)
                return;
            if (Find(Player.whoAmI, ModContent.ProjectileType<MuramasaStanceProjectile>()) != null)
                return;
            if (Find(Player.whoAmI, ModContent.ProjectileType<MuramasaSwingProjectile>()) != null)
                return;
            if (Find(Player.whoAmI, ModContent.ProjectileType<MuramasaIaidoProjectile>()) != null)
                return;
            MuramasaStanceProjectile.Spawn(Player.GetSource_ItemUse(Player.HeldItem), Player,
                MathHelper.ToRadians(MuramasaTuning.ReadyArmDeg), MathHelper.ToRadians(MuramasaTuning.ReadyTiltDeg));
        }
    }
}