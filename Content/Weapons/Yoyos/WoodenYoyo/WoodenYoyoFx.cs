using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.WoodenYoyo
{
    /// <summary>
    /// The dust and sounds of the WoodenYoyo (vanilla only). The defaults are in YoyoFx; override a method here for a different feel.
    /// </summary>
    public class WoodenYoyoFx : YoyoFx
    {
        // Wood chips (vanilla wood dust).
        protected override int ChipDust => DustID.WoodFurniture;
    }
}