using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// The Muramasa's green easter egg: in the getfixedboi seed (Main.zenithWorld), or on any world while the character on this machine is
    /// named Eudaemonia, the sword uses GreenMuramasa.png (next to this file) as its item art and its sword art, every blue becomes green
    /// (MuramasaTuning's colors, dust, lights) and the tooltip gets an extra line. Sounds are unchanged. A missing PNG falls back to the
    /// vanilla art.
    /// MULTIPLAYER: visuals only, nothing synced. The name rule is read on each machine from its own character, so the Eudaemonia player
    /// sees every Muramasa green on their own screen; other players see theirs the normal way.
    /// </summary>
    public static class MuramasaGreen
    {
        public static bool Active => EasterEgg.Zenith || EasterEgg.LocalPlayerNamed("Eudaemonia");

        static Asset<Texture2D> art;
        static bool artTried;

        // GreenMuramasa.png while active and the file exists, otherwise null.
        public static Texture2D Texture()
        {
            return Active ? EasterEgg.Load("IsmailsWeaponReworks/Content/Weapons/Muramasa/GreenMuramasa", ref art, ref artTried) : null;
        }

        // The sword sprite the held projectiles draw: the green one when it applies, otherwise the vanilla one they were given.
        public static Texture2D Sprite(Texture2D vanilla)
        {
            return Texture() ?? vanilla;
        }

        // Called from MuramasaGlobalItem.Unload so a reload never keeps a texture of the unloaded mod.
        public static void Unload()
        {
            art = null;
            artTried = false;
        }
    }
}