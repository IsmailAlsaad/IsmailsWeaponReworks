using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// What the Amazon's barbed ground vines do to an enemy they hold. The vine projectile (on every machine, the server included) refreshes
    /// HoldTick every tick while it holds the enemy, so a vine that dies or lets go for any reason releases the enemy by itself a moment
    /// later. While held the enemy cannot walk: its horizontal speed is zeroed after its AI has run (and its vertical speed too when it
    /// flies), so gravity, jumping up and down and knockback in the air still work but it goes nowhere. It runs on every machine from the
    /// synced vine, so the enemy does not rubber-band on a client.
    /// ResnareTicks (local to the machine that decides, the vine's owner) is the short pause after a release during which the same enemy
    /// cannot be snared again.
    /// </summary>
    public class AmazonSnareNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public long HoldTick = -1000;   // Main.GameUpdateCount stamp refreshed by the vine while it holds the enemy
        public int ResnareTicks;        // ticks left before a vine may snare this enemy again

        public bool Held => Main.GameUpdateCount - HoldTick <= 2;

        public override void PostAI(NPC npc)
        {
            if (ResnareTicks > 0)
                ResnareTicks--;
            if (!Held)
                return;
            npc.velocity.X = 0f;
            if (npc.noGravity)
                npc.velocity.Y = 0f;   // a flying enemy is held completely
        }
    }
}