using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Spear
{
    /// <summary>
    /// Overhauls the vanilla Spear: the item itself throws nothing. Left click starts a SpearThrustProjectile (the held spear): a burst of
    /// three thrusts toward the cursor, each a little off the aim, then a short cooldown. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class SpearGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Spear;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<SpearThrustProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the spear
            item.channel = false;
            item.autoReuse = true;                // hold the button to keep thrusting
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        // One burst at a time, and not during the cooldown after it.
        public override bool CanUseItem(Item item, Player player)
        {
            return ThrustPlayer.CanUse(player, ItemID.Spear, ModContent.ProjectileType<SpearThrustProjectile>());
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else. ai[0] = seed (the angle
            // variance every machine derives the same way), ai[1] = 0, ai[2] = the aim in the player's own frame.
            int seed = Main.rand.Next(10000);
            int thrustDamage = Math.Max(1, (int)(damage * SpearTuning.DamageMultiplier));
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, thrustDamage, knockback, player.whoAmI, seed, 0f, HeldAim.Capture(player));
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "Spear1", $"Thrusts {SpearTuning.Thrusts} times in a row toward the cursor"));
        }
    }
}