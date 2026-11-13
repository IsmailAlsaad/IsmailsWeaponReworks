using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// What the weapons' easter eggs share: the getfixedboi seed (the Zenith seed, Main.zenithWorld), a check for the local character's
    /// name, and a loader for the egg's own textures (a missing PNG gives null, so the caller falls back to the normal art).
    /// Visuals only, nothing synced.
    /// </summary>
    public static class EasterEgg
    {
        public static bool Zenith => Main.zenithWorld;

        // True when the character on this machine has this name (ignoring case). Never true on a dedicated server.
        public static bool LocalPlayerNamed(string name)
        {
            return !Main.dedServ && Main.LocalPlayer != null && string.Equals(Main.LocalPlayer.name, name, StringComparison.OrdinalIgnoreCase);
        }

        // The texture at path ("ModName/Folder/File", no extension) if the PNG exists, otherwise null. The caller keeps the asset and the
        // tried flag, so the file is only looked up once.
        public static Texture2D Load(string path, ref Asset<Texture2D> asset, ref bool tried)
        {
            if (!tried)
            {
                tried = true;
                ModContent.RequestIfExists<Texture2D>(path, out asset);
            }
            return asset != null && asset.IsLoaded ? asset.Value : null;
        }
    }
}