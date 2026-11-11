using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The Artery rework (vanilla ItemID.CrimsonYoyo): everything shared is in YoyoGlobalItem, this only names the item. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class ArteryGlobalItem : YoyoGlobalItem
    {
        protected override int YoyoItemType => ItemID.CrimsonYoyo;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Artery1", "Left click to perform Man on Trapeze"));
            //tooltips.Add(new TooltipLine(Mod, "Artery2", "A closed loop shreds the enemy, then explodes in blood and makes enemies bleed"));
        }
    }
}