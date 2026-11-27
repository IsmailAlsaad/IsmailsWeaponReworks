using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using IsmailsWeaponReworks.Common.Rework;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.Katana
{
    /// <summary>
    /// Overhauls the vanilla Katana: the item itself swings nothing. While it is held, a KatanaStanceProjectile keeps it in the sheathed
    /// pose; a click throws the blade (KatanaBladeProjectile) along the aim line, and releasing the button teleports the player to it
    /// and cuts everything on the way. A click before the sword is fully sheathed is a plain swing (KatanaSwingProjectile) instead.
    /// Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class KatanaGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return ReworkToggle.Applies(entity.type, entity.type == ItemID.Katana);
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<KatanaBladeProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the projectiles pose the arm themselves
            item.noMelee = true;                  // the projectiles are the weapon
            item.noUseGraphic = true;             // the projectiles draw the sword
            item.channel = false;
            item.autoReuse = true;               // one throw per press
            item.UseSound = null;                 // the projectiles play their own sounds
        }

        // A click is a use unless something is already going on: a swing, a flying blade or the dash. Whether it becomes the throw or the
        // plain swing is decided in Shoot.
        public override bool CanUseItem(Item item, Player player)
        {
            if (KatanaPlayer.Find(player.whoAmI, ModContent.ProjectileType<KatanaSwingProjectile>()) != null)
                return false;
            if (KatanaPlayer.BladeFlying(player.whoAmI) || player.GetModPlayer<DashPlayer>().Phase == DashPhase.Dash)
                return false; // a blade stuck in a tile or falling does not block anything
            if (player.GetModPlayer<KatanaPlayer>().ThrewThisPress)
                return false; // the press that threw the blade must be let go before anything else starts
            return true;
        }

        // Only a fully sheathed sword (clicked shut, cooldown over, the white glow) may throw the blade: the cooldown alone is shorter than the sheathing.
        static bool CanThrow(Player player)
        {
            if (!player.GetModPlayer<KatanaPlayer>().Ready)
                return false;
            Projectile stance = KatanaPlayer.Find(player.whoAmI, ModContent.ProjectileType<KatanaStanceProjectile>());
            return stance != null && stance.ModProjectile is KatanaStanceProjectile sheathed && sheathed.ReadyToThrow;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            if (CanThrow(player))
            {
                KatanaBladeProjectile.Throw(player, source);
                player.GetModPlayer<KatanaPlayer>().ThrewThisPress = true;
            }
            else
                StartSwing(player, source, damage, knockback); // not fully sheathed yet: a plain swing at the cursor
            return false;
        }

        // Starts the plain swing at the cursor (owner's client only). It starts from the pose the stance is in right now, so there is no
        // jump; the stance ends.
        public static void StartSwing(Player player, IEntitySource source, int damage, float knockback)
        {
            int type = ModContent.ProjectileType<KatanaSwingProjectile>();
            float startArm = MathHelper.ToRadians(KatanaTuning.ReadyArmDeg);
            float startTilt = MathHelper.ToRadians(KatanaTuning.ReadyTiltDeg);
            Projectile stance = KatanaPlayer.Find(player.whoAmI, ModContent.ProjectileType<KatanaStanceProjectile>());
            if (stance != null)
            {
                if (stance.ModProjectile is KatanaStanceProjectile stanceState)
                {
                    startArm = stanceState.ArmLocal;
                    startTilt = stanceState.TiltRad;
                }
                stance.Kill();
            }

            // ai[0] = the arm's start angle (facing-local), ai[1] = the blade tilt's start angle, ai[2] = the aim captured at the click.
            float aim = HeldAim.Capture(player);
            player.ChangeDir(HeldAim.Facing(aim)); // always face the side the cursor is on, from the first tick of the swing
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, startArm, startTilt, aim);
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //tooltips.Add(new TooltipLine(Mod, "Katana1", "Hold to throw the blade, let go or hit a wall to dash to it and cut everything on the way"));
        }
    }
}