namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Valor
{
    /// <summary>
    /// The Valor's right click: the AroundTheWorldTrick pattern from the Yoyos folder, the same as the Hive-Five's (without the charge, the
    /// bees and the honey). The number of turns is ValorTuning.AroundSpins (3). Custom behavior goes here (override and call the base).
    /// </summary>
    public class ValorAroundTheWorldTrick : AroundTheWorldTrick
    {
        public ValorAroundTheWorldTrick(YoyoTuning tuning) : base(tuning)
        {
        }
    }
}