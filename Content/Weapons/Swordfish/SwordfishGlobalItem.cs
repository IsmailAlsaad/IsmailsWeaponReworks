using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Swordfish
{
    /// <summary>
    /// Overhauls the vanilla Swordfish: the item itself throws nothing. Left click starts a SwordfishThrustProjectile: a burst of five quick
    /// thrusts toward the cursor, or, when the player is in liquid, an endless enraged chain of thrusts with no cooldown for as long as the
    /// button is held. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class SwordfishGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Swordfish;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<SwordfishThrustProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the spear
            item.channel = false;
            item.autoReuse = true;                // hold the button to keep thrusting
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        public override bool CanUseItem(Item item, Player player)
        {
            return ThrustPlayer.CanUse(player, ItemID.Swordfish, ModContent.ProjectileType<SwordfishThrustProjectile>());
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client. The rage is decided HERE (in liquid or not) and travels in the spawn
            // packet: ai[0] = seed + 10000 * mode (1 = enraged), ai[1] = 0, ai[2] = the aim in the player's own frame.
            bool rage = LiquidState.Touching(player);
            int seed = Main.rand.Next(10000);
            float multiplier = rage ? SwordfishTuning.RageDamageMultiplier : SwordfishTuning.DamageMultiplier;
            int thrustDamage = Math.Max(1, (int)(damage * multiplier));
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, thrustDamage, knockback, player.whoAmI, seed + (rage ? 10000f : 0f), 0f, HeldAim.Capture(player));
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Swordfish1", $"Thrusts {SwordfishTuning.Thrusts} times in a row toward the cursor"));
            tooltips.Add(new TooltipLine(Mod, "Swordfish2", "Enraged in liquid: thrusts without stopping"));
        }
    }
}