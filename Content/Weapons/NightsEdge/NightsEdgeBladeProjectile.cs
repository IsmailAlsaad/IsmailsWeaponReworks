using IsmailsWeaponReworks.Common.ChargedBlade;
using IsmailsWeaponReworks.Content.Buffs;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The held Night's Edge: the shared charged blade (Common/ChargedBlade/ChargedBladeProjectile holds the whole swing, charge, outline
    /// and trail machinery) with the Night's Edge's numbers, sounds, trail and charge plugged in. The charge lives in NightsEdgePlayer
    /// (it fades there). Right click at full charge is the self-stab (patch 5): it gives the Shadow Armor buff. The impale below full charge is patch 6.
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

        // Right click at full charge is the self-stab (the shared blade plays it); not while the Shadow Armor is on. Below full charge it is the impale (patch 6).
        protected override bool StabAvailable(Player owner)
        {
            return owner.GetModPlayer<NightsEdgePlayer>().ShadowTicks <= 0;
        }

        // Owner's client, at the moment the blade sinks in: the buff is the whole effect (NightsEdgePlayer and the buff react to it from any source,
        // vanilla syncs the buff list), plus a first fan of dark droplets (spawned by the owner, so tModLoader syncs them).
        protected override void OnStabImpacted(Player owner)
        {
            owner.AddBuff(ModContent.BuffType<NightsEdgeShadowArmorBuff>(), NightsEdgeTuning.ShadowArmorTicks);
            owner.AddBuff(ModContent.BuffType<LightsBaneImbueBuff>(), NightsEdgeTuning.ShadowArmorTicks);
            NightsEdgeDropProjectile.Throw(owner, NightsEdgeTuning.StabBurstDrops);
        }

        // ---- the impale (patch 6a): right click below full charge; the shared blade runs it, these are the Night's Edge's numbers, sounds and finisher ----
        IImpaleTuning impaleTune;
        protected override IImpaleTuning ImpaleTune => impaleTune ?? (impaleTune = new NightsEdgeImpaleTune());

        // Not while the Shadow Armor is on (the charge is held full then, so the impale could not start anyway).
        protected override bool ImpaleAvailable(Player owner)
        {
            return owner.GetModPlayer<NightsEdgePlayer>().ShadowTicks <= 0;
        }

        protected override void FxImpaleWindup(Vector2 hand) { NightsEdgeFx.ImpaleWindup(hand); }
        protected override void FxImpalePierce(Vector2 point, Vector2 direction, bool caught) { NightsEdgeFx.ImpalePierce(point, direction, caught); }
        protected override void FxImpaleBeat(Vector2 point, int beat) { NightsEdgeFx.ImpaleBeat(point, beat); }
        protected override void FxImpaleFinish(Vector2 point) { NightsEdgeFx.ImpaleFinish(point); }

        // Owner's client, at the finisher: the dark flame explosion on the impaled enemy (patch 6b makes it bigger and nicer). Spawned by the owner, so it syncs.
        protected override void OnImpaleFinish(Player owner, Vector2 point)
        {
            int damage = (int)(Projectile.damage * NightsEdgeTuning.ImpaleFinishDamageMultiplier);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), point, Vector2.Zero, ModContent.ProjectileType<NightsEdgeBlastProjectile>(), damage, 0f, Projectile.owner);
        }

        // THE SWING SLOWS DOWN WITH THE CHARGE (the Volcano's pattern): fastest when empty, slowest when full. The Shadow Armor makes it fast again.
        protected override float ChargeSpeedScale(Player owner, float charge)
        {
            if (owner.GetModPlayer<NightsEdgePlayer>().ShadowTicks > 0)
                return 1f / Math.Max(0.1f, NightsEdgeTuning.ArmorTimeScale);
            float heavy = MathHelper.Lerp(1f, NightsEdgeTuning.FullTimeScale, (float)Math.Pow(MathHelper.Clamp(charge, 0f, 1f), Math.Max(0.01f, NightsEdgeTuning.SpeedCurvePower)));
            return 1f / Math.Max(0.1f, heavy);
        }

        // Owner's client, once per swing when the strike begins: a swing that STARTS with a fully charged sword throws one short dark flame wave toward the
        // cursor (the swing that fills the sword fills it later, so it throws none). NOT while the Shadow Armor is on. Spawned by the owner, so it syncs.
        protected override void OnOwnerStrikeStart(Vector2 hand, Vector2 direction)
        {
            wavePending = false;
            Player owner = Main.player[Projectile.owner];
            if (OwnerCharge(owner) < NightsEdgeTuning.ChargedAt || owner.GetModPlayer<NightsEdgePlayer>().ShadowTicks > 0)
                return;
            // The wave appears when the cut has got WaveSpawnProgress of the way (0 = right now).
            if (NightsEdgeTuning.WaveSpawnProgress <= 0f)
            {
                SpawnWave(hand, direction);
                return;
            }
            wavePending = true;
            waveDirection = direction;
        }

        bool wavePending;       // owner's client: the wave of this swing is still to be thrown (WaveSpawnProgress)
        Vector2 waveDirection;

        void SpawnWave(Vector2 hand, Vector2 direction)
        {
            Vector2 spawn = hand + direction * (Tune.BladeLength * Projectile.scale * NightsEdgeTuning.WaveSpawnFraction);
            int damage = (int)(Projectile.damage * NightsEdgeTuning.WaveDamageMultiplier);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, direction * NightsEdgeTuning.WaveSpeed, ModContent.ProjectileType<NightsEdgeWaveProjectile>(),
                damage, Projectile.knockBack * NightsEdgeTuning.WaveKnockbackMultiplier, Projectile.owner);
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
            // Owner's client: the delayed wave leaves when the cut has reached WaveSpawnProgress; a swing that was cut short (stab, impale) throws none.
            if (wavePending)
            {
                if (!InStrikePhase || Projectile.owner != Main.myPlayer)
                    wavePending = false;
                else if (StrikeProgress >= MathHelper.Clamp(NightsEdgeTuning.WaveSpawnProgress, 0f, 1f))
                {
                    wavePending = false;
                    SpawnWave(Main.player[Projectile.owner].MountedCenter, waveDirection);
                }
            }
            NightsEdgeFlames.Feed(pose, mask, flameState);
        }

        // The metaball layers are drawn from here, in front of the enemies.
        protected override void DrawAfterBlade(ChargedBladePose pose)
        {
            NightsEdgeFx.DrawLayers();
        }

        // Owner's client. A true melee hit WHILE THE SHADOW ARMOR IS ON (a full charge alone is not enough any more): the enemy bursts into a dark
        // flame explosion that also hurts what stands next to it and sets it on Shadowflame. The projectile is spawned by the owner, so it syncs.
        protected override void OnOwnerHit(NPC target, NPC.HitInfo hit, bool fullBefore)
        {
            if (Main.player[Projectile.owner].GetModPlayer<NightsEdgePlayer>().ShadowTicks <= 0)
                return;
            Vector2 point = Vector2.Clamp(Projectile.Center + Projectile.rotation.ToRotationVector2() * Tune.BladeLength * Projectile.scale * 0.7f, target.TopLeft, target.BottomRight);
            int damage = (int)(Projectile.damage * NightsEdgeTuning.BlastDamageMultiplier);
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), point, Vector2.Zero, ModContent.ProjectileType<NightsEdgeBlastProjectile>(), damage, 0f, Projectile.owner);
        }
    }
}