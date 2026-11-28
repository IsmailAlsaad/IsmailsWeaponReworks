using System.Collections.Generic;
using IsmailsWeaponReworks.Common.Rework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// Overhauls the vanilla Candy Cane Sword: the item itself swings nothing. While it is held, a CandyCaneSwordBladeProjectile (spawned by
    /// CandyCaneSwordPlayer) holds the sword for as long as it is in the hand and swings it while the left button is down (the shared charged blade,
    /// the plain swing of the Light's Bane). Hits make candies (see CandyProjectile). Recipe, drops and base stats stay vanilla.
    /// </summary>
    public class CandyCaneSwordGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return ReworkToggle.Applies(entity.type, entity.type == ItemID.CandyCaneSword);
        }

        public override void SetDefaults(Item item)
        {
            item.useStyle = ItemUseStyleID.Shoot; // the held projectile moves the arm itself
            item.noMelee = true;                  // the projectile is the weapon
            item.noUseGraphic = true;             // the projectile draws the sword
            item.channel = false;
            item.UseSound = null;                 // the projectile plays its own sound
        }

        // The item is never "used": the held blade watches the button itself.
        public override bool CanUseItem(Item item, Player player)
        {
            return false;
        }

        // The purple seed (getfixedboi, see CandyCaneSwordPurple): the quote in purple, on two lines like the Meatball's (the quote, then who said it).
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!CandyCaneSwordPurple.Active)
                return;
            tooltips.Add(new TooltipLine(Mod, "CandyCaneSwordPurple1", "\"I love purple color\"") { OverrideColor = CandyCaneSwordTuning.PurpleText });
            tooltips.Add(new TooltipLine(Mod, "CandyCaneSwordPurple2", "-GooseMazz") { OverrideColor = CandyCaneSwordTuning.PurpleText });
        }

        // ---- the item's own art follows the color of the next candy (CandyCaneSword.png); the vanilla art until the file exists ----

        public override void Unload()
        {
            CandyCaneSwordSprites.Unload();
            CandyCaneSwordPurple.Unload();
        }

        static Texture2D CurrentSprite()
        {
            if (Main.dedServ || Main.LocalPlayer == null)
                return null;
            return CandyCaneSwordPurple.Sword() ?? CandyCaneSwordSprites.Cell(Main.LocalPlayer.GetModPlayer<CandyCaneSwordPlayer>().NextCandy);
        }

        // Inventory, hotbar, chests, shops: the colored sword at the same spot and scale as the vanilla icon.
        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = CurrentSprite();
            if (texture == null)
                return true;
            Vector2 center = position - origin * scale + frame.Size() * scale * 0.5f;   // where the vanilla icon's middle would be
            spriteBatch.Draw(texture, center, null, drawColor, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        // The item lying in the world: bottom-centered on the item's box, like vanilla.
        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = CurrentSprite();
            if (texture == null)
                return true;
            Vector2 position = item.position - Main.screenPosition + new Vector2(item.width * 0.5f, item.height - texture.Height * 0.5f);
            spriteBatch.Draw(texture, position, null, alphaColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}