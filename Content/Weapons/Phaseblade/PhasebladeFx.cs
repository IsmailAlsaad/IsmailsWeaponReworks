using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Phaseblade
{
    /// <summary>All dust and sound for the Phaseblades (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class PhasebladeFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position, bool up, bool spin)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = up ? 0.2f : 0f, PitchVariance = 0.1f }, position);
            //if (spin)
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.45f, Pitch = 0.35f, PitchVariance = 0.2f }, position);
        }

        public static void PlayLoop(Vector2 position)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.45f, Pitch = -0.2f, PitchVariance = 0.1f }, position);
        }

        // ---- dust ----

        static void Spark(Vector2 position, Vector2 velocity, float scale, Color color)
        {
            Dust dust = Dust.NewDustPerfect(position, DustID.TintableDustLighted, velocity, 0, new Color(color.R, color.G, color.B), scale);
            dust.noGravity = true;
        }

        // A few sparks along the outer half of the blade while it strikes.
        public static void SlashDust(Vector2 hand, Vector2 tip, Color color)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 3; i++)
                Spark(Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.5f, 1f)), Main.rand.NextVector2Circular(1.2f, 1.2f), 1.1f, color);
        }

        // Extra sparks thrown off the tip of a spinning blade.
        public static void SpinDust(Vector2 tip, Vector2 tipVelocity, Color color)
        {
            if (Main.dedServ)
                return;
            Spark(tip, -tipVelocity * 0.08f + Main.rand.NextVector2Circular(1f, 1f), 1.2f, color);
        }

        public static void HitSpark(Vector2 position, Color color)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Spark(position, Main.rand.NextVector2Circular(3f, 3f), 1.2f, color);
        }
    }
}