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
            return new WalkTheDogTrick();
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