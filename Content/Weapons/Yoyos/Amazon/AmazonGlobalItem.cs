using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// The Amazon rework (vanilla ItemID.Amazon): everything shared is in YoyoGlobalItem, this only names the item. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class AmazonGlobalItem : YoyoGlobalItem
    {
        protected override int YoyoItemType => ItemID.JungleYoyo;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Amazon1", "Left click to perform a Forward Pass"));
            tooltips.Add(new TooltipLine(Mod, "Amazon2", "Every hit makes the yoyo more barbed; the third hit latches barbed vines onto the enemy"));
            tooltips.Add(new TooltipLine(Mod, "Amazon3", "Right click to perform Walk the Dog"));
            tooltips.Add(new TooltipLine(Mod, "Amazon4", "It plants barbed vines in the ground that hold the enemies that touch them"));
        }
    }
}