using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace IsmailsWeaponReworks.Common.Rework
{
    /// <summary>
    /// The config screen's weapon grid (attached to ReworksToggleConfig.DisabledReworks with [CustomModConfigItem]): one cell per
    /// reworked item (ReworkRegistry.ItemTypes), a scroll bar when they do not all fit. A click toggles that item in the PENDING config
    /// list (the screen's Save button applies it, a reload makes it real). A switched-off item is dimmed and crossed out; hovering a
    /// cell shows its name and Active (green) or Inactive (red) at the cursor. It plays no sounds. The grid scrolls a whole row at a
    /// time, so nothing is ever drawn half outside the grid and no clipping is needed.
    /// </summary>
    public class ReworkGridElement : ConfigElement<List<ItemDefinition>>
    {
        private readonly UIScrollbar scrollbar = new UIScrollbar();
        private List<int> types = new List<int>();
        private Func<string> fieldTooltip;
        private Asset<Texture2D> crossArt;

        private static string ActiveText => Language.GetOrRegister("Mods.IsmailsWeaponReworks.Configs.ReworksToggleConfig.Active", () => "Active").Value;
        private static string InactiveText => Language.GetOrRegister("Mods.IsmailsWeaponReworks.Configs.ReworksToggleConfig.Inactive", () => "Inactive").Value;

        public override void OnBind()
        {
            base.OnBind();
            fieldTooltip = TooltipFunction;

            ReworkRegistry registry = ModContent.GetInstance<ReworkRegistry>();
            types = registry.ItemTypes.OrderBy(type => type).ToList();

            float rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            Height.Set(ReworkGridTuning.HeaderHeight + ReworkGridTuning.VisibleRows * rowHeight + ReworkGridTuning.BottomPadding, 0f);

            scrollbar.Top.Set(ReworkGridTuning.HeaderHeight, 0f);
            scrollbar.Height.Set(ReworkGridTuning.VisibleRows * rowHeight - ReworkGridTuning.CellGap, 0f);
            scrollbar.HAlign = 1f;
            scrollbar.Left.Set(-ReworkGridTuning.EdgePadding, 0f);
            scrollbar.SetView(ReworkGridTuning.VisibleRows * rowHeight, ReworkGridTuning.VisibleRows * rowHeight);
            Append(scrollbar);

            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Common/Rework/ReworkOffX", out crossArt, AssetRequestMode.ImmediateLoad);
        }

        // ------------------------------------------------------------------ layout

        private int Columns(CalculatedStyle dims)
        {
            float rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            float width = dims.Width - 2f * ReworkGridTuning.EdgePadding - ReworkGridTuning.ScrollbarSpace + ReworkGridTuning.CellGap;
            return Math.Max(1, (int)(width / rowHeight));
        }

        private int FirstRow()
        {
            float rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            return Math.Max(0, (int)Math.Round(scrollbar.ViewPosition / rowHeight));
        }

        private Rectangle CellRect(CalculatedStyle dims, int row, int column)
        {
            int rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            int x = (int)dims.X + ReworkGridTuning.EdgePadding + column * rowHeight;
            int y = (int)dims.Y + ReworkGridTuning.HeaderHeight + row * rowHeight;
            return new Rectangle(x, y, ReworkGridTuning.CellSize, ReworkGridTuning.CellSize);
        }

        /// <summary>The index into types of the cell under this screen point, or -1.</summary>
        private int CellAt(CalculatedStyle dims, int columns, int mouseX, int mouseY)
        {
            int first = FirstRow();
            for (int row = 0; row < ReworkGridTuning.VisibleRows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = (first + row) * columns + column;
                    if (index >= types.Count)
                        return -1;
                    if (CellRect(dims, row, column).Contains(mouseX, mouseY))
                        return index;
                }
            }
            return -1;
        }

        private HashSet<int> DisabledSet()
        {
            HashSet<int> set = new HashSet<int>();
            List<ItemDefinition> list = Value;
            if (list != null)
            {
                foreach (ItemDefinition definition in list)
                {
                    if (definition != null && definition.Type > 0)
                        set.Add(definition.Type);
                }
            }
            return set;
        }

        // ------------------------------------------------------------------ input

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            CalculatedStyle dims = GetDimensions();
            int index = CellAt(dims, Columns(dims), Main.mouseX, Main.mouseY);
            if (index >= 0)
                Toggle(types[index]);
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            // Scroll the grid one row per notch. When the grid is already at its end, the wheel goes on to the config page instead.
            float rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            float before = scrollbar.ViewPosition;
            scrollbar.ViewPosition -= Math.Sign(evt.ScrollWheelValue) * rowHeight;
            if (scrollbar.ViewPosition == before)
                base.ScrollWheel(evt);
        }

        private void Toggle(int type)
        {
            List<ItemDefinition> list = Value ?? new List<ItemDefinition>();
            int at = list.FindIndex(definition => definition != null && definition.Type == type);
            if (at >= 0)
                list.RemoveAt(at);
            else
                list.Add(new ItemDefinition(type));
            Value = list; // marks the config as having pending changes (the Save button lights up)
        }

        // ------------------------------------------------------------------ drawing

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            int columns = Columns(dims);
            int rows = (types.Count + columns - 1) / columns;
            float rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            scrollbar.SetView(ReworkGridTuning.VisibleRows * rowHeight, Math.Max(rows, ReworkGridTuning.VisibleRows) * rowHeight);

            int hovered = IsMouseHovering ? CellAt(dims, columns, Main.mouseX, Main.mouseY) : -1;

            // The panel, the label and the field's own tooltip. While a cell is hovered the field tooltip is switched off so only the cell's text shows.
            TooltipFunction = hovered >= 0 ? null : fieldTooltip;
            base.DrawSelf(spriteBatch);

            HashSet<int> disabled = DisabledSet();

            int activeCount = types.Count(type => !disabled.Contains(type));
            Utils.DrawBorderString(spriteBatch, $"{ActiveText}: {activeCount}/{types.Count}",
                new Vector2(dims.X + dims.Width - ReworkGridTuning.EdgePadding, dims.Y + 8f), Color.White, 0.8f, 1f, 0f);

            int first = FirstRow();
            for (int row = 0; row < ReworkGridTuning.VisibleRows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = (first + row) * columns + column;
                    if (index >= types.Count)
                        break;
                    int type = types[index];
                    DrawCell(spriteBatch, CellRect(dims, row, column), type, !disabled.Contains(type), index == hovered);
                }
            }

            if (hovered >= 0)
            {
                int type = types[hovered];
                bool active = !disabled.Contains(type);
                string state = active ? $"[c/55FF55:{ActiveText}]" : $"[c/FF5555:{InactiveText}]";
                ShowTip(Lang.GetItemNameValue(type) + "\n" + state);
            }
        }

        private void DrawCell(SpriteBatch spriteBatch, Rectangle rect, int type, bool active, bool hovered)
        {
            Color background = active ? ReworkGridTuning.ActiveCell : ReworkGridTuning.InactiveCell;
            if (hovered)
                background = Color.Lerp(background, Color.White, ReworkGridTuning.HoverBrighten);
            Utils.DrawInvBG(spriteBatch, rect, background);

            Main.instance.LoadItem(type);
            Texture2D texture = TextureAssets.Item[type].Value;
            Rectangle frame = Main.itemAnimations[type] != null ? Main.itemAnimations[type].GetFrame(texture) : texture.Frame();
            float scale = Math.Min(1f, (ReworkGridTuning.CellSize - 2f * ReworkGridTuning.IconPadding) / Math.Max(frame.Width, frame.Height));
            Color iconColor = active ? Color.White : Color.White * ReworkGridTuning.InactiveIconAlpha;
            spriteBatch.Draw(texture, rect.Center.ToVector2(), frame, iconColor, 0f, frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            if (!active)
                DrawCross(spriteBatch, rect);
        }

        private void DrawCross(SpriteBatch spriteBatch, Rectangle rect)
        {
            if (crossArt != null && crossArt.IsLoaded)
            {
                Texture2D cross = crossArt.Value;
                float scale = Math.Min(1f, (ReworkGridTuning.CellSize - 4f) / Math.Max(cross.Width, cross.Height));
                spriteBatch.Draw(cross, rect.Center.ToVector2(), null, Color.White, 0f, cross.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                return;
            }

            // No ReworkOffX.png yet: a red X made of 2x2 pixel squares, with a dark shadow.
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int size = ReworkGridTuning.CellSize - 2 * ReworkGridTuning.IconPadding;
            int left = rect.X + ReworkGridTuning.IconPadding;
            int top = rect.Y + ReworkGridTuning.IconPadding;
            for (int i = 0; i < size; i += 2)
            {
                spriteBatch.Draw(pixel, new Rectangle(left + i + 1, top + i + 1, 2, 2), Color.Black * 0.6f);
                spriteBatch.Draw(pixel, new Rectangle(left + size - i - 2 + 1, top + i + 1, 2, 2), Color.Black * 0.6f);
            }
            for (int i = 0; i < size; i += 2)
            {
                spriteBatch.Draw(pixel, new Rectangle(left + i, top + i, 2, 2), ReworkGridTuning.FallbackCross);
                spriteBatch.Draw(pixel, new Rectangle(left + size - i - 2, top + i, 2, 2), ReworkGridTuning.FallbackCross);
            }
        }

        /// <summary>The same cursor tooltip tModLoader's config screen uses (it keeps the opaque box setting working).</summary>
        private static void ShowTip(string text)
        {
            if (Main.SettingsEnabled_OpaqueBoxBehindTooltips)
            {
                Terraria.Item fakeItem = new Terraria.Item();
                fakeItem.SetDefaults(0, noMatCheck: true);
                fakeItem.SetNameOverride(text);
                fakeItem.type = 1;
                fakeItem.scale = 0f;
                fakeItem.rare = 0;
                fakeItem.value = -1;
                Main.HoverItem = fakeItem;
                Main.instance.MouseText("");
                Main.mouseText = true;
            }
            else
            {
                Main.instance.MouseText(text);
            }
        }
    }
}