namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Code1
{
    /// <summary>
    /// The Code1's trick: the ForwardPassTrick pattern from the Yoyos folder. Custom behavior for this yoyo goes here (override Begin, Tick, OnHit or
    /// any virtual and call the base to keep the pattern); until then it is the pattern as it is.
    /// </summary>
    public class Code1Trick : ForwardPassTrick
    {
        public Code1Trick(YoyoTuning tuning) : base(tuning)
        {
        }

    }
}