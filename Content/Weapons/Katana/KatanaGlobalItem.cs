using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Katana
{
    /// <summary>
    /// Overhauls the vanilla Katana: the item itself swings nothing. While it is held, a KatanaStanceProjectile keeps it in the sheathed
    /// pose; a click throws the blade (KatanaBladeProjectile) along the aim line, and releasing the button teleports the player to it
    /// and cuts everything on the way. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class KatanaGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Katana;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<KatanaBladeProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the projectiles pose the arm themselves
            item.noMelee = true;                  // the projectiles are the weapon
            item.noUseGraphic = true;             // the projectiles draw the sword
            item.channel = false;
            item.autoReuse = false;               // one throw per press
            item.UseSound = null;                 // the projectiles play their own sounds
        }

        // One throw at a time, and not while the cooldown runs.
        public override bool CanUseItem(Item item, Player player)
        {
            if (!player.GetModPlayer<KatanaPlayer>().Ready)
                return false;
            return !KatanaPlayer.BladeFlying(player.whoAmI); // a blade stuck in a tile or falling does not block the next throw
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            KatanaBladeProjectile.Throw(player, source);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Katana1", "Hold to throw the blade, let go or hit a wall to dash to it and cut everything on the way"));
        }
    }
}