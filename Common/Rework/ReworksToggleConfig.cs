using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
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

        // tModLoader keeps a copy of the config from when the mod loaded and compares it with the saved one; the default copy shares the
        // list, so make a real copy.
        public override ModConfig Clone()
        {
            ReworksToggleConfig clone = (ReworksToggleConfig)base.Clone();
            clone.DisabledReworks = DisabledReworks == null ? new List<ItemDefinition>() : new List<ItemDefinition>(DisabledReworks);
            return clone;
        }

        // Inside a world the changes may be SAVED (tModLoader would refuse a [ReloadRequired] change there): the running game keeps
        // the switches it loaded with (ReworkRegistry reads the list once, while the mod loads), so nothing changes until a reload.
        // At the main menu the saved list is compared with the one the mod was loaded with, so tModLoader flags the mod as "Reload
        // Required" in the mod list and forces the reload when the Mods screen is left.
        public override bool NeedsReload(ModConfig pendingConfig)
        {
            if (!Main.gameMenu)
                return false;

            ReworksToggleConfig other = pendingConfig as ReworksToggleConfig;
            return other != null && !ReworkRegistry.SameTypes(DisabledReworks, other.DisabledReworks);
        }

        // Multiplayer: a client's request to change the server's list is always refused (the list is edited in singleplayer or before hosting).
        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
        {
            message = NetworkText.FromLiteral("Reworks can only be changed in singleplayer or before hosting.");
            return false;
        }

        // Called after a save made inside a world: the config screen shows its orange notification (see ReworkGridElement).
        public override void OnChanged()
        {
            if (!Main.gameMenu)
            {
                ReworkRegistry registry = ModContent.GetInstance<ReworkRegistry>();
                if (registry != null)
                    registry.SaveNoticeAt = Environment.TickCount64;
            }
        }
    }
}