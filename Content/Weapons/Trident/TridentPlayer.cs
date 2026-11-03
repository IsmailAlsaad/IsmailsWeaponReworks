using IsmailsWeaponReworks.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Trident
{
    /// <summary>
    /// Holding the Trident with the head under water grants Gills (breathing) and Flipper (swimming), refreshed every tick so the buffs
    /// vanish a moment after the player surfaces or swaps items. Runs on every machine for every player (the head check reads the
    /// synced position, and Player.AddBuff on all machines keeps the buff lists in agreement without a packet). A longer potion
    /// buff the player already has is left alone.
    /// </summary>
    public class TridentPlayer : ModPlayer
    {
        public override void PostUpdateEquips()
        {
            if (Player.HeldItem.type != ItemID.Trident)
                return;
            if (!LiquidState.HeadUnder(Player) || Player.lavaWet || Player.honeyWet)
                return;
            Player.AddBuff(BuffID.Gills, TridentTuning.BuffRefreshTicks);
            Player.AddBuff(BuffID.Flipper, TridentTuning.BuffRefreshTicks);
        }
    }
}