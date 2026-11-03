using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.RottedFork
{
    /// <summary>
    /// Overhauls the vanilla Rotted Fork: the item itself throws nothing. Left click (hold) starts a RottedForkSpinProjectile: the fork
    /// spins around the player for as long as the button is held. Right click throws the fork as a RottedForkJavelinProjectile (patch 5). Recipe,
    /// drops and base stats stay vanilla.
    /// </summary>
    public class RottedForkGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.TheRottedFork;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<RottedForkSpinProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the fork
            item.channel = false;                 // the projectile watches the button itself (controlUseItem)
            item.autoReuse = true;                // hold the button to spin again as soon as the cooldown ends
            item.UseSound = null;                 // the projectile plays its own sounds
        }

        public override bool AltFunctionUse(Item item, Player player)
        {
            return true; // right click throws the javelin
        }

        // The fork is either spinning, or flying as the javelin, or in the hand: no spin and no javelin may be out, and the cooldown
        // after the last one must be over (for both clicks).
        public override bool CanUseItem(Item item, Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<RottedForkSpinProjectile>()] > 0 || player.ownedProjectileCounts[ModContent.ProjectileType<RottedForkJavelinProjectile>()] > 0)
                return false;
            return !player.GetModPlayer<ThrustPlayer>().OnCooldown(ItemID.TheRottedFork);
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            // ai[0] = the spin direction (1 = clockwise on screen: toward the side the cursor is on, -1 = the other way), ai[1] = 0 (the
            // owner writes the end age + 1 here when the button is released), ai[2] = the first aim in the player's own frame.
            if (player.altFunctionUse == 2)
            {
                // Right click: the javelin flies straight at the cursor (its velocity is the throw, synced with the spawn).
                Vector2 direction = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
                player.ChangeDir(direction.X >= 0f ? 1 : -1);
                int javelinDamage = Math.Max(1, (int)(damage * RottedForkTuning.JavelinDamageMultiplier));
                Projectile.NewProjectile(source, player.MountedCenter + direction * 24f, direction * RottedForkTuning.JavelinSpeed, ModContent.ProjectileType<RottedForkJavelinProjectile>(), javelinDamage, knockback, player.whoAmI);
                return false;
            }

            float aim = HeldAim.Capture(player);
            int spinDamage = Math.Max(1, (int)(damage * RottedForkTuning.DamageMultiplier));
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, spinDamage, knockback, player.whoAmI, HeldAim.Facing(aim), 0f, aim);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "RottedFork1", "Hold to spin the fork around you"));
        }
    }
}