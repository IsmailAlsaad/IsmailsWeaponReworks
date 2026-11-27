using IsmailsWeaponReworks.Common.Rework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// Overhauls the vanilla Candy Cane Sword: the item itself swings nothing. While it is held, a CandyCaneSwordBladeProjectile (spawned by
    /// CandyCaneSwordPlayer) holds the sword for as long as it is in the hand and swings it while the left button is down (the shared charged blade,
    /// the plain swing of the Light's Bane). Hits make candies (see CandyProjectile). Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class CandyCaneSwordGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return ReworkToggle.Applies(entity.type, entity.type == ItemID.CandyCaneSword);
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