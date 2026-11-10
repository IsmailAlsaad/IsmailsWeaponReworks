using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Rally
{
    /// <summary>
    /// The Rally's trick: the ForwardPassTrick pattern from the Yoyos folder, plus the cracks: every enemy hit (the owner's client only)
    /// cracks the yoyo through RallyProjectile.Crack. The pass itself is not touched: the yoyo never stops or slows on a hit.
    /// </summary>
    public class RallyTrick : ForwardPassTrick
    {
        public RallyTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        public override void OnHit(YoyoProjectile yoyo, NPC target)
        {
            base.OnHit(yoyo, target);
            if (yoyo.Hits <= ((RallyTuning)T).MaxCountedHitsPerSwing)   // hits past the limit in one swing still hurt but do not crack it
                (yoyo as RallyProjectile)?.Crack();
        }
    }
}