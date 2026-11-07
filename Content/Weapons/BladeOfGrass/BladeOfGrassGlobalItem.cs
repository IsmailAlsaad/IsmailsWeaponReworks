using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BladeOfGrass
{
    /// <summary>
    /// Overhauls the vanilla Blade of Grass (ItemID.BladeofGrass): the item itself hits and fires nothing. Holding left click starts a
    /// BladeOfGrassHoldProjectile that points the blade at the cursor and grows three whip-like vines from its edge; the vines loop around
    /// enemies near the cursor and the looped sections break off as BladeOfGrassSpineProjectile. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class BladeOfGrassGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.BladeofGrass;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<BladeOfGrassHoldProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile poses the arm itself
            item.noMelee = true;                  // the projectiles are the weapon
            item.noUseGraphic = true;             // the held projectile draws the sword
            item.channel = true;                  // held while the button is down
            item.autoReuse = false;
            item.UseSound = null;                 // the projectiles play their own sounds
        }

        // One held blade at a time. (A manual scan instead of ownedProjectileCounts, which lags a tick behind a projectile that was just killed.)
        public override bool CanUseItem(Item item, Player player)
        {
            int type = ModContent.ProjectileType<BladeOfGrassHoldProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == player.whoAmI && projectile.type == type)
                    return false;
            }
            return true;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            // ai[0] = the aim in the player's own frame (the first sync; the owner then keeps it up to date through SendExtraAI).
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, HeldAim.Capture(player));
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "BladeOfGrass1", "Hold to grow vines toward the cursor, the longer you hold the farther they reach"));
            tooltips.Add(new TooltipLine(Mod, "BladeOfGrass2", "Vines loop around enemies near the cursor and break off as thorny coils that keep hurting them"));
        }
    }
}