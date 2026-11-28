using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// A held projectile that wants its sprite drawn INSIDE the player's layers, just before the front arm and hand, so the hand is drawn
    /// over it (the sprite is behind the hand). Set Player.heldProj to the projectile, implement this, and queue the sprite's DrawData in
    /// AddBehindHandDrawData (positions are screen positions: world position minus Main.screenPosition); the projectile's own PreDraw then
    /// draws only what belongs over the player (trails, ropes). Used by the Rotted Fork's spin and the Combat Wrench.
    /// </summary>
    public interface IBehindHandDrawer
    {
        void AddBehindHandDrawData(ref PlayerDrawSet drawInfo);
    }

    /// <summary>
    /// Runs AddBehindHandDrawData of the player's held projectile, just before the front arm. Visual only: every machine draws the sprite of
    /// every player who holds such a projectile, nothing is synced.
    /// </summary>
    public class BehindHandLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new BeforeParent(PlayerDrawLayers.ArmOverItem);
        }

        static IBehindHandDrawer Drawer(Player player)
        {
            int index = player.heldProj;
            if (index < 0 || index >= Main.maxProjectiles)
                return null;
            Projectile projectile = Main.projectile[index];
            if (!projectile.active || projectile.owner != player.whoAmI)
                return null;
            return projectile.ModProjectile as IBehindHandDrawer;
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f || drawInfo.drawPlayer.dead)
                return false;
            return Drawer(drawInfo.drawPlayer) != null;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Drawer(drawInfo.drawPlayer)?.AddBehindHandDrawData(ref drawInfo);
        }
    }
}