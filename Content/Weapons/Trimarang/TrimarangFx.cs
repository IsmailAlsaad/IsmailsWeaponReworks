using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Trimarang
{
    /// <summary>All dust and sound for the Trimarang (vanilla dust and sounds only).</summary>
    public static class TrimarangFx
    {
        // Called every tick by every machine while the boomerang spins (spinT: 0 slow to 1 full speed).
        public static void Spinning(Vector2 center, float spinT, int tick)
        {
            if (Main.dedServ)
                return;
            int interval = (int)MathHelper.Lerp(22f, 9f, spinT);
            if (tick % interval == 0)
                SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.3f + 0.25f * spinT, Pitch = -0.3f + 0.7f * spinT }, center);
            if (Main.rand.NextFloat() < 0.15f + 0.6f * spinT)
            {
                // Stardust palette. Every particle leaves the edge moving outward AND around in the spin direction (the rotation grows
                // along this tangent), so the dust it sheds draws a spiral that opens up as the boomerang speeds up.
                int[] palette = { DustID.Enchanted_Gold, DustID.MagicMirror, DustID.BlueTorch, DustID.PinkFairy };
                Vector2 radial = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 spot = center + radial * 26f;
                Vector2 tangent = new Vector2(-radial.Y, radial.X);
                Vector2 velocity = tangent * (1.5f + 3.5f * spinT) + radial * (0.8f + 2.2f * spinT);
                Dust dust = Dust.NewDustPerfect(spot, palette[Main.rand.Next(palette.Length)], velocity + Main.rand.NextVector2Circular(0.4f, 0.4f), 80, default, 0.9f + 0.4f * spinT);
                dust.noGravity = true;
            }
        }

        public static void HitSparks(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), DustID.PurpleTorch, Main.rand.NextVector2Circular(3.5f, 3.5f), 80, default, 1.1f);
                dust.noGravity = true;
            }
        }

        public static void Catch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.PurpleTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 80, default, 1f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.45f, Pitch = 0.1f }, position);
        }

        // A new ethereal form leaves the main boomerang.
        public static void EtherealSpawn(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, i % 2 == 0 ? DustID.MagicMirror : DustID.BlueTorch, direction * Main.rand.NextFloat(1f, 5f) + Main.rand.NextVector2Circular(1.5f, 1.5f), 60, default, 1.2f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = 0.4f }, position);
        }

        public static void EtherealTrail(Vector2 position, Vector2 velocity)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;
            Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(6f, 6f), DustID.MagicMirror, -velocity * 0.05f + Main.rand.NextVector2Circular(0.4f, 0.4f), 120, default, 1f);
            dust.noGravity = true;
        }

        // The ethereal form's end: only a visual, it does no damage.
        public static void EtherealExplode(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 26; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, i % 3 == 0 ? DustID.BlueTorch : DustID.MagicMirror, Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f), 60, default, Main.rand.NextFloat(1f, 1.8f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), DustID.MagicMirror, Main.rand.NextVector2Circular(1f, 1f), 150, default, 2f);
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.3f }, position);
        }
    }
}