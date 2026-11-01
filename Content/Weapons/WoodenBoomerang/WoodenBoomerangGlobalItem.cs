using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.WoodenBoomerang
{
    /// <summary>
    /// Overhauls the vanilla Wooden Boomerang: it throws WoodenBoomerangProjectile, which flies and returns like a regular boomerang but
    /// breaks in half on the first enemy it hits, leaving two shards that linger and hurt. Only one boomerang can be in the air; the shards
    /// do not block the next throw. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class WoodenBoomerangGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.WoodenBoomerang;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<WoodenBoomerangProjectile>();
            item.shootSpeed = WoodenBoomerangTuning.ThrowSpeed;
            item.autoReuse = true;
        }

        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<WoodenBoomerangProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the throwing player's own client; the projectile is then synced to everyone else.
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, WoodenBoomerangTuning.ThrowRange);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "WoodenBoomerang1", "Breaks in half when it hits an enemy; the two halves linger and keep hurting"));
        }
    }
}