using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// The Hive-Five rework (vanilla ItemID.HiveFive): everything shared is in YoyoGlobalItem, this only names the item. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class HiveFiveGlobalItem : YoyoGlobalItem
    {
        protected override int YoyoItemType => ItemID.HiveFive;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "HiveFive1", "Left click to perform Around the World"));
        }
    }
}
