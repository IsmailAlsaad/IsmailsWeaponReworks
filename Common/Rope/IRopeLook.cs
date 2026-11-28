using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Common.Rope
{
    /// <summary>
    /// What a VerletRope needs to know about its look and feel. A weapon's Tuning class implements it (YoyoTuning does, so every yoyo
    /// keeps its own numbers). Properties instead of fields, so hot reload can tweak them.
    /// </summary>
    public interface IRopeLook
    {
        int RopePoints { get; }           // points along the string (more = smoother and a bit slower)
        float RopeMinLength { get; }      // the shortest the string gets
        float RopeDamping { get; }        // velocity kept per tick
        int RopeIterations { get; }       // constraint passes per tick (more = stiffer string)
        bool RopeCollides { get; }        // the string lies on the ground instead of going through it
        int RopeSubdivisions { get; }     // smoothing: curve pieces drawn between two rope points
        float RopeWidth { get; }
        float RopeOutlineWidth { get; }
        Color RopeColor { get; }
        Color RopeAltColor { get; }       // the twist: every other piece of the string
        Color RopeOutlineColor { get; }
    }
}