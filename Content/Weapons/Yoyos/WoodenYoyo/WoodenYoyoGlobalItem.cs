using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.WoodenYoyo
{
    /// <summary>
    /// The WoodenYoyo rework (vanilla ItemID.WoodYoyo): everything shared is in YoyoGlobalItem, this only names the item. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class WoodenYoyoGlobalItem : YoyoGlobalItem
    {
        protected override int YoyoItemType => ItemID.WoodYoyo;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "WoodenYoyo1", "Hold left click to Walk the Dog"));
        }
    }
}