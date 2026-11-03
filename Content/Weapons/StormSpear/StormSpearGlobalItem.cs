using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.StormSpear
{
    /// <summary>
    /// Overhauls the vanilla Storm Spear (ItemID.ThunderSpear): the item itself throws nothing. Left click starts a StormSpearThrustProjectile:
    /// one strong thrust that dashes the player toward the cursor, piercing and electrifying everything on the way, immune while it lasts.
    /// Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class StormSpearGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.ThunderSpear;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<StormSpearThrustProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.channel = false;
            item.autoReuse = true;
            item.UseSound = null;
        }

        public override bool CanUseItem(Item item, Player player)
        {
            return ThrustPlayer.CanUse(player, ItemID.ThunderSpear, ModContent.ProjectileType<StormSpearThrustProjectile>());
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Owner's client only. ai[0] = seed (unused: no angle variance), ai[1] = 0, ai[2] = the aim in the player's own frame.
            int thrustDamage = Math.Max(1, (int)(damage * StormSpearTuning.DamageMultiplier));
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, thrustDamage, knockback, player.whoAmI, Main.rand.Next(10000), 0f, HeldAim.Capture(player));
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "StormSpear1", "Dashes toward the cursor, piercing and electrifying everything"));
            //tooltips.Add(new TooltipLine(Mod, "StormSpear2", "You cannot be hurt during the dash or for half a second after it"));
        }
    }
}