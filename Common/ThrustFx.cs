using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>The colors and dust a thrusting weapon is made of. Each weapon's Tuning supplies one; ThrustFx and ThrustTrailStyle use it.</summary>
    public readonly struct ThrustPalette
    {
        public readonly Color Core;   // the bright edge of the trail
        public readonly Color Mid;    // the body of the trail and the glow
        public readonly Color Halo;   // the outer, faint color
        public readonly int DustA;    // the two dust types the tip sheds (vanilla DustID)
        public readonly int DustB;

        public ThrustPalette(Color core, Color mid, Color halo, int dustA, int dustB)
        {
            Core = core;
            Mid = mid;
            Halo = halo;
            DustA = dustA;
            DustB = dustB;
        }
    }

    /// <summary>
    /// The dust and sound every held spear shares (vanilla dust and sounds only); a weapon only supplies a ThrustPalette and, for anything
    /// special, its own Fx class next to it. Every machine calls these for the same event; none of it is synced.
    /// </summary>
    public static class ThrustFx
    {
        public static void PlayThrust(Vector2 position, float pitch)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.75f, Pitch = pitch, PitchVariance = 0.08f }, position);
        }

        // Dust shed behind the tip while the spear is out.
        public static void TipStreak(Vector2 tip, Vector2 direction, ThrustPalette palette)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = tip - direction * Main.rand.NextFloat(0f, 22f) + Main.rand.NextVector2Circular(3f, 3f);
                int type = Main.rand.NextBool() ? palette.DustA : palette.DustB;
                if(type == -1)
                {
                    continue;
                }
                Dust dust = Dust.NewDustPerfect(position, type, -direction * Main.rand.NextFloat(0.4f, 2f), 100, default, Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }
        }

        public static void HitSpark(Vector2 position, ThrustPalette palette)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
            {
                int type = i % 2 == 0 ? palette.DustA : palette.DustB;
                if (type == -1)
                {
                    continue;
                }
                Dust dust = Dust.NewDustPerfect(position, type, Main.rand.NextVector2Circular(3.2f, 3.2f), 100, default, Main.rand.NextFloat(1f, 1.4f));
                dust.noGravity = true;
            }
        }
    }
}