using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>All dust, sound and procedural drawing for the Blood Butcherer (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class BloodButchererFx
    {
        // ---- sounds (every one is a plain swap here if it sounds wrong) ----

        public static void PlayLunge(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = 0.2f, PitchVariance = 0.1f }, position);
        }

        public static void PlayOpen(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.6f, Pitch = -0.6f }, position);
        }

        public static void PlayBite(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = 0.6f }, position);
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.8f, Pitch = -0.2f }, position);
        }

        public static void PlayClamp(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 1f, Pitch = -0.7f }, position);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f, Pitch = 0.5f }, position);
        }

        public static void PlaySpin(Vector2 position, float pitch)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = pitch, PitchVariance = 0.05f }, position);
        }

        public static void PlayThrow(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = 0.7f }, position);
            SoundEngine.PlaySound(SoundID.NPCDeath5 with { Volume = 0.7f, Pitch = Main.rand.NextFloat(0f,-0.3f)}, position);
        }

        // ---- dust ----

        // The throw: the jaws fly open and the enemy leaves in a spray of blood along its flight direction.
        public static void ThrowBurst(Vector2 center, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.6f) * Main.rand.NextFloat(3f, 11f);
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 8f), DustID.Blood, velocity, 0, default, Main.rand.NextFloat(1.2f, 2f));
                dust.noGravity = Main.rand.NextBool(2);
            }
            for (int i = 0; i < 6; i++)
            {
                Dust smoke = Dust.NewDustPerfect(center, DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f), 150, default, 1.5f);
                smoke.noGravity = true;
            }
        }

        // Small drips from the enemy while the jaws hold it.
        public static void GripDrip(Vector2 center)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(10f, 10f), DustID.Blood, Main.rand.NextVector2Circular(1.5f, 1.5f), 0, default, 1.2f);
            dust.noGravity = false;
        }

        // Blood flung off the tip of the spinning blade, along the tangent.
        public static void SpinFling(Vector2 tip, Vector2 velocity)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(tip, DustID.Blood, velocity + Main.rand.NextVector2Circular(1f, 1f), 0, default, Main.rand.NextFloat(1f, 1.5f));
            dust.noGravity = true;
        }

        // A drop falling off the slack tether.
        public static void TetherDrip(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(position, DustID.Blood, new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextFloat(0.2f, 0.8f)), 0, default, 1f);
            dust.noGravity = false;
        }

        // The jaws part: a spray of blood at the pivot, thrown forward along the blade.
        public static void MouthMist(Vector2 pivot, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.7f) * Main.rand.NextFloat(1.5f, 5f);
                Dust dust = Dust.NewDustPerfect(pivot + direction * Main.rand.NextFloat(4f, 30f), DustID.Blood, velocity, 0, default, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
        }

        // A drip from a jaw tip while the mouth hangs open.
        public static void Drool(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(tip, DustID.Blood, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(0.2f, 1f)), 0, default, 1.1f);
            dust.noGravity = false;
        }

        // The jaws slam shut: a burst in the middle of the mouth.
        public static void BiteSnap(Vector2 center)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.Blood, Main.rand.NextVector2Circular(5f, 5f), 0, default, Main.rand.NextFloat(1.1f, 1.8f));
                dust.noGravity = Main.rand.NextBool(2);
            }
        }

        // The jaws close on an enemy: a big red burst with a little smoke.
        public static void ClampBurst(Vector2 center)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 28; i++)
            {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 8f), DustID.Blood, Main.rand.NextVector2Circular(7f, 7f), 0, default, Main.rand.NextFloat(1.3f, 2.2f));
                dust.noGravity = Main.rand.NextBool(3);
            }
            for (int i = 0; i < 8; i++)
            {
                Dust smoke = Dust.NewDustPerfect(center, DustID.Smoke, Main.rand.NextVector2Circular(3f, 3f), 150, default, 1.6f);
                smoke.noGravity = true;
            }
        }

        // One chomp closing: a small burst in the middle of the mouth.
        public static void ChompSnap(Vector2 center)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(6f, 6f), DustID.Blood, Main.rand.NextVector2Circular(4f, 4f), 0, default, Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = Main.rand.NextBool(2);
            }
        }

        public static void PlayChomp(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.5f, PitchVariance = 0.15f }, position);
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = -0.1f, PitchVariance = 0.15f }, position);
        }

        // A thrown enemy slams into a tile: blood sprays back off the surface, more of it the more speed it lost.
        public static void ImpactBurst(Vector2 point, Vector2 normal, float speed)
        {
            if (Main.dedServ)
                return;
            int count = 10 + (int)MathHelper.Clamp(speed * 1.5f, 0f, 30f);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = normal.RotatedByRandom(1.1f) * Main.rand.NextFloat(2f, 4f + speed * 0.5f);
                Dust dust = Dust.NewDustPerfect(point + Main.rand.NextVector2Circular(6f, 6f), DustID.Blood, velocity, 0, default, Main.rand.NextFloat(1.2f, 2f));
                dust.noGravity = Main.rand.NextBool(3);
            }
            for (int i = 0; i < 5; i++)
            {
                Dust smoke = Dust.NewDustPerfect(point, DustID.Smoke, normal * Main.rand.NextFloat(0.5f, 2.5f) + Main.rand.NextVector2Circular(1f, 1f), 150, default, 1.4f);
                smoke.noGravity = true;
            }
        }

        public static void PlayImpact(Vector2 position, float speed)
        {
            if (Main.dedServ)
                return;
            float strength = MathHelper.Clamp(speed / Math.Max(1f, BloodButchererTuning.LaunchSpeed), 0.3f, 1f);
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = strength, Pitch = -0.5f }, position);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f * strength, Pitch = -0.2f }, position);
        }

        public static void HitBlood(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 6; i++)
                Dust.NewDustPerfect(position, DustID.Blood, Main.rand.NextVector2Circular(4f, 4f), 0, default, 1.3f);
        }

        // ---- drawing ----

        // The fallback teeth (used until the jaw art exists; the art draws its own): a row of small bone pixel spikes along the inner edge
        // of a jaw, growing with how far the mouth is open and shorter toward the tip. Client only, spriteBatch already in world space.
        public static void DrawTeeth(Vector2 pivot, Vector2 jawDirection, float jawLength, Vector2 inward, float open, float scale, Color light)
        {
            if (Main.dedServ || open < 0.15f)
                return;

            float grid = BloodButchererTuning.PixelSize;
            int count = Math.Max(2, BloodButchererTuning.TeethCount);
            Color color = Color.Lerp(BloodButchererTuning.BoneColor, light, 0.25f);
            for (int i = 0; i < count; i++)
            {
                float along = i / (float)(count - 1);
                Vector2 root = pivot + jawDirection * (MathHelper.Lerp(0.3f, 0.95f, along) * jawLength);
                float length = BloodButchererTuning.ToothLength * scale * open * (1f - 0.35f * along);
                int steps = Math.Max(1, (int)(length / grid));
                for (int s = 0; s < steps; s++)
                {
                    float size = MathHelper.Lerp(grid * 3f, grid, s / (float)steps);
                    IceFx.DrawPixel(root + inward * (s * grid), size, color, grid);
                }
            }
        }
    }
}