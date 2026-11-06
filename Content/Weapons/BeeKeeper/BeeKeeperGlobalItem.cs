using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// Overhauls the vanilla Bee Keeper (ItemID.BeeKeeper): the item itself fires and hits nothing (so the vanilla bees on hit never happen).
    /// Left click starts a BeeKeeperSwingProjectile that swings the sword the way the Starfury does (alternating down and up, centered on the
    /// cursor). The passive swarm of bees lives in BeeKeeperPlayer. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class BeeKeeperGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.BeeKeeper;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<BeeKeeperSwingProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the sword
            item.channel = false;
            item.autoReuse = true;                // hold the button to keep swinging
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        // One swing at a time.
        public override bool CanUseItem(Item item, Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<BeeKeeperSwingProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            BeeKeeperPlayer state = player.GetModPlayer<BeeKeeperPlayer>();
            bool combo = state.LastPhase >= 0 && Main.GameUpdateCount - state.LastEndTick <= (uint)BeeKeeperTuning.ComboTicks;

            // The aim is captured here, once, and travels in the spawn packet (ai[2]) so every machine poses the same swing. It is kept in the
            // player's own frame (mirrored when gravity is flipped), like the arm angles; the whole swing is relative to it.
            Vector2 toMouse = Main.MouseWorld - player.MountedCenter;
            float aim = toMouse.LengthSquared() > 1f ? toMouse.ToRotation() : (player.direction >= 0 ? 0f : MathHelper.Pi);
            if (player.gravDir == -1f)
                aim = -aim;

            // Alternate: a swing right after a down swing goes up; anything else (fresh combo, after an up swing) goes down.
            bool afterDown = combo && state.LastPhase == (int)BeeKeeperSwingProjectile.Phase.SwingDown;
            BeeKeeperSwingProjectile.Phase phase = afterDown ? BeeKeeperSwingProjectile.Phase.SwingUp : BeeKeeperSwingProjectile.Phase.SwingDown;
            float startArm = combo ? state.LastEndArm : MathHelper.ToRadians(BeeKeeperTuning.RestArmDeg); // relative to the aim

            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, (float)phase, startArm, aim);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "BeeKeeper1", "Bees gather above you while it is held"));
            tooltips.Add(new TooltipLine(Mod, "BeeKeeper2", "A bee that dies leaves a poison cloud that poisons everything inside, you too"));
            tooltips.Add(new TooltipLine(Mod, "BeeKeeper3", "Hits send bees that latch on and sting: the more hits, the more bees"));
        }
    }
}