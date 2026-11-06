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
            item.shoot = ModContent.ProjectileType<DarkLanceStrikeProjectile>();   // the left click: one flowing sweep-and-thrust strike
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
            // A charged lance (all 8 flames gathered and waiting): right click does nothing, and the left click releases the thrust once the auto sweep is done.
            Projectile primed = DarkLanceStrikeProjectile.FindPrimed(player);
            if (primed != null)
            {
                if (player.altFunctionUse == 2 || !((DarkLanceStrikeProjectile)primed.ModProjectile).Ready)
                    return false;
                item.UseSound = null;
                return true;
            }

            // One thing at a time: no charge, sweep or thrust may be out, and the cooldown after the last one must be over (both clicks).
            if (AnyOut(player) || player.GetModPlayer<ThrustPlayer>().OnCooldown(ItemID.DarkLance))
                return false;
            item.UseSound = null;   // the projectiles play their own sounds
            return true;
        }

        static bool AnyOut(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<DarkLanceChargeProjectile>()] > 0
                || player.ownedProjectileCounts[ModContent.ProjectileType<DarkLanceStrikeProjectile>()] > 0;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            // The click on a charged lance does not spawn anything: the waiting strike runs on into its thrust.
            Projectile primed = DarkLanceStrikeProjectile.FindPrimed(player);
            if (primed != null && player.altFunctionUse != 2)
            {
                ((DarkLanceStrikeProjectile)primed.ModProjectile).Release();
                return false;
            }

            // Right click: ai[0] = 0, ai[1] = 0 (the owner writes the end age + 1 here when the button is released). Left click: vanilla.
            if (player.altFunctionUse == 2)
            {
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<DarkLanceChargeProjectile>(), 0, 0f, player.whoAmI);
                return false;
            }

            // The left click is ONE strike (wind back, accelerating sweep that gathers the orbs, then a thrust). Decided here, on the owner's
            // client, and carried in the spawn: ai[0] = spin direction * (1 + gathered orbs, starts at 0), ai[1] = the thrust aim (starts as the
            // aim, the projectile updates it), ai[2] = the aim.
            float aim = HeldAim.Capture(player);
            int strikeDamage = System.Math.Max(1, (int)(damage * DarkLanceTuning.SweepDamageMultiplier));
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<DarkLanceStrikeProjectile>(), strikeDamage, knockback, player.whoAmI, HeldAim.Facing(aim), aim, aim);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "DarkLance1", "Hold right click to accumulate dark flames"));
            //tooltips.Add(new TooltipLine(Mod, "DarkLance2", "Left click sweeps to gather the flames, then thrusts with them"));
        }
    }
}