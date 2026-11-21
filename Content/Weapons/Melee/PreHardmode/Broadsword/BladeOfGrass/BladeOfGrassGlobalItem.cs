using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using IsmailsWeaponReworks.Common.Rework;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// Overhauls the vanilla Blade of Grass (ItemID.BladeofGrass): the item itself swings nothing. While it is held, a BladeOfGrassBladeProjectile
    /// (spawned by BladeOfGrassPlayer) holds the sword for as long as it is in the hand and swings it while the left button is down (the
    /// shared charged blade, like the Light's Bane). Every swing that hits something makes the three vines growing from the blade longer; the vines
    /// float around the player searching, loop enemies in reach and the looped sections break off as BladeOfGrassSpineProjectile.
    /// Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class BladeOfGrassGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return ReworkToggle.Applies(entity.type, entity.type == ItemID.BladeofGrass);
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
            tooltips.RemoveAll(line => line.Mod == "Terraria" && line.Name.StartsWith("Tooltip"));
            tooltips.Add(new TooltipLine(Mod, "BladeOfGrass0", "Right click to implant your vines in the ground"));
            tooltips.Add(new TooltipLine(Mod, "BladeOfGrass1", "Rooted vines passivly grow on their own"));
        }
    }
}