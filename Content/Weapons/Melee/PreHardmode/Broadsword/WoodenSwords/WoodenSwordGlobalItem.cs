using IsmailsWeaponReworks.Common.Rework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords
{
    /// <summary>
    /// Overhauls the eight wooden swords (Wooden, Boreal, Palm, Rich Mahogany, Shadewood, Ebonwood, Ashwood and Cactus; vanilla item types): the item
    /// itself swings nothing. While it is held, a WoodenSwordBladeProjectile (spawned by WoodenSwordPlayer) holds the sword for as long as it is
    /// in the hand and swings it while the left button is down (the shared charged blade, as the plain swing of the Light's Bane). Every true
    /// melee hit lodges a splinter. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class WoodenSwordGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return ReworkToggle.Applies(entity.type, WoodenSwordWoods.ColumnOf(entity.type) >= 0);
        }

        public override void SetDefaults(Item item)
        {
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the sword
            item.channel = false;
            item.UseSound = null;                 // the projectiles play their own sounds
        }

        // The item is never "used": the held blade watches the button itself.
        public override bool CanUseItem(Item item, Player player)
        {
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //WoodSpec spec = WoodenSwordWoods.Spec(WoodenSwordWoods.ColumnOf(item.type));
            //float seconds = spec.LifeTicks / 60f;
            //tooltips.Add(new TooltipLine(Mod, "WoodenSword1", "Hits lodge splinters in enemies (up to " + spec.MaxSplinters + ")"));
            //tooltips.Add(new TooltipLine(Mod, "WoodenSword2", "Each splinter hurts for " + WoodenSwordTuning.DamagePerHit + " damage per second for " + seconds.ToString("0.#") + " seconds"));
        }
    }
}