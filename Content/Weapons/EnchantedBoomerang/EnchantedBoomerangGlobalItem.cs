using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedBoomerang
{
    /// <summary>
    /// Overhauls the vanilla Enchanted Boomerang. Left click throws the normal boomerang, right click throws the ethereal one; one of
    /// each can be out at the same time, and when they hit each other they explode in stardust and bounce apart. Recipe, drops and
    /// base stats stay vanilla.
    /// </summary>
    public class EnchantedBoomerangGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.EnchantedBoomerang;
        }

        public override void SetDefaults(Item item)
        {
            item.useTime = 12;
            item.useAnimation = 12;
            item.shoot = ModContent.ProjectileType<EnchantedBoomerangProjectile>();
            item.shootSpeed = EnchantedBoomerangTuning.ThrowSpeed;
            item.autoReuse = true;
        }

        public override bool AltFunctionUse(Item item, Player player)
        {
            return true; // right click is the ethereal throw
        }

        // One of each kind at a time; the two kinds do not block each other.
        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ProjectileTypeFor(player)] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the throwing player's own client; the projectile is then synced to everyone else.
            Projectile.NewProjectile(source, position, velocity, ProjectileTypeFor(player), damage, knockback, player.whoAmI, 0f, EnchantedBoomerangTuning.ThrowRange);
            return false;
        }

        static int ProjectileTypeFor(Player player)
        {
            return player.altFunctionUse == 2 ? ModContent.ProjectileType<EtherealBoomerangProjectile>() : ModContent.ProjectileType<EnchantedBoomerangProjectile>();
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "EnchantedBoomerang1", "Right click to throw an ethereal boomerang"));
            //tooltips.Add(new TooltipLine(Mod, "EnchantedBoomerang2", "When the two boomerangs collide they burst into stardust and bounce apart"));
        }
    }
}