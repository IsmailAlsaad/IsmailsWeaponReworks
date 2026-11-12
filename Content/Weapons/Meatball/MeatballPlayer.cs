using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Meatball
{
    /// <summary>
    /// The Meatball's regrow timer: set by the owner's client when the ball tears loose, counted down every tick, and read by
    /// MeatballGlobalItem.CanUseItem so the hand stays empty for a moment. Only the owner's client ever reads it (CanUseItem runs there).
    /// </summary>
    public class MeatballPlayer : ModPlayer
    {
        public int RegrowTicks;

        public override void PostUpdate()
        {
            if (RegrowTicks > 0)
                RegrowTicks--;
        }
    }
}