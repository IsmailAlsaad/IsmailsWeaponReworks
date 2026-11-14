using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// Overhauls the vanilla Night's Edge (ItemID.NightsEdge): the item itself swings nothing. While it is held, a NightsEdgeBladeProjectile
    /// (spawned by NightsEdgePlayer) holds the sword for as long as it is in the hand and swings it while the left button is down (the
    /// swings flow into each other, because it is ONE projectile and never ends between two swings). Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class NightsEdgeGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.NightsEdge;
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
            tooltips.Add(new TooltipLine(Mod, "NightsEdge0", "Right click to stab enemies and charge the blade"));
            tooltips.Add(new TooltipLine(Mod, "NightsEdge1", "Right click again at full charge to stab yourself"));
        }
    }
}