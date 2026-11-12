using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>
    /// Overhauls the vanilla Sunfury: pressing the attack button starts SunfuryFlailProjectile, a held flail head that runs the whole
    /// hold-charge rhythm loop (see that class). Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class SunfuryGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Sunfury;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<SunfuryFlailProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;
            item.channel = true;
            item.autoReuse = false;
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        // One flail head at a time: the rhythm loop lives inside that one projectile.
        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<SunfuryFlailProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, SunfuryFlailProjectile.Encode(SunfuryFlailProjectile.Phase.Charge, 1));
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Sunfury1", "Re-engage while the flail head is flying to speed up"));
            //tooltips.Add(new TooltipLine(Mod, "Sunfury2", "Three perfect rounds in a row unleash a sun-like explosion"));
        }
    }
}