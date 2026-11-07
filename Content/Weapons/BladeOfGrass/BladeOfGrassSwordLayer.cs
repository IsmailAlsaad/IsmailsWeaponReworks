using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BladeOfGrass
{
    /// <summary>
    /// Draws the held Blade of Grass as part of the player (just before the front arm is drawn over the item), so the hand covers the handle
    /// instead of the sword floating in front of the arm. The vines are still drawn by the projectile. Visual only: every machine draws the
    /// sword of every player that has a BladeOfGrassHoldProjectile in the hand.
    /// </summary>
    public class BladeOfGrassSwordLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new BeforeParent(PlayerDrawLayers.ArmOverItem);
        }

        static BladeOfGrassHoldProjectile HeldBlade(Player player)
        {
            int index = player.heldProj;
            if (index < 0 || index >= Main.maxProjectiles)
                return null;
            Projectile projectile = Main.projectile[index];
            if (!projectile.active || projectile.owner != player.whoAmI)
                return null;
            return projectile.ModProjectile as BladeOfGrassHoldProjectile;
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f || drawInfo.drawPlayer.dead)
                return false;
            BladeOfGrassHoldProjectile blade = HeldBlade(drawInfo.drawPlayer);
            return blade != null && blade.DrawsSword;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            BladeOfGrassHoldProjectile blade = HeldBlade(drawInfo.drawPlayer);
            if (blade != null && blade.DrawsSword)
                blade.AddSwordDrawData(ref drawInfo);
        }
    }
}