using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The shared part of every yoyo's GlobalItem (each yoyo has its own subclass in its folder that names its vanilla item): the item
    /// itself throws nothing. While it is held, the yoyo's projectile (spawned by YoyoPlayer) is the yoyo: it idles in the hand, and the
    /// left click runs the yoyo's trick on its rope. Recipe, drops and base stats stay vanilla (item.shoot stays the vanilla yoyo
    /// projectile: its range sets the rope length).
    /// </summary>
    public abstract class YoyoGlobalItem : GlobalItem
    {
        protected abstract int YoyoItemType { get; }

        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == YoyoItemType;
        }

        public override void SetDefaults(Item item)
        {
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the yoyo projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the yoyo
            item.channel = false;
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        // The item is never "used": the yoyo watches the button itself.
        public override bool CanUseItem(Item item, Player player)
        {
            return false;
        }

    }
}