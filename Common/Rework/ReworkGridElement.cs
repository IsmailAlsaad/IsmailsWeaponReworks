using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.GameInput;
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
        private enum SortKey { Rarity, Damage, Id }
        private enum FilterMode { All, Active, Inactive }

        private readonly UIScrollbar scrollbar = new UIScrollbar();
        private List<int> allTypes = new List<int>(); // every reworked item
        private List<int> types = new List<int>();    // the cells currently shown (after search, filter and sort)
        private SortKey sortKey = SortKey.Id;
        private bool descending;
        private FilterMode filter = FilterMode.All;
        private string search = "";
        private bool searchFocused;
        private int blinkTimer;
        private Func<string> fieldTooltip;
        private Asset<Texture2D> crossArt;

        private static string ActiveText => Language.GetOrRegister("Mods.IsmailsWeaponReworks.Configs.ReworksToggleConfig.Active", () => "Active").Value;
        private static string InactiveText => Language.GetOrRegister("Mods.IsmailsWeaponReworks.Configs.ReworksToggleConfig.Inactive", () => "Inactive").Value;

        public override void OnBind()
        {
            base.OnBind();
            fieldTooltip = TooltipFunction;

            ReworkRegistry registry = ModContent.GetInstance<ReworkRegistry>();
            allTypes = registry.ItemTypes.OrderBy(type => type).ToList();
            types = new List<int>(allTypes);

            float rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            Height.Set(ReworkGridTuning.GridTop + ReworkGridTuning.VisibleRows * rowHeight + ReworkGridTuning.BottomPadding, 0f);

            scrollbar.Top.Set(ReworkGridTuning.GridTop, 0f);
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
            int y = (int)dims.Y + ReworkGridTuning.GridTop + row * rowHeight;
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
            ControlRects(dims, out Rectangle searchRect, out Rectangle sortRect, out Rectangle directionRect, out Rectangle filterRect, out Rectangle allRect);
            Point mouse = new Point(Main.mouseX, Main.mouseY);

            searchFocused = false;
            if (searchRect.Contains(mouse))
            {
                searchFocused = true;
                Main.clrInput(); // forget keys that were typed before the box was clicked
                return;
            }
            if (sortRect.Contains(mouse))
            {
                sortKey = (SortKey)(((int)sortKey + 1) % 3);
                ResetScroll();
                return;
            }
            if (directionRect.Contains(mouse))
            {
                descending = !descending;
                ResetScroll();
                return;
            }
            if (filterRect.Contains(mouse))
            {
                filter = (FilterMode)(((int)filter + 1) % 3);
                ResetScroll();
                return;
            }
            if (allRect.Contains(mouse))
            {
                ToggleAll(DisabledSet());
                return;
            }

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

        // ------------------------------------------------------------------ search, sort, filter, toggle all

        private static string Text(string key, string fallback)
        {
            return Language.GetOrRegister("Mods.IsmailsWeaponReworks.Configs.ReworksToggleConfig." + key, () => fallback).Value;
        }

        private string SortName
        {
            get
            {
                switch (sortKey)
                {
                    case SortKey.Rarity: return Text("SortRarity", "Rarity");
                    case SortKey.Damage: return Text("SortDamage", "Damage");
                    default: return Text("SortId", "Item id");
                }
            }
        }

        private string FilterName
        {
            get
            {
                switch (filter)
                {
                    case FilterMode.Active: return Text("Active", "Active");
                    case FilterMode.Inactive: return Text("Inactive", "Inactive");
                    default: return Text("ShowAll", "All");
                }
            }
        }

        private void ResetScroll()
        {
            scrollbar.ViewPosition = 0f;
        }

        /// <summary>The controls row, left to right: search box, sort button, direction button, filter button, toggle-all button.</summary>
        private void ControlRects(CalculatedStyle dims, out Rectangle searchRect, out Rectangle sortRect, out Rectangle directionRect, out Rectangle filterRect, out Rectangle allRect)
        {
            int height = ReworkGridTuning.ButtonHeight;
            int gap = ReworkGridTuning.ControlGap;
            int y = (int)dims.Y + ReworkGridTuning.HeaderHeight + (ReworkGridTuning.ControlsHeight - height) / 2;
            int right = (int)(dims.X + dims.Width) - ReworkGridTuning.EdgePadding;

            allRect = new Rectangle(right - ReworkGridTuning.ToggleAllWidth, y, ReworkGridTuning.ToggleAllWidth, height);
            filterRect = new Rectangle(allRect.X - gap - ReworkGridTuning.FilterWidth, y, ReworkGridTuning.FilterWidth, height);
            directionRect = new Rectangle(filterRect.X - gap - ReworkGridTuning.DirectionWidth, y, ReworkGridTuning.DirectionWidth, height);
            sortRect = new Rectangle(directionRect.X - gap - ReworkGridTuning.SortWidth, y, ReworkGridTuning.SortWidth, height);

            int left = (int)dims.X + ReworkGridTuning.EdgePadding;
            searchRect = new Rectangle(left, y, Math.Max(ReworkGridTuning.SearchMinWidth, sortRect.X - gap - left), height);
        }

        private bool OverControls(CalculatedStyle dims)
        {
            ControlRects(dims, out Rectangle searchRect, out Rectangle sortRect, out Rectangle directionRect, out Rectangle filterRect, out Rectangle allRect);
            Point mouse = new Point(Main.mouseX, Main.mouseY);
            return searchRect.Contains(mouse) || sortRect.Contains(mouse) || directionRect.Contains(mouse) || filterRect.Contains(mouse) || allRect.Contains(mouse);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // A press anywhere outside the search box ends typing.
            if (searchFocused && Main.mouseLeft)
            {
                ControlRects(GetDimensions(), out Rectangle searchRect, out _, out _, out _, out _);
                if (!searchRect.Contains(Main.mouseX, Main.mouseY))
                    searchFocused = false;
            }
        }

        /// <summary>Reads the keyboard into the search text while the box has focus (the same calls tModLoader's own text fields make).</summary>
        private void TypeIntoSearch()
        {
            if (!searchFocused)
                return;

            PlayerInput.WritingText = true;
            Main.instance.HandleIME();
            string typed = Main.GetInputText(search);

            if (Main.inputTextEscape)
            {
                Main.inputTextEscape = false;
                searchFocused = false;
            }
            if (Main.inputText.IsKeyDown(Keys.Enter) && !Main.oldInputText.IsKeyDown(Keys.Enter))
                searchFocused = false;

            if (typed.Length > ReworkGridTuning.SearchMaxLength)
                typed = typed.Substring(0, ReworkGridTuning.SearchMaxLength);
            if (typed != search)
            {
                search = typed;
                ResetScroll();
            }
        }

        private int SortValue(int type)
        {
            Terraria.Item sample = Terraria.ID.ContentSamples.ItemsByType[type];
            switch (sortKey)
            {
                case SortKey.Rarity: return sample.rare;
                case SortKey.Damage: return sample.damage;
                default: return type;
            }
        }

        private int CompareTypes(int a, int b)
        {
            int result = SortValue(a).CompareTo(SortValue(b));
            if (descending)
                result = -result;
            return result != 0 ? result : a.CompareTo(b); // ties always by item id, lowest first
        }

        /// <summary>Fills types (the shown cells) from allTypes: the search text, then the filter, then the sort.</summary>
        private void RebuildTypes(HashSet<int> disabled)
        {
            string needle = search.Trim();
            types.Clear();
            foreach (int type in allTypes)
            {
                if (filter == FilterMode.Active && disabled.Contains(type))
                    continue;
                if (filter == FilterMode.Inactive && !disabled.Contains(type))
                    continue;
                if (needle.Length > 0 && Lang.GetItemNameValue(type).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) < 0)
                    continue;
                types.Add(type);
            }
            types.Sort(CompareTypes);
        }

        /// <summary>Switches EVERY rework off, or, when every rework is already off, every rework on (the filter and the search do not matter).</summary>
        private void ToggleAll(HashSet<int> disabled)
        {
            bool allOff = allTypes.All(disabled.Contains);
            List<ItemDefinition> list = new List<ItemDefinition>();
            if (!allOff)
            {
                foreach (int type in allTypes)
                    list.Add(new ItemDefinition(type));
            }
            Value = list;
        }

        private void DrawControls(SpriteBatch spriteBatch, CalculatedStyle dims, HashSet<int> disabled)
        {
            ControlRects(dims, out Rectangle searchRect, out Rectangle sortRect, out Rectangle directionRect, out Rectangle filterRect, out Rectangle allRect);
            Point mouse = new Point(Main.mouseX, Main.mouseY);
            bool over = IsMouseHovering;
            float scale = ReworkGridTuning.ControlTextScale;
            var font = FontAssets.MouseText.Value;

            // Search box: the hint while empty, the text with a blinking bar while typing; a long text shows its end.
            Utils.DrawInvBG(spriteBatch, searchRect, searchFocused ? ReworkGridTuning.SearchFocusedColor : ReworkGridTuning.ControlColor);
            blinkTimer++;
            Vector2 textPosition = new Vector2(searchRect.X + 8f, searchRect.Center.Y);
            if (search.Length == 0 && !searchFocused)
            {
                Utils.DrawBorderString(spriteBatch, Text("SearchHint", "Search..."), textPosition, Color.Gray, scale, 0f, 0.5f);
            }
            else
            {
                string shown = search + (searchFocused && (blinkTimer / 20) % 2 == 1 ? "|" : "");
                float maxWidth = searchRect.Width - 16f;
                while (shown.Length > 1 && font.MeasureString(shown).X * scale > maxWidth)
                    shown = shown.Substring(1);
                Utils.DrawBorderString(spriteBatch, shown, textPosition, Color.White, scale, 0f, 0.5f);
            }

            bool allOff = allTypes.All(disabled.Contains);
            DrawButton(spriteBatch, sortRect, Text("SortLabel", "Sort") + ": " + SortName, over && sortRect.Contains(mouse));
            DrawButton(spriteBatch, directionRect, null, over && directionRect.Contains(mouse));
            DrawButton(spriteBatch, filterRect, Text("ShowLabel", "Show") + ": " + FilterName, over && filterRect.Contains(mouse));
            DrawButton(spriteBatch, allRect, allOff ? Text("EnableAll", "Enable all") : Text("DisableAll", "Disable all"), over && allRect.Contains(mouse));

            // Direction arrow: points up for lowest first, down for highest first.
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Point center = directionRect.Center;
            for (int row = 0; row < 6; row++)
            {
                int half = descending ? 6 - row : row + 1;
                spriteBatch.Draw(pixel, new Rectangle(center.X - half, center.Y - 3 + row, half * 2, 1), Color.White);
            }

            if (over)
            {
                if (sortRect.Contains(mouse))
                    ShowTip(Text("SortTip", "Click to sort by rarity, damage or item id"));
                else if (directionRect.Contains(mouse))
                    ShowTip(descending ? Text("DescendingTip", "Highest first (click to reverse)") : Text("AscendingTip", "Lowest first (click to reverse)"));
                else if (filterRect.Contains(mouse))
                    ShowTip(Text("FilterTip", "Click to show all, only active or only inactive reworks"));
                else if (allRect.Contains(mouse))
                    ShowTip(Text("ToggleAllTip", "Switches every rework off, or every rework on when all are off. Search and filter are ignored."));
            }
        }

        private void DrawButton(SpriteBatch spriteBatch, Rectangle rect, string label, bool hovered)
        {
            Color background = ReworkGridTuning.ControlColor;
            if (hovered)
                background = Color.Lerp(background, Color.White, ReworkGridTuning.HoverBrighten);
            Utils.DrawInvBG(spriteBatch, rect, background);
            if (label != null)
                Utils.DrawBorderString(spriteBatch, label, rect.Center.ToVector2(), Color.White, ReworkGridTuning.ControlTextScale, 0.5f, 0.5f);
        }

        // ------------------------------------------------------------------ drawing

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            TypeIntoSearch();
            HashSet<int> disabled = DisabledSet();
            RebuildTypes(disabled);
            int columns = Columns(dims);
            int rows = (types.Count + columns - 1) / columns;
            float rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            scrollbar.SetView(ReworkGridTuning.VisibleRows * rowHeight, Math.Max(rows, ReworkGridTuning.VisibleRows) * rowHeight);

            int hovered = IsMouseHovering ? CellAt(dims, columns, Main.mouseX, Main.mouseY) : -1;

            // The panel, the label and the field's own tooltip. While a cell is hovered the field tooltip is switched off so only the cell's text shows.
            TooltipFunction = hovered >= 0 || (IsMouseHovering && OverControls(dims)) ? null : fieldTooltip;
            base.DrawSelf(spriteBatch);

            int activeCount = allTypes.Count(type => !disabled.Contains(type));
            Utils.DrawBorderString(spriteBatch, $"{ActiveText}: {activeCount}/{allTypes.Count}",
                new Vector2(dims.X + dims.Width - ReworkGridTuning.EdgePadding, dims.Y + 8f), Color.White, 0.8f, 1f, 0f);

            DrawControls(spriteBatch, dims, disabled);

            if (types.Count == 0)
            {
                Utils.DrawBorderString(spriteBatch, Text("NoMatches", "No reworks match"),
                    new Vector2(dims.X + dims.Width * 0.5f, dims.Y + ReworkGridTuning.GridTop + 20f), Color.Gray, 0.9f, 0.5f, 0f);
            }

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