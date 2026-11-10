using System.Collections.Generic;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// Which projectile is which yoyo's: every YoyoProjectile subclass registers its vanilla item in SetStaticDefaults, and YoyoPlayer asks
    /// here which projectile to spawn for the held item (0 = this item is not a reworked yoyo).
    /// </summary>
    public class YoyoRegistry : ModSystem
    {
        static Dictionary<int, int> projectiles = new Dictionary<int, int>();

        public static void Register(int itemType, int projectileType)
        {
            projectiles[itemType] = projectileType;
        }

        public static int ProjectileFor(int itemType)
        {
            return projectiles.TryGetValue(itemType, out int type) ? type : 0;
        }

        public override void Load()
        {
            projectiles = new Dictionary<int, int>();
        }

        public override void Unload()
        {
            projectiles = null;
        }
    }
}