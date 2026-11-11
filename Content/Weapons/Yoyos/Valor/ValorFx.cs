using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Valor
{
    /// <summary>
    /// The dust and sounds of the Valor (vanilla only). The defaults are in YoyoFx; override a method here for a different feel.
    /// </summary>
    public class ValorFx : YoyoFx
    {
        // Bright chips on a hit and on a block (change ChipDust for another dust).
        protected override int ChipDust => DustID.WhiteTorch;

        // The wind-up starts (YoyoFx.Throw is called when the throw begins): a soft, low swish as the hand goes up.
        public override void Throw(Vector2 position, int side)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.3f, Pitch = -0.3f, PitchVariance = 0.1f }, position);
        }

        // THE WIND-UP: called every tick while the yoyo dangles behind the back and spins up (charge01: 0 = just started, 1 = about to be
        // thrown). A whirring swish that gets quicker and higher, and sparks flung off the spinning yoyo (more and harder as it speeds up).
        public override void Charging(Vector2 position, float charge01)
        {
            if (Main.dedServ)
                return;
            int interval = charge01 < 0.5f ? 8 : charge01 < 0.8f ? 5 : 3;
            if (Main.GameUpdateCount % (uint)interval == 0)
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.15f + 0.2f * charge01, Pitch = -0.3f + 0.9f * charge01, PitchVariance = 0.05f }, position);
            if (Main.rand.NextFloat() < 0.1f + 0.5f * charge01)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * (1.5f + 3.5f * charge01);
                Dust dust = Dust.NewDustPerfect(position, DustID.WhiteTorch, velocity, 0, default, 0.8f + 0.4f * charge01);
                dust.noGravity = true;
            }
        }
    }
}