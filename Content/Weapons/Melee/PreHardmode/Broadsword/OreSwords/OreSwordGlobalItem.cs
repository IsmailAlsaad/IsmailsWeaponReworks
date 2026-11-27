using IsmailsWeaponReworks.Common.Rework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.OreSwords
{
    /// <summary>
    /// Overhauls the eight ore broadswords (Copper, Tin, Iron, Lead, Silver, Tungsten, Gold and Platinum; vanilla item types): the item itself
    /// swings nothing. While it is held, an OreSwordBladeProjectile (spawned by OreSwordPlayer) holds the sword for as long as it is in the
    /// hand and swings it while the left button is down: exactly the wooden swords' swing and idle pose, with a faint trail in the metal's
    /// color and a small flare on every hit. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class OreSwordGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return ReworkToggle.Applies(entity.type, OreSwordMetals.ColumnOf(entity.type) >= 0);
        }

        public override void SetDefaults(Item item)
        {
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the sword
            item.channel = false;
            item.UseSound = null;                 // the projectile plays its own sound
        }

        // The item is never "used": the held blade watches the button itself.
        public override bool CanUseItem(Item item, Player player)
        {
            return false;
        }
    }
}