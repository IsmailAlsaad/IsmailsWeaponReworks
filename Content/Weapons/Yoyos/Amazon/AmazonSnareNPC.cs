using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// What the Amazon's barbed ground vines do to an enemy they hold. The vine projectile (on every machine, the server included) refreshes
    /// HoldTick every tick while it holds the enemy, so a vine that dies or lets go for any reason releases the enemy by itself a moment
    /// later. While held the enemy stays exactly in place: its vanilla AI does not run at all (SnareStopsAI) and its speed is zeroed, so it
    /// does not walk, jump, fly, fall, attack or get knocked away (with SnareStopsAI off only its horizontal speed is zeroed, and its
    /// vertical speed too when it flies). It runs on every machine from the
    /// synced vine, so the enemy does not rubber-band on a client.
    /// ResnareTicks (local to the machine that decides, the vine's owner) is the short pause after a release during which the same enemy
    /// cannot be snared again.
    /// </summary>
    public class AmazonSnareNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public long HoldTick = -1000;   // Main.GameUpdateCount stamp refreshed by the vine while it holds the enemy
        public long HolderKey = -1;     // the vine that holds it (AmazonBarbedVineProjectile.HolderKey, the same number on every machine)
        public int ResnareTicks;        // ticks left before a vine may snare this enemy again

        public bool Held => Main.GameUpdateCount - HoldTick <= 2;

        public long PendingTick = -1000;   // owner's client: refreshed by a vine whose enemy touched it in the air and is still falling
        public long PendingKey = -1;       // the vine that waits for it to land
        public bool Pending => Main.GameUpdateCount - PendingTick <= 2;

        // A vine holds the enemy: stamps the lease at once, so a vine checked later in the same tick already sees the enemy as held.
        // Returns false when another vine with a LOWER key holds it too: the caller (the higher key) is the loser and lets go. A lower key is an
        // OLDER vine (the plant order comes first in the key). The keys are the same on every machine, so every machine picks the same winner.
        public bool Claim(long key)
        {
            if (Held && HolderKey != key && HolderKey < key)
                return false;
            HolderKey = key;
            HoldTick = (long)Main.GameUpdateCount;
            return true;
        }

        // A vine waits for this enemy to land (owner's client): stamped at once so no other vine starts a snare or a wait on it.
        public void Pend(long key)
        {
            PendingKey = key;
            PendingTick = (long)Main.GameUpdateCount;
        }

        // The wait is over (landed, cancelled or the enemy got held): the enemy is free for other vines at once, not after the lease runs out.
        public void EndPend(long key)
        {
            if (PendingKey == key)
                PendingTick = -1000;
        }

        readonly AmazonTuning tune = new AmazonTuning();

        // A held enemy runs no AI: nothing moves it, and nothing it would do (walk, jump, fly, shoot) happens.
        public override bool PreAI(NPC npc)
        {
            if (!Held || !tune.SnareStopsAI)
                return true;
            npc.velocity = Vector2.Zero;
            return false;
        }

        public override void PostAI(NPC npc)
        {
            if (ResnareTicks > 0)
                ResnareTicks--;
            if (!Held)
                return;
            if (tune.SnareStopsAI)
            {
                npc.velocity = Vector2.Zero;   // nothing is left over from a hit this tick
                return;
            }
            npc.velocity.X = 0f;
            if (npc.noGravity)
                npc.velocity.Y = 0f;   // a flying enemy is held completely
        }
    }
}