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
        // The collapse triangle next to a header name is a text glyph (U+25BC down, U+25B6 right) drawn like the header text itself.
        public static float TriangleScale(int level) => HeaderScale(level) * 1f; // 1f = as big as the header text; change the factor to resize
        public static int TriangleGap => 6;                      // pixels between the header name and the triangle
        public static int TriangleOffsetX => 0;                  // nudges the triangle (positive = right)
        public static int TriangleOffsetY => 0;                  // nudges the triangle (positive = down)
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
        public static Color HeaderHoverColor => new Color(255, 240, 150);
        public static Color ActiveColor => new Color(85, 255, 85);       // green text: "Active" in the filter button, "Enable all"
        public static Color InactiveColor => new Color(255, 85, 85);     // red text: "Inactive" in the filter button, "Disable all"
        public static Color MeleeColor => new Color(230, 50, 50);        // the Melee header (a class without a color here stays white)
        public static Color SubHeaderTint => new Color(255, 225, 235);   // pinkish white: the lower header levels lerp toward it
        public static Color HeaderBackgroundColor => Color.Lerp(HeaderHoverColor, Color.White, 0.5f); // whitish version of the yellow the text turns on hover
        public static float HeaderBackgroundAlpha => 0.22f;              // the band behind a hovered header: HeaderBackgroundColor times this
        public static int HeaderBackgroundInset => 2;                    // the band stops this many pixels short of the panel's left and right edge
        public static Color ClassColor(string className) => className == "Melee" ? MeleeColor : Color.White;
        public static float SubHeaderLerp(int level) => level == 0 ? 0f : level == 1 ? 0.45f : 0.8f; // how far each level lerps toward SubHeaderTint
        public static Color HeaderColor(string className, int level) => ClassColor(className) == Color.White ? Color.White : Color.Lerp(ClassColor(className), SubHeaderTint, SubHeaderLerp(level));
        public static Color SearchFocusedColor => new Color(95, 115, 190) * 0.9f; // the search box while typing
    }
}