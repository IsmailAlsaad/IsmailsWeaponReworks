using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// Overhauls the vanilla Muramasa: the item itself swings nothing. While it is held, a MuramasaStanceProjectile keeps the sword in a
    /// slow sheathing animation; a click starts a MuramasaSwingProjectile (a plain swing toward the cursor, patch 1) and ends the stance.
    /// Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class MuramasaGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.type == ItemID.Muramasa;
        }

        public override void SetDefaults(Item item)
        {
            item.shoot = ModContent.ProjectileType<MuramasaSwingProjectile>();
            item.shootSpeed = 1f;
            item.useStyle = ItemUseStyleID.Shoot; // the held projectiles move the arm themselves
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectiles draw the sword
            item.channel = false;
            item.autoReuse = true;                // holding the button repeats the plain swing; a fully sheathed sword refuses the use (CanUseItem) and opens the draw window instead
            item.UseSound = null;                 // the projectiles play their own sounds
        }

        // One swing at a time. A fully sheathed sword is not "used": the stance watches the button itself (the draw window, patch 2) and
        // starts the plain swing on its own when the drawn path turns out to be short.
        public override bool CanUseItem(Item item, Player player)
        {
            if (MuramasaPlayer.Find(player.whoAmI, ModContent.ProjectileType<MuramasaSwingProjectile>()) != null
                || MuramasaPlayer.Find(player.whoAmI, ModContent.ProjectileType<MuramasaIaidoProjectile>()) != null)
                return false;
            Projectile stance = MuramasaPlayer.Find(player.whoAmI, ModContent.ProjectileType<MuramasaStanceProjectile>());
            if (stance != null && stance.ModProjectile is MuramasaStanceProjectile stanceState && stanceState.FullySheathed)
                return false;
            return true;
        }

        public override bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // Shoot only runs on the attacking player's own client; the projectile is then synced to everyone else.
            StartSwing(player, source, damage, knockback);
            return false;
        }

        // Starts the plain swing at the cursor (owner's client only). It starts from the pose the stance is in right now, so there is no
        // jump; the stance ends. Used by Shoot (a click before the sword is shut) and by the stance (a drawn path that was too short).
        public static void StartSwing(Player player, IEntitySource source, int damage, float knockback)
        {
            int type = ModContent.ProjectileType<MuramasaSwingProjectile>();
            float startArm = MathHelper.ToRadians(MuramasaTuning.ReadyArmDeg);
            float startTilt = MathHelper.ToRadians(MuramasaTuning.ReadyTiltDeg);
            Projectile stance = MuramasaPlayer.Find(player.whoAmI, ModContent.ProjectileType<MuramasaStanceProjectile>());
            if (stance != null)
            {
                if (stance.ModProjectile is MuramasaStanceProjectile stanceState)
                {
                    startArm = stanceState.ArmLocal;
                    startTilt = stanceState.TiltRad;
                }
                stance.Kill();
            }

            // ai[0] = the arm's start angle (facing-local), ai[1] = the blade tilt's start angle, ai[2] = the aim captured at the click.
            float aim = HeldAim.Capture(player);
            player.ChangeDir(HeldAim.Facing(aim)); // always face the side the cursor is on, from the first tick of every swing
            Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, startArm, startTilt, aim);
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Muramasa1", "Draw quick slashes when fully sheathed"));
            if (MuramasaGreen.Active)
                tooltips.Add(new TooltipLine(Mod, "MuramasaGreen1", "\"You've seen blue, you've seen red, but get ready for...\"") { OverrideColor = new Color(90, 230, 110) });
        }

        // ---- the green easter egg (see MuramasaGreen): the item's own art ----

        public override void Unload()
        {
            MuramasaGreen.Unload();
        }

        // Inventory, hotbar, chests, shops: GreenMuramasa.png at the same spot and scale as the vanilla icon.
        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = MuramasaGreen.Texture();
            if (texture == null)
                return true;
            Vector2 center = position - origin * scale + frame.Size() * scale * 0.5f;   // where the vanilla icon's middle would be
            spriteBatch.Draw(texture, center, null, drawColor, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        // The item lying in the world: bottom-centered on the item's box, like vanilla.
        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = MuramasaGreen.Texture();
            if (texture == null)
                return true;
            Vector2 position = item.position - Main.screenPosition + new Vector2(item.width * 0.5f, item.height - texture.Height * 0.5f);
            spriteBatch.Draw(texture, position, null, alphaColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}