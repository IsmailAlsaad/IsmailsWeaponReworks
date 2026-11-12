using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Meatball
{
    /// <summary>
    /// Overhauls the vanilla Meatball: pressing the attack button starts MeatballFlailProjectile, a held flail head that runs the whole
    /// hold-charge rhythm loop (see FlailProjectile). Everything else (use setup, one head at a time, the tooltip) is in FlailGlobalItem.
    /// The only addition: after the tier 3 special tears the ball off, the hand stays empty for MeatballTuning.RegrowTicks before a new
    /// loop can start (MeatballPlayer counts it).
    /// </summary>
    public class MeatballGlobalItem : FlailGlobalItem
    {
        protected override int ItemType => ItemID.TheMeatball;
        protected override int ProjectileType => ModContent.ProjectileType<MeatballFlailProjectile>();

        public override bool CanUseItem(Item item, Player player)
        {
            return base.CanUseItem(item, player) && player.GetModPlayer<MeatballPlayer>().RegrowTicks <= 0;
        }

        // ---- the getfixedboi seed (see MeatballSwedish): own item art and an extra tooltip ----

        Asset<Texture2D> swedishItem;
        bool swedishItemTried;

        // Inventory, hotbar, chests, shops: the Swedish art drawn at the same spot and scale as the vanilla icon.
        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = MeatballSwedish.Texture("MeatballItem_Swedish", ref swedishItem, ref swedishItemTried);
            if (texture == null)
                return true;
            Vector2 center = position - origin * scale + frame.Size() * scale * 0.5f;   // where the vanilla icon's middle would be
            spriteBatch.Draw(texture, center, null, drawColor, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        // The item lying in the world: bottom-centered on the item's box, like vanilla.
        public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = MeatballSwedish.Texture("MeatballItem_Swedish", ref swedishItem, ref swedishItemTried);
            if (texture == null)
                return true;
            Vector2 position = item.position - Main.screenPosition + new Vector2(item.width * 0.5f, item.height - texture.Height * 0.5f);
            spriteBatch.Draw(texture, position, null, alphaColor, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(item, tooltips);
            if (!MeatballSwedish.Active)
                return;
            tooltips.Add(new TooltipLine(Mod, "MeatballSwedish1", "It's extra swedish") { OverrideColor = new Color(254, 204, 0) });
            tooltips.Add(new TooltipLine(Mod, "MeatballSwedish2", "-Modded Tessarian") { OverrideColor = new Color(90, 165, 225) });
        }
    }
}