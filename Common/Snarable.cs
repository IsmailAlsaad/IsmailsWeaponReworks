using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Which enemies a grabbing weapon may take hold of (the Blood Butcherer's jaws, the Artery's loop). CanBite: any enemy that can be
    /// damaged (NOT NPC.CanBeChasedBy: it is false for the target dummy and for critters). CanGrip: one that can also be held and moved:
    /// not a boss, not immovable, not a worm segment, not bigger than maxSize (width or height). Everything else is still bitten or
    /// looped, just not held.
    /// </summary>
    public static class Snarable
    {
        public static bool CanBite(NPC npc)
        {
            return npc.active && !npc.friendly && !npc.dontTakeDamage;
        }

        public static bool CanGrip(NPC npc, float maxSize)
        {
            return !npc.boss && npc.knockBackResist > 0f && npc.realLife < 0 && Math.Max(npc.width, npc.height) <= maxSize;
        }
    }
}