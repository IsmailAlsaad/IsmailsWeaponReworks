using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// The Amazon's left click: the ForwardPassTrick pattern from the Yoyos folder, plus the barbs: every enemy hit (the owner's client only)
    /// makes the yoyo more barbed through AmazonProjectile.Barb. The pass itself is not touched: the yoyo never stops or slows on a hit.
    /// </summary>
    public class AmazonTrick : ForwardPassTrick
    {
        public AmazonTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        public override void OnHit(YoyoProjectile yoyo, NPC target)
        {
            base.OnHit(yoyo, target);
            (yoyo as AmazonProjectile)?.Barb(target);
        }
    }
}