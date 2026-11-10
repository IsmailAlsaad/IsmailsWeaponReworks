using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The dust and sounds of the Artery (vanilla only). The defaults are in YoyoFx; override a method here for a different feel.
    /// </summary>
    public class ArteryFx : YoyoFx
    {
        // Blood on a hit (change ChipDust for another dust).
        protected override int ChipDust => DustID.Blood;
    }
}