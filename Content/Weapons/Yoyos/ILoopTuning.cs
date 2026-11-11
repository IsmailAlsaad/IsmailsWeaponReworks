using System;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The numbers of the Man on Trapeze pattern (the lasso, the loop, the noose and the shred), read by ManOnTrapezeTrick. A yoyo that uses
    /// the trick (the Artery, the Malaise) lists all of them in its own Tuning, so each yoyo can set them differently. What happens when the
    /// loop ends (the Artery's blood explosion, the Malaise's confusion) has its own numbers in that yoyo's Tuning.
    /// </summary>
    public interface ILoopTuning
    {
        float LassoMinReachPx { get; }        // the yoyo must be at least this far from the hand before the string can catch anything
        float LassoTouchPaddingPx { get; }    // how thick the string counts when it touches an enemy
        int LoopCatchTicks { get; }           // the curve from where the string caught the enemy to the string line
        int LoopConstrictTicks { get; }       // the walk back along the string line while the noose shrinks
        float LoopWalkSpeedPx { get; }        // px per tick the yoyo walks toward the player while it constricts
        float LoopGapPx { get; }              // where the yoyo arrives on the string line: this far outside the noose
        float LoopMinHandDistancePx { get; }  // the yoyo never walks closer to the hand than this
        float LoopRadiusPadPx { get; }        // the noose starts this much wider than half the enemy's size
        float LoopMinRadiusPx { get; }        // never smaller than this
        float LoopMaxRadiusPx { get; }        // nor bigger than this
        float LoopCloseRadiusPx { get; }      // the noose is this big when it has closed
        float LoopSpinSpeed { get; }          // radians per tick the yoyo spins while it walks
        float LoopStringStepPx { get; }       // the string is drawn in pieces about this long
        int LoopArcSamples { get; }           // points around the noose
        float SnareMaxSizePx { get; }         // an enemy bigger than this is looped but not held
        int ShredIntervalTicks { get; }       // a shred hit this often while the noose shrinks
        float ShredDamageFraction { get; }    // each shred hit, as a fraction of the yoyo's damage
        float ShredShake { get; }             // camera punch per shred hit (0 = none), the owner only
    }
}