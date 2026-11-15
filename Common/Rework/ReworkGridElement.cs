using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private enum FilterMode { All, Active, Inactive }

        /// <summary>One line of the grid: a section header (Level 0 = class, 1 = progression, 2 = subclass) or a row of cells.</summary>
        private class GridLine
        {
            public int Level = -1;                    // -1 = a row of cells
            public string Key;                        // headers: "Melee", "Melee/PreHardmode", "Melee/PreHardmode/Flails"
            public string Label;
            public bool Collapsed;
            public List<int> Cells = new List<int>(); // rows of cells: the item types in this row
        }

        private readonly UIScrollbar scrollbar = new UIScrollbar();
        private List<int> allTypes = new List<int>();                       // every reworked item
        private readonly List<GridLine> lines = new List<GridLine>();       // what the grid shows right now (rebuilt every frame)
        private readonly HashSet<string> collapsed = new HashSet<string>(); // keys of the sections the player collapsed
        private int shownCount;
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

        /// <summary>The index of the first line shown (a header and a row of cells are both one line tall).</summary>
        private int FirstLine()
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

        /// <summary>The clickable area of a header (its name and its triangle) and where the triangle is centered.</summary>
        private Rectangle HeaderRect(CalculatedStyle dims, int row, GridLine line, out Point triangleCenter)
        {
            var font = FontAssets.MouseText.Value;
            float textWidth = font.MeasureString(line.Label).X * ReworkGridTuning.HeaderScale(line.Level);
            int rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            int x = (int)dims.X + ReworkGridTuning.EdgePadding + line.Level * ReworkGridTuning.HeaderIndent;
            int y = (int)dims.Y + ReworkGridTuning.GridTop + row * rowHeight;
            int size = ReworkGridTuning.TriangleSize(line.Level);
            triangleCenter = new Point(x + (int)textWidth + 10 + size, y + ReworkGridTuning.CellSize / 2);
            int right = triangleCenter.X + size + 6;
            return new Rectangle(x - 4, y + 2, right - x + 4, ReworkGridTuning.CellSize - 6);
        }

        /// <summary>What is under the mouse: a header (type = -1, lineIndex = its line) or a cell (type = its item).</summary>
        private bool HoverAt(CalculatedStyle dims, out int lineIndex, out int type)
        {
            lineIndex = -1;
            type = -1;
            int first = FirstLine();
            for (int row = 0; row < ReworkGridTuning.VisibleRows; row++)
            {
                int index = first + row;
                if (index >= lines.Count)
                    return false;

                GridLine line = lines[index];
                if (line.Level >= 0)
                {
                    if (HeaderRect(dims, row, line, out _).Contains(Main.mouseX, Main.mouseY))
                    {
                        lineIndex = index;
                        return true;
                    }
                    continue;
                }

                for (int column = 0; column < line.Cells.Count; column++)
                {
                    if (CellRect(dims, row, column).Contains(Main.mouseX, Main.mouseY))
                    {
                        lineIndex = index;
                        type = line.Cells[column];
                        return true;
                    }
                }
            }
            return false;
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
            ControlRects(dims, out Rectangle searchRect, out Rectangle filterRect, out Rectangle allRect);
            Point mouse = new Point(Main.mouseX, Main.mouseY);

            searchFocused = false;
            if (searchRect.Contains(mouse))
            {
                searchFocused = true;
                Main.clrInput(); // forget keys that were typed before the box was clicked
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

            if (HoverAt(dims, out int lineIndex, out int type))
            {
                if (type >= 0)
                    Toggle(type);
                else
                    ToggleSection(lines[lineIndex].Key);
            }
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            // Scroll the grid one line per notch. When the grid is already at its end, the wheel goes on to the config page instead.
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

        private void ToggleSection(string key)
        {
            if (!collapsed.Remove(key))
                collapsed.Add(key);
        }

        // ------------------------------------------------------------------ search, filter, toggle all

        private static string Text(string key, string fallback)
        {
            return Language.GetOrRegister("Mods.IsmailsWeaponReworks.Configs.ReworksToggleConfig." + key, () => fallback).Value;
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

        /// <summary>The controls row, left to right: search box (fills the free width), filter button, toggle-all button.</summary>
        private void ControlRects(CalculatedStyle dims, out Rectangle searchRect, out Rectangle filterRect, out Rectangle allRect)
        {
            int height = ReworkGridTuning.ButtonHeight;
            int gap = ReworkGridTuning.ControlGap;
            int y = (int)dims.Y + ReworkGridTuning.HeaderHeight + (ReworkGridTuning.ControlsHeight - height) / 2;
            int right = (int)(dims.X + dims.Width) - ReworkGridTuning.EdgePadding;

            allRect = new Rectangle(right - ReworkGridTuning.ToggleAllWidth, y, ReworkGridTuning.ToggleAllWidth, height);
            filterRect = new Rectangle(allRect.X - gap - ReworkGridTuning.FilterWidth, y, ReworkGridTuning.FilterWidth, height);

            int left = (int)dims.X + ReworkGridTuning.EdgePadding;
            searchRect = new Rectangle(left, y, Math.Max(ReworkGridTuning.SearchMinWidth, filterRect.X - gap - left), height);
        }

        private bool OverControls(CalculatedStyle dims)
        {
            ControlRects(dims, out Rectangle searchRect, out Rectangle filterRect, out Rectangle allRect);
            Point mouse = new Point(Main.mouseX, Main.mouseY);
            return searchRect.Contains(mouse) || filterRect.Contains(mouse) || allRect.Contains(mouse);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            ShowSavedNoticeIfRequested();

            // A press anywhere outside the search box ends typing.
            if (searchFocused && Main.mouseLeft)
            {
                ControlRects(GetDimensions(), out Rectangle searchRect, out _, out _);
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

        // ------------------------------------------------------------------ saved inside a world

        /// <summary>
        /// After the config was saved inside a world (ReworksToggleConfig.OnChanged leaves a time stamp) the screen's notification line
        /// says the changes wait for the next reload. tModLoader's own message is green "Config saved"; the line is reached through
        /// reflection because its class is internal. If that ever fails, the same text goes to the chat instead.
        /// </summary>
        private void ShowSavedNoticeIfRequested()
        {
            ReworkRegistry registry = ModContent.GetInstance<ReworkRegistry>();
            if (registry == null || registry.SaveNoticeAt < 0)
                return;

            long age = Environment.TickCount64 - registry.SaveNoticeAt;
            registry.SaveNoticeAt = -1;
            if (age > ReworkGridTuning.SaveNoticeMilliseconds)
                return;

            string text = Text("SavedNotice", "Changes saved and will be applied on next reload");
            try
            {
                Type interfaceType = typeof(Mod).Assembly.GetType("Terraria.ModLoader.UI.Interface");
                object screen = interfaceType?.GetField("modConfig", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
                MethodInfo setMessage = screen?.GetType().GetMethod("SetMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (setMessage != null)
                {
                    setMessage.Invoke(screen, new object[] { text, Color.Orange });
                    return;
                }
            }
            catch (Exception)
            {
            }
            Main.NewText(text, Color.Orange);
        }

        // ------------------------------------------------------------------ sections

        private static int CompareNames(int a, int b)
        {
            int result = string.Compare(Lang.GetItemNameValue(a), Lang.GetItemNameValue(b), StringComparison.CurrentCultureIgnoreCase);
            return result != 0 ? result : a.CompareTo(b);
        }

        private static string[] SplitFolder(string folder)
        {
            string[] raw = folder.Split('/');
            return new[] { raw.Length > 0 ? raw[0] : "Melee", raw.Length > 1 ? raw[1] : "PreHardmode", raw.Length > 2 ? raw[2] : "Others" };
        }

        private static int OrderIndex(string[] order, string name)
        {
            int index = Array.IndexOf(order, name);
            return index < 0 ? order.Length : index;
        }

        private static int CompareFolders(string a, string b)
        {
            string[] partsA = SplitFolder(a);
            string[] partsB = SplitFolder(b);
            string[][] orders = { ReworkGridTuning.ClassOrder, ReworkGridTuning.ProgressionOrder, ReworkGridTuning.SubclassOrder };
            for (int level = 0; level < 3; level++)
            {
                int result = OrderIndex(orders[level], partsA[level]).CompareTo(OrderIndex(orders[level], partsB[level]));
                if (result == 0)
                    result = string.Compare(partsA[level], partsB[level], StringComparison.Ordinal);
                if (result != 0)
                    return result;
            }
            return 0;
        }

        /// <summary>"PreHardmode" becomes "Pre-Hardmode", "Broadsword" becomes "Broadswords", anything else gets spaces between its words.</summary>
        private static string SectionLabel(string part)
        {
            string fallback;
            switch (part)
            {
                case "PreHardmode": fallback = "Pre-Hardmode"; break;
                case "Broadsword": fallback = "Broadswords"; break;
                case "Shortsword": fallback = "Shortswords"; break;
                default: fallback = System.Text.RegularExpressions.Regex.Replace(part, "(?<=[a-z])(?=[A-Z])", " "); break;
            }
            return Text("Section_" + part, fallback);
        }

        /// <summary>
        /// Fills lines from allTypes: the search text and the filter pick the items, they are sorted by name, grouped into sections
        /// (class, progression, subclass; a section with no matching item is left out), a collapsed section keeps only its header, and
        /// the items of a section are cut into rows of columns cells.
        /// </summary>
        private void BuildLines(HashSet<int> disabled, int columns)
        {
            lines.Clear();
            string needle = search.Trim();

            List<int> shown = new List<int>();
            foreach (int type in allTypes)
            {
                if (filter == FilterMode.Active && disabled.Contains(type))
                    continue;
                if (filter == FilterMode.Inactive && !disabled.Contains(type))
                    continue;
                if (needle.Length > 0 && Lang.GetItemNameValue(type).IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) < 0)
                    continue;
                shown.Add(type);
            }
            shown.Sort(CompareNames);
            shownCount = shown.Count;

            ReworkRegistry registry = ModContent.GetInstance<ReworkRegistry>();
            Dictionary<string, List<int>> byFolder = new Dictionary<string, List<int>>();
            foreach (int type in shown)
            {
                if (!registry.Folders.TryGetValue(type, out string folder))
                    folder = "Melee/PreHardmode/Others";
                if (!byFolder.TryGetValue(folder, out List<int> items))
                {
                    items = new List<int>();
                    byFolder[folder] = items;
                }
                items.Add(type);
            }

            List<string> folders = byFolder.Keys.ToList();
            folders.Sort(CompareFolders);

            string[] lastKey = new string[3];
            foreach (string folder in folders)
            {
                string[] parts = SplitFolder(folder);
                bool hidden = false; // a collapsed section above hides everything below it
                for (int level = 0; level < 3; level++)
                {
                    string key = string.Join("/", parts.Take(level + 1));
                    bool isCollapsed = collapsed.Contains(key);
                    if (!hidden && key != lastKey[level])
                        lines.Add(new GridLine { Level = level, Key = key, Label = SectionLabel(parts[level]), Collapsed = isCollapsed });
                    lastKey[level] = key;
                    hidden |= isCollapsed;
                }

                if (hidden)
                    continue;

                List<int> sectionItems = byFolder[folder];
                for (int start = 0; start < sectionItems.Count; start += columns)
                {
                    GridLine row = new GridLine();
                    for (int i = start; i < Math.Min(sectionItems.Count, start + columns); i++)
                        row.Cells.Add(sectionItems[i]);
                    lines.Add(row);
                }
            }
        }

        // ------------------------------------------------------------------ controls row

        private void DrawControls(SpriteBatch spriteBatch, CalculatedStyle dims, HashSet<int> disabled)
        {
            ControlRects(dims, out Rectangle searchRect, out Rectangle filterRect, out Rectangle allRect);
            Point mouse = new Point(Main.mouseX, Main.mouseY);
            bool over = IsMouseHovering;
            float scale = ReworkGridTuning.ControlTextScale;
            var font = FontAssets.MouseText.Value;

            // Search box: the hint while empty, the text with a blinking bar while typing; a long text shows its end.
            Utils.DrawInvBG(spriteBatch, searchRect, searchFocused ? ReworkGridTuning.SearchFocusedColor : ReworkGridTuning.ControlColor);
            blinkTimer++;
            Vector2 textPosition = new Vector2(searchRect.X + 8f, searchRect.Center.Y + ReworkGridTuning.ButtonTextOffsetY);
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
            DrawButton(spriteBatch, filterRect, Text("ShowLabel", "Show") + ": " + FilterName, over && filterRect.Contains(mouse));
            DrawButton(spriteBatch, allRect, allOff ? Text("EnableAll", "Enable all") : Text("DisableAll", "Disable all"), over && allRect.Contains(mouse));

            if (over)
            {
                if (filterRect.Contains(mouse))
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
            {
                Vector2 position = rect.Center.ToVector2() + new Vector2(0f, ReworkGridTuning.ButtonTextOffsetY);
                Utils.DrawBorderString(spriteBatch, label, position, Color.White, ReworkGridTuning.ControlTextScale, 0.5f, 0.5f);
            }
        }

        // ------------------------------------------------------------------ drawing

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dims = GetDimensions();
            TypeIntoSearch();
            HashSet<int> disabled = DisabledSet();
            int columns = Columns(dims);
            BuildLines(disabled, columns);
            float rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            scrollbar.SetView(ReworkGridTuning.VisibleRows * rowHeight, Math.Max(lines.Count, ReworkGridTuning.VisibleRows) * rowHeight);

            int hoverLine = -1;
            int hoverType = -1;
            bool hovering = IsMouseHovering && HoverAt(dims, out hoverLine, out hoverType);

            // The panel, the label and the field's own tooltip. While a cell, a header or a control is hovered the field tooltip is switched off.
            TooltipFunction = hovering || (IsMouseHovering && OverControls(dims)) ? null : fieldTooltip;
            base.DrawSelf(spriteBatch);

            int activeCount = allTypes.Count(type => !disabled.Contains(type));
            Utils.DrawBorderString(spriteBatch, $"{ActiveText}: {activeCount}/{allTypes.Count}",
                new Vector2(dims.X + dims.Width - ReworkGridTuning.EdgePadding, dims.Y + 8f), Color.White, 0.8f, 1f, 0f);

            DrawControls(spriteBatch, dims, disabled);

            if (lines.Count == 0)
            {
                Utils.DrawBorderString(spriteBatch, Text("NoMatches", "No reworks match"),
                    new Vector2(dims.X + dims.Width * 0.5f, dims.Y + ReworkGridTuning.GridTop + 20f), Color.Gray, 0.9f, 0.5f, 0f);
            }

            int first = FirstLine();
            for (int row = 0; row < ReworkGridTuning.VisibleRows; row++)
            {
                int index = first + row;
                if (index >= lines.Count)
                    break;

                GridLine line = lines[index];
                if (line.Level >= 0)
                {
                    DrawHeader(spriteBatch, dims, row, line, index == hoverLine);
                    continue;
                }

                for (int column = 0; column < line.Cells.Count; column++)
                {
                    int type = line.Cells[column];
                    DrawCell(spriteBatch, CellRect(dims, row, column), type, !disabled.Contains(type), index == hoverLine && type == hoverType);
                }
            }

            if (hovering && hoverType >= 0)
            {
                bool active = !disabled.Contains(hoverType);
                string state = active ? $"[c/55FF55:{ActiveText}]" : $"[c/FF5555:{InactiveText}]";
                ShowTip(Lang.GetItemNameValue(hoverType) + "\n" + state);
            }
        }

        /// <summary>A section header: its name, a triangle to the right of it (pointing down while open, right while collapsed) and a thin line under it.</summary>
        private void DrawHeader(SpriteBatch spriteBatch, CalculatedStyle dims, int row, GridLine line, bool hovered)
        {
            Rectangle hit = HeaderRect(dims, row, line, out Point triangleCenter);
            Color color = hovered ? ReworkGridTuning.HeaderHoverColor : ReworkGridTuning.HeaderColor;
            int rowHeight = ReworkGridTuning.CellSize + ReworkGridTuning.CellGap;
            int x = (int)dims.X + ReworkGridTuning.EdgePadding + line.Level * ReworkGridTuning.HeaderIndent;
            int y = (int)dims.Y + ReworkGridTuning.GridTop + row * rowHeight;

            Vector2 position = new Vector2(x, y + ReworkGridTuning.CellSize * 0.5f + ReworkGridTuning.HeaderTextOffsetY);
            Utils.DrawBorderString(spriteBatch, line.Label, position, color, ReworkGridTuning.HeaderScale(line.Level), 0f, 0.5f);

            // The triangle: one pixel row (down) or column (right) at a time.
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int size = ReworkGridTuning.TriangleSize(line.Level);
            for (int i = 0; i < size; i++)
            {
                int half = size - i;
                if (line.Collapsed)
                    spriteBatch.Draw(pixel, new Rectangle(triangleCenter.X - size / 2 + i, triangleCenter.Y - half + 1, 1, 2 * half - 1), color);
                else
                    spriteBatch.Draw(pixel, new Rectangle(triangleCenter.X - half + 1, triangleCenter.Y - size / 2 + i, 2 * half - 1, 1), color);
            }

            int lineRight = (int)(dims.X + dims.Width) - ReworkGridTuning.EdgePadding - ReworkGridTuning.ScrollbarSpace + ReworkGridTuning.CellGap;
            spriteBatch.Draw(pixel, new Rectangle(x, y + ReworkGridTuning.CellSize - 1, Math.Max(0, lineRight - x), 1), Color.White * ReworkGridTuning.HeaderLineAlpha(line.Level));
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