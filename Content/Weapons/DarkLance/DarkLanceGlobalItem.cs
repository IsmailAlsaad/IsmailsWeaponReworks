using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// Overhauls the vanilla Dark Lance. Patch 6: right click (hold) raises the lance and charges dark flame orbs (a
    /// DarkLanceChargeProjectile). The left click is still the vanilla lance until patch 7 (sweep and thrust). Recipe, drops and base
    /// stats stay vanilla.
    /// </summary>
    public class DarkLanceGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.DarkLance;
        }

        public override void SetDefaults(Item item)
        {
            item.noMelee = true;       // the projectiles are the weapon
            item.noUseGraphic = true;  // the projectiles draw the lance
        }

        public override bool AltFunctionUse(Item item, Player player)
        {
            return true; // right click charges the orbs
        }

        // One charge at a time, and not right after it. The right click plays no use sound (the charge plays its own).
        public override bool CanUseItem(Item item, Player player)
        {
            if (!ThrustPlayer.CanUse(player, ItemID.DarkLance, ModContent.ProjectileType<DarkLanceChargeProjectile>()))
                return false;
            item.UseSound = player.altFunctionUse == 2 ? null : ContentSamples.ItemsByType[ItemID.DarkLance].UseSound;
            return true;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            // Right click: ai[0] = 0, ai[1] = 0 (the owner writes the end age + 1 here when the button is released). Left click: vanilla.
            if (player.altFunctionUse == 2)
            {
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<DarkLanceChargeProjectile>(), 0, 0f, player.whoAmI);
                return false;
            }
            return true;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "DarkLance1", "Hold right click to accumulate dark flames"));
        }
    }
}