using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// The Hive-Five's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and trick. The vanilla
    /// sprite is used until the yoyo's own art exists (its path is in HiveFiveTuning).
    /// </summary>
    public class HiveFiveProjectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.HiveFive;

        public override int YoyoItemType => ItemID.HiveFive;

        protected override YoyoTuning CreateTuning()
        {
            return new HiveFiveTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new HiveFiveFx();
        }

        HiveFiveTrick hiveTrick;   // the left click trick (the projectile reads its spin for the honey trail)
        PixelTrail trail;          // the honey trail of a charged Around the World (visual only: every machine builds its own)

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            hiveTrick = new HiveFiveTrick(tuning);
            return hiveTrick;
        }

        // The right click: the Sleeper hold (charges the yoyo).
        protected override YoyoTrick CreateRightTrick(YoyoTuning tuning)
        {
            return new SleeperHoldTrick(tuning);
        }

        // Every machine: the honey trail while a charged Around the World flies (several points per tick along the circle, so it is a
        // smooth ring), and golden sparkles on a charged yoyo that waits. A PixelTrail (rule 11): OnKill hands it to DetachedTrails.
        public override void PostAI()
        {
            base.PostAI();
            if (Main.dedServ || !Projectile.active)
                return;
            HiveFiveTuning tuning = (HiveFiveTuning)Tuning;
            trail ??= new PixelTrail(new HiveFiveTrailStyle(tuning), IsOwner);
            if (hiveTrick != null && State == YoyoState.Out && hiveTrick.TrailActive)
            {
                int samples = Math.Max(1, tuning.HoneyTrailSamplesPerTick);
                for (int i = 1; i < samples; i++)
                    trail.Add(hiveTrick.PathPoint(this, i / (float)samples));
                trail.Add(Projectile.Center);   // the newest point is exactly where the yoyo is
            }
            trail.Tick();

            if (Charged && State != YoyoState.Out)
            {
                Lighting.AddLight(Projectile.Center, 0.5f, 0.35f, 0.05f);
                if (Main.rand.NextBool(5) && Fx is HiveFiveFx fx)
                    fx.ChargedSparkle(Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();
            return base.PreDraw(ref lightColor);
        }

        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
            DetachedTrails.Hand(trail);
            trail = null;
        }
    }
}
