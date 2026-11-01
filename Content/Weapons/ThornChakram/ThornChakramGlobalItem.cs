using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.ThornChakram
{
    /// <summary>
    /// Overhauls the vanilla Thorn Chakram: it throws ThornChakramProjectile, which flies and returns like a regular boomerang while
    /// spitting rings of stingers, latches onto the first enemy it hits, shreds it and explodes into piercing stingers. Only one can be in
    /// the air. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class ThornChakramGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.ThornChakram;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<ThornChakramProjectile>();
            item.shootSpeed = ThornChakramTuning.ThrowSpeed;
            item.autoReuse = true;
        }

        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<ThornChakramProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the throwing player's own client; the projectile is then synced to everyone else.
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, ThornChakramTuning.ThrowRange);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "ThornChakram1", "Spits rings of stingers while it spins"));
            //tooltips.Add(new TooltipLine(Mod, "ThornChakram2", "Latches onto the first enemy it hits, shreds it, then explodes into piercing stingers"));
        }
    }
}