using IsmailsWeaponReworks.Common.ChargedBlade;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>Hands the impale numbers of NightsEdgeTuning to the shared blade (live, so hot reload works). Add a line only when IImpaleTuning gets a new member.</summary>
    public class NightsEdgeImpaleTune : IImpaleTuning
    {
        public int ImpaleBufferTicks => NightsEdgeTuning.ImpaleBufferTicks;
        public int ImpaleWindupTicks => NightsEdgeTuning.ImpaleWindupTicks;
        public float ImpaleWindupArmDeg => NightsEdgeTuning.ImpaleWindupArmDeg;
        public float ImpaleSwingEndDeg => NightsEdgeTuning.ImpaleSwingEndDeg;
        public float ImpaleBladeAngleDeg => NightsEdgeTuning.ImpaleBladeAngleDeg;
        public float ImpaleWindupPull => NightsEdgeTuning.ImpaleWindupPull;
        public int ImpaleSwingTicks => NightsEdgeTuning.ImpaleSwingTicks;
        public float ImpaleReach => NightsEdgeTuning.ImpaleReach;
        public float ImpaleMaxSize => NightsEdgeTuning.ImpaleMaxSize;
        public int ImpaleWaitTicks => NightsEdgeTuning.ImpaleWaitTicks;
        public int ImpaleMissTicks => NightsEdgeTuning.ImpaleMissTicks;
        public int ImpaleSeatTicks => NightsEdgeTuning.ImpaleSeatTicks;
        public float ImpaleAnchorFraction => NightsEdgeTuning.ImpaleAnchorFraction;
        public int ImpaleBeats => NightsEdgeTuning.ImpaleBeats;
        public int ImpaleBeatTicks => NightsEdgeTuning.ImpaleBeatTicks;
        public float ImpaleRisePortion => NightsEdgeTuning.ImpaleRisePortion;
        public float ImpaleHeldAngleDeg => NightsEdgeTuning.ImpaleHeldAngleDeg;
        public float ImpaleShake => NightsEdgeTuning.ImpaleShake;
        public int ImpaleFinishTicks => NightsEdgeTuning.ImpaleFinishTicks;
        public float ImpaleHitShake => NightsEdgeTuning.ImpaleHitShake;
        public float ImpaleBeatShake => NightsEdgeTuning.ImpaleBeatShake;
        public float ImpaleStabDamageMultiplier => NightsEdgeTuning.ImpaleStabDamageMultiplier;
        public float ImpaleBeatDamageMultiplier => NightsEdgeTuning.ImpaleBeatDamageMultiplier;
    }
}