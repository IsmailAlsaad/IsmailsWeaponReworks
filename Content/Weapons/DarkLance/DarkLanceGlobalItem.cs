using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// Overhauls the vanilla Dark Lance. Patch 6: right click (hold) raises the lance and charges dark flame orbs (a
    /// DarkLanceChargeProjectile). The left click is still the vanilla lance until patch 7 (sweep and thrust). Recipe, drops and base
    /// stats stay vanilla.
    /// </summary>
    public class DarkLanceGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.DarkLance;
        }

        public override void SetDefaults(Item item)
        {
            item.noMelee = true;       // the projectiles are the weapon
            item.noUseGraphic = true;  // the projectiles draw the lance
            item.shoot = ModContent.ProjectileType<DarkLanceSweepProjectile>();   // the left click: Shoot picks the sweep or the thrust
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot;
            item.channel = false;
            item.autoReuse = true;     // hold the button to keep cycling sweep, thrust, sweep ...
        }

        public override bool AltFunctionUse(Item item, Player player)
        {
            return true; // right click charges the orbs
        }

        // One charge at a time, and not right after it. The right click plays no use sound (the charge plays its own).
        public override bool CanUseItem(Item item, Player player)
        {
            // One thing at a time: no charge, sweep or thrust may be out, and the cooldown after the last one must be over (both clicks).
            if (AnyOut(player) || player.GetModPlayer<ThrustPlayer>().OnCooldown(ItemID.DarkLance))
                return false;
            item.UseSound = null;   // the projectiles play their own sounds
            return true;
        }

        static bool AnyOut(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<DarkLanceChargeProjectile>()] > 0
                || player.ownedProjectileCounts[ModContent.ProjectileType<DarkLanceSweepProjectile>()] > 0
                || player.ownedProjectileCounts[ModContent.ProjectileType<DarkLanceThrustProjectile>()] > 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            // Right click: ai[0] = 0, ai[1] = 0 (the owner writes the end age + 1 here when the button is released). Left click: vanilla.
            if (player.altFunctionUse == 2)
            {
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<DarkLanceChargeProjectile>(), 0, 0f, player.whoAmI);
                return false;
            }

            // Left click cycles a SWEEP (a fast 360 degree turn that gathers every orb the lance touches) and a THRUST (bigger and stronger
            // with every gathered orb; with all of them it ends in an explosion). Decided here, on the owner's client, and carried in the spawn:
            // sweep: ai[0] = spin direction, ai[2] = aim; thrust: ai[0] = seed + 10000 * gathered orbs, ai[2] = aim.
            DarkLancePlayer state = player.GetModPlayer<DarkLancePlayer>();
            float aim = HeldAim.Capture(player);
            if (!state.NextIsThrust)
            {
                int sweepDamage = System.Math.Max(1, (int)(damage * DarkLanceTuning.SweepDamageMultiplier));
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<DarkLanceSweepProjectile>(), sweepDamage, knockback, player.whoAmI, HeldAim.Facing(aim), 0f, aim);
                state.NextIsThrust = true;
            }
            else
            {
                int gathered = System.Math.Min(state.Collected, DarkLanceTuning.MaxOrbs);
                state.Collected = 0;
                float multiplier = DarkLanceTuning.ThrustDamageMultiplier * (1f + DarkLanceTuning.DamagePerOrb * gathered);
                int thrustDamage = System.Math.Max(1, (int)(damage * multiplier));
                int seed = Main.rand.Next(10000);
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<DarkLanceThrustProjectile>(), thrustDamage, knockback, player.whoAmI, seed + 10000f * gathered, 0f, aim);
                state.NextIsThrust = false;
            }
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "DarkLance1", "Hold right click to accumulate dark flames"));
            //tooltips.Add(new TooltipLine(Mod, "DarkLance2", "Left click sweeps to gather the flames, then thrusts with them"));
        }
    }
}