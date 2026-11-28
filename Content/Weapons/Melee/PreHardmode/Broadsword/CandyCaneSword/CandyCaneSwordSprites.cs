using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// Ismail's CandyCaneSword.png: the sword in the four candy colors, 4 cells side by side (red, green, blue, yellow; 44 x 70 each). Each cell is cut out
    /// once into its own texture (the shared blade and the item draw a whole texture). Null / zero until the file exists: the vanilla sprite is used then.
    /// Visual only, nothing synced.
    /// </summary>
    public static class CandyCaneSwordSprites
    {
        public static string SheetPath => "IsmailsWeaponReworks/Content/Weapons/Melee/PreHardmode/Broadsword/CandyCaneSword/CandyCaneSword";
        public static int Cells => 4;

        static Asset<Texture2D> sheet;
        static bool tried;
        static Texture2D[] cells;

        static Texture2D Sheet()
        {
            if (Main.dedServ)
                return null;
            if (!tried)
            {
                tried = true;
                ModContent.RequestIfExists<Texture2D>(SheetPath, out sheet, AssetRequestMode.ImmediateLoad);
            }
            return sheet != null && sheet.IsLoaded ? sheet.Value : null;
        }

        // The size of one cell, or zero while the sheet does not exist.
        public static Vector2 CellSize
        {
            get
            {
                Texture2D source = Sheet();
                return source == null ? Vector2.Zero : new Vector2(source.Width / Cells, source.Height);
            }
        }

        // The sword in the color of 'kind', or null while the sheet does not exist.
        public static Texture2D Cell(CandyKind kind)
        {
            Texture2D source = Sheet();
            int index = (int)kind;
            if (source == null || index < 0 || index >= Cells)
                return null;

            if (cells == null)
                cells = new Texture2D[Cells];
            if (cells[index] == null)
            {
                int width = source.Width / Cells;
                int height = source.Height;
                Color[] data = new Color[width * height];
                source.GetData(0, new Rectangle(index * width, 0, width, height), data, 0, data.Length);
                Texture2D cell = new Texture2D(Main.instance.GraphicsDevice, width, height);
                cell.SetData(data);
                cells[index] = cell;
            }
            return cells[index];
        }

        // Called from CandyCaneSwordGlobalItem.Unload so a reload never keeps a texture of the unloaded mod.
        public static void Unload()
        {
            Texture2D[] old = cells;
            cells = null;
            sheet = null;
            tried = false;
            if (old == null)
                return;
            Main.QueueMainThreadAction(() =>
            {
                foreach (Texture2D texture in old)
                    texture?.Dispose();
            });
        }
    }
}