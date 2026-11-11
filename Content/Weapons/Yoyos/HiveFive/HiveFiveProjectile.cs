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
        float outlineShown;        // how visible the golden outline is (0..1, eased; visual only)

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
            float want = Charged || holdReady ? 1f : 0f;
            outlineShown += (want - outlineShown) * MathHelper.Clamp(tuning.OutlineEase, 0.01f, 1f);
            if (outlineShown > 0.05f)
            {
                Lighting.AddLight(Projectile.Center, 0.5f * outlineShown, 0.35f * outlineShown, 0.05f * outlineShown);
                if (State != YoyoState.Out && Main.rand.NextBool(8) && Fx is HiveFiveFx fx)
                    fx.ChargedDrip(Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();
            DrawOutline();
            return base.PreDraw(ref lightColor);
        }

        // The golden honey outline: a soft glow copy of the yoyo sprite behind it, then the sprite repeated in eight directions around it in
        // the outline color and four times closer in the brighter core color (the look of the other pulsing outlines, PixelOutline.Pulse).
        // Colors have alpha 0 = additive. Visual only: every machine draws its own.
        void DrawOutline()
        {
            if (outlineShown < 0.02f || Main.dedServ)
                return;
            HiveFiveTuning tuning = (HiveFiveTuning)Tuning;
            Texture2D texture = GetTexture();
            Vector2 position = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float rotation = SpinAngle;
            float strength = outlineShown * PixelOutline.Pulse((int)Main.GameUpdateCount, tuning.OutlinePulseTicks, tuning.OutlineMin, tuning.OutlineMax);

            Main.EntitySpriteDraw(texture, position, null, tuning.OutlineColor * (tuning.GlowAlpha * strength), rotation, origin, tuning.GlowScale, SpriteEffects.None, 0);
            for (int i = 0; i < 8; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / 8f).ToRotationVector2();
                Main.EntitySpriteDraw(texture, position + direction * tuning.OutlineWidth, null, tuning.OutlineColor * (0.9f * strength), rotation, origin, 1f, SpriteEffects.None, 0);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 direction = (MathHelper.PiOver2 * i).ToRotationVector2();
                Main.EntitySpriteDraw(texture, position + direction * (tuning.OutlineWidth * 0.5f), null, tuning.OutlineCoreColor * (0.5f * strength), rotation, origin, 1f, SpriteEffects.None, 0);
            }
        }

        public override void OnKill(int timeLeft)
        {
            base.OnKill(timeLeft);
            DetachedTrails.Hand(trail);
            trail = null;
        }
    }
}
