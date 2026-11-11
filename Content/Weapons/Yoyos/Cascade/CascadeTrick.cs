using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Cascade
{
    /// <summary>
    /// The Cascade's left click: the PopTheClutchTrick pattern from the Yoyos folder (Pop the Clutch), with the same numbers as the Valor.
    /// A pop that starts while the yoyo is CHARGED (the Sleeper hold reached max speed) is a charged pop: ChargedPop is true on every machine
    /// (read from the synced Charged flag when the throw starts). The charge is spent when the pop ends, but a pop that was cancelled in
    /// its wind-up (the button let go) keeps it. A charged pop sets every enemy it hits On Fire and makes a tiny explosion on it (CascadeBlastProjectile).
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

        // The owner's client: the yoyo hit an enemy. During a charged pop it is set On Fire (AddBuff syncs itself) and a tiny explosion is spawned
        // at the spot the yoyo touched it (a real projectile, so tModLoader syncs it and every machine sees it; it hurts the OTHER enemies near
        // the hit one, the hit one already took the yoyo's hit).
        public override void OnHit(YoyoProjectile yoyo, NPC target)
        {
            base.OnHit(yoyo, target);
            if (!chargedPop)
                return;
            CascadeTuning tuning = (CascadeTuning)T;
            target.AddBuff(BuffID.OnFire, tuning.PopOnFireTicks);
            int damage = Math.Max(1, (int)(yoyo.Projectile.damage * tuning.BlastDamageFraction));
            Vector2 spot = target.Hitbox.ClosestPointInRect(yoyo.Projectile.Center);
            Projectile.NewProjectile(yoyo.Projectile.GetSource_FromThis(), spot, Vector2.Zero, ModContent.ProjectileType<CascadeBlastProjectile>(),
                damage, yoyo.Projectile.knockBack * tuning.BlastKnockbackScale, yoyo.Projectile.owner, target.whoAmI);
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
