using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Buffs
{
    /// <summary>
    /// The Light's Bane's imbue as a real buff (given by the owner's stab: LightsBaneBladeProjectile.StabImpact). The buff only marks and
    /// times the imbue: the run speed boost is applied in LightsBanePlayer.PostUpdateRunSpeeds while the buff exists, and the dust and
    /// the light are LightsBaneImbueProjectile (which ends with the buff). Vanilla syncs the buff list, so every machine sees it.
    /// Icon: Ismail's art goes to Content/Weapons/LightsBane/LightsBaneImbueBuff.png (vanilla buffs are 32x32); until it exists the
    /// vanilla Swiftness icon is used.
    /// </summary>
    public class LightsBaneImbueBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true; // never saved with the character
        }
    }
}
