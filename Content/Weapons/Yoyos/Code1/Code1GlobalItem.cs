using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Code1
{
    /// <summary>
    /// The Code1 rework (vanilla ItemID.Code1): everything shared is in YoyoGlobalItem, this only names the item. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class Code1GlobalItem : YoyoGlobalItem
    {
        protected override int YoyoItemType => ItemID.Code1;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Code11", "Left click to perform a Forward Pass"));
        }
    }
}