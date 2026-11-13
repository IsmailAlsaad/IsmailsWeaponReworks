using IsmailsWeaponReworks.Common.ChargedBlade;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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

        // The edge mask (your PNG), read on first use; the metaballs sit on its pixels.
        readonly NightsEdgeMask mask = new NightsEdgeMask();
        readonly NightsEdgeFlameState flameState = new NightsEdgeFlameState();

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

        // Every machine, every tick: the dark flame metaballs along the blade's edge, the rising flames and the embers (visual only, nothing is synced).
        protected override void TickVisuals(ChargedBladePose pose)
        {
            NightsEdgeFlames.Feed(pose, mask, flameState);
        }

        // The metaball layers are drawn from here, in front of the enemies.
        protected override void DrawAfterBlade(ChargedBladePose pose)
        {
            NightsEdgeFx.DrawLayers();
        }

        // Owner's client, once per swing when the strike begins: a swing that STARTS with a fully charged sword throws one short flame wave toward
        // the cursor (the swing that fills the sword fills it later, during the strike, so it throws none). The projectile is spawned by the owner,
        // so tModLoader syncs it; every machine plays the same visuals, the owner's client applies the hits.
        protected override void OnOwnerStrikeStart(Vector2 hand, Vector2 direction)
        {
            if (OwnerCharge(Main.player[Projectile.owner]) < NightsEdgeTuning.ChargedAt)
                return;
            Vector2 spawn = hand + direction * (Tune.BladeLength * Projectile.scale * NightsEdgeTuning.WaveSpawnFraction);
            int damage = (int)(Projectile.damage * NightsEdgeTuning.WaveDamageMultiplier);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, direction * NightsEdgeTuning.WaveSpeed, ModContent.ProjectileType<NightsEdgeWaveProjectile>(),
                damage, Projectile.knockBack * NightsEdgeTuning.WaveKnockbackMultiplier, Projectile.owner);
        }

        // Owner's client. A hit while the sword was already fully charged (the swing that fills it does not explode): the enemy bursts into a dark
        // flame explosion that also hurts what stands next to it and sets it on Shadowflame. The projectile is spawned by the owner, so it syncs.
        protected override void OnOwnerHit(NPC target, NPC.HitInfo hit, bool fullBefore)
        {
            if (!fullBefore)
                return;
            Vector2 point = Vector2.Clamp(Projectile.Center + Projectile.rotation.ToRotationVector2() * Tune.BladeLength * Projectile.scale * 0.7f, target.TopLeft, target.BottomRight);
            int damage = (int)(Projectile.damage * NightsEdgeTuning.BlastDamageMultiplier);
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), point, Vector2.Zero, ModContent.ProjectileType<NightsEdgeBlastProjectile>(), damage, 0f, Projectile.owner);
        }
    }
}