using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// Draws the Blade of Grass vines as part of the player, before every vanilla player layer, so the vines are BEHIND the player's body
    /// (and, because the player is drawn after the enemies, still in front of them). The chain is added as sprite stamps (a player layer can
    /// only queue sprites, not the mesh the coils use). The sword and its trail are still drawn by the blade projectile, in front of the
    /// player. Visual only: every machine draws the vines of every player that holds the blade.
    /// </summary>
    public class BladeOfGrassVineLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new BeforeParent(PlayerDrawLayers.Skin);
        }

        static BladeOfGrassBladeProjectile HeldBlade(Player player)
        {
            int index = player.heldProj;
            if (index < 0 || index >= Main.maxProjectiles)
                return null;
            Projectile projectile = Main.projectile[index];
            if (!projectile.active || projectile.owner != player.whoAmI)
                return null;
            return projectile.ModProjectile as BladeOfGrassBladeProjectile;
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f || drawInfo.drawPlayer.dead)
                return false;
            return HeldBlade(drawInfo.drawPlayer) != null;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            HeldBlade(drawInfo.drawPlayer)?.AddVineDrawData(ref drawInfo);
        }
    }
}