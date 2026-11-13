using IsmailsWeaponReworks.Content.Weapons.NightsEdge;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Buffs
{
    /// <summary>
    /// The Night's Edge's self-stab as a real buff. The stab (NightsEdgeBladeProjectile.OnStabImpacted) only adds it; EVERYTHING else follows the buff,
    /// whoever gave it: the damage reduction (here), the sword held at full charge until it ends, the droplets (NightsEdgePlayer) and the body flames
    /// (NightsEdgeFx.ArmorLook, every machine). Vanilla syncs the buff list, so every machine sees it.
    /// Icon: Content/Buffs/NightsEdgeShadowArmorBuff.png (32x32, like vanilla buff icons); the vanilla Ironskin icon until that file exists.
    /// </summary>
    public class NightsEdgeShadowArmorBuff : ModBuff
    {
        public override string Texture => ModContent.HasAsset("IsmailsWeaponReworks/Content/Buffs/NightsEdgeShadowArmorBuff")
            ? "IsmailsWeaponReworks/Content/Buffs/NightsEdgeShadowArmorBuff"
            : "Terraria/Images/Buff_" + BuffID.Ironskin;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true; // never saved with the character
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.endurance += NightsEdgeTuning.ShadowArmorReduction;
        }
    }
}