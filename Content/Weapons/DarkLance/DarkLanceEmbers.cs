using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>The Dark Lance's look for the shared metaball tool (Common/MetaballLayer): every number reads DarkLanceTuning live.</summary>
    public class DarkLanceBlobStyle : MetaballStyle
    {
        public override float Grid => DarkLanceTuning.PixelSize;
        public override float Merge => DarkLanceTuning.EmberMerge;
        public override float OutlineWidth => DarkLanceTuning.EmberOutline;
        public override int Bands => DarkLanceTuning.ColorBands;
        public override float GlowStrength => DarkLanceTuning.EmberGlow;
        public override int MaxParticles => DarkLanceTuning.EmberMax;
        public override float Rise => DarkLanceTuning.EmberRise;
        public override float Drag => DarkLanceTuning.EmberDrag;
        public override float Wobble => DarkLanceTuning.EmberWobble;

        // an ember starts as hot as the body it left (not white) and cools to nothing
        public override float HeatOverLife(float life) => DarkLanceTuning.EmberHeat * (1f - life);

        public override Color GetFill(float heat)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;
            return heat < 0.5f ? Color.Lerp(palette.Halo, palette.Mid, heat * 2f) : Color.Lerp(palette.Mid, palette.Core, (heat - 0.5f) * 2f);
        }

        public override Color GetOutline() => Color.Lerp(DarkLanceTuning.Palette.Halo, Color.Black, 0.35f);

        public override Color GetGlow() => DarkLanceTuning.Palette.Mid;
    }

    /// <summary>
    /// What the orbs hand to the metaball layer: the orb's body (a few round blobs: a big dark-purple one, a small hot core and flame
    /// tongues that wobble, so the whole thing is one living blob) every tick, and the embers it sheds, which live on their own in the world.
    /// Visual only: nothing is synced, every machine does it for the orbs it sees. Client only.
    /// </summary>
    public static class DarkLanceEmbers
    {
        // The orb's body, called every tick from the orb's AI. 'scale' 0 to about 1.25 is the pop-in, 'time' in seconds, 'seed' something every machine knows.
        public static void Ball(Vector2 center, float scale, float time, float seed)
        {
            if (Main.dedServ || scale <= 0.05f)
                return;

            MetaballLayer layer = MetaballLayers.Of<DarkLanceBlobStyle>();
            float r = DarkLanceTuning.BallRadius * scale;
            layer.AddBlob(center, r, DarkLanceTuning.BallBodyHeat);                                       // the dark body
            layer.AddBlob(center + new Vector2(0f, r * 0.1f), r * 0.55f, DarkLanceTuning.BallCoreHeat);   // the hot core (the bands show it as a bright middle)
            for (int i = 0; i < 3; i++)
            {
                // flame tongues: they sway and stretch upward, out of step with each other
                float k = seed * 1.7f + i * 2.1f;
                Vector2 offset = new Vector2((float)Math.Sin(time * 3.1f + k) * 0.5f, -0.55f - 0.25f * (float)Math.Sin(time * 5.3f + k * 1.3f)) * r;
                layer.AddBlob(center + offset, r * (0.5f + 0.1f * (float)Math.Sin(time * 4.2f + k)), DarkLanceTuning.BallBodyHeat * 0.8f);
            }
        }

        // One ember leaves the upper rim of the orb (call every EmberSpawnTicks). 'orbVelocity' is how far the orb moved this tick.
        public static void Spawn(Vector2 orbCenter, Vector2 orbVelocity)
        {
            if (Main.dedServ)
                return;

            float angle = MathHelper.ToRadians(Main.rand.NextFloat(-145f, -35f));   // -90 degrees = straight up on screen
            Vector2 direction = angle.ToRotationVector2();
            Vector2 velocity = direction * DarkLanceTuning.EmberKick * Main.rand.NextFloat(0.5f, 1.2f) + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), 0f) + orbVelocity * DarkLanceTuning.EmberInherit;
            int life = Math.Max(4, (int)(DarkLanceTuning.EmberLifeTicks * Main.rand.NextFloat(0.7f, 1.3f)));
            float size = DarkLanceTuning.EmberSize * Main.rand.NextFloat(0.6f, 1.4f);
            MetaballLayers.Of<DarkLanceBlobStyle>().Spawn(orbCenter + direction * DarkLanceTuning.BallRadius * 0.9f + Main.rand.NextVector2Circular(2f, 2f), velocity, life, size);
        }
    }
}