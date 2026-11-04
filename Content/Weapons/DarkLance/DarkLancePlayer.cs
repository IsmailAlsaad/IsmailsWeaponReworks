using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// The little state the Dark Lance keeps per player. OrbTurn is the clock the flame orbs orbit by: it counts ticks while the player has
    /// any orb and resets when there are none, on EVERY machine (so every machine spaces the orbs the same way; it is never sent).
    /// Serial is the owner's spawn counter: every orb carries its number in ai[1], which gives each orb a stable place in the ring.
    /// </summary>
    public class DarkLancePlayer : ModPlayer
    {
        public float OrbTurn;
        public int Serial;   // owner's client only
        public bool NextIsThrust;   // owner's client only: the left click cycles sweep, thrust, sweep ...
        public int Collected;       // owner's client only: orbs the last sweep gathered, spent by the next thrust
        public Vector2 LanceTip;    // every machine: where the lance tip is (the sweep and the thrust write it every tick); gathered orbs fly to it

        int pingCount;
        uint pingResetAt;

        // The pitch step of the gather chime: it rises with every orb gathered in quick succession (every machine counts for itself).
        public int NextPing()
        {
            if (Main.GameUpdateCount > pingResetAt)
                pingCount = 0;
            pingResetAt = Main.GameUpdateCount + 40;
            return pingCount++;
        }

        public override void PostUpdate()
        {
            if (Player.dead || Player.HeldItem.type != ItemID.DarkLance)
            {
                Collected = 0;           // the lance was put away: the gathered charge is gone
                NextIsThrust = false;
            }
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<DarkLanceOrbProjectile>()] > 0)
                OrbTurn += 1f;
            else
                OrbTurn = 0f;
        }
    }
}