using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The held Night's Edge: the shared charged blade (Common/ChargedBlade/ChargedBladeProjectile holds the whole swing, charge, outline
    /// and trail machinery) with the Night's Edge's numbers, sounds, trail and charge plugged in. The charge lives in NightsEdgePlayer
    /// (it fades there). Right click does nothing yet: the stab is switched off until the impale and the self-stab arrive (patches 5 and 6).
    /// </summary>
    public class NightsEdgeBladeProjectile : ChargedBladeProjectile
    {
        // The vanilla item sprite is the sword (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.NightsEdge;

        IChargedBladeTuning tune;
        protected override IChargedBladeTuning Tune => tune ?? (tune = new NightsEdgeBladeTune());

        protected override int ItemType => ItemID.NightsEdge;

        protected override float OwnerCharge(Player owner)
        {
            return owner.GetModPlayer<NightsEdgePlayer>().Charge;
        }

        protected override void AddOwnerCharge(Player owner, float amount)
        {
            NightsEdgePlayer.AddCharge(owner, amount);
        }

        protected override ChargedBladeTrailStyle CreateTrailStyle()
        {
            return new NightsEdgeSweepTrailStyle();
        }

        // Right click does nothing for now (patch 5 = the self-stab at full charge, patch 6 = the impale below full charge).
        protected override bool StabAvailable(Player owner)
        {
            return false;
        }

        protected override void OnStabImpacted(Player owner)
        {
        }

        protected override void FxPlaySwing(Vector2 position, float charge) { NightsEdgeFx.PlaySwing(position, charge); }
        protected override void FxSwingDust(Vector2 tip, Vector2 motion, float charge) { NightsEdgeFx.SwingDust(tip, motion, charge); }
        protected override void FxHitSpark(Vector2 position, float charge) { NightsEdgeFx.HitSpark(position, charge); }
        protected override void FxChargeHit(Vector2 tip) { NightsEdgeFx.ChargeHit(tip); }
        protected override void FxCharged(Vector2 hand, Vector2 tip) { NightsEdgeFx.Charged(hand, tip); }
        protected override void FxPlayStabWindup(Vector2 position) { NightsEdgeFx.PlayStabWindup(position); }
        protected override void FxStabImpact(Vector2 chest) { NightsEdgeFx.StabImpact(chest); }
        protected override void FxSparkle(Vector2 hand, Vector2 tip) { NightsEdgeFx.Sparkle(hand, tip); }
    }
}