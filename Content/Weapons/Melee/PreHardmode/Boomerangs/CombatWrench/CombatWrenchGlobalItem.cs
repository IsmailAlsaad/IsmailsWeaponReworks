using IsmailsWeaponReworks.Common.Rework;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Boomerangs.CombatWrench
{
    /// <summary>
    /// Overhauls the vanilla Combat Wrench: left click throws CombatWrenchProjectile along the Forward Pass ellipse toward the cursor
    /// on a rope, only one in flight. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class CombatWrenchGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return ReworkToggle.Applies(entity.type, entity.type == ItemID.CombatWrench);
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<CombatWrenchProjectile>();
            item.shootSpeed = 1f;
            item.autoReuse = true;
            item.noMelee = true;                  // the thrown wrench is the weapon
            item.noUseGraphic = true;             // the projectile draws the wrench
            item.useStyle = ItemUseStyleID.Shoot; // the projectile moves the arm itself
        }

        // Only one wrench in flight; it counts as in flight until it is caught.
        public override bool CanUseItem(Item item, Player player)
        {
            // (the heavy swing counts too: no new throw until it is over)
            return player.ownedProjectileCounts[ModContent.ProjectileType<CombatWrenchProjectile>()] <= 0
                && player.ownedProjectileCounts[ModContent.ProjectileType<CombatWrenchSwingProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the throwing player's own client. The aim angle (world, toward the cursor) travels in ai[2] of the
            // spawn packet; every machine builds the same ellipse from it.
            Vector2 toCursor = Main.MouseWorld - player.MountedCenter;
            float angle = toCursor.LengthSquared() > 1f ? toCursor.ToRotation() : (player.direction >= 0 ? 0f : MathHelper.Pi);
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, 0f, 0f, angle);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.RemoveAll(line => line.Mod == "Terraria" && line.Name.StartsWith("Tooltip"));
            tooltips.Add(new TooltipLine(Mod, "CombatWrench1", "'For reeling things, and bonking them'"));
            //tooltips.Add(new TooltipLine(Mod, "CombatWrench2", "Snaps onto the first enemy it touches and reels it in, then flings it away with a heavy swing"));
            //tooltips.Add(new TooltipLine(Mod, "CombatWrench3", "Bosses and immovable enemies are not dragged, but still take the hit"));
        }
    }
}