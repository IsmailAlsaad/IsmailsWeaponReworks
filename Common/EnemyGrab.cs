using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The two questions a weapon asks before it grabs an enemy. Extracted from the Blood Butcherer (its jaws) when the Night's Edge needed
    /// the same rule for its impale; CarriedNPC.CanCarry builds on CanGrip too. Pure functions of the NPC, so every machine agrees.
    /// </summary>
    public static class EnemyGrab
    {
        // Any enemy that can be damaged. NOT NPC.CanBeChasedBy: it is false for the target dummy (and for critters), so those were never caught.
        public static bool CanBite(NPC npc)
        {
            return npc.active && !npc.friendly && !npc.dontTakeDamage;
        }

        // An enemy that can be MOVED (pinned, carried, impaled, thrown): not a boss, not a worm segment, not immovable, not bigger than
        // maxSize pixels (width or height).
        public static bool CanGrip(NPC npc, float maxSize)
        {
            return !npc.boss && npc.knockBackResist > 0f && npc.realLife < 0 && Math.Max(npc.width, npc.height) <= maxSize;
        }
    }
}