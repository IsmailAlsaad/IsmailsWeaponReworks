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

        // An ember that leaves exactly 'position' with 'velocity' (the sweep's lance tip sheds these).
        public static void Shed(Vector2 position, Vector2 velocity)
        {
            if (Main.dedServ)
                return;

            Vector2 v = velocity + Main.rand.NextVector2Circular(0.8f, 0.8f) + new Vector2(0f, -DarkLanceTuning.EmberKick * 0.5f * Main.rand.NextFloat());
            int life = Math.Max(4, (int)(DarkLanceTuning.EmberLifeTicks * Main.rand.NextFloat(0.6f, 1.1f)));
            float size = DarkLanceTuning.EmberSize * Main.rand.NextFloat(0.5f, 1.2f);
            MetaballLayers.Of<DarkLanceBlobStyle>().Spawn(position + Main.rand.NextVector2Circular(2f, 2f), v, life, size);
        }

        // The lance tip carries 'count' gathered orbs: a living blob that grows with the count. Call it every tick while the lance is out.
        public static void TipCharge(Vector2 tip, int count, float time)
        {
            if (Main.dedServ || count <= 0)
                return;

            MetaballLayer layer = MetaballLayers.Of<DarkLanceBlobStyle>();
            float fill = MathHelper.Clamp(count / (float)Math.Max(1, DarkLanceTuning.MaxOrbs), 0f, 1f);
            float r = DarkLanceTuning.BallRadius * (0.35f + 0.8f * fill);
            layer.AddBlob(tip, r, DarkLanceTuning.BallBodyHeat);
            layer.AddBlob(tip, r * 0.55f, DarkLanceTuning.BallCoreHeat);
            for (int i = 0; i < 2; i++)
            {
                float k = i * 2.7f;
                Vector2 offset = new Vector2((float)Math.Sin(time * 3.4f + k) * 0.5f, -0.5f - 0.2f * (float)Math.Sin(time * 5.1f + k * 1.3f)) * r;
                layer.AddBlob(tip + offset, r * 0.5f, DarkLanceTuning.BallBodyHeat * 0.8f);
            }
        }

        // A burst of blobs thrown outward from 'center' (the 8-orb explosion); they merge, rise, shrink and cool like the embers.
        public static void Burst(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;

            MetaballLayer layer = MetaballLayers.Of<DarkLanceBlobStyle>();
            for (int i = 0; i < 36; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Vector2 velocity = direction * radius * 0.03f * Main.rand.NextFloat(0.4f, 1.2f);
                layer.Spawn(center + direction * Main.rand.NextFloat(0f, radius * 0.15f), velocity, Main.rand.Next(40, 71), Main.rand.NextFloat(6f, 11f));
            }
        }
    }
}