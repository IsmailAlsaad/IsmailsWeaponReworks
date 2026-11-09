using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// Overhauls the vanilla Enchanted Sword (ItemID.EnchantedSword): the item itself swings and shoots nothing, so the vanilla swing and the
    /// vanilla sword beam are gone. While it is held, an EnchantedSwordBladeProjectile (spawned by EnchantedSwordPlayer) holds the sword for as
    /// long as it is in the hand and swings it while the left button is down: up, down, thrust, up, down, thrust ... (the swings flow into each
    /// other, because it is ONE projectile and never ends between two swings). Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class EnchantedSwordGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.EnchantedSword;
        }

        public override void SetDefaults(Item item)
        {
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the sword
            item.channel = false;
            item.UseSound = null;                 // the projectile plays its own sounds
            item.shoot = ProjectileID.None;       // the vanilla sword beam is gone
            item.shootSpeed = 0f;
        }

        // The item is never "used": the held blade watches the button itself.
        public override bool CanUseItem(Item item, Player player)
        {
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.RemoveAll(line => line.Mod == "Terraria" && line.Name.StartsWith("Tooltip"));
            tooltips.Add(new TooltipLine(Mod, "EnchantedSword0", "Right click at full charge to shoot an enchanted sword"));
        }
    }
}