namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Valor
{
    /// <summary>
    /// The Valor's left click: the PopTheClutchTrick pattern from the Yoyos folder (Pop the Clutch). Custom behavior for this yoyo goes here
    /// (override Begin, Tick, OnHit or any virtual and call the base to keep the pattern); until then it is the pattern as it is.
    /// </summary>
    public class ValorTrick : PopTheClutchTrick
    {
        public ValorTrick(YoyoTuning tuning) : base(tuning)
        {
        }
    }
}