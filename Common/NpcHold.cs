using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Holds an enemy exactly in place for as long as something keeps renewing the hold (the Artery's loop). The holder calls
    /// NpcHold.Hold(npc) every tick on EVERY machine (the server included) from state it already shares, so a holder that dies or lets go
    /// for any reason frees the enemy by itself a moment later (the lease runs out after 2 ticks). While held the enemy's vanilla AI does
    /// not run and its speed is zeroed: it does not walk, jump, fly, fall, attack or get knocked away. Nothing is synced here.
    /// </summary>
    public class NpcHold : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public long HoldTick = -1000;   // Main.GameUpdateCount stamp of the last renewal

        public bool Held => Main.GameUpdateCount - HoldTick <= 2;

        public static void Hold(NPC npc)
        {
            npc.GetGlobalNPC<NpcHold>().HoldTick = (long)Main.GameUpdateCount;
        }

        public override bool PreAI(NPC npc)
        {
            if (!Held)
                return true;
            npc.velocity = Vector2.Zero;
            return false;
        }

        public override void PostAI(NPC npc)
        {
            if (Held)
                npc.velocity = Vector2.Zero;   // nothing is left over from a hit this tick
        }
    }
}