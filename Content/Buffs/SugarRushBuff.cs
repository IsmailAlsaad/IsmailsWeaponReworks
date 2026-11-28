using IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Buffs
{
    /// <summary>
    /// Sugar Rush: +30% melee attack speed and +15% movement speed, given by picking up a yellow candy of the Candy Cane Sword (CandyPickup adds 2 s per candy, max 10 s). The speed
    /// follows the buff, whoever gave it; vanilla syncs the buff list, so every machine sees it.
    /// Icon: Content/Buffs/SugarRushBuff.png (32x32, like vanilla buff icons); the vanilla Swiftness icon until that file exists.
    /// </summary>
    public class SugarRushBuff : ModBuff
    {
        public override string Texture => ModContent.HasAsset("IsmailsWeaponReworks/Content/Buffs/SugarRushBuff")
            ? "IsmailsWeaponReworks/Content/Buffs/SugarRushBuff"
            : "Terraria/Images/Buff_" + BuffID.Swiftness;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true; // never saved with the character
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetAttackSpeed(DamageClass.Melee) += CandyCaneSwordTuning.SugarRushAttackSpeed;
            player.moveSpeed += CandyCaneSwordTuning.SugarRushMoveSpeed;
        }
    }
}