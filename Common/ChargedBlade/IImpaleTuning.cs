using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common.ChargedBlade
{
    /// <summary>
    /// The numbers of the shared blade's IMPALE (the dagger stab that spears an enemy on the blade, lifts it in beats and ends in a finisher).
    /// A weapon that wants the impale returns an object of this type from ChargedBladeProjectile.ImpaleTune; the Light's Bane returns null (no impale).
    /// Every property forwards live to the weapon's Tuning class, so hot reload works. Angles: degrees, relative to the aim, positive = below it.
    /// </summary>
    public interface IImpaleTuning
    {
        int ImpaleBufferTicks { get; }              // a right click during a strike is remembered this long
        int ImpaleWindupTicks { get; }              // the pull-back before the thrust
        float ImpaleWindupArmDeg { get; }           // the arm drops this far below the aim in the wind-up (the uppercut)
        float ImpaleSwingEndDeg { get; }            // the swing ends with the arm this far from the aim (negative = above the cursor line)
        float ImpaleBladeAngleDeg { get; }          // THE BLADE IS HELD THIS FAR OFF THE ARM: 90 = across the hand like a hook (the uppercut), 0 = straight along the arm
        float ImpaleWindupPull { get; }             // the sword is drawn back this far (px) in the wind-up
        int ImpaleSwingTicks { get; }               // the upward swing (the only part that can catch an enemy)
        float ImpaleReach { get; }                  // the sword is driven this far (px) past its normal length
        float ImpaleMaxSize { get; }                // an enemy bigger than this (width or height) is stabbed but not impaled
        int ImpaleWaitTicks { get; }                // other machines wait this long for the owner's catch before they call it a miss
        int ImpaleMissTicks { get; }                // a stab that caught nothing holds its pose this long
        int ImpaleSeatTicks { get; }                // the caught enemy slides from where it was onto the blade over this long
        float ImpaleAnchorFraction { get; }         // the enemy sits this far along the blade (1 = at the tip)
        int ImpaleBeats { get; }                    // how many beats (each ends in a pulse of damage)
        int ImpaleBeatTicks { get; }                // one beat
        float ImpaleRisePortion { get; }            // the lift happens in this first part of each beat, the rest is a pause before the pulse
        float ImpaleHeldAngleDeg { get; }           // THE ARM LIFTS to this angle above the horizontal (on the facing side; 90 = straight up) while the enemy is held; the blade keeps the angle it stabbed at
        float ImpaleShake { get; }                  // the blade trembles this much (px) while it holds the enemy
        int ImpaleFinishTicks { get; }              // the pose is held this long after the finisher
        float ImpaleHitShake { get; }               // camera punch of the stab
        float ImpaleBeatShake { get; }              // camera punch of a beat
        float ImpaleStabDamageMultiplier { get; }   // the stab itself, of the item's damage
        float ImpaleBeatDamageMultiplier { get; }   // one beat, of the item's damage
    }
}