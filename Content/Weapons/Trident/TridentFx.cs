using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Trident
{
    /// <summary>The Trident's own dust, sounds and camera punch (the shared thrust effects live in Common/ThrustFx). Vanilla dust and sounds only.</summary>
    public static class TridentFx
    {
        // The dash begins (every machine): a ring around the player and a spray of dust left behind.
        public static void DashStart(Vector2 center, Vector2 direction, bool liquid)
        {
            if (Main.dedServ)
                return;
            ThrustPalette palette = TridentTuning.Palette;
            if (liquid)
                SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.6f, Pitch = 0.1f }, center);

            for (int i = 0; i < 16; i++)
            {
                Vector2 ring = (MathHelper.TwoPi * i / 16f).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(center + ring * 12f, i % 2 == 0 ? palette.DustA : palette.DustB, ring * 2.5f - direction * 1.5f, 100, default, Main.rand.NextFloat(1.1f, 1.6f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 12; i++)
            {
                Vector2 spread = (-direction).RotatedByRandom(0.5f) * Main.rand.NextFloat(3f, 9f);
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 8f), palette.DustA, spread, 100, default, Main.rand.NextFloat(1.2f, 1.9f));
                dust.noGravity = true;
            }
        }

        // Every tick of the dash: a stream of dust shed by the body (bubbles in liquid).
        public static void DashBody(Vector2 center, Vector2 direction, bool liquid)
        {
            if (Main.dedServ)
                return;
            ThrustPalette palette = TridentTuning.Palette;
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = center + Main.rand.NextVector2Circular(10f, 14f);
                Dust dust = Dust.NewDustPerfect(position, liquid && i == 0 ? DustID.Water : palette.DustA, -direction * Main.rand.NextFloat(0.5f, 3f) + Main.rand.NextVector2Circular(1f, 1f), 100, default, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
        }

        // The impact (the owner's client calls it from DashPlayer / the projectile): sparks fanned out along the normal, a ring, a clang
        // and a camera punch. Other players see the enemy hit's sparks through the held projectile; the shake is the owner's alone.
        public static void Bounce(Vector2 position, Vector2 normal, bool tile)
        {
            if (Main.dedServ)
                return;
            ThrustPalette palette = TridentTuning.Palette;
            SoundEngine.PlaySound((tile ? SoundID.Tink : SoundID.NPCHit4) with { Volume = 0.9f, Pitch = -0.1f }, position);

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = normal.RotatedByRandom(1.2f) * Main.rand.NextFloat(3f, 9f);
                Dust dust = Dust.NewDustPerfect(position - normal * 6f, i % 3 == 0 ? palette.DustB : palette.DustA, velocity, 100, default, Main.rand.NextFloat(1.2f, 1.9f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 14; i++)
            {
                Vector2 ring = (MathHelper.TwoPi * i / 14f).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(position + ring * 8f, DustID.MagicMirror, ring * 4f, 100, default, 1.2f);
                dust.noGravity = true;
            }
            CameraShake.Punch(position, TridentTuning.BounceShake, 14, "TridentBounce");
        }
    }
}