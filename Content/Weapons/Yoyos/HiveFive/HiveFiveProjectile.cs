using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        SleeperHoldTrick sleeperTrick;   // the right click trick (its Ready flag shows the outline when max speed is reached)
        readonly YoyoChargeOutline chargeOutline = new YoyoChargeOutline();   // the golden outline (shared helper; visual only: every machine draws its own)

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            hiveTrick = new HiveFiveTrick(tuning);
            return hiveTrick;
        }

        // The right click: the Sleeper hold (charges the yoyo).
        protected override YoyoTrick CreateRightTrick(YoyoTuning tuning)
        {
            sleeperTrick = new SleeperHoldTrick(tuning);
            return sleeperTrick;
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

            // The golden outline: shown while the yoyo is charged, and while the Sleeper hold has reached max speed (the same moment as the chime).
            bool holdReady = State == YoyoState.Out && UsingRight && sleeperTrick != null && sleeperTrick.Ready;
            bool want = Charged || holdReady ? true : false;
            chargeOutline.Tick(want, tuning);
            float outlineShown = chargeOutline.Shown;
            if (outlineShown > 0.05f)
            {
                Lighting.AddLight(Projectile.Center, 0.5f * outlineShown, 0.35f * outlineShown, 0.05f * outlineShown);
                if (State != YoyoState.Out && Main.rand.NextBool(8) && Fx is HiveFiveFx fx)
                    fx.ChargedDrip(Projectile.Center);
            }
        }

        // The string: white normally, honey colored while the yoyo is charged (or holding at max speed), blended by the outline's fade.
        protected override bool StringColors(out Color main, out Color alt, out Color outline)
        {
            HiveFiveTuning tuning = (HiveFiveTuning)Tuning;
            float k = chargeOutline.Shown;
            main = Color.Lerp(tuning.RopeColor, tuning.ChargedRopeColor, k);
            alt = Color.Lerp(tuning.RopeAltColor, tuning.ChargedRopeAltColor, k);
            outline = Color.Lerp(tuning.RopeOutlineColor, tuning.ChargedRopeOutlineColor, k);
            return k > 0.01f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();
            chargeOutline.Draw(GetTexture(), Projectile.Center, SpinAngle, (HiveFiveTuning)Tuning);   // the golden honey outline (shared helper)
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
