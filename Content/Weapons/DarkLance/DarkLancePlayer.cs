using Terraria;
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

        public override void PostUpdate()
        {
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<DarkLanceOrbProjectile>()] > 0)
                OrbTurn += 1f;
            else
                OrbTurn = 0f;
        }
    }
}