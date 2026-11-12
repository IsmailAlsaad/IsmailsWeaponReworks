using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Phaseblade
{
    /// <summary>
    /// The little bit of state the Phaseblades keep between swings, on the owner's machine only (nothing here is synced: the owner decides
    /// and passes what the other machines need through the swing projectile's spawn values).
    /// </summary>
    public class PhasebladePlayer : ModPlayer
    {
        public int LastPhase = -1;   // PhasebladeSwingProjectile.Phase of the previous swing, -1 = none yet
        public float LastEndArm;     // where the arm was when the previous swing ended (relative to its aim, wrapped to -180..180 degrees)
        public uint LastEndTick;     // Main.GameUpdateCount when it ended
        public float LastEndWorldArm; // the same end pose as a world angle (a plant starts from it, whatever the aim was)
        public bool SwingAfterCatch;  // a LEFT click pulled the planted blade back: the combo starts when it is caught
        public uint LastStartTick;    // Main.GameUpdateCount when the last swing or plant was started (the projectile is not counted for a tick or two)
    }
}