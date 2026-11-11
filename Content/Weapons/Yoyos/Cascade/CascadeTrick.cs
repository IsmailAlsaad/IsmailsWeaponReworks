using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Cascade
{
    /// <summary>
    /// The Cascade's left click: the PopTheClutchTrick pattern from the Yoyos folder (Pop the Clutch), with the same numbers as the Valor.
    /// A pop that starts while the yoyo is CHARGED (the Sleeper hold reached max speed) is a charged pop: ChargedPop is true on every machine
    /// (read from the synced Charged flag when the throw starts). The charge is spent when the pop ends, but a pop that was cancelled in
    /// its wind-up (the button let go) keeps it. What a charged pop does (the explosion and On Fire on every hit) is patch C2.
    /// </summary>
    public class CascadeTrick : PopTheClutchTrick
    {
        bool chargedPop;   // this pop uses the charge (read when the throw starts, on every machine)

        public CascadeTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        public bool ChargedPop => chargedPop;

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            base.Begin(yoyo, authoritative);
            chargedPop = yoyo.Charged;
        }

        // The owner's client: the yoyo is pulled back. If the swings had started, the charge is spent (the flag travels with the sync);
        // a cancelled wind-up keeps it.
        public override void OnRetract(YoyoProjectile yoyo)
        {
            base.OnRetract(yoyo);
            if (chargedPop && phase != PopPhase.Windup)
                yoyo.Charged = false;
        }
    }
}
