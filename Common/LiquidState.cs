using Terraria;

namespace IsmailsWeaponReworks.Common
{
    public enum LiquidKind
    {
        None,
        Water,
        Lava,
        Honey,
        Shimmer
    }

    /// <summary>
    /// Questions about the liquid a player stands in, asked the same way by every weapon (the Swordfish's rage, the Trident's gills).
    /// Touching = any part of the player is in any liquid; HeadUnder = the head is under water (the breath check).
    /// </summary>
    public static class LiquidState
    {
        public static bool Touching(Player player)
        {
            return player.wet;
        }

        public static bool HeadUnder(Player player)
        {
            return Collision.DrownCollision(player.position, player.width, player.height, player.gravDir);
        }

        public static LiquidKind Kind(Player player)
        {
            if (!player.wet)
                return LiquidKind.None;
            if (player.lavaWet)
                return LiquidKind.Lava;
            if (player.honeyWet)
                return LiquidKind.Honey;
            if (player.shimmerWet)
                return LiquidKind.Shimmer;
            return LiquidKind.Water;
        }
    }
}