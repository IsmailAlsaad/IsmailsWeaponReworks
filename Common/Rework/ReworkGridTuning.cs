using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common.Rework
{
    /// <summary>
    /// Every number of the Reworks toggle config's weapon grid in one place (expression-bodied properties on purpose: hot reload can
    /// change a method body but not a const or static field).
    /// </summary>
    public static class ReworkGridTuning
    {
        // --- Layout (pixels) ---
        public static int CellSize => 44;
        public static int CellGap => 4;
        public static int VisibleRows => 6;            // rows shown at once; more rows scroll
        public static int HeaderHeight => 34;          // the label row above the grid
        public static int EdgePadding => 8;
        public static int ScrollbarSpace => 28;        // room kept free on the right for the scroll bar
        public static int BottomPadding => 8;
        public static int IconPadding => 6;            // space between a cell's edge and its item icon

        // --- Controls row (search box and buttons, between the header and the grid) ---
        public static int ControlsHeight => 38;        // the row's height
        public static int ButtonHeight => 28;
        public static int ControlGap => 6;
        public static int FilterWidth => 140;
        public static int ToggleAllWidth => 112;
        public static int SearchMinWidth => 100;
        public static int SearchMaxLength => 40;
        public static float ControlTextScale => 0.8f;
        public static int GridTop => HeaderHeight + ControlsHeight; // where the first row of cells starts
        public static int ButtonTextOffsetY => 4;      // moves the text of the buttons and the search box DOWN this many pixels (negative = up)

        // --- Section headers (class, progression, subclass) ---
        public static int HeaderIndent => 0;          // every header level is indented this much more than the one above
        public static int HeaderTextOffsetY => 4;      // moves the header text DOWN this many pixels (negative = up)
        public static float HeaderScale(int level) => level == 0 ? 1.15f : level == 1 ? 0.95f : 0.8f;
        public static int TriangleSize(int level) => 7 - level; // the collapse triangle next to a header name
        public static float HeaderLineAlpha(int level) => level == 0 ? 0.6f : level == 1 ? 0.4f : 0.25f; // the thin line under a header

        // --- Order of the sections (folder names; anything not listed comes after the listed ones, alphabetically) ---
        public static string[] ClassOrder => new[] { "Melee", "Ranged", "Magic", "Summon" };
        public static string[] ProgressionOrder => new[] { "PreHardmode", "Hardmode" };
        public static string[] SubclassOrder => new[] { "Broadsword", "Shortsword", "Spears", "Boomerangs", "Flails", "Yoyos", "Others" };

        // --- Saving inside a world ---
        public static int SaveNoticeMilliseconds => 3000; // the orange "saved" notification is only shown for a save made this recently

        // --- Look ---
        public static float InactiveIconAlpha => 0.4f; // a switched-off weapon's icon is dimmed
        public static float HoverBrighten => 0.25f;    // the hovered cell is lerped this much toward white
        public static Color ActiveCell => new Color(63, 82, 151) * 0.8f;
        public static Color InactiveCell => new Color(80, 45, 55) * 0.8f;
        public static Color FallbackCross => new Color(220, 40, 40); // the X drawn in code until ReworkOffX.png exists
        public static Color ControlColor => new Color(63, 82, 151) * 0.8f;       // buttons and the search box
        public static Color HeaderColor => Color.White;
        public static Color HeaderHoverColor => new Color(255, 240, 150);
        public static Color SearchFocusedColor => new Color(95, 115, 190) * 0.9f; // the search box while typing
    }
}