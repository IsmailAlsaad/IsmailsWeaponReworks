using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Doing something to every enemy inside a circle (a blood explosion now, the Dark Lance's pop in patch 6). OWNER'S CLIENT ONLY: the
    /// caller decides who is hit and whatever the action does must sync by itself (NPC.SimpleStrikeNPC and NPC.AddBuff both do).
    /// ApplyBuff puts a buff on every enemy inside the circle (the Dark Lance's Shadowflame pop).
    /// </summary>
    public static class AreaEffect
    {
        // Calls 'action' for every active, hittable enemy whose box is within 'radius' of 'center' (its own size counts toward the reach).
        public static void ForEachNpc(Vector2 center, float radius, Action<NPC> action)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.CanBeChasedBy())
                    continue;
                float reach = radius + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.DistanceSquared(npc.Center, center) <= reach * reach)
                    action(npc);
            }
        }

        // Puts 'buffId' for 'ticks' on every enemy in the circle. Owner's client only; NPC.AddBuff syncs it.
        public static void ApplyBuff(Vector2 center, float radius, int buffId, int ticks)
        {
            ForEachNpc(center, radius, npc => npc.AddBuff(buffId, ticks));
        }
    }
}