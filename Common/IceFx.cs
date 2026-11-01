using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Ice dust and pixel drawing shared by the ice weapons. Burst, Ring and Gather were moved here from IceBoomerangFx when the
    /// Ice Blade needed the same helpers (vanilla dust only).
    /// </summary>
    public static class IceFx
    {
        public static void Burst(Vector2 center, int count, float speed, float scale)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(speed, speed);
                int dustType = Main.rand.NextBool(3) ? DustID.IceTorch : DustID.Ice;
                Dust dust = Dust.NewDustPerfect(center, dustType, velocity, 100, default, scale * Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }
        }

        public static void Ring(Vector2 center, int count, float radius, float outwardSpeed)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Vector2 direction = angle.ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(center + direction * radius, DustID.IceTorch, direction * outwardSpeed, 100, default, 1.3f);
                dust.noGravity = true;
            }
        }

        // Frost gathering toward a point (used while the boomerang settles).
        public static void Gather(Vector2 center, float radius)
        {
            if (Main.dedServ)
                return;
            Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
            Dust dust = Dust.NewDustPerfect(center + offset, DustID.Ice, -offset * 0.12f, 100, default, 1.1f);
            dust.noGravity = true;
        }

        // A square on the world's pixel grid (size is rounded to whole grid cells), drawn from the 1x1 magic pixel. Pass a color
        // with alpha 0 for additive glow.
        public static void DrawPixel(Vector2 worldCenter, float size, Color color, float grid)
        {
            grid = Math.Max(1f, grid);
            size = Math.Max(grid, (float)Math.Round(size / grid) * grid);
            Vector2 corner = worldCenter - new Vector2(size * 0.5f);
            corner = new Vector2((float)Math.Floor(corner.X / grid) * grid, (float)Math.Floor(corner.Y / grid) * grid);
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, corner - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, 0f, Vector2.Zero, size, SpriteEffects.None, 0);
        }
    }
}