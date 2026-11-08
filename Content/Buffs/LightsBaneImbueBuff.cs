using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Buffs
{
    /// <summary>
    /// The Light's Bane's imbue as a real buff. The stab (LightsBaneBladeProjectile.StabImpact) only adds it; EVERYTHING else follows the
    /// buff, whoever gave it: the run speed boost (LightsBanePlayer.PostUpdateRunSpeeds), the sword held at full charge until the buff ends
    /// (LightsBanePlayer.PostUpdate), and the dust, light, afterimages and Phase dye look (LightsBaneImbueProjectile, which the owner's
    /// LightsBanePlayer keeps alive while the buff exists). Vanilla syncs the buff list, so every machine sees it.
    /// Icon: Content/Buffs/LightsBaneImbueBuff.png (32x32, like vanilla buff icons).
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
