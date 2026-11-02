using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>
    /// Overhauls the vanilla Starfury: the item itself fires nothing. Left click starts a StarfurySwingProjectile that swings the sword
    /// (alternating down and up), right click starts the same projectile in its call pose, which sends a star falling from offscreen
    /// through the cursor point. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class StarfuryGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Starfury;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<StarfurySwingProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the sword
            item.channel = false;
            item.autoReuse = true;                // hold the button to keep swinging
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        public override bool AltFunctionUse(Item item, Player player)
        {
            return true; // right click calls a star
        }

        // One swing or call at a time; calls also have a short cooldown.
        public override bool CanUseItem(Item item, Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<StarfurySwingProjectile>()] > 0)
                return false;
            if (player.altFunctionUse == 2)
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<StarfuryStarProjectile>()] >= StarfuryTuning.MaxStars)
                    return false;
                return Main.GameUpdateCount - player.GetModPlayer<StarfuryPlayer>().LastCallTick >= (uint)StarfuryTuning.CallCooldownTicks;
            }
            return true;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            StarfuryPlayer state = player.GetModPlayer<StarfuryPlayer>();
            bool call = player.altFunctionUse == 2;
            bool combo = state.LastPhase >= 0 && Main.GameUpdateCount - state.LastEndTick <= (uint)StarfuryTuning.ComboTicks;
            float startArm = combo ? state.LastEndArm : MathHelper.ToRadians(StarfuryTuning.RestArmDeg);

            StarfurySwingProjectile.Phase phase;
            if (call)
            {
                phase = StarfurySwingProjectile.Phase.Call;
                state.LastCallTick = Main.GameUpdateCount;
                Vector2 offset = Main.MouseWorld - player.MountedCenter;
                if (offset.Length() > StarfuryTuning.CallMaxRange)
                    offset = offset.SafeNormalize(Vector2.UnitX * player.direction) * StarfuryTuning.CallMaxRange;
                state.CallTarget = player.MountedCenter + offset;
            }
            else
            {
                // Alternate: a swing right after a down swing goes up; anything else (fresh combo, after a call, after an up swing) goes down.
                bool afterDown = combo && state.LastPhase == (int)StarfurySwingProjectile.Phase.SwingDown;
                phase = afterDown ? StarfurySwingProjectile.Phase.SwingUp : StarfurySwingProjectile.Phase.SwingDown;
            }

            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, (float)phase, startArm);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Starfury1", "Right click to call a star down at the cursor"));
            tooltips.Add(new TooltipLine(Mod, "Starfury2", "Hit a falling star with a swing to bat it away; every hit doubles it"));
        }
    }
}