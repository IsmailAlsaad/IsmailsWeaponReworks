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

        // The held blade, or the plant swing that replaces it for a moment (it keeps the player's vines and ungrows them).
        static ModProjectile HeldVines(Player player)
        {
            int index = player.heldProj;
            if (index < 0 || index >= Main.maxProjectiles)
                return null;
            Projectile projectile = Main.projectile[index];
            if (!projectile.active || projectile.owner != player.whoAmI)
                return null;
            if (projectile.ModProjectile is BladeOfGrassBladeProjectile || projectile.ModProjectile is BladeOfGrassPlantSwingProjectile)
                return projectile.ModProjectile;
            return null;
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            if (drawInfo.shadow != 0f || drawInfo.drawPlayer.dead)
                return false;
            return HeldVines(drawInfo.drawPlayer) != null;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            ModProjectile held = HeldVines(drawInfo.drawPlayer);
            if (held is BladeOfGrassBladeProjectile blade)
                blade.AddVineDrawData(ref drawInfo);
            else if (held is BladeOfGrassPlantSwingProjectile swing)
                swing.AddVineDrawData(ref drawInfo);
        }
    }
}