using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.IceBlade
{
    /// <summary>
    /// Overhauls the vanilla Ice Blade: pressing the attack button starts IceBladeSwingProjectile, a held projectile that draws
    /// the blade and runs the whole mechanic (tap = plain swing, hold = charge, release = slash wave, too long = frozen hand).
    /// Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class IceBladeGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.IceBlade;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<IceBladeSwingProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the blade
            item.channel = true;                  // player.channel tells the projectile when the button is released
            item.autoReuse = false;
        }

        // One swing at a time.
        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<IceBladeSwingProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(item, player, target, hit, damageDone);
            //target.AddBuff(BuffID.Frostburn, IceBladeTuning.FreezeDebuffTicks);
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "IceBlade1", "Tap for a plain swing, hold to charge"));
            //tooltips.Add(new TooltipLine(Mod, "IceBlade2", "Release on one of the first three clicks to send out an ice wave; later clicks are stronger"));
            //tooltips.Add(new TooltipLine(Mod, "IceBlade3", "Hold past the fourth click and your hand freezes"));
        }
    }
}