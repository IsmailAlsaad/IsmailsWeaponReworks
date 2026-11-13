using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// Your mask art (NightsEdgeBladeMask.png, the same canvas as the vanilla sprite; opaque = where the edge metaballs should show), read once
    /// into a list of pixel centers. If the file is missing the list stays empty and the effect runs along the blade line instead. Client only.
    /// </summary>
    public class NightsEdgeMask
    {
        Vector2[] points = new Vector2[0];
        Vector2 size = Vector2.One;
        bool tried;

        public int Count => points.Length;

        public void EnsureLoaded()
        {
            if (tried || Main.dedServ)
                return;
            tried = true;
            if (!ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/NightsEdge/NightsEdgeBladeMask", out Asset<Texture2D> asset, AssetRequestMode.ImmediateLoad))
                return;
            Texture2D texture = asset.Value;
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            System.Collections.Generic.List<Vector2> found = new System.Collections.Generic.List<Vector2>();
            for (int y = 0; y < texture.Height; y++)
                for (int x = 0; x < texture.Width; x++)
                    if (data[y * texture.Width + x].A >= 128)
                        found.Add(new Vector2(x + 0.5f, y + 0.5f));
            points = found.ToArray();
            size = new Vector2(texture.Width, texture.Height);
        }

        // Point 'index' as a pixel of the sword's own texture (the mask is scaled if its canvas differs from the sprite's).
        public Vector2 SpritePixel(int index, Vector2 spriteSize)
        {
            return points[index] * spriteSize / size;
        }
    }

    /// <summary>
    /// What the fully charged Night's Edge hands to the metaball layers every tick: small blobs on the mask's pixels (the edge), dark flames
    /// that rise off it, a few vanilla embers and a purple light. Strength follows the blade's outline (0..1), so it grows in and dies out with
    /// the charge. Visual only: nothing is synced, every machine feeds its own layers. Client only.
    /// </summary>
    public static class NightsEdgeFlames
    {
        static int Chance(float rate)
        {
            int count = (int)rate;
            if (Main.rand.NextFloat() < rate - count)
                count++;
            return count;
        }

        // A fixed pseudo-random 0..1 per index: as the strength falls, the edge blobs drop out one by one in the same order.
        static float Hash01(int index)
        {
            unchecked
            {
                return (((uint)index * 2654435761u) >> 8 & 0xFFu) / 256f;
            }
        }

        // Where the effect sits: a mask pixel (index) or, without a mask, along the blade from FallbackStart to the tip.
        static Vector2 EdgePoint(ChargedBladePose pose, NightsEdgeMask mask, int index, int total)
        {
            if (mask.Count > 0)
                return pose.ToWorld(mask.SpritePixel(index, pose.SpriteSize));
            float t = NightsEdgeTuning.FallbackStart + (1f - NightsEdgeTuning.FallbackStart) * (index + 0.5f) / Math.Max(1, total);
            return pose.Hand + pose.Direction * (Vector2.Distance(pose.Hand, pose.Tip) * t);
        }

        public static void Feed(ChargedBladePose pose, NightsEdgeMask mask, NightsEdgeFlameState state)
        {
            if (Main.dedServ)
                return;
            Vector2 motion = state.Update(pose); // the blade's motion the flames may inherit (zero right after the sword flips sides)
            float show = state.Strength;         // follows the charge down, so the flames die out gradually
            if (show < 0.02f || pose.SpriteSize.X < 1f)
                return;
            mask.EnsureLoaded();

            MetaballLayer edge = MetaballLayers.Of<NightsEdgeEdgeStyle>();
            MetaballLayer flames = MetaballLayers.Of<NightsEdgeFlameStyle>();
            float scale = pose.Scale;
            float time = pose.Ticks / 60f;
            Vector2 normal = new Vector2(-pose.Direction.Y, pose.Direction.X);
            float length = Vector2.Distance(pose.Hand, pose.Tip);
            int total = mask.Count > 0 ? mask.Count : Math.Max(6, (int)(length / Math.Max(1f, NightsEdgeTuning.EdgeRadius * scale * 1.6f)));

            // ---- the edge: a small round blob on every Nth mask pixel, wobbling a little so the edge lives ----
            float radius = NightsEdgeTuning.EdgeRadius * scale * (0.4f + 0.6f * show);
            int stride = Math.Max(1, NightsEdgeTuning.EdgeBlobStride);
            for (int i = 0; i < total; i += stride)
            {
                if (Hash01(i) >= show)
                    continue; // thinning out as the charge drains
                float phase = time * NightsEdgeTuning.EdgeWobbleSpeed + i * 1.9f;
                Vector2 position = EdgePoint(pose, mask, i, total) + normal * ((float)Math.Sin(phase) * NightsEdgeTuning.EdgeWobble * scale);
                float r = radius * (1f + 0.2f * (float)Math.Sin(phase * 1.3f + 1f));
                float heat = NightsEdgeTuning.EdgeHeat + NightsEdgeTuning.EdgeHeatFlicker * (float)Math.Sin(phase * 0.8f);
                edge.AddBlob(position, r, MathHelper.Clamp(heat, 0f, 1f));
            }

            // ---- flames rising off the edge (they inherit a part of the blade's motion) ----
            int flameCount = Chance(NightsEdgeTuning.FlameRate * show);
            for (int i = 0; i < flameCount; i++)
            {
                Vector2 position = EdgePoint(pose, mask, Main.rand.Next(total), total) + Main.rand.NextVector2Circular(1.5f, 1.5f);
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(0.5f, 1.3f)) + motion * 0.25f;
                int life = Main.rand.Next(NightsEdgeTuning.FlameLifeMin, NightsEdgeTuning.FlameLifeMax + 1);
                flames.Spawn(position, velocity, life, NightsEdgeTuning.FlameSize * scale * Main.rand.NextFloat(0.7f, 1.15f) * (0.6f + 0.4f * show));
            }

            // ---- vanilla dust embers and the light ----
            if (Main.rand.NextFloat() < NightsEdgeTuning.EmberDustChance * show)
                NightsEdgeFx.Ember(EdgePoint(pose, mask, Main.rand.Next(total), total), motion);
            float glow = NightsEdgeTuning.LightStrength * show;
            Lighting.AddLight(Vector2.Lerp(pose.Hand, pose.Tip, 0.5f), 0.55f * glow, 0.18f * glow, 0.95f * glow);
        }
    }
}