using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// Overhauls the vanilla Light's Bane (ItemID.LightsBane): the item itself swings nothing. While it is held, a LightsBaneBladeProjectile
    /// (spawned by LightsBanePlayer) holds the sword for as long as it is in the hand and swings it while the left button is down (the
    /// swings flow into each other, because it is ONE projectile and never ends between two swings). Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class LightsBaneGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.LightsBane;
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
            tooltips.Add(new TooltipLine(Mod, "Light'sBane1", "Right click at max speed to stab yourself"));
        }
    }
}