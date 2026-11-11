using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The numbers of the Pop the Clutch pattern, with defaults (see IWalkTheDogTuning for how a yoyo uses them). It also uses the numbers of
    /// IForwardPassTuning (the ellipse: width ratio, ease, reach margin, platforms). Properties, so hot reload can tweak them.
    /// </summary>
    public interface IPopTheClutchTuning
    {
        int PopWindupTicks => 26;             // the wind-up: the yoyo dangles behind the back and spins up (60 = 1 second)
        float PopDangleBackPx => 12f;         // the dangling yoyo hangs this far behind the hand (away from where the player faces) ...
        float PopDangleDownPx => 30f;         // ... and this far below it
        float PopDangleSwayPx => 2f;          // a little side sway
        int PopDangleDropTicks => 6;          // it drops from the player to the dangle spot over this many ticks
        float PopWindupArmDeg => -125f;       // the arm's angle while winding up, facing-local (0 = forward, -90 = straight up, below -90 = up and back)
        float PopWindupSpinStart => 0.15f;    // the yoyo's sprite turns this fast (radians per tick) at the start of the wind-up ...
        float PopWindupSpinMax => 1.2f;       // ... and this fast at its end
        float PopWindupSpinCurve => 2f;       // 1 = a steady speed-up, 2 = slow at first and faster and faster
        int PopPasses => 2;                   // swings in a row: the 1st bulges up, the 2nd down, the 3rd up again ...
        int PopPassTicks => 26;               // one pass (out along the ellipse and back to the player), shorter than the Forward Pass so it is quick
        int PopThrowBlendTicks => 8;          // the first pass leaves the dangle spot (behind the back) and swings through the player over this many ticks
        float PopRecallStartSpeed => 12f;     // a pass that hits a block is pulled straight back to the player: speed at the start ...
        float PopRecallAccel => 1.6f;         // ... speeding up by this much per tick ...
        float PopRecallMaxSpeed => 26f;       // ... up to this speed (px per tick)
        float PopRecallArrivePx => 14f;       // back at the player when this close (the next pass starts there)
        bool PopResetHitsPerPass => true;     // every pass starts with fresh hit cooldowns, so the second swing can hit the same enemy again
    }

    class PopTheClutchDefaults : IPopTheClutchTuning
    {
    }

    /// <summary>
    /// Pop the Clutch (pattern; the Valor's left click): like a charge before the Forward Pass. The player lifts the hand up and back and
    /// dangles the yoyo from behind the back; it spins up for a moment (the wind-up; the yoyo does not hurt yet). Then it is thrown like the
    /// Forward Pass, but as PopPasses quick passes in a row with no catch between them: the first bulges up, the second is the mirror image
    /// and bulges down (an ellipse each, out toward the aim and back to the player). The aim is taken when the wind-up ends, so the cursor
    /// can be moved while it spins. A solid block does NOT end the combo: it only cuts that swing short: the yoyo is pulled straight back to
    /// the player (still hurting on the way) and the next swing starts from there. A block in the LAST swing ends the combo like the Forward
    /// Pass (the normal pull back, still hurting). Letting go of the button does not cut it short; a held button starts the next combo after
    /// the catch.
    /// MULTIPLAYER: the owner decides the click, the aim and the hits. The wind-up and every pass are a function of the ticks since the
    /// throw (OutTicks), the side and the aim, so every machine places the yoyo itself. The owner's aim is chosen at the end of the wind-up and
    /// travels in the existing extra sync (YoyoProjectile.Aim, zero until chosen: the other machines keep the yoyo dangling until it arrives).
    /// Blocks are found by every machine itself (the tiles are the same everywhere); only the owner decides when the combo ends. The arm, the
    /// string and the effects are never synced.
    /// </summary>
    public class PopTheClutchTrick : ForwardPassTrick
    {
        protected enum PopPhase { Windup, Pass, Recall }

        protected readonly IPopTheClutchTuning Pop;
        protected PopPhase phase;
        protected int passIndex;        // 0 = the first pass
        protected int phaseStart;       // OutTicks when the current pass started
        protected bool aimed;           // the owner has chosen the aim
        protected float windup01;       // how far the wind-up is (0..1)
        protected float recallSpeed;    // the pull back after a block
        protected Vector2 launchOffset; // where the dangling yoyo was relative to the player when the first pass started

        public PopTheClutchTrick(YoyoTuning tuning) : base(tuning)
        {
            Pop = tuning as IPopTheClutchTuning ?? new PopTheClutchDefaults();
        }

        public override string Name => "Pop the Clutch";
        public override int MaxHits => int.MaxValue;
        public override bool RetractsOnRelease => false;
        public override bool HurtsWhileOut => phase != PopPhase.Windup;   // the dangling yoyo behind the back is harmless

        public float Windup01 => windup01;
        public int PassIndex => passIndex;
        public bool Winding => phase == PopPhase.Windup;

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            phase = PopPhase.Windup;
            passIndex = 0;
            phaseStart = 0;
            aimed = false;
            windup01 = 0f;
            recallSpeed = 0f;
            launchOffset = Vector2.Zero;
            stopped = false;
            comingBack = false;
            blockedComingBack = false;
            if (authoritative)
                yoyo.Aim = Vector2.Zero;   // not chosen yet: the owner picks it at the end of the wind-up (zero travels with the throw)
            yoyo.Vel = Vector2.Zero;
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            if (stopped)
            {
                yoyo.Vel = Vector2.Zero;
                return;
            }
            switch (phase)
            {
                case PopPhase.Windup:
                    TickWindup(yoyo);
                    break;
                case PopPhase.Pass:
                    TickPass(yoyo);
                    break;
                default:
                    TickRecall(yoyo);
                    break;
            }
        }

        // The wind-up: the yoyo drops from the player to the spot behind the back (below the raised hand) and dangles there, spinning up.
        void TickWindup(YoyoProjectile yoyo)
        {
            int ticks = yoyo.OutTicks;
            windup01 = MathHelper.Clamp(ticks / (float)Math.Max(1, Pop.PopWindupTicks), 0f, 1f);
            float lower = 1f - MathHelper.Clamp(ticks / (float)Math.Max(1, Pop.PopDangleDropTicks), 0f, 1f);
            lower = 1f - lower * lower * lower;   // eased out: it drops quickly and settles
            float sway = (float)Math.Sin(ticks * 0.35f) * Pop.PopDangleSwayPx * lower;
            Vector2 local = new Vector2(-yoyo.Side * Pop.PopDangleBackPx + sway, yoyo.GravDir * Pop.PopDangleDownPx);   // behind and toward the feet
            Vector2 dangle = yoyo.HandFollowed + local.RotatedBy(yoyo.Tilt);
            Vector2 target = Vector2.Lerp(yoyo.Owner.MountedCenter, dangle, lower);
            yoyo.Vel = target - yoyo.Projectile.Center;
            yoyo.Projectile.Center = target;   // no block check: like the idle yoyo it may hang inside a block behind the player
            yoyo.Fx.Charging(target, windup01);

            if (ticks < Pop.PopWindupTicks)
                return;
            if (yoyo.IsOwner && !aimed)
            {
                // The owner chooses the aim now (the cursor as it is at the end of the wind-up) and sends it.
                aimed = true;
                Vector2 toCursor = Main.MouseWorld - yoyo.Owner.MountedCenter;
                float reach = Math.Max(48f, T.RopeLength() - P.ForwardPassReachMargin);   // always the full distance, like the Forward Pass
                Vector2 direction = toCursor.LengthSquared() > 1f ? Vector2.Normalize(toCursor) : new Vector2(yoyo.Side, 0f);
                yoyo.Aim = direction * reach;
                yoyo.Projectile.netUpdate = true;
            }
            if (yoyo.Aim != Vector2.Zero)
                BeginFirstPass(yoyo);   // the other machines wait here until the aim has reached them
        }

        void BeginFirstPass(YoyoProjectile yoyo)
        {
            Projectile projectile = yoyo.Projectile;
            PrepareEllipse(yoyo);
            launchOffset = projectile.Center - yoyo.Owner.MountedCenter;
            if (Collision.SolidCollision(projectile.position, projectile.width, projectile.height))
            {
                // The dangle spot is inside a block (a wall behind the player): start from the player instead, so the first move is not stuck.
                projectile.Center = yoyo.Owner.MountedCenter;
                launchOffset = Vector2.Zero;
            }
            passIndex = 0;
            StartPass(yoyo);
            yoyo.Fx.ClutchPop(projectile.Center, axis);
        }

        void StartPass(YoyoProjectile yoyo)
        {
            phase = PopPhase.Pass;
            phaseStart = yoyo.OutTicks;
            comingBack = false;
        }

        // The next swing (after a pass has ended at the player, or after the pull back from a block).
        void NextPass(YoyoProjectile yoyo)
        {
            passIndex++;
            StartPass(yoyo);
            if (yoyo.IsOwner && Pop.PopResetHitsPerPass)
                Array.Clear(yoyo.Projectile.localNPCImmunity, 0, yoyo.Projectile.localNPCImmunity.Length);   // fresh hit cooldowns for this swing
            yoyo.Fx.ClutchSwing(yoyo.Projectile.Center, passIndex);
        }

        bool LastPass => passIndex + 1 >= Math.Max(1, Pop.PopPasses);

        // One pass: an ellipse from the player toward the aim and back, bulging up (even passes) or down (odd passes).
        void TickPass(YoyoProjectile yoyo)
        {
            int passTick = yoyo.OutTicks - phaseStart;
            float t = MathHelper.Clamp(passTick / (float)Math.Max(1, Pop.PopPassTicks), 0f, 1f);
            float eased = MathHelper.Lerp(t, t * t * (3f - 2f * t), P.ForwardPassEase);
            comingBack = eased > 0.5f;
            Vector2 offset = EllipseOffset(eased, passIndex % 2 == 0 ? 1f : -1f);
            if (passIndex == 0)
            {
                // The first pass starts behind the back: that offset fades out, so the yoyo swings out through the player.
                float blend = MathHelper.Clamp(passTick / (float)Math.Max(1, Pop.PopThrowBlendTicks), 0f, 1f);
                blend = blend * blend * (3f - 2f * blend);
                offset += launchOffset * (1f - blend);
            }
            Vector2 target = yoyo.Owner.MountedCenter + offset;

            YoyoContact contact = yoyo.Move(target - yoyo.Projectile.Center, P.ForwardPassThroughPlatforms);
            if (contact.Blocked)
            {
                yoyo.Fx.Impact(yoyo.Projectile.Center, contact.Normal, contact.Impact);
                if (LastPass)
                {
                    // The last swing: the combo ends like the Forward Pass (the normal pull back, still hurting).
                    stopped = true;
                    blockedComingBack = P.ForwardPassBlockedReturnHurts;
                    if (yoyo.IsOwner)
                        yoyo.BeginReturn();
                }
                else
                {
                    // Only this swing is cut short: pulled straight back to the player, then the next swing starts.
                    phase = PopPhase.Recall;
                    comingBack = true;
                    recallSpeed = Pop.PopRecallStartSpeed;
                }
                return;
            }
            if (t >= 1f)
            {
                if (LastPass)
                {
                    if (yoyo.IsOwner)
                        yoyo.BeginReturn();   // the combo is done: a short pull to the hand and the catch
                }
                else
                    NextPass(yoyo);
            }
        }

        // The pull back after a block: straight to the player's centre (through blocks, hurting on the way), faster and faster; then the next pass.
        void TickRecall(YoyoProjectile yoyo)
        {
            Vector2 goal = yoyo.Owner.MountedCenter;
            Vector2 to = goal - yoyo.Projectile.Center;
            float distance = to.Length();
            recallSpeed = Math.Min(recallSpeed + Pop.PopRecallAccel, Pop.PopRecallMaxSpeed);
            if (distance <= Math.Max(Pop.PopRecallArrivePx, recallSpeed))
            {
                yoyo.Vel = to;
                yoyo.Projectile.Center = goal;
                NextPass(yoyo);
                return;
            }
            Vector2 step = to / distance * recallSpeed;
            yoyo.Vel = step;
            yoyo.Projectile.Center += step;
        }

        // The arm: raised up and back during the wind-up (the base then turns it to point at the yoyo for the throw).
        public override bool ArmPose(YoyoProjectile yoyo, out float localArmRadians)
        {
            localArmRadians = MathHelper.ToRadians(Pop.PopWindupArmDeg);
            return phase == PopPhase.Windup;
        }

        // The dangling yoyo hangs on a straight string; so does every way back.
        public override bool RopeTaut(YoyoProjectile yoyo)
        {
            return comingBack || phase == PopPhase.Windup;
        }

        public override float SpinStep(YoyoProjectile yoyo)
        {
            if (phase == PopPhase.Windup)
            {
                float c = (float)Math.Pow(windup01, Math.Max(0.1f, Pop.PopWindupSpinCurve));
                return yoyo.Side * MathHelper.Lerp(Pop.PopWindupSpinStart, Pop.PopWindupSpinMax, c);
            }
            return base.SpinStep(yoyo);
        }
    }
}