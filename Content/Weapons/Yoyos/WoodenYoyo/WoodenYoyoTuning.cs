using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.WoodenYoyo
{
    /// <summary>
    /// Every tunable number of the WoodenYoyo (ticks: 60 = 1 second, distances in pixels, 16 px = 1 tile). The shared defaults are in
    /// YoyoTuning (rope, idle, return, body, juice); override any of them here. The numbers of its trick come from IWalkTheDogTuning.
    /// </summary>
    public class WoodenYoyoTuning : YoyoTuning, IWalkTheDogTuning
    {
        public override string TexturePath => "IsmailsWeaponReworks/Content/Weapons/Yoyos/WoodenYoyo/WoodenYoyoProjectile"; // about 16x16; until it exists the vanilla sprite is used

        // ---- juice: none (no shake, no afterimages) ----

        // ---- Walk the Dog (left click) ----
        public int WalkHitsPerThrow => 3;          // enemy hits before it retracts
        public float WalkThrowDownSpeed => 5f;     // the throw: down ...
        public float WalkThrowForwardSpeed => 3f;  // ... and a bit toward the cursor's side
        public float WalkSwitchDeadzonePx => 10f;  // while it is out, the side only switches when the cursor is this far past the player's center
        public float WalkGravity => 0.45f;
        public float WalkMaxFall => 14f;
        public float WalkSpeed => 4.2f;            // rolling speed along the ground
        public float WalkAccel => 0.35f;           // on the ground
        public float WalkAirAccel => 0.06f;        // in the air
        public float WalkLandBounce => 0.3f;       // fraction of the fall speed that bounces back up
        public float WalkBounceMinImpact => 3f;    // softer landings do not bounce
        public float WalkHopSpeed => 5.5f;         // a small hop when a block stops the roll (steps and slopes)
        public int WalkHopCooldownTicks => 10;
        public float WalkHitRecoil => 3.5f;        // pushed back from an enemy ...
        public float WalkHitHop => 3.5f;           // ... and up
    }
}