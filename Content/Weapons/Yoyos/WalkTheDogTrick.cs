using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The numbers of the Walk the Dog pattern, with defaults. A yoyo that uses the pattern lets its tuning implement this interface and
    /// lists the values it wants (a public property with the same name replaces the default); a yoyo that does not implement it gets
    /// every default.
    /// </summary>
    public interface IWalkTheDogTuning
    {
        int WalkHitsPerThrow => 3;             // enemy hits before it retracts
        float WalkThrowDownSpeed => 5f;        // the throw: down ...
        float WalkThrowForwardSpeed => 3f;     // ... and a bit toward the cursor's side
        float WalkSwitchDeadzonePx => 10f;     // while it is out, the side only switches when the cursor is this far past the player's center
        float WalkGravity => 0.45f;
        float WalkMaxFall => 14f;
        float WalkSpeed => 4.2f;               // rolling speed along the ground
        float WalkAccel => 0.35f;              // on the ground
        float WalkAirAccel => 0.06f;           // in the air
        float WalkLandBounce => 0.3f;          // fraction of the fall speed that bounces back up
        float WalkBounceMinImpact => 3f;       // softer landings do not bounce
        float WalkHopSpeed => 5.5f;            // a small hop when a block stops the roll (steps and slopes)
        int WalkHopCooldownTicks => 10;
        float WalkHitRecoil => 3.5f;           // pushed back from an enemy ...
        float WalkHitHop => 3.5f;              // ... and up
    }

    class WalkTheDogDefaults : IWalkTheDogTuning
    {
    }

    /// <summary>
    /// Walk the Dog (pattern; the Wooden Yoyo's left click): the yoyo is thrown down and forward toward the side of the screen the cursor
    /// is on, hits the ground and rolls on in that direction, pulled by gravity and stopped by blocks, until it has hit enemies
    /// WalkHitsPerThrow times, reaches the end of its string, or the button is let go. A block in the way does not count as a hit: the
    /// yoyo hops (so one-block steps and slopes do not stop it) and keeps pushing. Another yoyo uses it as it is, or subclasses it.
    /// </summary>
    public class WalkTheDogTrick : YoyoTrick
    {
        protected readonly IWalkTheDogTuning P;
        protected bool grounded;
        protected int hopCooldown;

        public WalkTheDogTrick(YoyoTuning tuning) : base(tuning)
        {
            P = tuning as IWalkTheDogTuning ?? new WalkTheDogDefaults();
        }

        public override string Name => "Walk the Dog";
        public override int MaxHits => P.WalkHitsPerThrow;
        public override bool FollowsCursorSide => true;
        public override float CursorSideDeadzonePx => P.WalkSwitchDeadzonePx;

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            grounded = false;
            hopCooldown = 0;
            if (authoritative)
                yoyo.Vel = new Vector2(yoyo.Side * P.WalkThrowForwardSpeed, yoyo.GravDir * P.WalkThrowDownSpeed);
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            if (hopCooldown > 0)
                hopCooldown--;

            // Keep rolling toward the side it was thrown to (quickly on the ground, hardly at all in the air).
            yoyo.Vel.X = Approach(yoyo.Vel.X, yoyo.Side * P.WalkSpeed, grounded ? P.WalkAccel : P.WalkAirAccel);

            YoyoContact contact = yoyo.Step(P.WalkGravity, P.WalkMaxFall);

            if (contact.Grounded)
            {
                if (!grounded && contact.Impact > P.WalkBounceMinImpact)
                {
                    yoyo.Vel.Y = -yoyo.GravDir * contact.Impact * P.WalkLandBounce;
                    yoyo.Fx.Land(yoyo.GravDir > 0 ? yoyo.Projectile.Bottom : yoyo.Projectile.Top, contact.Impact, yoyo.GravDir);
                }
                grounded = true;
                if (contact.BlockedX && hopCooldown <= 0)
                {
                    yoyo.Vel.Y = -yoyo.GravDir * P.WalkHopSpeed;
                    hopCooldown = P.WalkHopCooldownTicks;
                }
            }
            else
                grounded = false;
        }

        public override void OnHit(YoyoProjectile yoyo, NPC target)
        {
            // Bounced off the enemy: back and up, then it rolls on toward it again.
            yoyo.Vel = new Vector2(-yoyo.Side * P.WalkHitRecoil, -yoyo.GravDir * P.WalkHitHop);
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