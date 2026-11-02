using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>
    /// The little bit of state the Starfury keeps between uses, on the owner's machine only (nothing here is synced: the owner decides
    /// and passes what the other machines need through the swing projectile's spawn values).
    /// </summary>
    public class StarfuryPlayer : ModPlayer
    {
        public int LastPhase = -1;       // StarfurySwingProjectile.Phase of the previous use, -1 = none yet
        public float LastEndArm;         // where the arm was when the previous use ended (local radians)
        public uint LastEndTick;         // Main.GameUpdateCount when it ended
        public uint LastCallTick;        // when the last right click happened (the call cooldown counts from it)
        public Vector2 CallTarget;       // the cursor point of the current call, clamped to range, fixed at the right click
    }
}