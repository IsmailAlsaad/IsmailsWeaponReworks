using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace IsmailsWeaponReworks.Common.Rework
{
    /// <summary>
    /// Knows every reworked item type (filled automatically while the mod loads, by ReworkToggle.Applies, so there is no hand-kept list)
    /// and which of them the config has switched off. The config screen's weapon grid reads ItemTypes.
    /// </summary>
    public class ReworkRegistry : ModSystem
    {
        /// <summary>Every item type some GlobalItem of this mod reworks, whether it is switched on or off.</summary>
        public readonly HashSet<int> ItemTypes = new HashSet<int>();

        private HashSet<int> disabledTypes;

        /// <summary>Where each reworked item lives, as "Class/Progression/Subclass" (for example "Melee/PreHardmode/Flails"); the config grid sorts its sections by it.</summary>
        public readonly Dictionary<int, string> Folders = new Dictionary<int, string>();

        /// <summary>Set (Environment.TickCount64) when the config was saved inside a world, so the config screen can show its orange notification.</summary>
        public long SaveNoticeAt = -1;

        public void Add(int itemType, string folder)
        {
            ItemTypes.Add(itemType);
            if (!string.IsNullOrEmpty(folder))
                Folders[itemType] = folder;
        }

        /// <summary>True when both lists switch off exactly the same items (order and duplicates do not matter).</summary>
        public static bool SameTypes(List<ItemDefinition> a, List<ItemDefinition> b)
        {
            return TypesOf(a).SetEquals(TypesOf(b));
        }

        private static HashSet<int> TypesOf(List<ItemDefinition> list)
        {
            HashSet<int> set = new HashSet<int>();
            if (list != null)
            {
                foreach (ItemDefinition definition in list)
                {
                    if (definition != null && definition.Type > 0)
                        set.Add(definition.Type);
                }
            }
            return set;
        }

        public bool IsDisabled(int itemType)
        {
            if (disabledTypes == null)
            {
                ReworksToggleConfig config = ModContent.GetInstance<ReworksToggleConfig>();
                if (config == null)
                    return false; // not loaded yet: do not remember an empty answer

                HashSet<int> set = new HashSet<int>();
                if (config.DisabledReworks != null)
                {
                    foreach (ItemDefinition definition in config.DisabledReworks)
                    {
                        if (definition != null && definition.Type > 0)
                            set.Add(definition.Type);
                    }
                }
                disabledTypes = set;
            }
            return disabledTypes.Contains(itemType);
        }

        public override void PostSetupContent()
        {
            int off = 0;
            foreach (int type in ItemTypes)
            {
                if (IsDisabled(type))
                    off++;
            }
            Mod.Logger.Info($"Reworks toggle config: {ItemTypes.Count} reworked items registered, {off} switched off.");
        }
    }
}