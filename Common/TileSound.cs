using System;
using System.Reflection;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Plays the sound vanilla plays when a tile is hit by a pick (dirt thud, stone tink, wood, glass, a modded tile's own sound), for the
    /// tile at (i, j). It calls the game's own WorldGen.KillTile_PlaySounds with fail = true (a hit that does not break the tile), found
    /// by reflection once because the method is private; if it cannot be found or fails, PlayHit returns false so the caller can use a
    /// fallback sound. Client only, never synced.
    /// </summary>
    public static class TileSound
    {
        static Action<int, int, bool, Tile> play;
        static bool unavailable;

        public static bool PlayHit(int i, int j)
        {
            if (Main.dedServ || unavailable || !WorldGen.InWorld(i, j, 1))
                return false;
            try
            {
                if (play == null)
                {
                    MethodInfo method = typeof(WorldGen).GetMethod("KillTile_PlaySounds", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int), typeof(int), typeof(bool), typeof(Tile) }, null);
                    if (method == null)
                    {
                        unavailable = true;
                        return false;
                    }
                    play = (Action<int, int, bool, Tile>)Delegate.CreateDelegate(typeof(Action<int, int, bool, Tile>), method);
                }
                play(i, j, true, Main.tile[i, j]);
                return true;
            }
            catch (Exception)
            {
                unavailable = true;
                return false;
            }
        }
    }
}