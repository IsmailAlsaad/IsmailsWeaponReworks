using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// Overhauls the vanilla Muramasa: the item itself swings nothing. While it is held, a MuramasaStanceProjectile keeps the sword in a
    /// slow sheathing animation; a click starts a MuramasaSwingProjectile (a plain swing toward the cursor, patch 1) and ends the stance.
    /// Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class MuramasaGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Muramasa;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<MuramasaSwingProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectiles move the arm themselves
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectiles draw the sword
            item.channel = false;
            item.autoReuse = false;               // patch 2 gives holding the button its own meaning
            item.UseSound = null;                 // the projectiles play their own sounds
        }

        // One swing at a time.
        public override bool CanUseItem(Item item, Player player)
        {
            return MuramasaPlayer.Find(player.whoAmI, ModContent.ProjectileType<MuramasaSwingProjectile>()) == null;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            // The swing starts from the pose the stance is in right now, so there is no jump; the stance ends.
            float startArm = MathHelper.ToRadians(MuramasaTuning.ReadyArmDeg);
            float startTilt = MathHelper.ToRadians(MuramasaTuning.ReadyTiltDeg);
            Projectile stance = MuramasaPlayer.Find(player.whoAmI, ModContent.ProjectileType<MuramasaStanceProjectile>());
            if (stance != null)
            {
                if (stance.ModProjectile is MuramasaStanceProjectile stanceState)
                {
                    startArm = stanceState.ArmLocal;
                    startTilt = stanceState.TiltRad;
                }
                stance.Kill();
            }

            // ai[0] = the arm's start angle (facing-local), ai[1] = the blade tilt's start angle, ai[2] = the aim captured at the click.
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, startArm, startTilt, HeldAim.Capture(player));
            return false;
        }
    }
}