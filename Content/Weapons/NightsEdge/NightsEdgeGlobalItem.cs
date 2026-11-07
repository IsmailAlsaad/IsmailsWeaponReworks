using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// Overhauls the vanilla Night's Edge (ItemID.NightsEdge): the item itself fires and hits nothing. Left click starts a
    /// NightsEdgeSwingProjectile, the Starfury's alternating down and up swing, but every consecutive swing is faster and charges the
    /// sword. Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class NightsEdgeGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.NightsEdge;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<NightsEdgeSwingProjectile>();
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
            return player.ownedProjectileCounts[ModContent.ProjectileType<NightsEdgeSwingProjectile>()] <= 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            NightsEdgePlayer state = player.GetModPlayer<NightsEdgePlayer>();
            uint since = Main.GameUpdateCount - state.LastEndTick;
            bool combo = state.LastPhase >= 0 && since <= (uint)NightsEdgeTuning.ComboWindowTicks;

            // The level is how many consecutive swings came before this one (0 = a fresh combo, ChargeSwings = a charged swing). A gap longer
            // than the combo window resets it, except that a fully charged sword stays charged for ChargedLingerTicks.
            int max = Math.Max(1, NightsEdgeTuning.ChargeSwings);
            int level = Math.Min(state.Charge, max);
            bool lingering = state.LastPhase >= 0 && level >= max && since <= (uint)NightsEdgeTuning.ChargedLingerTicks;
            if (!combo && !lingering)
                level = 0;
            state.Charge = Math.Min(max, level + 1);

            // The aim is captured here, once, and travels in the spawn packet (ai[2]) so every machine poses the same swing. It is kept in the
            // player's own frame (mirrored when gravity is flipped), like the arm angles; the whole swing is relative to it.
            float aim = HeldAim.Capture(player);

            // Alternate: a swing right after a down swing goes up; anything else (fresh combo, after an up swing) goes down.
            bool afterDown = combo && state.LastPhase == (int)NightsEdgeSwingProjectile.Phase.SwingDown;
            NightsEdgeSwingProjectile.Phase phase = afterDown ? NightsEdgeSwingProjectile.Phase.SwingUp : NightsEdgeSwingProjectile.Phase.SwingDown;
            float startArm = combo ? state.LastEndArm : MathHelper.ToRadians(NightsEdgeTuning.RestArmDeg); // relative to the aim

            // The timeline speed (the combo times the owner's melee attack speed) travels in the spawn velocity's X: the projectile never moves
            // by its velocity, reads it back on its first tick on every machine and clears it, so no machine has to guess another player's stats.
            float attackSpeed = 1f + (player.GetTotalAttackSpeed(DamageClass.Melee) - 1f) * NightsEdgeTuning.AttackSpeedInfluence;
            float speed = MathHelper.Clamp(NightsEdgeTuning.ComboSpeedAt(level) * attackSpeed, NightsEdgeTuning.SpeedScaleMin, NightsEdgeTuning.SpeedScaleMax);

            // ai[0] = phase + 10 * level, ai[1] = start arm angle (relative to the aim), ai[2] = the aim.
            int swingDamage = (int)Math.Round(damage * NightsEdgeTuning.SwingDamageMultiplier);
            float swingKnockback = knockback * NightsEdgeTuning.SwingKnockbackMultiplier;
            Projectile.NewProjectile(source, player.MountedCenter, new Vector2(speed, 0f), type, swingDamage, swingKnockback, player.whoAmI, (float)phase + 10f * level, startArm, aim);
            return false;
        }
    }
}