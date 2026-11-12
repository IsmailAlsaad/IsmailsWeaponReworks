using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BallOHurt
{
    /// <summary>
    /// The Ball O' Hurt's flail head: the shared flail loop (Common/Flail/FlailProjectile.cs) plus its own parts: numbers and effects, and the
    /// tier 3 special: the head flies straight at the cursor (hurting enemies on the way); at the apex (the cursor, or a step before the
    /// first tile it touches) the ball tears loose into a BallOHurtBallProjectile (it bounces on tiles, hops between enemies and comes home by
    /// itself) and the snapped chain whips back to the hand with the head hidden. When the chain is home the loop restarts at tier 1 if the
    /// button is held, otherwise it ends and the weapon can be used again at once, whatever the ball is doing.
    /// MULTIPLAYER: the shared loop's rules. The straight flight is deterministic on every machine (a straight line to the target); the
    /// owner's client decides the tear-off and spawns the ball as a normal synced projectile.
    /// </summary>
    public class BallOHurtFlailProjectile : FlailProjectile
    {
        // Vanilla art until a BallOHurtFlailProjectile.png exists next to this file (the loose ball uses the same art).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallOHurt;

        protected override int ItemType => ItemID.BallOHurt;
        protected override FlailTuning Tuning => BallOHurtTuning.Instance;
        protected override FlailFx Fx => BallOHurtFx.Instance;
        protected override bool SweepsPath => CurrentPhase == Phase.Charge || CurrentPhase == Phase.Special;  // the throw is far faster than the head is wide
        protected override bool HeadHidden => CurrentPhase == Phase.Retract;   // the ball is gone: only the snapped chain whips back

        static BallOHurtTuning Ball => BallOHurtTuning.Instance;

        Vector2 specialTarget;

        // ---- the special: the straight throw ----

        protected override void BeginSpecial(Player owner)
        {
            specialTarget = owner.MountedCenter + AimAngle.ToRotationVector2() * AimReach;
        }

        protected override void UpdateSpecial(Player owner)
        {
            Vector2 oldCenter = Projectile.Center;
            Vector2 toTarget = specialTarget - Projectile.Center;
            float speed = Ball.SpecialSpeed;
            bool arrived = toTarget.Length() <= speed;
            Projectile.Center = arrived ? specialTarget : Projectile.Center + toTarget.SafeNormalize(Vector2.UnitX) * speed;
            trail?.Add(Projectile.Center);
            Fx.FlightDust(Projectile.Center, Projectile.Center - oldCenter, 3);
            Lighting.AddLight(Projectile.Center, 0.6f, 0.25f, 0.95f);

            if (!IsOwner)
                return;
            bool blocked = phaseTimer > 2 && Collision.SolidCollision(Projectile.Center - new Vector2(6f), 12, 12);
            if (arrived || blocked)
                Detach(blocked ? oldCenter : Projectile.Center);
        }

        // Owner only: the ball tears loose where the head is (a step back from the tile if the head ran into one), flying on in the direction
        // of the throw. If MaxBalls balls are already out, the oldest one is recalled to make room. The chain whips back.
        void Detach(Vector2 at)
        {
            BallOHurtBallProjectile.MakeRoom(Projectile.owner, Ball.MaxBalls);
            Vector2 direction = AimAngle.ToRotationVector2();
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), at, direction * Ball.BallSpeed, ModContent.ProjectileType<BallOHurtBallProjectile>(),
                Math.Max(1, (int)(lockedDamage * Ball.BallDamageMultiplier)), Projectile.knockBack, Projectile.owner);
            EndSpecial();
        }
    }
}