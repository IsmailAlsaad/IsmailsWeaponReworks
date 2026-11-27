using IsmailsWeaponReworks.Common.Rework;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Keeps the idle sword alive: whenever the owner holds an item that one of the HeldIdleSwordProjectile classes belongs to (and its rework
    /// is on) and that idle sword does not exist, the owner's client spawns it. The projectile removes itself when another item is held.
    /// A new weapon only needs its own HeldIdleSwordProjectile subclass; nothing is registered anywhere.
    /// </summary>
    public class HeldIdleSwordPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Player.dead || Player.noItems || Player.CCed)
                return;
            Item item = Player.HeldItem;
            if (item == null || item.IsAir)
                return;

            foreach (HeldIdleSwordProjectile idle in ModContent.GetContent<HeldIdleSwordProjectile>())
            {
                if (!idle.Holds(item.type) || !ReworkToggle.Holds(Player, item.type))
                    continue;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == idle.Type)
                        return;
                }
                Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero, idle.Type, 0, 0f, Player.whoAmI);
                return;
            }
        }
    }
}