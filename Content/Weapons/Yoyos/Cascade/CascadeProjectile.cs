using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Cascade
{
    /// <summary>
    /// The Cascade's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and tricks. The vanilla
    /// sprite is used until the yoyo's own art exists (its path is in CascadeTuning). It also owns the charge look: the hot red outline with
    /// fire embers (the shared YoyoChargeOutline) and the string that turns white to fire colored.
    /// </summary>
    public class CascadeProjectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Cascade;

        public override int YoyoItemType => ItemID.Cascade;

        protected override YoyoTuning CreateTuning()
        {
            return new CascadeTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new CascadeFx();
        }

        readonly YoyoChargeOutline chargeOutline = new YoyoChargeOutline();   // the red outline (visual only: every machine draws its own)
        SleeperHoldTrick sleeperTrick;   // the right click trick (its Ready flag shows the outline when max speed is reached)

        // The left click: Pop the Clutch.
        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new CascadeTrick(tuning);
        }

        // The right click: the Sleeper hold (charges the yoyo).
        protected override YoyoTrick CreateRightTrick(YoyoTuning tuning)
        {
            sleeperTrick = new SleeperHoldTrick(tuning);
            return sleeperTrick;
        }

        // Every machine: the outline fades in while the yoyo is charged, and while the Sleeper hold has reached max speed (the same moment
        // as the chime); a charged yoyo is lit and sends up embers (more while it flies).
        public override void PostAI()
        {
            base.PostAI();
            if (Main.dedServ || !Projectile.active)
                return;
            CascadeTuning tuning = (CascadeTuning)Tuning;
            bool holdReady = State == YoyoState.Out && UsingRight && sleeperTrick != null && sleeperTrick.Ready;
            chargeOutline.Tick(Charged || holdReady, tuning);
            float shown = chargeOutline.Shown;
            if (shown > 0.05f)
            {
                Lighting.AddLight(Projectile.Center, tuning.LightRed * shown, tuning.LightGreen * shown, tuning.LightBlue * shown);
                float chance = (State == YoyoState.Out ? tuning.EmberChanceFlying : tuning.EmberChanceWaiting) * shown;
                if (Main.rand.NextFloat() < chance && Fx is CascadeFx fx)
                    fx.ChargedEmber(Projectile.Center);
            }
        }

        // The string: white normally, red and orange fire colored while the yoyo is charged (blended by the outline's fade). The two twist
        // colors swap back and forth smoothly, so the string flickers.
        protected override bool StringColors(out Color main, out Color alt, out Color outline)
        {
            CascadeTuning tuning = (CascadeTuning)Tuning;
            float k = chargeOutline.Shown;
            float flicker = 0.5f + 0.5f * (float)System.Math.Sin(Main.GameUpdateCount * tuning.FireRopeFlickerSpeed);
            main = Color.Lerp(tuning.RopeColor, Color.Lerp(tuning.ChargedRopeColor, tuning.ChargedRopeAltColor, flicker), k);
            alt = Color.Lerp(tuning.RopeAltColor, Color.Lerp(tuning.ChargedRopeAltColor, tuning.ChargedRopeColor, flicker), k);
            outline = Color.Lerp(tuning.RopeOutlineColor, tuning.ChargedRopeOutlineColor, k);
            return k > 0.01f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            chargeOutline.Draw(GetTexture(), Projectile.Center, SpinAngle, (CascadeTuning)Tuning);
            return base.PreDraw(ref lightColor);
        }
    }
}
