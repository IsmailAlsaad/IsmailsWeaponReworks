using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Trimarang
{
    /// <summary>
    /// Overhauls the vanilla Trimarang: hold left click and the boomerang flies out, slows to a stop 20 tiles away and spins there,
    /// shredding everything, until the button is released. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class TrimarangGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Trimarang;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<TrimarangProjectile>();
            item.shootSpeed = TrimarangTuning.ThrowSpeed;
            item.autoReuse = false; // one throw per click: holding is what keeps it out
        }

        // One at a time.
        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<TrimarangProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the throwing player's own client; the projectile is then synced to everyone else.
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, TrimarangTuning.HoldRange);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "Trimarang1", "Hold to keep the boomerang spinning 20 tiles away; it returns when you let go"));
            //tooltips.Add(new TooltipLine(Mod, "Trimarang2", "Every 5th hit sends out an ethereal boomerang that hunts other enemies"));
        }
    }
}