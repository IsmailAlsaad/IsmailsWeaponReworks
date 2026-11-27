using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Runs every idle sword (HeldIdleSwordProjectile) once per tick AFTER all projectiles have updated. A swing projectile that ends this
    /// tick is then already gone when the idle sword decides whether to show itself, so the sword and the arm are never missing for one frame
    /// between two swings, whatever projectile slots the two happen to have.
    /// </summary>
    public class HeldIdleSwordSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.ModProjectile is HeldIdleSwordProjectile idle)
                    idle.Tick();
            }
        }
    }
}