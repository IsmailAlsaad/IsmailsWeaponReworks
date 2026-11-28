using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Buffs
{
    /// <summary>
    /// Grape ("You are now grape flavored"), only in the getfixedboi seed: a green or yellow candy of the Candy Cane Sword gives it for GrapeTicks (10 s,
    /// refreshed, see CandyPickup.Apply). While it runs:
    /// - total immunity to every debuff: this Update marks every debuff in buffImmune each tick (Player.AddBuff refuses those), and
    ///   CandyCaneSwordPlayer.PostUpdateBuffs removes the debuffs the player already has;
    /// - every dye slot of the player is the Purple Dye (CandyCaneSwordPlayer.UpdateDyes).
    /// Everything follows the buff, whoever gave it; vanilla syncs the buff list, so every machine sees it.
    /// Icon: Content/Buffs/GrapeBuff.png (32x32, like vanilla buff icons); the vanilla Endurance icon until that file exists.
    /// </summary>
    public class GrapeBuff : ModBuff
    {
        public override string Texture => ModContent.HasAsset("IsmailsWeaponReworks/Content/Buffs/GrapeBuff")
            ? "IsmailsWeaponReworks/Content/Buffs/GrapeBuff"
            : "Terraria/Images/Buff_" + BuffID.Endurance;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true; // never saved with the character
        }

        public override void Update(Player player, ref int buffIndex)
        {
            for (int i = 0; i < Main.debuff.Length && i < player.buffImmune.Length; i++)
            {
                if (Main.debuff[i])
                    player.buffImmune[i] = true;
            }
        }
    }
}
