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

        public void Add(int itemType)
        {
            ItemTypes.Add(itemType);
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