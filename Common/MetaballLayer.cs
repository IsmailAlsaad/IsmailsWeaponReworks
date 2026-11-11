using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// One set of merging round things drawn with one MetaballStyle. Get it with MetaballLayers.Of&lt;YourStyle&gt;() (it is created on first use and
    /// ticked and drawn for you). Two kinds of things go in:
    ///  - Spawn(...): a PARTICLE that lives on its own in the world (moves by the style, shrinks and cools, then disappears).
    ///  - Blob(...): a thing that exists for THIS TICK only (call it every tick from an AI: an orb's body, a core). It follows its owner exactly.
    /// Drawn after the tiles, behind players, enemies and projectiles. Purely visual, client only, never synced: every machine spawns its own
    /// from the state it already has. Never gives damage or buffs.
    /// </summary>
    public class MetaballLayer
    {
        public readonly MetaballStyle Style;

        struct Blob
        {
            public Vector2 Position;
            public float Radius;
            public float Heat;
            public int Stamp;
        }

        readonly List<MetaballParticle> particles = new List<MetaballParticle>();
        readonly List<Blob> blobs = new List<Blob>();
        int tick;

        // Scratch space for one frame of drawing: the summed field and the summed (field x heat) of every touched grid cell.
        readonly Dictionary<long, int> cellIndex = new Dictionary<long, int>();
        float[] fieldSum = new float[2048];
        float[] heatSum = new float[2048];
        int[] cellX = new int[2048];
        int[] cellY = new int[2048];
        int cellCount;

        public MetaballLayer(MetaballStyle style)
        {
            Style = style;
        }

        public bool IsEmpty => particles.Count == 0 && blobs.Count == 0;

        /// <summary>A particle that lives on its own. 'size' is its radius at birth in px, 'lifeTicks' how long it lives.</summary>
        public void Spawn(Vector2 position, Vector2 velocity, int lifeTicks, float size)
        {
            if (Main.dedServ)
                return;

            particles.Add(new MetaballParticle
            {
                Position = position,
                Velocity = velocity,
                Life = Math.Max(2, lifeTicks),
                Size = size,
                Wobble = Main.rand.NextFloat(MathHelper.TwoPi)
            });
            if (particles.Count > Math.Max(1, Style.MaxParticles))
                particles.RemoveAt(0);
        }

        /// <summary>A thing for this tick only. Call it every tick (from AI) for as long as the thing exists.</summary>
        public void AddBlob(Vector2 center, float radius, float heat)
        {
            if (Main.dedServ)
                return;

            blobs.Add(new Blob { Position = center, Radius = radius, Heat = heat, Stamp = tick });
        }

        // Once per tick (MetaballLayers). Blobs added during this tick's AI stay for the draw after it; older ones go.
        public void Tick(int newTick)
        {
            blobs.RemoveAll(b => b.Stamp < newTick - 1);
            tick = newTick;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                MetaballParticle particle = particles[i];
                particle.Age++;
                if (particle.Age >= particle.Life)
                {
                    particles.RemoveAt(i);
                    continue;
                }
                Style.Step(particle);
            }
        }

        public void Clear()
        {
            particles.Clear();
            blobs.Clear();
            cellIndex.Clear();
        }

        // Set by MetaballLayers every frame; DrawInBatch draws at most once per frame (several projectiles may ask).
        public bool DrawnThisFrame;

        public void Draw()
        {
            DrawCells(true);
        }

        /// <summary>
        /// For a style with DrawInProjectile: call from a projectile's PreDraw (a spritebatch is already running there, so this does not
        /// begin its own). Draws the whole layer once per frame, in the projectile's draw order, i.e. in front of enemies.
        /// </summary>
        public void DrawInBatch()
        {
            if (DrawnThisFrame)
                return;
            DrawnThisFrame = true;
            DrawCells(false);
        }

        void DrawCells(bool beginBatch)
        {
            if (Main.dedServ || IsEmpty)
                return;

            float grid = Math.Max(1f, Style.Grid);
            float merge = Math.Max(1.2f, Style.Merge);
            Vector2 screen = Main.screenPosition;
            float margin = 160f;

            cellIndex.Clear();
            cellCount = 0;
            for (int i = 0; i < particles.Count; i++)
            {
                MetaballParticle particle = particles[i];
                float life = particle.Age / (float)particle.Life;
                float radius = particle.Size * Style.SizeOverLife(life);
                if (radius < 0.8f || !OnScreen(particle.Position, radius * merge + margin * 0.25f, screen))
                    continue;
                Accumulate(particle.Position, radius * merge, Style.HeatOverLife(life), grid);
            }
            for (int i = 0; i < blobs.Count; i++)
            {
                Blob blob = blobs[i];
                if (blob.Radius < 0.8f || !OnScreen(blob.Position, blob.Radius * merge + margin * 0.25f, screen))
                    continue;
                Accumulate(blob.Position, blob.Radius * merge, blob.Heat, grid);
            }
            if (cellCount == 0)
                return;

            // A lone thing has its field at 'threshold' exactly one radius from its middle, so its blob is as big as its radius says;
            // where fields overlap the sum is higher and the blob swells and joins the others.
            float threshold = (float)Math.Pow(1f - 1f / (merge * merge), 2.0);
            float outline = threshold + Math.Max(0.02f, Style.OutlineWidth);
            float glowFrom = threshold * 0.45f;

            Color outlineColor = Style.GetOutline();
            Color glowColor = Style.GetGlow();
            glowColor = new Color(glowColor.R, glowColor.G, glowColor.B, 0) * Style.GlowStrength;   // alpha 0 = additive
            float bands = Math.Max(2f, Style.Bands);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int originX = (int)Math.Floor(screen.X);
            int originY = (int)Math.Floor(screen.Y);
            int size = (int)Math.Round(grid);

            if (beginBatch)
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < cellCount; i++)
            {
                float f = fieldSum[i];
                if (f < glowFrom)
                    continue;

                Color color;
                if (f < threshold)
                {
                    color = glowColor;   // the faint additive halo around a blob
                }
                else if (f < outline)
                {
                    color = outlineColor;   // the contour of the merged shape
                }
                else
                {
                    // inside: the heat of everything that overlaps here, averaged and posterized into hard bands
                    float heat = MathHelper.Clamp(heatSum[i] / f, 0f, 1f);
                    if (Style.EdgeCool > 0f)
                        heat *= MathHelper.Clamp((f - outline) / Style.EdgeCool, 0f, 1f);
                    heat = (float)Math.Floor(heat * bands) / (bands - 1f);
                    color = Style.GetFill(MathHelper.Clamp(heat, 0f, 1f));
                }
                Main.spriteBatch.Draw(pixel, new Rectangle(cellX[i] * size - originX, cellY[i] * size - originY, size, size), color);
            }
            if (beginBatch)
                Main.spriteBatch.End();
        }

        static bool OnScreen(Vector2 position, float margin, Vector2 screen)
        {
            return position.X > screen.X - margin && position.Y > screen.Y - margin && position.X < screen.X + Main.screenWidth + margin && position.Y < screen.Y + Main.screenHeight + margin;
        }

        // Adds one thing's field: 0 outside 'support' px from its middle, rising smoothly to 1 at the middle. 'heat' 1 = hot, 0 = cold.
        void Accumulate(Vector2 position, float support, float heat, float grid)
        {
            float support2 = support * support;
            int minX = (int)Math.Floor((position.X - support) / grid);
            int maxX = (int)Math.Floor((position.X + support) / grid);
            int minY = (int)Math.Floor((position.Y - support) / grid);
            int maxY = (int)Math.Floor((position.Y + support) / grid);
            for (int cx = minX; cx <= maxX; cx++)
            {
                float dx = (cx + 0.5f) * grid - position.X;
                for (int cy = minY; cy <= maxY; cy++)
                {
                    float dy = (cy + 0.5f) * grid - position.Y;
                    float d2 = dx * dx + dy * dy;
                    if (d2 >= support2)
                        continue;
                    float u = 1f - d2 / support2;
                    float f = u * u;

                    long key = ((long)cx << 32) ^ (uint)cy;
                    if (!cellIndex.TryGetValue(key, out int index))
                    {
                        index = cellCount++;
                        if (index >= fieldSum.Length)
                        {
                            int bigger = fieldSum.Length * 2;
                            Array.Resize(ref fieldSum, bigger);
                            Array.Resize(ref heatSum, bigger);
                            Array.Resize(ref cellX, bigger);
                            Array.Resize(ref cellY, bigger);
                        }
                        cellIndex[key] = index;
                        cellX[index] = cx;
                        cellY[index] = cy;
                        fieldSum[index] = 0f;
                        heatSum[index] = 0f;
                    }
                    fieldSum[index] += f;
                    heatSum[index] += f * heat;
                }
            }
        }
    }
}