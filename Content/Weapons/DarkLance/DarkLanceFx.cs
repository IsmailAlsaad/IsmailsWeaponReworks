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

        // The orb body is drawn by the shared metaball layer (DarkLanceEmbers.Ball); its halo is DrawGlow below.

        // A soft round glow (Effects/PixelGlow.fx): the palette's core in the middle, then its mid and halo colors, fading smoothly to
        // nothing at 'radius' (px). 'strength' is the peak brightness (0 to 1). Visual only.
        public static void DrawGlow(Vector2 center, float radius, float strength)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;
            PixelShaderParams p = new PixelShaderParams
            {
                Grid = DarkLanceTuning.PixelSize,
                ColorA = palette.Halo,
                ColorB = palette.Mid,
                ColorC = palette.Core,
                Params = new Vector4(radius, DarkLanceTuning.TipGlowFalloff, strength, 0f),
                FallbackColor = palette.Mid
            };
            PixelShaderFx.Draw("PixelGlow", center, (int)System.Math.Ceiling(radius * 2f) + 4, p);
        }

        // The sweep begins: a low whoosh and a ring of dark flame dust around the player.
        public static void SweepStart(Vector2 center)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.4f }, center);
            Ring(center, 12, 20f, 2.2f);
        }

        // The turn itself gets going: a whoosh at the tip.
        public static void SweepWhoosh(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = 0.1f, PitchVariance = 0.05f }, tip);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.35f, Pitch = -0.5f }, tip);
        }

        // Dark flecks thrown off the tip while it is fast ('motion' is how far the tip moved this tick).
        public static void Flecks(Vector2 tip, Vector2 motion, float speed, ThrustPalette palette)
        {
            if (Main.dedServ || Main.rand.NextFloat() > 0.8f * speed)
                return;
            int type = Main.rand.NextBool() ? palette.DustA : palette.DustB;
            Dust dust = Dust.NewDustPerfect(tip, type, motion * 0.4f + Main.rand.NextVector2Circular(1f, 1f), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
            dust.noGravity = true;
        }

        // The sweep gathers an orb: a chime that rises with every orb gathered in quick succession, and a small flash.
        public static void CollectPing(Vector2 position, int chain)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, Pitch = System.Math.Min(0.9f, -0.3f + 0.15f * chain) , MaxInstances = 0}, position);
            Ring(position, 8, 8f, 2.5f);
        }

        // A gathered orb arrives in the lance tip: dust that falls inward.
        public static void Absorb(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(14f, 26f);
                Dust dust = Dust.NewDustPerfect(position + offset, i % 2 == 0 ? DustID.Shadowflame : DustID.PurpleTorch, -offset * 0.1f, 100, default, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
        }

        // The thrust shoots out: deeper and louder with every gathered orb; with all of them a heavy boom.
        public static void ThrustStart(Vector2 tip, int gathered)
        {
            if (Main.dedServ)
                return;
            float t = gathered / (float)System.Math.Max(1, DarkLanceTuning.MaxOrbs);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.2f - 0.4f * t }, tip);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.4f + 0.4f * t, Pitch = -0.4f }, tip);
            if (gathered >= DarkLanceTuning.MaxOrbs)
                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.8f, Pitch = -0.6f }, tip);
        }

        // The evil explosion of a thrust with all 8 orbs: a deep boom, rings, a flash of dust and a burst of metaball blobs.
        public static void Blast(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.5f }, center);
            SoundEngine.PlaySound(SoundID.Item45 with { Volume = 1f, Pitch = -0.7f }, center);
            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = -0.4f }, center);
            CameraShake.Punch(center, DarkLanceTuning.BlastShake, 22, "DarkLanceBlast");
            Lighting.AddLight(center, DarkLanceTuning.Palette.Mid.ToVector3() * 2f);
            for (int i = 0; i < 50; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.Shadowflame, Main.rand.NextVector2Circular(11f, 11f), 100, default, Main.rand.NextFloat(1.4f, 2.2f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.PurpleTorch, Main.rand.NextVector2Circular(7f, 7f), 100, default, Main.rand.NextFloat(1.2f, 1.8f));
                dust.noGravity = true;
            }
            Ring(center, 24, 16f, 9f);
            Ring(center, 36, radius * 0.5f, 5f);
            DarkLanceEmbers.Burst(center, radius);
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
            //Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextBool(3) ? DustID.PurpleTorch : DustID.Shadowflame, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.6f, -0.4f)), 100, default, Main.rand.NextFloat(0.9f, 1.4f));
            //dust.noGravity = true;
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
            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.PurpleTorch, Main.rand.NextVector2Circular(8.5f, 8.5f), 100, default, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
            //Ring(center, 14, 14f, 4.5f);
            //Ring(center, 28, radius * 0.9f, 0.6f); // the edge of the cloud
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