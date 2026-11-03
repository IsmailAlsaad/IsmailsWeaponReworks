using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.FlowerOfFire
{
    /// <summary>
    /// Test host for the shader fireball: the vanilla Flower of Fire shoots PixelFireballProjectile instead of its Ball of Fire.
    /// Recipe, drops, mana cost, damage and use time stay vanilla. Shoot is not overridden: the default shot spawns item.shoot on the
    /// attacking player's own client and tModLoader syncs it.
    /// </summary>
    public class FlowerOfFireGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.FlowerofFire;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<PixelFireballProjectile>();
            item.shootSpeed = FlowerOfFireTuning.Speed;
        }
    }
}