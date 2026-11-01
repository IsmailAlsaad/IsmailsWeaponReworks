using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Flamerang
{
    /// <summary>
    /// Overhauls the vanilla Flamarang: it throws FlamerangProjectile at the cursor like the Ice Boomerang, only one can be in the
    /// air, and instead of coming back it explodes in fire and turns into molten boomerangs. Recipe, drops and stats stay vanilla.
    /// </summary>
    public class FlamerangGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Flamarang;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<FlamerangProjectile>();
            item.shootSpeed = FlamerangTuning.ThrowSpeed;
            item.autoReuse = true;
        }

        // Only one thrown boomerang at a time; the molten ones it turns into do not block the next throw.
        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<FlamerangProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the throwing player's own client. The throw range (ai[1]) is the distance to the cursor, clamped.
            float range = MathHelper.Clamp(Vector2.Distance(position, Main.MouseWorld), FlamerangTuning.MinThrowRange, FlamerangTuning.MaxThrowRange);
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, range);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "Flamerang1", "Thrown at your cursor, it explodes at the end of its range and sends two molten boomerangs arcing back to you"));
            //tooltips.Add(new TooltipLine(Mod, "Flamerang2", "Hitting an enemy or a wall explodes it into falling obsidian shards, and it returns as a molten boomerang"));
            //tooltips.Add(new TooltipLine(Mod, "Flamerang3", "Molten boomerangs leave a trail of fire that burns enemies"));
        }
    }
}