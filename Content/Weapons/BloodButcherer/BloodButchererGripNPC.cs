using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>
    /// What the Blood Butcherer does to an enemy it has gripped. The swing projectile (on every machine, the server included) refreshes
    /// HoldTick every tick while the enemy is pinned in the jaws, so a projectile that dies for any reason releases the enemy by itself a
    /// moment later. While held the enemy's AI is off and it cannot hurt the player; after the throw (LaunchTick) its AI stays off for
    /// BloodButchererTuning.LaunchAiOffTicks, with gravity applied here by hand because the AI that normally does it is not running.
    /// Normal tile collision and movement still run. Purely a function of the synced projectile, so it needs no packets; the server
    /// also flags netUpdate while the enemy is held so its copy stays the authority.
    /// </summary>
    public class BloodButchererGripNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public long HoldTick = -1000;    // Main.GameUpdateCount stamp refreshed by the projectile while the enemy is pinned
        public long LaunchTick = -1000;  // Main.GameUpdateCount stamp of the throw

        public bool Held => Main.GameUpdateCount - HoldTick <= 2;
        public bool Launched => Main.GameUpdateCount - LaunchTick < BloodButchererTuning.LaunchAiOffTicks;

        public override bool PreAI(NPC npc)
        {
            if (Held)
            {
                npc.velocity = Vector2.Zero;
                return false;
            }
            if (Launched)
            {
                if (!npc.noGravity)
                    npc.velocity.Y = System.Math.Min(npc.velocity.Y + BloodButchererTuning.LaunchGravity, BloodButchererTuning.LaunchMaxFall);
                return false;
            }
            return true;
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            return !(Held || Launched);
        }
    }
}