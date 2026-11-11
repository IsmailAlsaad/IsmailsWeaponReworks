namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// The Hive-Five's left click trick: the AroundTheWorldTrick pattern from the Yoyos folder. The honey outline, the bees and the honey buff
    /// (patch H3) go here (override Begin, Tick, OnHit or any virtual and call the base to keep the pattern); until then it is the pattern as it is.
    /// </summary>
    public class HiveFiveTrick : AroundTheWorldTrick
    {
        bool chargedSpin;   // this Around the World uses the charge (read from the synced flag when the throw starts, on every machine)

        public HiveFiveTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        // A charged Around the World makes more turns (HiveFiveTuning.ChargedSpins); an uncharged one the usual AroundSpins.
        protected override int Spins => chargedSpin ? ((HiveFiveTuning)T).ChargedSpins : base.Spins;

        // True while a charged spin is flying: the projectile lays the honey trail.
        public bool TrailActive => chargedSpin && !stopped && !finished;

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            base.Begin(yoyo, authoritative);
            chargedSpin = yoyo.Charged;
        }

        // The owner's client: the spin has ended, so the charge is spent (the flag travels with the sync).
        public override void OnRetract(YoyoProjectile yoyo)
        {
            if (chargedSpin)
                yoyo.Charged = false;
        }
    }
}
