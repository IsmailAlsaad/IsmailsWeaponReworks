using IsmailsWeaponReworks.Common.ChargedBlade;
using IsmailsWeaponReworks.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// The held Light's Bane: the shared charged blade (Common/ChargedBlade/ChargedBladeProjectile holds the whole swing, charge, outline
    /// and stab machinery) with the Light's Bane's numbers, sounds, trail and stab result plugged in. The stab only adds LightsBaneImbueBuff
    /// (LightsBanePlayer reacts to the buff from any source); vanilla syncs the buff list to the other players.
    /// </summary>
    public class LightsBaneBladeProjectile : ChargedBladeProjectile
    {
        // The vanilla item sprite is the sword (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.LightsBane;

        IChargedBladeTuning tune;
        protected override IChargedBladeTuning Tune => tune ?? (tune = new LightsBaneBladeTune());

        protected override int ItemType => ItemID.LightsBane;

        protected override float OwnerCharge(Player owner)
        {
            return owner.GetModPlayer<LightsBanePlayer>().Charge;
        }

        protected override void AddOwnerCharge(Player owner, float amount)
        {
            LightsBanePlayer.AddCharge(owner, amount);
        }

        protected override ChargedBladeTrailStyle CreateTrailStyle()
        {
            return new LightsBaneSweepTrailStyle();
        }

        // No second stab while the imbue is on.
        protected override bool StabAvailable(Player owner)
        {
            return owner.GetModPlayer<LightsBanePlayer>().ImbueTicks <= 0;
        }

        // The buff is the whole imbue (LightsBanePlayer reacts to it from any source): vanilla syncs the buff list to the others.
        protected override void OnStabImpacted(Player owner)
        {
            owner.AddBuff(ModContent.BuffType<LightsBaneImbueBuff>(), LightsBaneTuning.ImbueTicks);
        }

        protected override void FxPlaySwing(Vector2 position, float charge) { LightsBaneFx.PlaySwing(position, charge); }
        protected override void FxSwingDust(Vector2 tip, Vector2 motion, float charge) { LightsBaneFx.SwingDust(tip, motion, charge); }
        protected override void FxHitSpark(Vector2 position, float charge) { LightsBaneFx.HitSpark(position, charge); }
        protected override void FxChargeHit(Vector2 tip) { LightsBaneFx.ChargeHit(tip); }
        protected override void FxCharged(Vector2 hand, Vector2 tip) { LightsBaneFx.Charged(hand, tip); }
        protected override void FxPlayStabWindup(Vector2 position) { LightsBaneFx.PlayStabWindup(position); }
        protected override void FxStabImpact(Vector2 chest) { LightsBaneFx.StabImpact(chest); }
        protected override void FxSparkle(Vector2 hand, Vector2 tip) { LightsBaneFx.Sparkle(hand, tip); }
    }
}