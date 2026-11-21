using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>Small world probes that more than one weapon needs. Extracted from PhasebladePlantedProjectile when the Blade of Grass needed the same ground search.</summary>
    public static class TileProbe
    {
        // The first solid tile (or platform) below the point (x, fromY), looking down 'distance' pixels: false when there is none.
        public static bool FindTileBelow(float x, float fromY, float distance, out Vector2 ground)
        {
            for (float y = fromY; y <= fromY + distance; y += 2f)
            {
                int tileX = (int)(x / 16f);
                int tileY = (int)(y / 16f);
                if (!WorldGen.InWorld(tileX, tileY, 10))
                    break;
                Tile tile = Main.tile[tileX, tileY];
                if (tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                {
                    ground = new Vector2(x, y);
                    return true;
                }
            }
            ground = default;
            return false;
        }
    }
}