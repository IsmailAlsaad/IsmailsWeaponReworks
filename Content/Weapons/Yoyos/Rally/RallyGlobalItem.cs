using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Rally
{
    /// <summary>
    /// The Rally rework (vanilla ItemID.Rally): everything shared is in YoyoGlobalItem, this only names the item. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class RallyGlobalItem : YoyoGlobalItem
    {
        protected override int YoyoItemType => ItemID.Rally;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Rally1", "Left click to perform a Forward Pass"));
            //tooltips.Add(new TooltipLine(Mod, "Rally2", "Every hit cracks the yoyo; the third hit breaks its shell into falling shards"));
        }
    }
}