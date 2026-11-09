using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// Overhauls the vanilla yoyos (the ones YoyoTuning.IsReworked lists; the Wooden Yoyo first): the item itself throws nothing. While it
    /// is held, a YoyoProjectile (spawned by YoyoPlayer) is the yoyo: it idles in the hand, and the left click runs the yoyo's trick on its
    /// rope. Recipe, drops and base stats stay vanilla (item.shoot stays the vanilla yoyo projectile: its range sets the rope length).
    /// </summary>
    public class YoyoGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return YoyoTuning.IsReworked(entity.type);
        }

        public override void SetDefaults(Item item)
        {
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the yoyo projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the yoyo
            item.channel = false;
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        // The item is never "used": the yoyo watches the button itself.
        public override bool CanUseItem(Item item, Player player)
        {
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "Yoyo1", "Hold left click to Walk the Dog: the yoyo rolls along the ground and hits 3 times"));
        }
    }
}