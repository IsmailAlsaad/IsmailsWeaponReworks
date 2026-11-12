using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Phaseblade
{
    /// <summary>
    /// Overhauls all seven vanilla Phaseblades (blue, red, green, purple, white, yellow, orange): the item itself fires nothing. Left click
    /// starts a PhasebladeSwingProjectile that runs one swing of a combo: swing down, then the arm loops over the head into a second swing
    /// down with a spinning blade, then a swing up with a spinning blade, then it starts over. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class PhasebladeGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return PhasebladeTuning.IsPhaseblade(entity.type);
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<PhasebladeSwingProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the blade
            item.channel = false;
            item.autoReuse = true;                // hold the button to keep swinging
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        public override bool AltFunctionUse(Item item, Player player)
        {
            return true; // right click plants the blade in the ground
        }

        // One swing at a time. A planted (or falling) blade is pulled back by any click (the click is used up by it; a LEFT click also starts the combo
        // when the blade is caught). A plant works anywhere: on the ground it is driven in, in the air the blade is shot straight down.
        public override bool CanUseItem(Item item, Player player)
        {
            // A swing in progress blocks the item, except in its chain window (the last ticks of the strike): then the next swing may start.
            PhasebladeSwingProjectile swing = PhasebladeSwingProjectile.Find(player);
            if (swing != null && !swing.InChainWindow)
                return false;
            if (Main.GameUpdateCount - player.GetModPlayer<PhasebladePlayer>().LastStartTick < 3)
                return false; // a swing started on the last ticks
            // Only a planted blade of the color in the hand is called back: the other colors stay planted, each color plants on its own.
            if (swing == null && PhasebladePlantedProjectile.Has(player, item.type))
            {
                PhasebladePlantedProjectile.PullBack(player, item.type, player.altFunctionUse != 2);
                return false;
            }
            if (player.altFunctionUse == 2)
                return player.gravDir == 1f && !PhasebladePlantedProjectile.IsReturning(player, item.type); // no second plant until the first blade is caught
            return true;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            PhasebladePlayer state = player.GetModPlayer<PhasebladePlayer>();

            // Chained from a swing that is still in its last ticks: end it now (its OnKill records where the arm is, and the next swing starts from
            // that pose in this same tick, so the blade is never missing for a frame; its trail keeps fading by itself).
            PhasebladeSwingProjectile.Find(player)?.Projectile.Kill();

            // RIGHT CLICK: the plant swing. It starts from the arm's pose where the last swing ended (converted to the facing-local angles the plant uses).
            if (player.altFunctionUse == 2)
            {
                int side = Main.MouseWorld.X >= player.Center.X ? 1 : -1;
                bool justSwung = state.LastPhase >= 0 && Main.GameUpdateCount - state.LastEndTick <= (uint)PhasebladeTuning.ComboTicks;
                float plantStart = justSwung ? MathHelper.WrapAngle(HeldSwordPose.ToWorld(side, state.LastEndWorldArm)) : MathHelper.ToRadians(PhasebladeTuning.PlantRestArmDeg);
                Vector2 toCursor = Main.MouseWorld - player.MountedCenter;
                float plantAim = toCursor.LengthSquared() > 1f ? toCursor.ToRotation() : (side == 1 ? 0f : MathHelper.Pi);
                int plantDamage = (int)(damage * PhasebladeTuning.PlantedDamageMultiplier);
                float plantKnockback = knockback * PhasebladeTuning.PlantedKnockbackMultiplier;
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, plantDamage, plantKnockback, player.whoAmI, (float)PhasebladeSwingProjectile.Phase.Plant, plantStart, plantAim);
                state.LastStartTick = Main.GameUpdateCount;
                return false;
            }

            StartCombo(player, source, damage, knockback);
            return false;
        }

        // Starts the next swing of the combo (owner only). Left click uses it through Shoot; a blade that was pulled back by a left click uses it when
        // it is caught.
        public static void StartCombo(Player player, IEntitySource source, int damage, float knockback)
        {
            PhasebladePlayer state = player.GetModPlayer<PhasebladePlayer>();
            int type = ModContent.ProjectileType<PhasebladeSwingProjectile>();
            bool combo = state.LastPhase >= 0 && Main.GameUpdateCount - state.LastEndTick <= (uint)PhasebladeTuning.ComboTicks;

            // The combo: swing down -> loop + swing down (spin) -> swing up (spin) -> swing down ... A fresh combo starts with the first swing down.
            PhasebladeSwingProjectile.Phase phase = PhasebladeSwingProjectile.Phase.SwingDown;
            if (combo)
            {
                if (state.LastPhase == (int)PhasebladeSwingProjectile.Phase.SwingDown)
                    phase = PhasebladeSwingProjectile.Phase.SpinDown;
                else if (state.LastPhase == (int)PhasebladeSwingProjectile.Phase.SpinDown)
                    phase = PhasebladeSwingProjectile.Phase.SpinUp;
            }
            float startArm = combo ? state.LastEndArm : MathHelper.ToRadians(PhasebladeTuning.RestArmDeg); // relative to the aim

            // The aim is captured here, once, and travels in the spawn packet (ai[2]) so every machine poses the same swing. It is kept in the
            // player's own frame (mirrored when gravity is flipped), like the arm angles; the whole swing is relative to it.
            Vector2 toMouse = Main.MouseWorld - player.MountedCenter;
            float aim = toMouse.LengthSquared() > 1f ? toMouse.ToRotation() : (player.direction >= 0 ? 0f : MathHelper.Pi);
            if (player.gravDir == -1f)
                aim = -aim;

            bool spin = phase != PhasebladeSwingProjectile.Phase.SwingDown;
            int scaledDamage = (int)(damage * (spin ? PhasebladeTuning.SpinDamageMultiplier : PhasebladeTuning.SwingDownDamageMultiplier));
            float scaledKnockback = knockback * (spin ? PhasebladeTuning.SpinKnockbackMultiplier : PhasebladeTuning.SwingDownKnockbackMultiplier);

            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, scaledDamage, scaledKnockback, player.whoAmI, (float)phase, startArm, aim);
            state.LastStartTick = Main.GameUpdateCount;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Phaseblade1", "Right click to plant the blade in the ground"));// (in the air it shoots straight down)"));
            //tooltips.Add(new TooltipLine(Mod, "Phaseblade2", "Click again to call it back: it flies to your hand, hurting enemies on the way"));
        }
    }
}