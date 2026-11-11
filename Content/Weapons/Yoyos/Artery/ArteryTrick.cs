using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The Artery's trick: the shared Man on Trapeze (ManOnTrapezeTrick) plus its own ending. When the loop CLOSES (the noose shut, or the
    /// enemy died inside it) the owner's client sets off the blood explosion at the enemy's last position: every enemy in the circle takes
    /// ExplosionDamageFraction of the yoyo's damage, is knocked away from the center and gets Bleeding. A BROKEN loop does neither.
    /// MULTIPLAYER: the owner's client alone strikes (SimpleStrikeNPC and AddBuff sync by themselves, as Common/AreaEffect says); every
    /// machine plays the explosion from the synced close (ArteryFx.LoopClose).
    /// </summary>
    public class ArteryTrick : ManOnTrapezeTrick
    {
        public ArteryTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        ArteryTuning Tune => (ArteryTuning)T;

        protected override void OnLoopStart(YoyoProjectile yoyo)
        {
            ArteryFx fx = yoyo.Fx as ArteryFx;
            if (fx != null)
            {
                fx.ExplosionRadius = Tune.ExplosionRadiusPx;   // the close plays on every machine and has no tuning of its own
                fx.ExplosionShake = Tune.ExplosionShake;
            }
        }

        protected override void OnLoopEnd(YoyoProjectile yoyo, NPC target, Vector2 at, bool closed)
        {
            if (closed)
                Explode(yoyo, at);
        }

        // The loop closed (owner's client): the blood explosion hits every enemy in the circle and makes it bleed. The looped enemy is in it
        // too unless it already died, in which case the explosion still goes off at the spot where it was. The visuals are not here: the
        // close plays them on every machine (LoopYoyoProjectile.CloseLoop -> ArteryFx.LoopClose).
        void Explode(YoyoProjectile yoyo, Vector2 at)
        {
            int damage = Math.Max(1, (int)(yoyo.Projectile.damage * Tune.ExplosionDamageFraction));
            AreaEffect.ForEachNpc(at, Tune.ExplosionRadiusPx, npc =>
            {
                npc.SimpleStrikeNPC(damage, npc.Center.X >= at.X ? 1 : -1, false, Tune.ExplosionKnockback, DamageClass.Melee);
                if (npc.active)   // the hit may have killed it
                    npc.AddBuff(BuffID.Bleeding, Tune.BleedingTicks);   // AddBuff syncs itself
            });
        }
    }
}