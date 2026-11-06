using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// Overhauls the vanilla Volcano (ItemID.FieryGreatsword): the item itself swings nothing. While it is held, a VolcanoBladeProjectile
    /// (spawned by VolcanoPlayer) holds the sword: the handle is the sprite, the blade is procedural lava; holding the button swings it.
    /// Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class VolcanoGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.FieryGreatsword;
        }

        public override void SetDefaults(Item item)
        {
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the sword
            item.channel = false;
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        // The item is never "used": the held blade watches the button itself.
        public override bool CanUseItem(Item item, Player player)
        {
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Volcano1", "The blade is lava: every hit heats it up"));
        }
    }
}