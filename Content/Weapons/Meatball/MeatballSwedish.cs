using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Meatball
{
    /// <summary>
    /// The Meatball's "Swedish" look, only in the getfixedboi seed (the Zenith seed, Main.zenithWorld): its own item art, head art (the
    /// spinning head and the torn-off ball share one texture) and chain art, plus an extra tooltip. Each PNG sits next to this file:
    /// MeatballItem_Swedish.png, MeatballProjectile_Swedish.png, MeatballChain_Swedish.png. A missing PNG falls back to the normal art.
    /// To change which seed it is, change Active only.
    /// MULTIPLAYER: visuals only, nothing synced. Every machine reads the world's own seed flag (the server sends it on join).
    /// </summary>
    public static class MeatballSwedish
    {
        public static bool Active => Main.zenithWorld;

        // The texture called name (a file next to this one) while the seed is on and the PNG exists, otherwise null.
        // The caller keeps the asset and tried flag so the file is only looked up once.
        public static Texture2D Texture(string name, ref Asset<Texture2D> asset, ref bool tried)
        {
            if (!Active)
                return null;
            return EasterEgg.Load("IsmailsWeaponReworks/Content/Weapons/Meatball/" + name, ref asset, ref tried);
        }
    }
}