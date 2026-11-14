using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>
    /// Overhauls the vanilla Blood Butcherer: the item itself swings nothing. Left click starts a BloodButchererSwingProjectile (the held,
    /// jawed blade): it lunges along the aim, the two halves open like a mouth and bite. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class BloodButchererGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.BloodButcherer;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<BloodButchererSwingProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the sword
            item.channel = false;
            item.autoReuse = true;                // hold the button to keep biting
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        // One bite at a time.
        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<BloodButchererSwingProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else. The aim is captured
            // here, once, and travels in the spawn packet (ai[2]) so every machine poses the same bite. It is kept in the player's own
            // frame (mirrored when gravity is flipped), like the Starfury's.
            Vector2 toMouse = Main.MouseWorld - player.MountedCenter;
            float aim = toMouse.LengthSquared() > 1f ? toMouse.ToRotation() : (player.direction >= 0 ? 0f : MathHelper.Pi);
            if (player.gravDir == -1f)
                aim = -aim;

            // The timeline speed (the owner's melee attack speed) travels in the spawn velocity's X: the projectile never moves by its
            // velocity, reads it back on its first tick on every machine and clears it, so no machine has to guess another player's stats.
            float speed = MathHelper.Clamp(1f + (player.GetTotalAttackSpeed(DamageClass.Melee) - 1f) * BloodButchererTuning.AttackSpeedInfluence, BloodButchererTuning.SpeedScaleMin, BloodButchererTuning.SpeedScaleMax);

            // ai[0] = the catch decision (0 = undecided yet), ai[1] = caught enemy index + 1 (0 = none), ai[2] = the aim angle.
            Projectile.NewProjectile(source, player.MountedCenter, new Vector2(speed, 0f), type, damage, knockback, player.whoAmI, 0f, 0f, aim);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "BloodButcherer0", "Throws small enemies"));
            tooltips.Add(new TooltipLine(Mod, "BloodButcherer1", "Hold to keep biting"));
        }
    }
}