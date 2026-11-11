using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Valor
{
    /// <summary>
    /// The Valor rework (vanilla ItemID.Valor): everything shared is in YoyoGlobalItem, this only names the item. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class ValorGlobalItem : YoyoGlobalItem
    {
        protected override int YoyoItemType => ItemID.Valor;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Valor1", "Left click to Pop the Clutch: two quick passes, one up and one down"));
            tooltips.Add(new TooltipLine(Mod, "Valor2", "Right click to perform Around the World"));
        }
    }
}