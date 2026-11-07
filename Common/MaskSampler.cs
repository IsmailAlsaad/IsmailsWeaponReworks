using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Where a sprite was drawn with SpriteBatch (position of its origin, rotation, origin, scale, flip), and the maths to go from a point
    /// of the sprite (in its texture pixels) to the world and back. The origin is given as SpriteBatch takes it: in the drawn quad, i.e.
    /// after the flip (this is how HeldSwordPose.Sprite hands it out). Visual helper, never synced.
    /// </summary>
    public struct SpriteTransform
    {
        public Vector2 Position;     // world position the origin is drawn at
        public float Rotation;       // the draw rotation (including any rotation offset)
        public Vector2 Origin;
        public float Scale;
        public SpriteEffects Effects;
        public Vector2 Size;         // the drawn texture's size in pixels

        public Vector2 TexelToWorld(Vector2 texel)
        {
            Vector2 quad = Flip(texel);
            return Position + (quad - Origin).RotatedBy(Rotation) * Scale;
        }

        public Vector2 WorldToTexel(Vector2 world)
        {
            Vector2 quad = (world - Position).RotatedBy(-Rotation) / Math.Max(0.0001f, Scale) + Origin;
            return Flip(quad);
        }

        // The flips swap a texel and its place in the quad, and swapping twice gives the original, so one function does both directions.
        Vector2 Flip(Vector2 point)
        {
            if ((Effects & SpriteEffects.FlipHorizontally) != 0)
                point.X = Size.X - point.X;
            if ((Effects & SpriteEffects.FlipVertically) != 0)
                point.Y = Size.Y - point.Y;
            return point;
        }
    }

    /// <summary>
    /// Reads a PNG's alpha once and answers "is this point of the sprite opaque" (used to clip metaballs to a hand-drawn shape, like the
    /// Night's Edge's edge mask). The mask is matched to the sprite by RELATIVE position, so it works when its canvas is not exactly the
    /// sprite's size, as long as the shape lines up. When the mask file does not exist the sprite itself is the mask (RequestIfExists logs a
    /// warning instead of crashing). Client only.
    /// </summary>
    public class MaskSampler
    {
        readonly bool[] opaque;
        readonly int width;
        readonly int height;
        readonly Dictionary<int, Vector2[]> anchorCache = new Dictionary<int, Vector2[]>();

        /// <summary>The centers of every opaque pixel, in the mask's own pixel coordinates.</summary>
        public readonly Vector2[] OpaqueTexels;

        static readonly Dictionary<string, MaskSampler> loaded = new Dictionary<string, MaskSampler>();

        MaskSampler(Texture2D texture)
        {
            width = Math.Max(1, texture.Width);
            height = Math.Max(1, texture.Height);
            opaque = new bool[width * height];
            try
            {
                Color[] data = new Color[width * height];
                texture.GetData(data);
                for (int i = 0; i < data.Length; i++)
                    opaque[i] = data[i].A > 20;
            }
            catch (Exception)
            {
                for (int i = 0; i < opaque.Length; i++)
                    opaque[i] = true;
            }

            List<Vector2> texels = new List<Vector2>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (opaque[y * width + x])
                        texels.Add(new Vector2(x + 0.5f, y + 0.5f));
                }
            }
            OpaqueTexels = texels.ToArray();
        }

        /// <summary>The sampler of the PNG at 'assetPath' (a mod asset path without extension); 'fallback' (the sprite) when the file is missing.</summary>
        public static MaskSampler Load(string assetPath, Texture2D fallback)
        {
            if (loaded.TryGetValue(assetPath, out MaskSampler sampler))
                return sampler;

            Texture2D texture = fallback;
            if (ModContent.RequestIfExists<Texture2D>(assetPath, out Asset<Texture2D> asset))
                texture = asset.Value;
            sampler = new MaskSampler(texture);
            loaded[assetPath] = sampler;
            return sampler;
        }

        /// <summary>A point in the mask's pixel coordinates as a point of the sprite (both cover the same shape).</summary>
        public Vector2 ToSprite(Vector2 maskTexel, Vector2 spriteSize)
        {
            return new Vector2(maskTexel.X * spriteSize.X / width, maskTexel.Y * spriteSize.Y / height);
        }

        /// <summary>Is the world point on an opaque pixel of the mask, with the sprite drawn as 'sprite' says?</summary>
        public bool Contains(SpriteTransform sprite, Vector2 world)
        {
            Vector2 texel = sprite.WorldToTexel(world);
            int x = (int)Math.Floor(texel.X * width / Math.Max(1f, sprite.Size.X));
            int y = (int)Math.Floor(texel.Y * height / Math.Max(1f, sprite.Size.Y));
            if (x < 0 || y < 0 || x >= width || y >= height)
                return false;
            return opaque[y * width + x];
        }

        /// <summary>
        /// 'bins' points spread along the blade (a vanilla sword sprite: hilt bottom-left, tip top-right): the middle of the opaque pixels in
        /// each slice from the hilt to the tip. Slices with no opaque pixel are left out. In the mask's pixel coordinates.
        /// </summary>
        public Vector2[] Anchors(int bins)
        {
            bins = Math.Max(1, bins);
            if (anchorCache.TryGetValue(bins, out Vector2[] cached))
                return cached;

            Vector2[] sum = new Vector2[bins];
            int[] count = new int[bins];
            for (int i = 0; i < OpaqueTexels.Length; i++)
            {
                Vector2 texel = OpaqueTexels[i];
                float along = (texel.X / width + (1f - texel.Y / height)) * 0.5f;   // 0 at the hilt corner, 1 at the tip corner
                int bin = Math.Min(bins - 1, Math.Max(0, (int)(along * bins)));
                sum[bin] += texel;
                count[bin]++;
            }

            List<Vector2> anchors = new List<Vector2>();
            for (int i = 0; i < bins; i++)
            {
                if (count[i] > 0)
                    anchors.Add(sum[i] / count[i]);
            }
            Vector2[] result = anchors.ToArray();
            anchorCache[bins] = result;
            return result;
        }
    }
}