using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Malaise
{
    /// <summary>
    /// The Malaise rework (vanilla ItemID.CorruptYoyo): everything shared is in YoyoGlobalItem, this only names the item. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class MalaiseGlobalItem : YoyoGlobalItem
    {
        protected override int YoyoItemType => ItemID.CorruptYoyo;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Malaise1", "Left click to perform Rock the Baby"));
        }
    }
}