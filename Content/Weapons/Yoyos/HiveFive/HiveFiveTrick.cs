namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// The Hive-Five's left click trick: the AroundTheWorldTrick pattern from the Yoyos folder. The honey outline, the bees and the honey buff
    /// (patch H3) go here (override Begin, Tick, OnHit or any virtual and call the base to keep the pattern); until then it is the pattern as it is.
    /// </summary>
    public class HiveFiveTrick : AroundTheWorldTrick
    {
        public HiveFiveTrick(YoyoTuning tuning) : base(tuning)
        {
        }
    }
}
