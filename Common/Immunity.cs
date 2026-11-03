using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// "Nothing can hurt this player for N ticks", for any weapon that needs i-frames (a dash, a spin). It does NOT touch vanilla's own
    /// immune timer: ImmunityPlayer answers ModPlayer.ImmuneTo, which Player.Hurt asks before it does anything, so there is no blinking,
    /// no knockback and no side effect on the player's normal hit cooldown, and a hit right after the immunity ends works as usual.
    /// MULTIPLAYER: Grant only works on the player's own client (the one that decides damage to its player); everyone else ignores it.
    /// </summary>
    public static class Immunity
    {
        // Immune for at least 'ticks' more ticks (a shorter grant never shortens a longer one).
        public static void Grant(Player player, int ticks)
        {
            if (ticks <= 0 || player.whoAmI != Main.myPlayer)
                return;
            ImmunityPlayer state = player.GetModPlayer<ImmunityPlayer>();
            state.TicksLeft = Math.Max(state.TicksLeft, ticks);
        }

        public static bool Active(Player player)
        {
            return player.GetModPlayer<ImmunityPlayer>().TicksLeft > 0;
        }

        public static void Clear(Player player)
        {
            player.GetModPlayer<ImmunityPlayer>().TicksLeft = 0;
        }
    }

    public class ImmunityPlayer : ModPlayer
    {
        public int TicksLeft;

        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
        {
            return TicksLeft > 0;
        }

        public override void PostUpdate()
        {
            if (TicksLeft > 0)
                TicksLeft--;
        }
    }
}