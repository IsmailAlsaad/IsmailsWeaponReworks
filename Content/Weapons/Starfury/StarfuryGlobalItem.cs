using IsmailsWeaponReworks.Common;
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
    /// (alternating down and up, centered on the cursor), right click starts the same projectile in its call pose, which sends a star
    /// falling from offscreen to a point in front of the player. Recipe, drops and base stats stay vanilla.
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
            bool afterSwing = combo && state.LastPhase != (int)StarfurySwingProjectile.Phase.Call;

            // The aim is captured here, once, and travels in the spawn packet (ai[2]) so every machine poses the same swing. It is kept in the
            // player's own frame (mirrored when gravity is flipped), like the arm angles; the whole swing is relative to it.
            Vector2 toMouse = Main.MouseWorld - player.MountedCenter;
            float aim = toMouse.LengthSquared() > 1f ? toMouse.ToRotation() : (player.direction >= 0 ? 0f : MathHelper.Pi);
            if (player.gravDir == -1f)
                aim = -aim;

            StarfurySwingProjectile.Phase phase;
            float startArm;
            if (call)
            {
                phase = StarfurySwingProjectile.Phase.Call;
                state.LastCallTick = Main.GameUpdateCount;
                // The call still faces the cursor's side, but where the star comes down does not depend on the cursor. It starts from the
                // arm's world angle of the previous swing (converted to the facing-local angle the call pose uses).
                int side = Main.MouseWorld.X >= player.MountedCenter.X ? 1 : -1;
                startArm = afterSwing ? HeldSwordPose.ToWorld(side, state.LastEndWorldArm) : MathHelper.ToRadians(StarfuryTuning.CallRestArmDeg);
            }
            else
            {
                // Alternate: a swing right after a down swing goes up; anything else (fresh combo, after a call, after an up swing) goes down.
                bool afterDown = combo && state.LastPhase == (int)StarfurySwingProjectile.Phase.SwingDown;
                phase = afterDown ? StarfurySwingProjectile.Phase.SwingUp : StarfurySwingProjectile.Phase.SwingDown;
                startArm = afterSwing ? state.LastEndArm : MathHelper.ToRadians(StarfuryTuning.RestArmDeg); // relative to the aim
            }

            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, (float)phase, startArm, aim);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Starfury1", "Right click to call a star down in front of you"));
            tooltips.Add(new TooltipLine(Mod, "Starfury2", "Hit a falling star with a swing to bat it at the closest enemy; every hit doubles it"));
        }
    }
}