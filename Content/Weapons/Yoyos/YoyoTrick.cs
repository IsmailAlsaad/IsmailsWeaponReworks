using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// A yoyo trick: what the yoyo does while it is out. The YoyoProjectile owns the state machine (idle, out, return), the rope, the arm
    /// and the net sync; a trick only moves the yoyo (through yoyo.Step) and reacts to hits. Every machine runs Tick from the synced start
    /// state; the owner's client alone decides hits and when to retract. Kept deliberately thin until a second trick shows what is shared.
    /// </summary>
    public abstract class YoyoTrick
    {
        public abstract string Name { get; }

        // Enemy hits before the yoyo retracts.
        public abstract int MaxHits { get; }

        // True: while the yoyo is out the owner's cursor side keeps deciding the yoyo's side (the trick can be turned around mid-way).
        public virtual bool FollowsCursorSide => false;

        // False: letting go of the button does not cut the trick short (it runs to its end by itself).
        public virtual bool RetractsOnRelease => true;

        // True: the yoyo leaves afterimages while it moves fast (the shared YoyoAfterimages helper).
        public virtual bool Afterimages => false;

        // Camera punch strength per enemy hit (0 = none; the shared YoyoShake helper).
        public virtual float HitShake => 0f;

        // How much the yoyo's sprite turns this tick.
        public virtual float SpinStep(YoyoProjectile yoyo)
        {
            return yoyo.Vel.X * YoyoTuning.SpinPerPixel;
        }

        // The throw starts. authoritative = the owner's client, which sets the launch velocity; the others get it from the sync.
        public abstract void Begin(YoyoProjectile yoyo, bool authoritative);

        // Once per tick while the yoyo is out, on every machine.
        public abstract void Tick(YoyoProjectile yoyo);

        // The owner's client only: the yoyo just hit an enemy.
        public virtual void OnHit(YoyoProjectile yoyo, NPC target)
        {
        }
    }

    public static class YoyoTricks
    {
        public static YoyoTrick Create(int itemType)
        {
            if (itemType == ItemID.Code1)
                return new ForwardPassTrick();
            return new WalkTheDogTrick();
        }
    }

    /// <summary>
    /// Forward Pass (Code 1's left click): the yoyo is thrown along an ellipse toward the cursor and comes back along the other half of the
    /// same ellipse, one whole loop that starts and ends at the player. Gravity does not touch it. The ellipse's long axis points at the
    /// cursor (the far end is the cursor's distance, kept inside the string's length), the outward half bulges to the upper side. It moves
    /// by following the player, so running does not break the loop. Blocks stop it: it does not pass through them, it hits the block
    /// (impact dust) and is pulled back. It hurts everything on the way (an enemy can be hit again after the hit cooldown), and it is not
    /// cut short by letting go of the button. Holding the button throws the next pass after the catch.
    /// MULTIPLAYER: the owner decides the aim (the vector to the far end, YoyoProjectile.Aim, travels in the existing extra sync), every
    /// machine then moves the yoyo along the same ellipse and stops it at blocks itself; the owner's client alone decides when it returns.
    /// </summary>
    public class ForwardPassTrick : YoyoTrick
    {
        Vector2 axis;       // unit vector from the player toward the far end
        Vector2 bulge;      // unit vector across the axis: the side the outward half swings to
        float halfReach;    // half the length of the ellipse
        bool stopped;       // a block stopped it on this machine; it waits for the owner's return

        public override string Name => "Forward Pass";
        public override int MaxHits => int.MaxValue;
        public override bool RetractsOnRelease => false;
        public override bool Afterimages => true;
        public override float HitShake => YoyoTuning.ForwardPassHitShake;

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            stopped = false;
            if (authoritative)
            {
                Player owner = yoyo.Owner;
                Vector2 toCursor = Main.MouseWorld - owner.MountedCenter;
                float longest = System.Math.Max(YoyoTuning.ForwardPassMinReach, YoyoTuning.RopeLength(owner.HeldItem) - YoyoTuning.ForwardPassReachMargin);
                float reach = MathHelper.Clamp(toCursor.Length(), YoyoTuning.ForwardPassMinReach, longest);
                Vector2 direction = toCursor.LengthSquared() > 1f ? Vector2.Normalize(toCursor) : new Vector2(yoyo.Side, 0f);
                yoyo.Aim = direction * reach;
            }
            float length = System.Math.Max(1f, yoyo.Aim.Length());
            axis = yoyo.Aim / length;
            halfReach = length * 0.5f;
            bulge = new Vector2(-axis.Y, axis.X);
            float lean = Vector2.Dot(bulge, new Vector2(0f, -yoyo.GravDir));  // toward "up" for this player's gravity
            if (System.Math.Abs(lean) < 0.15f)
                lean = Vector2.Dot(bulge, new Vector2(yoyo.Side, 0f));        // aiming straight up or down: bulge toward the throwing side
            if (lean < 0f)
                bulge = -bulge;
            yoyo.Vel = Vector2.Zero;
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            if (stopped)
            {
                yoyo.Vel = Vector2.Zero;
                return;
            }

            float t = MathHelper.Clamp(yoyo.OutTicks / (float)System.Math.Max(1, YoyoTuning.ForwardPassTicks), 0f, 1f);
            float eased = MathHelper.Lerp(t, t * t * (3f - 2f * t), YoyoTuning.ForwardPassEase);
            float theta = eased * MathHelper.TwoPi;
            Vector2 offset = axis * (halfReach * (1f - (float)System.Math.Cos(theta)))
                           + bulge * (halfReach * YoyoTuning.ForwardPassWidthRatio * (float)System.Math.Sin(theta));
            Vector2 target = yoyo.Owner.MountedCenter + offset;

            YoyoContact contact = yoyo.Move(target - yoyo.Projectile.Center);
            if (contact.Blocked)
            {
                // A block in the way: it does not pass through. Impact dust, then the owner pulls it back.
                stopped = true;
                YoyoFx.Impact(yoyo.Projectile.Center, contact.Normal, contact.Impact);
                if (yoyo.IsOwner)
                    yoyo.BeginReturn();
                return;
            }
            if (t >= 1f && yoyo.IsOwner)
                yoyo.BeginReturn();   // the loop is done: a short pull to the hand and the catch
        }

        public override float SpinStep(YoyoProjectile yoyo)
        {
            return yoyo.Side * yoyo.Vel.Length() * YoyoTuning.SpinPerPixel;
        }
    }

    /// <summary>
    /// Walk the Dog (the Wooden Yoyo's left click): the yoyo is thrown down and forward toward the side of the screen the cursor is on, hits
    /// the ground and rolls on in that direction, pulled by gravity and stopped by blocks, until it has hit enemies WalkHitsPerThrow times,
    /// reaches the end of its string, or the button is let go. A block in the way does not count as a hit: the yoyo hops (so one-block
    /// steps and slopes do not stop it) and keeps pushing.
    /// </summary>
    public class WalkTheDogTrick : YoyoTrick
    {
        bool grounded;
        int hopCooldown;

        public override string Name => "Walk the Dog";
        public override int MaxHits => YoyoTuning.WalkHitsPerThrow;
        public override bool FollowsCursorSide => true;

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            grounded = false;
            hopCooldown = 0;
            if (authoritative)
                yoyo.Vel = new Vector2(yoyo.Side * YoyoTuning.WalkThrowForwardSpeed, yoyo.GravDir * YoyoTuning.WalkThrowDownSpeed);
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            if (hopCooldown > 0)
                hopCooldown--;

            // Keep rolling toward the side it was thrown to (quickly on the ground, hardly at all in the air).
            yoyo.Vel.X = Approach(yoyo.Vel.X, yoyo.Side * YoyoTuning.WalkSpeed, grounded ? YoyoTuning.WalkAccel : YoyoTuning.WalkAirAccel);

            YoyoContact contact = yoyo.Step(YoyoTuning.WalkGravity, YoyoTuning.WalkMaxFall);

            if (contact.Grounded)
            {
                if (!grounded && contact.Impact > YoyoTuning.WalkBounceMinImpact)
                {
                    yoyo.Vel.Y = -yoyo.GravDir * contact.Impact * YoyoTuning.WalkLandBounce;
                    YoyoFx.Land(yoyo.GravDir > 0 ? yoyo.Projectile.Bottom : yoyo.Projectile.Top, contact.Impact, yoyo.GravDir);
                }
                grounded = true;
                if (contact.BlockedX && hopCooldown <= 0)
                {
                    yoyo.Vel.Y = -yoyo.GravDir * YoyoTuning.WalkHopSpeed;
                    hopCooldown = YoyoTuning.WalkHopCooldownTicks;
                }
            }
            else
                grounded = false;
        }

        public override void OnHit(YoyoProjectile yoyo, NPC target)
        {
            // Bounced off the enemy: back and up, then it rolls on toward it again.
            yoyo.Vel = new Vector2(-yoyo.Side * YoyoTuning.WalkHitRecoil, -yoyo.GravDir * YoyoTuning.WalkHitHop);
            grounded = false;
        }

        static float Approach(float value, float target, float step)
        {
            if (value < target)
                return System.Math.Min(value + step, target);
            return System.Math.Max(value - step, target);
        }
    }
}