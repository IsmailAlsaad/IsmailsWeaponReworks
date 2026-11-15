using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace IsmailsWeaponReworks.Common.Rework
{
    /// <summary>
    /// "Reworks toggle config": the list of reworks the player has switched OFF. An empty list means every rework is on, so a new rework
    /// is on by default and an item that is renamed or removed never breaks anything. Server-side: the server's list is synced to every
    /// client, so all machines agree on which reworks exist. A change takes effect after a reload (ReloadRequired), because which
    /// GlobalItems apply to an item type is worked out once while the mod loads.
    /// </summary>
    public class ReworksToggleConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [ReloadRequired]
        [CustomModConfigItem(typeof(ReworkGridElement))]
        public List<ItemDefinition> DisabledReworks = new List<ItemDefinition>();
    }
}