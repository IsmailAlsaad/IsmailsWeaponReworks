using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.RottedFork
{
    /// <summary>The Rotted Fork's own dust and sounds (the shared hit sparks live in Common/ThrustFx). Vanilla dust and sounds only. Every machine calls these for the same event; none of it is synced.</summary>
    public static class RottedForkFx
    {
        // The spin begins: a ring of blood leaves the player and a deep whoosh plays.
        public static void SpinUp(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.35f }, center);
            int count = 14;
            for (int i = 0; i < count; i++)
            {
                float angle = OrbitRing.Angle(0f, i, count, 0f);
                Dust dust = Dust.NewDustPerfect(OrbitRing.Position(center, radius, angle), DustID.Blood, angle.ToRotationVector2() * 2.2f, 100, default, 1.3f);
                dust.noGravity = true;
            }
        }

        // Every half turn: a whoosh that rises in pitch and volume with the spin speed (0 to 1).
        public static void Whoosh(Vector2 position, float speed)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.45f + 0.25f * speed, Pitch = -0.2f + 0.45f * speed, PitchVariance = 0.08f }, position);
        }

        // Flecks thrown off the tip while it is fast, along the way the tip is moving ('motion' is the tip's movement this tick).
        public static void Flecks(Vector2 tip, Vector2 motion, float speed, ThrustPalette palette)
        {
            if (Main.dedServ || Main.rand.NextFloat() > 0.8f * speed)
                return;
            int type = Main.rand.NextBool() ? palette.DustA : palette.DustB;
            Dust dust = Dust.NewDustPerfect(tip, type, motion * 0.4f + Main.rand.NextVector2Circular(1f, 1f), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
            dust.noGravity = true;
        }
    }
}