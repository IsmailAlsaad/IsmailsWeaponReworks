using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Shroomerang
{
    /// <summary>
    /// Overhauls the vanilla Shroomerang: it throws ShroomerangProjectile, which flies and returns like a regular boomerang, pierces
    /// forever and drops glowing mushrooms that explode into spores. Only one can be in the air. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class ShroomerangGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Shroomerang;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<ShroomerangProjectile>();
            item.shootSpeed = ShroomerangTuning.ThrowSpeed;
            item.autoReuse = true;
        }

        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<ShroomerangProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the throwing player's own client; the projectile is then synced to everyone else.
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, ShroomerangTuning.ThrowRange);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "Shroomerang1", "Drops glowing mushrooms while it flies"));
            //tooltips.Add(new TooltipLine(Mod, "Shroomerang2", "Mushrooms explode into spores after a while, when an enemy touches them, or when more than 8 are out"));
        }
    }
}