using IsmailsWeaponReworks.Common.Rework;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.OreSwords
{
    /// <summary>
    /// Keeps the held blade alive: whenever one of the eight ore broadswords is held (and its rework is on) and no OreSwordBladeProjectile
    /// exists, the owner's client spawns one. The blade removes itself when another item is held (also another ore sword, which then gets
    /// a fresh blade). Nothing here is synced: the projectile is a normal owner-spawned projectile.
    /// </summary>
    public class OreSwordPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Player.dead || Player.noItems || Player.CCed)
                return;
            Item item = Player.HeldItem;
            if (OreSwordMetals.ColumnOf(item.type) < 0 || !ReworkToggle.Holds(Player, item.type))
                return;

            int type = ModContent.ProjectileType<OreSwordBladeProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == type)
                    return;
            }
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero, type,
                Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack), Player.whoAmI);
        }
    }
}