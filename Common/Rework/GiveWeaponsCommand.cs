using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common.Rework
{
    /// <summary>
    /// TEMPORARY test command: /giveweapons gives the player one of every item this mod reworks.
    /// The list is ReworkRegistry.ItemTypes (filled by ReworkToggle.Applies), so new reworks show up here with no edit.
    /// Delete this file to remove the command.
    /// Multiplayer: a Chat command runs on the player's own client; QuickSpawnItem syncs the spawned items.
    /// </summary>
    public class GiveWeaponsCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "giveweapons";

        public override string Description => "Gives you one of every weapon reworked by Ismail's Weapon Reworks (testing only)";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Player player = caller.Player;
            ReworkRegistry registry = ModContent.GetInstance<ReworkRegistry>();
            if (player == null || registry == null || registry.ItemTypes.Count == 0)
            {
                caller.Reply("No reworked items are registered.", Microsoft.Xna.Framework.Color.OrangeRed);
                return;
            }

            // Sorted by folder (class / progression / subclass) then item id, so the pile is in a stable order.
            List<int> types = registry.ItemTypes
                .OrderBy(type => registry.Folders.TryGetValue(type, out string folder) ? folder : "")
                .ThenBy(type => type)
                .ToList();

            var source = player.GetSource_Misc("IsmailsWeaponReworks/giveweapons");
            int off = 0;
            foreach (int type in types)
            {
                player.QuickSpawnItem(source, type, 1);
                if (!ReworkToggle.IsActive(type))
                    off++;
            }

            string note = off > 0 ? $" ({off} switched off in the config, so they are plain vanilla)" : "";
            caller.Reply($"Gave {types.Count} reworked weapons{note}.", Microsoft.Xna.Framework.Color.LightGreen);
        }
    }
}