using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// The dust and sounds of the Hive-Five (vanilla only). The defaults are in YoyoFx; override a method here for a different feel.
    /// </summary>
    public class HiveFiveFx : YoyoFx
    {
        // Honey chips on a hit.
        protected override int ChipDust => DustID.Honey;

        // THE SOUND OF THE SPIN: played every half turn of Around the World (AroundWhooshPerTurn in HiveFiveTuning sets how often);
        // speed01 is 0 when slow and 1 at full speed. Change the SoundID, the volume or the pitch here (the pitch rises with the speed).
        // The default for every yoyo is YoyoFx.SpinWhoosh; the throw, the hit, the retract and the catch have their own overrides
        // (Throw, Hit, Retract, Catch) that can be added to this class the same way.
        public override void SpinWhoosh(Vector2 position, float speed01)
        {
            if (Main.dedServ)
                return;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.15f + 0.3f * speed01, Pitch = -0.2f + 0.7f * speed01, PitchVariance = 0.08f }, position);
        }

        // THE SOUNDS AND DUST OF THE SLEEPER HOLD: called every tick while the yoyo is held down and spinning up (charge01: 0 to 1).
        // Honey is flung off it (more and harder as it speeds up) and a whirring swish gets quicker and higher.
        public override void Charging(Vector2 position, float charge01)
        {
            if (Main.dedServ)
                return;
            if (Main.rand.NextFloat() < 0.15f + 0.45f * charge01)
                Dust.NewDustPerfect(position, DustID.Honey, Main.rand.NextVector2Circular(1f, 1f) * (1f + 4f * charge01), 100, default, Main.rand.NextFloat(0.7f, 1.1f));
            int interval = System.Math.Max(3, (int)(14f - 10f * charge01));
            if (Main.GameUpdateCount % (uint)interval == 0)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.1f + 0.2f * charge01, Pitch = -0.4f + 0.9f * charge01, PitchVariance = 0.05f }, position);
        }

        // Max speed reached: a chime and a burst of honey.
        public override void ChargeReady(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = 0.4f }, position);
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.Honey, Main.rand.NextVector2Circular(4f, 4f), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
            }
        }

        // A charged yoyo that waits: a drop of honey drips off it now and then.
        public void ChargedDrip(Vector2 position)
        {
            Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(6f, 6f), DustID.Honey, Main.rand.NextVector2Circular(0.4f, 0.4f), 100, default, Main.rand.NextFloat(0.7f, 1f));
        }

        // A bee leaves the spinning yoyo: a little puff of honey (the buzz of the bee itself is the vanilla bee's).
        public void BeeBurst(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.Honey, Main.rand.NextVector2Circular(2f, 2f), 100, default, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        // A thin trail of honey drops thrown off the spinning yoyo, more the faster it goes.
        public override void SpinTrail(Vector2 position, Vector2 velocity, float speed01)
        {
            if (Main.dedServ)
                return;

            if (speed01 < 0.4f)
                return;

            //if (!Main.rand.NextBool(2))
            //    return;

            for (int i = 0; i < 1; i++)
            {
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f,8f), DustID.Honey, -velocity * 0.03f + Main.rand.NextVector2Circular(0.8f, 0.8f), 100, default, Main.rand.NextFloat(0.7f, 1.1f));
                dust.noGravity = true;            
            }
        }
    }
}
