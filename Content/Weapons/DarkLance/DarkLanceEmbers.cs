using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// The Dark Lance orbs' embers: small round particles that break off the rim of an orb and then live on their own in the WORLD (they are
    /// not anchored to the orb, which keeps moving). They are drawn as metaballs on the CPU, no shader: every ember adds a smooth round
    /// field to the world pixel grid, the fields of overlapping embers add up, and every grid cell whose total is above a threshold is a
    /// filled square. So two embers that come close grow a bridge and become one blob, with one merged outline around both and the colors
    /// blended by temperature (hot at birth, cold at the end). Pixels are DarkLanceTuning.PixelSize world px, like the orb.
    /// MULTIPLAYER: visual only, nothing is synced. Every machine that sees an orb spawns its own embers from the orb's own state; the owner
    /// and the others will not have the same embers, and that is fine. Embers never deal damage. Client only.
    /// </summary>
    public class DarkLanceEmbers : ModSystem
    {
        class Ember
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public int Age;
            public int Life;
            public float Size;
            public float Wobble;
        }

        readonly List<Ember> embers = new List<Ember>();

        // Scratch space for one frame of drawing: the summed field and the summed (field x temperature) of every touched grid cell.
        readonly Dictionary<long, int> cellIndex = new Dictionary<long, int>();
        float[] fieldSum = new float[2048];
        float[] heatSum = new float[2048];
        int[] cellX = new int[2048];
        int[] cellY = new int[2048];
        int cellCount;

        // Called by an orb (every machine, client only): one ember leaves its upper rim. 'orbVelocity' is how far the orb moved this tick.
        public static void Spawn(Vector2 orbCenter, Vector2 orbVelocity)
        {
            if (Main.dedServ)
                return;

            DarkLanceEmbers system = ModContent.GetInstance<DarkLanceEmbers>();
            float angle = MathHelper.ToRadians(Main.rand.NextFloat(-145f, -35f));   // -90 degrees = straight up on screen
            Vector2 direction = angle.ToRotationVector2();
            Ember ember = new Ember
            {
                Position = orbCenter + direction * DarkLanceTuning.OrbUnitPx * 0.4f + Main.rand.NextVector2Circular(2f, 2f),
                Velocity = direction * DarkLanceTuning.EmberKick * Main.rand.NextFloat(0.5f, 1.2f) + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), 0f) + orbVelocity * DarkLanceTuning.EmberInherit,
                Life = Math.Max(4, (int)(DarkLanceTuning.EmberLifeTicks * Main.rand.NextFloat(0.7f, 1.3f))),
                Size = DarkLanceTuning.EmberSize * Main.rand.NextFloat(0.6f, 1.4f),
                Wobble = Main.rand.NextFloat(MathHelper.TwoPi)
            };
            system.embers.Add(ember);
            if (system.embers.Count > Math.Max(1, DarkLanceTuning.EmberMax))
                system.embers.RemoveAt(0);
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gameMenu || Main.gamePaused)
                return;

            for (int i = embers.Count - 1; i >= 0; i--)
            {
                Ember ember = embers[i];
                ember.Age++;
                if (ember.Age >= ember.Life)
                {
                    embers.RemoveAt(i);
                    continue;
                }
                ember.Velocity.Y -= DarkLanceTuning.EmberRise;
                ember.Velocity.X += (float)Math.Sin(ember.Age * 0.25f + ember.Wobble) * DarkLanceTuning.EmberWobble;
                ember.Velocity *= DarkLanceTuning.EmberDrag;
                ember.Position += ember.Velocity;
            }
        }

        public override void PostDrawTiles()
        {
            if (Main.dedServ || embers.Count == 0)
                return;

            float grid = Math.Max(1f, DarkLanceTuning.PixelSize);
            float merge = Math.Max(1.2f, DarkLanceTuning.EmberMerge);
            Vector2 screen = Main.screenPosition;
            float margin = DarkLanceTuning.EmberSize * merge * 2f + 8f;

            cellIndex.Clear();
            cellCount = 0;
            for (int i = 0; i < embers.Count; i++)
            {
                Ember ember = embers[i];
                if (ember.Position.X < screen.X - margin || ember.Position.Y < screen.Y - margin || ember.Position.X > screen.X + Main.screenWidth + margin || ember.Position.Y > screen.Y + Main.screenHeight + margin)
                    continue;

                float life = ember.Age / (float)ember.Life;
                float radius = ember.Size * (1f - life * life);   // shrinks to nothing, slowly at first
                if (radius < 0.8f)
                    continue;
                Accumulate(ember.Position, radius * merge, 1f - life, grid);
            }
            if (cellCount == 0)
                return;

            // A lone ember has its field at 'threshold' exactly one radius from its middle, so its blob is as big as its radius says;
            // where fields overlap the sum is higher and the blob swells and joins the others.
            float threshold = (float)Math.Pow(1f - 1f / (merge * merge), 2.0);
            float outline = threshold + Math.Max(0.02f, DarkLanceTuning.EmberOutline);
            float glowFrom = threshold * 0.45f;

            ThrustPalette palette = DarkLanceTuning.Palette;
            Color outlineColor = Color.Lerp(palette.Halo, Color.Black, 0.35f);
            Color glowColor = DarkLanceFx.Glow(palette.Mid) * DarkLanceTuning.EmberGlow;
            float bands = Math.Max(2f, DarkLanceTuning.ColorBands);
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int originX = (int)Math.Floor(screen.X);
            int originY = (int)Math.Floor(screen.Y);
            int size = (int)Math.Round(grid);

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
                    // inside: the temperature of the embers that overlap here, averaged and posterized into hard bands
                    float temp = MathHelper.Clamp(heatSum[i] / f, 0f, 1f);
                    temp = (float)Math.Floor(temp * bands) / (bands - 1f);
                    temp = MathHelper.Clamp(temp, 0f, 1f);
                    color = temp < 0.5f ? Color.Lerp(palette.Halo, palette.Mid, temp * 2f) : Color.Lerp(palette.Mid, palette.Core, (temp - 0.5f) * 2f);
                }
                Main.spriteBatch.Draw(pixel, new Rectangle(cellX[i] * size - originX, cellY[i] * size - originY, size, size), color);
            }
            Main.spriteBatch.End();
        }

        // Adds one ember's field: 0 outside 'support' px from its middle, rising smoothly to 1 at the middle. 'heat' 1 = hot, 0 = cold.
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

        public override void OnWorldUnload()
        {
            embers.Clear();
        }

        public override void Unload()
        {
            embers.Clear();
            cellIndex.Clear();
        }
    }
}