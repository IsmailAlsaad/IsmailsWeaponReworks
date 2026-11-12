using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common.Flail
{
    /// <summary>
    /// The shared GlobalItem of every reworked flail: pressing the attack button starts the flail's FlailProjectile, a held flail head that
    /// runs the whole hold-charge rhythm loop. A flail only names its vanilla item and its projectile. Recipe, drops and base stats stay
    /// vanilla. Every flail shows the same two tooltip lines.
    /// </summary>
    public abstract class FlailGlobalItem : GlobalItem
    {
        protected abstract int ItemType { get; }
        protected abstract int ProjectileType { get; }

        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemType;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ProjectileType;
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
            return player.ownedProjectileCounts[ProjectileType] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, FlailProjectile.Encode(FlailProjectile.Phase.Charge, 1));
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Flail1", "Catch the flail as it returns to speed up"));
            //tooltips.Add(new TooltipLine(Mod, "Flail2", "Three perfect rounds in a row unleash a special"));
        }
    }
}