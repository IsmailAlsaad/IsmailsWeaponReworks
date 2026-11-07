using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.BladeOfGrass
{
    /// <summary>Lays sprites along a polyline: the vines (hold projectile) and the coils (spine projectile) both draw this way.</summary>
    public static class BladeOfGrassVineDraw
    {
        // The chain is drawn as ONE triangle strip along the points instead of one rotated sprite per link: at every bend the two quads on
        // each side share their corner vertices (a mitered join), so the links stay connected instead of leaving a gap on the outside of
        // the bend. The texture repeats along the strip. BeginMesh/EndMesh wrap the calls, because the sprite batch must be paused for it.
        static BasicEffect meshEffect;
        static Vector2[] pointBuffer;
        static Vector2[] directionBuffer;
        static VertexPositionColorTexture[] vertexBuffer;

        public static void BeginMesh()
        {
            Main.spriteBatch.End();
        }

        public static void EndMesh()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        // 'scale' goes from startScale at the first point to endScale at the last; trimStart/trimEnd shorten the strip at each end in pixels.
        public static void DrawStrip(Texture2D texture, Vector2[] points, int count, float startScale, float endScale, float opacity, float trimStart, float trimEnd)
        {
            if (count < 3 || opacity <= 0.003f)
                return;
            GraphicsDevice device = Main.graphics.GraphicsDevice;
            if (meshEffect == null || meshEffect.IsDisposed)
                meshEffect = new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true };
            if (pointBuffer == null || pointBuffer.Length < count)
            {
                pointBuffer = new Vector2[count];
                directionBuffer = new Vector2[count];
                vertexBuffer = new VertexPositionColorTexture[count * 2];
            }

            for (int i = 0; i < count; i++)
                pointBuffer[i] = points[i];
            for (int i = 0; i < count - 1; i++)
            {
                Vector2 delta = pointBuffer[i + 1] - pointBuffer[i];
                float length = delta.Length();
                directionBuffer[i] = length > 0.001f ? delta / length : (i > 0 ? directionBuffer[i - 1] : Vector2.UnitX);
            }
            float firstLength = Vector2.Distance(pointBuffer[0], pointBuffer[1]);
            pointBuffer[0] += directionBuffer[0] * Math.Min(trimStart, firstLength * 0.8f);
            float lastLength = Vector2.Distance(pointBuffer[count - 2], pointBuffer[count - 1]);
            pointBuffer[count - 1] -= directionBuffer[count - 2] * Math.Min(trimEnd, lastLength * 0.8f);

            float v = 0f;
            for (int k = 0; k < count; k++)
            {
                Vector2 directionIn = k > 0 ? directionBuffer[k - 1] : directionBuffer[0];
                Vector2 directionOut = k < count - 1 ? directionBuffer[k] : directionBuffer[count - 2];
                Vector2 tangent = directionIn + directionOut;
                tangent = tangent.LengthSquared() < 0.0001f ? directionOut : Vector2.Normalize(tangent);
                Vector2 normal = new Vector2(-tangent.Y, tangent.X);
                Vector2 segmentNormal = new Vector2(-directionOut.Y, directionOut.X);
                float miter = Math.Max(0.5f, Vector2.Dot(normal, segmentNormal)); // 1 on a straight stretch, smaller on a sharp bend (the strip gets wider there)
                float scale = MathHelper.Lerp(startScale, endScale, k / (float)(count - 1));
                float half = texture.Width * 0.5f * scale / miter;
                if (k > 0)
                    v += Vector2.Distance(pointBuffer[k], pointBuffer[k - 1]) / (texture.Height * scale);
                Color color = Lighting.GetColor(pointBuffer[k].ToTileCoordinates()) * opacity;
                Vector2 left = pointBuffer[k] + normal * half - Main.screenPosition;
                Vector2 right = pointBuffer[k] - normal * half - Main.screenPosition;
                vertexBuffer[k * 2] = new VertexPositionColorTexture(new Vector3(left, 0f), color, new Vector2(0f, v));
                vertexBuffer[k * 2 + 1] = new VertexPositionColorTexture(new Vector3(right, 0f), color, new Vector2(1f, v));
            }

            meshEffect.World = Matrix.Identity;
            meshEffect.View = Main.GameViewMatrix.TransformationMatrix;
            meshEffect.Projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -1f, 1f);
            meshEffect.Texture = texture;
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            device.SamplerStates[0] = SamplerState.PointWrap;
            foreach (EffectPass pass in meshEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertexBuffer, 0, count * 2 - 2);
            }
        }

        // A hook head over the end of a vine: its tip is at 'end' (plus HeadForwardPx) and its back lies over the last links of the chain,
        // so nothing floats. The head sprites point up at rotation 0, hence the quarter turn.
        public static void DrawHead(Texture2D head, Vector2 end, float angle, float scale, Color color)
        {
            Vector2 direction = angle.ToRotationVector2();
            Vector2 center = end - direction * (head.Height * 0.5f * scale) + direction * BladeOfGrassTuning.HeadForwardPx;
            Main.EntitySpriteDraw(head, center - Main.screenPosition, null, color, angle + MathHelper.PiOver2, head.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }

        // Resamples the first 'count' points into positions 'spacing' pixels apart along the line (the first one at the start), each with
        // the angle of the line there. The lists are cleared first and reused by the caller, so drawing allocates nothing.
        public static void Resample(Vector2[] points, int count, float spacing, List<Vector2> positions, List<float> angles)
        {
            positions.Clear();
            angles.Clear();
            spacing = MathHelper.Max(1f, spacing);
            float next = 0f;
            for (int k = 1; k < count; k++)
            {
                Vector2 a = points[k - 1];
                Vector2 b = points[k];
                float length = Vector2.Distance(a, b);
                if (length < 0.001f)
                    continue;
                Vector2 direction = (b - a) / length;
                float angle = direction.ToRotation();
                while (next <= length)
                {
                    positions.Add(a + direction * next);
                    angles.Add(angle);
                    next += spacing;
                }
                next -= length;
            }
        }
    }
}