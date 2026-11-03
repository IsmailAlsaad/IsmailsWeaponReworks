using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>The Dark Lance's own dust and sounds. Vanilla dust and sounds only. Every machine calls these for the same event; none of it is synced.</summary>
    public static class DarkLanceFx
    {
        // The same color with alpha 0: drawn additively (glow).
        public static Color Glow(Color color)
        {
            return new Color(color.R, color.G, color.B, 0);
        }

        // Draws one orb: Effects/DarkFlameOrb.fx over a square quad centered on it (visual only, never synced, every machine draws it from
        // the projectile's own state). 'time' in seconds, 'seed' something every machine knows (the spawn serial), 'appear' 0 to about 1.25
        // (the pop-in with overshoot), 'flicker' around 1. A missing .xnb shows a small purple square instead of the orb.
        public static void DrawOrb(Vector2 center, float time, float seed, float appear, float flicker)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;
            PixelShaderParams p = new PixelShaderParams
            {
                Grid = DarkLanceTuning.PixelSize,
                Time = time,
                Seed = seed,
                Intensity = flicker,
                Bands = DarkLanceTuning.ColorBands,
                ColorA = palette.Halo,
                ColorB = palette.Mid,
                ColorC = palette.Core,
                Params = new Vector4(DarkLanceTuning.OrbUnitPx, appear, 0f, 0f),
                FallbackColor = palette.Mid
            };
            PixelShaderFx.Draw("DarkFlameOrb", center, DarkLanceTuning.OrbQuadSize, p);
        }

        // The lance goes up and the charge begins.
        public static void ChargeStart(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.6f }, position);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.45f, Pitch = -0.7f }, position);
        }

        // Dark energy streaming into the lance tip; 'strength' is 0 to 1 (how charged it is).
        public static void Gather(Vector2 tip, float strength)
        {
            if (Main.dedServ)
                return;
            int count = strength > 0.6f ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(26f, 46f);
                int type = Main.rand.NextBool(3) ? DustID.PurpleTorch : DustID.Shadowflame;
                Dust dust = Dust.NewDustPerfect(tip + offset, type, -offset * 0.09f, 100, default, 0.9f + 0.7f * strength);
                dust.noGravity = true;
            }
        }

        // A ring of dark flame dust.
        public static void Ring(Vector2 center, int count, float radius, float outwardSpeed)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                float angle = OrbitRing.Angle(0f, i, count, 0f);
                Vector2 direction = angle.ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(center + direction * radius, i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch, direction * outwardSpeed, 100, default, 1.3f);
                dust.noGravity = true;
            }
        }

        // A new orb leaves the lance: a rising chime (higher with every orb) and a puff; the last one gets a deep boom and a big ring.
        public static void OrbBorn(Vector2 position, int count, int max)
        {
            if (Main.dedServ)
                return;
            float t = count / (float)System.Math.Max(1, max);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.8f, Pitch = -0.4f + 0.9f * t }, position);
            Ring(position, 10, 6f, 3f);
            if (count >= max)
            {
                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.9f, Pitch = -0.5f }, position);
                Ring(position, 24, 10f, 6f);
            }
        }

        // Dust an orb sheds while it orbits.
        public static void Shed(Vector2 center)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextBool(3) ? DustID.PurpleTorch : DustID.Shadowflame, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.6f, -0.4f)), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
            dust.noGravity = true;
        }

        // An enemy walked into an orb: it pops in a cloud of Shadowflame (no damage, the debuff is applied by the owner).
        public static void Pop(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.8f, Pitch = -0.2f }, center);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0.3f }, center);
            for (int i = 0; i < 26; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.Shadowflame, Main.rand.NextVector2Circular(5f, 5f), 100, default, Main.rand.NextFloat(1.1f, 1.7f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.PurpleTorch, Main.rand.NextVector2Circular(3.5f, 3.5f), 100, default, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
            Ring(center, 14, 14f, 4.5f);
            Ring(center, 28, radius * 0.9f, 0.6f); // the edge of the cloud
        }

        // An orb ends without being popped (the item was swapped or its 30 s ran out).
        public static void Fizzle(Vector2 center)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.35f, Pitch = -0.7f }, center);
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.PurpleTorch, Main.rand.NextVector2Circular(2f, 2f), 100, default, 1f);
                dust.noGravity = true;
            }
        }
    }
}