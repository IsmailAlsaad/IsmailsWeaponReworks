using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Doing something to every enemy inside a circle (a blood explosion now, the Dark Lance's pop in patch 6). OWNER'S CLIENT ONLY: the
    /// caller decides who is hit and whatever the action does must sync by itself (NPC.SimpleStrikeNPC and NPC.AddBuff both do).
    /// (Patch 6 adds ApplyBuff when the Dark Lance's Shadowflame pop needs it.)
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
    }
}