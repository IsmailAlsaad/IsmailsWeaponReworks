using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Cascade
{
    /// <summary>
    /// The Cascade rework (vanilla ItemID.Cascade): everything shared is in YoyoGlobalItem, this only names the item. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class CascadeGlobalItem : YoyoGlobalItem
    {
        protected override int YoyoItemType => ItemID.Cascade;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Cascade1", "Hold left click to Pop the Clutch"));
            tooltips.Add(new TooltipLine(Mod, "Cascade2", "Hold right click to charge the yoyo with a Sleeper hold"));
            //tooltips.Add(new TooltipLine(Mod, "Cascade3", "A charged Pop the Clutch sets enemies on fire with tiny explosions"));
        }
    }
}
