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

        // The javelin leaves the hand: a deep whoosh and a sharp one on top.
        public static void Throw(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.45f }, position);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, Pitch = 0.3f }, position);
        }

        // The javelin goes through an enemy that can be carried: a wet thud and a spray of blood along the way it was going.
        public static void Impale(Vector2 point, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.9f, Pitch = -0.25f }, point);
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(point, DustID.Blood, direction.RotatedByRandom(0.7f) * Main.rand.NextFloat(2f, 6f), 100, default, Main.rand.NextFloat(1.1f, 1.7f));
                dust.noGravity = true;
            }
        }

        // A drop of blood falling off a carried enemy.
        public static void Drip(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), DustID.Blood, new Vector2(0f, 0.5f), 100, default, 1.1f);
        }

        // The blood explosion at the end of the flight (scale 1), or a smaller burst at each released enemy (scale under 1: no sound, no shake).
        public static void Explode(Vector2 center, float scale = 1f)
        {
            if (Main.dedServ)
                return;
            if (scale >= 1f)
            {
                SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 1f, Pitch = -0.5f }, center);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.3f }, center);
                CameraShake.Punch(center, RottedForkTuning.BlastShake, 18, "RottedForkBlast");
            }
            Lighting.AddLight(center, 1.2f * scale, 0.2f * scale, 0.25f * scale);

            int count = (int)(18 * scale) + 6;
            for (int i = 0; i < count; i++)
            {
                float angle = OrbitRing.Angle(0f, i, count, 0f);
                Dust ring = Dust.NewDustPerfect(OrbitRing.Position(center, 10f * scale, angle), DustID.Blood, angle.ToRotationVector2() * 5f * scale, 100, default, 1.5f * scale + 0.3f);
                ring.noGravity = true;
            }
            int burst = (int)(26 * scale) + 8;
            for (int i = 0; i < burst; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, i % 3 == 0 ? DustID.RedTorch : DustID.Blood, Main.rand.NextVector2Circular(8f, 8f) * scale, 100, default, Main.rand.NextFloat(1.2f, 2f) * scale + 0.2f);
                dust.noGravity = Main.rand.NextBool();
            }
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