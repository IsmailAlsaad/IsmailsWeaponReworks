using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Cascade
{
    /// <summary>
    /// The dust and sounds of the Cascade (vanilla only): fire. The defaults are in YoyoFx; override a method here for a different feel.
    /// </summary>
    public class CascadeFx : YoyoFx
    {
        // Hot chips on a hit and on a block (change ChipDust for another dust).
        protected override int ChipDust => DustID.Torch;

        // How many chips fly off per enemy hit.
        protected virtual int ChipsPerHit => 3;

        public override void Hit(Vector2 position, int hitNumber)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < ChipsPerHit; i++)
                Chip(position, Main.rand.NextVector2Circular(3.5f, 3.5f), Main.rand.NextFloat(0.8f, 1.2f));
        }

        // The throw starts (the wind-up of Pop the Clutch): a soft, low swish as the hand goes up.
        public override void Throw(Vector2 position, int side)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.3f, Pitch = -0.3f, PitchVariance = 0.1f }, position);
        }

        // Called every tick while the yoyo spins up: the wind-up of Pop the Clutch AND the Sleeper hold (charge01: 0 to 1). A whirring swish
        // that gets quicker and higher, and fire flung off the spinning yoyo (more and harder as it speeds up).
        public override void Charging(Vector2 position, float charge01)
        {
            if (Main.dedServ)
                return;
            int interval = charge01 < 0.5f ? 8 : charge01 < 0.8f ? 5 : 3;
            if (Main.GameUpdateCount % (uint)interval == 0)
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.08f + 0.2f * charge01, Pitch = -0.3f + 0.9f * charge01, PitchVariance = 0.05f }, position);
            if (Main.rand.NextFloat() < 0.15f + 0.5f * charge01)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * (1.5f + 3.5f * charge01);
                Dust dust = Dust.NewDustPerfect(position, DustID.Torch, velocity, 0, default, 0.9f + 0.6f * charge01);
                dust.noGravity = true;
            }
        }

        // Max speed reached: a chime, a whoosh of flame and a burst of fire.
        public override void ChargeReady(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = -0.1f }, position);
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, Pitch = 0.1f }, position);
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.Torch, Main.rand.NextVector2Circular(5f, 5f), 100, default, Main.rand.NextFloat(1.1f, 1.7f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 6; i++)
                Dust.NewDustPerfect(position, DustID.Smoke, Main.rand.NextVector2Circular(2.5f, 2.5f), 150, default, Main.rand.NextFloat(0.9f, 1.3f));
        }

        // A charged yoyo (waiting or flying): an ember rises off it now and then, sometimes with a puff of smoke.
        public void ChargedEmber(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), DustID.Torch,
                new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.6f, 1.8f)), 100, default, Main.rand.NextFloat(1f, 1.7f));
            dust.noGravity = true;
            if (Main.rand.NextBool(4))
                Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), DustID.Smoke, new Vector2(0f, -Main.rand.NextFloat(0.4f, 1f)), 150, default, Main.rand.NextFloat(0.7f, 1f));
        }
    }
}
