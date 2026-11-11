using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The three loop moments of a Man on Trapeze yoyo, as empty hooks (the Artery draws blood, the Malaise corruption): the string caught
    /// an enemy (LoopCatch), one shred hit (LoopShred) and the loop closed (LoopClose). Every machine calls them for the same event; all of
    /// it is visual, nothing here is synced.
    /// </summary>
    public class LoopYoyoFx : YoyoFx
    {
        public virtual void LoopCatch(Vector2 position)
        {
        }

        public virtual void LoopShred(Vector2 center, float noose)
        {
        }

        public virtual void LoopClose(Vector2 position)
        {
        }
    }
}