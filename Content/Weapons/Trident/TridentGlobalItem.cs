using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Trident
{
    /// <summary>
    /// Overhauls the vanilla Trident: the item itself throws nothing. Left click starts a TridentThrustProjectile: one strong thrust that
    /// dashes the player toward the cursor and bounces them off the first enemy or tile they hit. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class TridentGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Trident;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<TridentThrustProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the spear
            item.channel = false;
            item.autoReuse = true;                // hold the button to dash again as soon as the cooldown ends
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        public override bool CanUseItem(Item item, Player player)
        {
            return ThrustPlayer.CanUse(player, ItemID.Trident, ModContent.ProjectileType<TridentThrustProjectile>());
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client. Whether the player is in liquid is decided HERE (it picks the longer
            // dash) and travels in the spawn packet: ai[0] = seed + 10000 * mode (1 = liquid), ai[1] = 0, ai[2] = the aim in the player's frame.
            bool liquid = LiquidState.Touching(player);
            int seed = Main.rand.Next(10000);
            int thrustDamage = Math.Max(1, (int)(damage * TridentTuning.DamageMultiplier));
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, thrustDamage, knockback, player.whoAmI, seed + (liquid ? 10000f : 0f), 0f, HeldAim.Capture(player));
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Trident1", "Dashes toward the cursor and bounces off what it hits"));
            tooltips.Add(new TooltipLine(Mod, "Trident2", "Grants Gills and Flipper while your head is under water"));
        }
    }
}