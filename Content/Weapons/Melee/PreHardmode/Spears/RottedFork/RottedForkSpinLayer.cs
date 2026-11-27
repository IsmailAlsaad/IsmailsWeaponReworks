using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Spears.RottedFork
{
    /// <summary>
    /// Draws the spinning Rotted Fork as part of the player, just before the front arm and hand, so the hand is drawn OVER the shaft (the
    /// fork is behind the hand). The spin projectile still draws the trail; this layer only queues the fork sprites. Visual only: every
    /// machine draws the fork of every player who is spinning one.
    /// </summary>
    public class RottedForkSpinLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new BeforeParent(PlayerDrawLayers.ArmOverItem);
        }

        static RottedForkSpinProjectile Spin(Player player)
        {
            int index = player.heldProj;
            if (index < 0 || index >= Main.maxProjectiles)
                return null;
            Projectile projectile = Main.projectile[index];
            if (!projectile.active || projectile.owner != player.whoAmI)
                return null;
            return projectile.ModProjectile as RottedForkSpinProjectile;
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f || drawInfo.drawPlayer.dead)
                return false;
            return Spin(drawInfo.drawPlayer) != null;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Spin(drawInfo.drawPlayer)?.AddForkDrawData(ref drawInfo);
        }
    }
}