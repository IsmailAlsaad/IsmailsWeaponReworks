using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// The Hive-Five's left click trick: the AroundTheWorldTrick pattern from the Yoyos folder. The honey outline, the bees and the honey buff
    /// (patch H3) go here (override Begin, Tick, OnHit or any virtual and call the base to keep the pattern); until then it is the pattern as it is.
    /// </summary>
    public class HiveFiveTrick : AroundTheWorldTrick
    {
        bool chargedSpin;   // this Around the World uses the charge (read from the synced flag when the throw starts, on every machine)
        int beesThisSpin;   // bees the owner has sent out in this spin

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
            beesThisSpin = 0;
        }

        // A charged spin sends out a bee every BeeIntervalTicks while it flies. Every machine runs this and plays the little honey puff at
        // the same ticks; only the owner's client spawns the bee (a normal projectile, which syncs itself).
        public override void Tick(YoyoProjectile yoyo)
        {
            base.Tick(yoyo);
            if (!chargedSpin || stopped || finished)
                return;
            HiveFiveTuning tuning = (HiveFiveTuning)T;
            if (yoyo.OutTicks % Math.Max(1, tuning.BeeIntervalTicks) != 0)
                return;
            if (yoyo.Fx is HiveFiveFx fx)
                fx.BeeBurst(yoyo.Projectile.Center);
            if (yoyo.IsOwner && beesThisSpin < tuning.BeeMaxPerSpin)
                SpawnBee(yoyo, tuning);
        }

        // The owner's client: a vanilla bee (now and then a giant one) leaves the yoyo, flying out and away from the player, and then hunts.
        void SpawnBee(YoyoProjectile yoyo, HiveFiveTuning tuning)
        {
            bool giant = Main.rand.NextFloat() < tuning.GiantBeeChance;
            int type = giant ? ProjectileID.GiantBee : ProjectileID.Bee;
            float fraction = giant ? tuning.GiantBeeDamageFraction : tuning.BeeDamageFraction;
            Vector2 outward = (yoyo.Projectile.Center - yoyo.HandFollowed).SafeNormalize(Vector2.UnitX);
            Vector2 velocity = outward * tuning.BeeSpeed + Main.rand.NextVector2Circular(1.5f, 1.5f);
            int damage = Math.Max(1, (int)(yoyo.Projectile.damage * fraction));
            Projectile.NewProjectile(yoyo.Projectile.GetSource_FromThis(), yoyo.Projectile.Center, velocity, type, damage, yoyo.Projectile.knockBack * 0.3f, yoyo.Projectile.owner);
            beesThisSpin++;
        }

        // The owner's client: the yoyo hit an enemy. During a charged spin every hit adds HoneyPerHitTicks (1 second) to the player's Honey
        // buff, never beyond HoneyBuffMaxTicks (AddBuff syncs itself).
        public override void OnHit(YoyoProjectile yoyo, NPC target)
        {
            base.OnHit(yoyo, target);
            if (!chargedSpin)
                return;
            HiveFiveTuning tuning = (HiveFiveTuning)T;
            Player owner = yoyo.Owner;
            int index = owner.FindBuffIndex(BuffID.Honey);
            int current = index >= 0 ? owner.buffTime[index] : 0;
            owner.AddBuff(BuffID.Honey, Math.Min(tuning.HoneyBuffMaxTicks, current + tuning.HoneyPerHitTicks));
        }

        // The owner's client: the spin has ended, so the charge is spent (the flag travels with the sync).
        public override void OnRetract(YoyoProjectile yoyo)
        {
            if (chargedSpin)
                yoyo.Charged = false;
        }
    }
}
