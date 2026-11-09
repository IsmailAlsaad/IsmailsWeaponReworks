using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// Keeps the held yoyo alive: whenever a reworked yoyo is held and the player has no YoyoProjectile, the owner's client spawns one
    /// (the projectile ends itself when the yoyo is swapped away, and is then spawned again for the next yoyo). Nothing here is synced:
    /// the projectile itself travels to the other machines.
    /// </summary>
    public class YoyoPlayer : ModPlayer
    {
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
            if (Player.whoAmI != Main.myPlayer || Player.dead || Player.noItems || Player.CCed)
                return;
            Item item = Player.HeldItem;
            if (!YoyoTuning.IsReworked(item.type))
                return;
            int type = ModContent.ProjectileType<YoyoProjectile>();
            if (Find(Player.whoAmI, type) != null)
                return;
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero, type,
                Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack), Player.whoAmI);
        }
    }
}