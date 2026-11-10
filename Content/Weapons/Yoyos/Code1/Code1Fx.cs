using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Code1
{
    /// <summary>
    /// The dust and sounds of the Code1 (vanilla only). The defaults are in YoyoFx; override a method here for a different feel.
    /// </summary>
    public class Code1Fx : YoyoFx
    {
        // Grey puffs of smoke on a hit (change ChipDust for another dust).
        protected override int ChipDust => DustID.BlueTorch;

        // A higher, snappier swish than the wooden yoyo's.
        public override void Throw(Vector2 position, int side)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.55f, PitchVariance = 0.1f }, position);
        }
    }
}