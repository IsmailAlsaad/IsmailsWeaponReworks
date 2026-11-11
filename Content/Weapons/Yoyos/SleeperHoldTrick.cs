using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The numbers of the Sleeper hold pattern, with defaults (see IWalkTheDogTuning for how a yoyo uses them). Properties, so hot reload can tweak them.
    /// </summary>
    public interface ISleeperTuning
    {
        float SleeperDropPx => 34f;         // how far below the hand the yoyo hangs while it is held (the idle pull-down is a bit shorter)
        int SleeperDropTicks => 14;         // letting it down
        float SleeperSwayPx => 2f;          // a little side sway
        int SleeperChargeTicks => 90;       // from the start of the hold until max speed (60 = 1 second)
        float SleeperStartSpin => 0.1f;     // the yoyo's sprite turns this fast (radians per tick) at the start ...
        float SleeperMaxSpin => 1.3f;       // ... and this fast at max speed
        float SleeperSpinCurve => 2f;       // 1 = a steady speed-up, 2 = slow at first and faster and faster
        float SleeperArmDeg => 68f;         // the arm's angle below the horizontal while holding (90 = straight down)
    }

    class SleeperDefaults : ISleeperTuning
    {
    }

    /// <summary>
    /// Sleeper hold (pattern; a right click any yoyo can use): the charging animation. It is the idle pull-down, but the yoyo is let a bit
    /// further down and kept there while the button is held; it starts spinning slowly and spins faster and faster until a max speed is
    /// reached (a chime and a burst: YoyoFx.ChargeReady). The yoyo does not hurt while it is held (HurtsWhileOut). Letting go pulls it back
    /// to the hand: if max speed was reached the yoyo is CHARGED (YoyoProjectile.Charged, synced), otherwise the charge is lost. A yoyo that
    /// is already charged spins at full speed at once. What the charge does is up to the yoyo (the Hive-Five's next Around the World).
    /// MULTIPLAYER: the owner's client decides the click and the release (the base syncs the state and the right click flag); every machine
    /// places the yoyo and spins it from the ticks since the hold started. Only the owner turns "max speed reached" into Charged, which
    /// travels in the base's extra sync. The arm, the string and the fx are never synced.
    /// </summary>
    public class SleeperHoldTrick : YoyoTrick
    {
        protected readonly ISleeperTuning S;
        protected float charge01;      // 0 = just started, 1 = max speed
        protected bool ready;          // max speed was reached (or the yoyo was already charged)
        protected bool startedCharged; // the yoyo was already charged when the hold started

        public SleeperHoldTrick(YoyoTuning tuning) : base(tuning)
        {
            S = tuning as ISleeperTuning ?? new SleeperDefaults();
        }

        public override string Name => "Sleeper hold";
        public override int MaxHits => int.MaxValue;
        public override bool RetractsOnRelease => true;
        public override bool FollowsCursorSide => true;   // the player can turn round (the cursor's side) while holding; the arm and the yoyo follow
        public override bool HurtsWhileOut => false;

        public float Charge01 => charge01;
        public bool Ready => ready;

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            startedCharged = yoyo.Charged;
            charge01 = startedCharged ? 1f : 0f;
            ready = startedCharged;
            yoyo.Vel = Vector2.Zero;
        }

        // 0 = in the hand, 1 = let all the way down (eased out, like the idle pull-down).
        float Extension(YoyoProjectile yoyo)
        {
            float u = 1f - MathHelper.Clamp(yoyo.OutTicks / (float)Math.Max(1, S.SleeperDropTicks), 0f, 1f);
            return 1f - u * u * u;
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            float extension = Extension(yoyo);
            float drop = Math.Min(S.SleeperDropPx, T.RopeLength()) * extension * yoyo.GravDir;   // toward the feet (up when gravity is flipped)
            float sway = (float)Math.Sin(yoyo.OutTicks * 0.12f) * S.SleeperSwayPx * extension;
            Vector2 target = yoyo.HandFollowed + new Vector2(sway, drop).RotatedBy(yoyo.Tilt);   // "down" is down for the (tilted) player
            yoyo.Vel = target - yoyo.Projectile.Center;
            yoyo.Projectile.Center = target;

            if (!startedCharged)
                charge01 = MathHelper.Clamp(yoyo.OutTicks / (float)Math.Max(1, S.SleeperChargeTicks), 0f, 1f);
            if (!ready && charge01 >= 1f)
            {
                ready = true;
                yoyo.Fx.ChargeReady(yoyo.Projectile.Center);
            }
            yoyo.Fx.Charging(yoyo.Projectile.Center, charge01);
        }

        // The owner's client: the button was let go. Max speed reached = the yoyo is charged; otherwise nothing (an earlier charge is kept only
        // when the hold started charged).
        public override void OnRetract(YoyoProjectile yoyo)
        {
            if (ready)
                yoyo.Charged = true;
        }

        public override bool ArmPose(YoyoProjectile yoyo, out float localArmRadians)
        {
            localArmRadians = MathHelper.ToRadians(MathHelper.Lerp(T.IdleArmRestDeg, S.SleeperArmDeg, Extension(yoyo)));
            return true;
        }

        public override bool RopeTaut(YoyoProjectile yoyo)
        {
            return true;
        }

        public override float SpinStep(YoyoProjectile yoyo)
        {
            float c = (float)Math.Pow(charge01, Math.Max(0.1f, S.SleeperSpinCurve));
            return yoyo.Side * MathHelper.Lerp(S.SleeperStartSpin, S.SleeperMaxSpin, c);
        }
    }
}
