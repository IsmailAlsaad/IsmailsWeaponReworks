using System;
using System.Collections.Generic;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// Visual-only hit effects for the OTHER players' machines. OnHitNPC only runs on the owner's client, so without this the
    /// owner's hit sparks would be invisible to everyone else. Every other machine runs the projectile's own hit test against its
    /// own copy of the enemies and spawns the effect itself (no damage, no packets). Extracted from the Ice Boomerang's R2 code when the
    /// Enchanted Boomerang needed the same thing.
    /// </summary>
    public class RemoteHitTracker
    {
        readonly HashSet<int> seen = new HashSet<int>();
        int segment = int.MinValue;

        // For a projectile that keeps flying: call once per tick on non-owner machines. An enemy triggers the effect once per
        // segment; whenever segmentId changes (a new phase or leg), every enemy may trigger it again.
        public void Update(Projectile projectile, int segmentId, Action<NPC> onFirstTouch)
        {
            if (Main.dedServ)
                return;
            if (segmentId != segment)
            {
                segment = segmentId;
                seen.Clear();
            }
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || seen.Contains(i) || !npc.CanBeChasedBy(projectile) || !Touching(projectile, npc))
                    continue;
                seen.Add(i);
                onFirstTouch(npc);
            }
        }

        // For a one-shot projectile such as a blast: call once on its first tick on non-owner machines.
        public static void TouchOnce(Projectile projectile, Action<NPC> onTouch)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.CanBeChasedBy(projectile) && Touching(projectile, npc))
                    onTouch(npc);
            }
        }

        // Uses the projectile's own Colliding (swept lines, circles, ...) when it has one.
        static bool Touching(Projectile projectile, NPC npc)
        {
            bool? custom = projectile.ModProjectile?.Colliding(projectile.Hitbox, npc.Hitbox);
            return custom ?? projectile.Hitbox.Intersects(npc.Hitbox);
        }
    }
}