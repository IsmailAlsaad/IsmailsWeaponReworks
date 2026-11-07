using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    public enum DashPhase
    {
        None,
        Dash,     // flying along the direction at full speed
        HitStop,  // frozen for a few ticks right after an impact
        Bounce    // thrown back, slowing down
    }

    /// <summary>
    /// The state of the local player's dash (use PlayerDash to drive it). While a dash runs, the player's velocity is overridden right
    /// before vanilla moves them (PreUpdateMovement, which also cancels gravity, jumps and fall damage), and right after the move
    /// (PostUpdate) the dash looks for a tile straight ahead: a head-on hit bounces (or stops) the player, a shallow graze slides along
    /// the surface so the player never gets stuck. An enemy hit arrives from the weapon through Bounce. One bounce per dash.
    /// MULTIPLAYER: only the owner's client runs any of this; vanilla syncs the player's position and velocity, so the other machines
    /// see a normal moving player (and the held projectile draws the pose and afterimages).
    /// </summary>
    public class DashPlayer : ModPlayer
    {
        public DashPhase Phase { get; private set; }
        public Vector2 Direction;
        public DashSettings Settings;
        public bool Bounced;

        Action<Vector2, Vector2, bool> onBounce;
        int ticksLeft;
        Vector2 prePosition;
        Vector2 bounceVelocity;
        float boost = 1f; // liquid slows the player's movement down; this scales the velocity up until the real travel matches the speed

        Vector2? aimPoint;   // a dash toward a point (PlayerDash.Begin with a target); null = a plain dash along Direction
        int stuckTicks;

        bool Local => Player.whoAmI == Main.myPlayer;

        public void Begin(Vector2 direction, DashSettings settings, Action<Vector2, Vector2, bool> bounceCallback, Vector2? target = null)
        {
            if (!Local)
                return;
            Direction = direction.SafeNormalize(new Vector2(Player.direction, 0f));
            Settings = settings;
            onBounce = bounceCallback;
            aimPoint = target;
            stuckTicks = 0;
            Bounced = false;
            boost = 1f;
            ticksLeft = settings.Ticks;
            Phase = DashPhase.Dash;
            prePosition = Player.position;
            if (settings.ImmuneTicks > 0)
                Immunity.Grant(Player, settings.ImmuneTicks);
            HoldFall();
        }

        public override void PreUpdateMovement()
        {
            if (Phase == DashPhase.None || !Local)
                return;
            if (Player.dead || !Player.active || Player.CCed)
            {
                Finish(false);
                return;
            }

            prePosition = Player.position;
            HoldFall();
            switch (Phase)
            {
                case DashPhase.Dash:
                    Player.velocity = Direction * (Settings.Speed * boost);
                    if (Settings.PassPlatforms && Direction.Y * Player.gravDir > 0.05f)
                        StepThroughPlatforms();
                    break;
                case DashPhase.HitStop:
                    Player.velocity = Vector2.Zero;
                    break;
                case DashPhase.Bounce:
                    Player.velocity = bounceVelocity;
                    break;
            }
        }

        public override void PostUpdate()
        {
            if (Phase == DashPhase.None || !Local)
                return;

            switch (Phase)
            {
                case DashPhase.Dash:
                    TickDash();
                    break;
                case DashPhase.HitStop:
                    if (--ticksLeft <= 0)
                        StartBounceFlight();
                    break;
                case DashPhase.Bounce:
                    bounceVelocity *= Settings.BounceDrag;
                    if (--ticksLeft <= 0)
                        Finish(true);
                    break;
            }
        }

        // Platforms: vanilla's move step only lets a body through a platform with its own "fall through" flag, which is tied to the real
        // Down key (forcing controlDown never reached it; the Factorraria mech suit found the same). So the dash does the step itself: when
        // the move is blocked only because of a platform (the same TileCollision with and without fall-through gives different answers),
        // the player is moved by the fall-through result and the velocity is cleared, so vanilla's step that follows has nothing left to
        // block. The velocity is rebuilt every tick of the dash, so clearing it costs nothing.
        void StepThroughPlatforms()
        {
            Vector2 velocity = Player.velocity;
            if (velocity == Vector2.Zero)
                return;
            int gravDir = (int)Player.gravDir;
            Vector2 solid = Collision.TileCollision(Player.position, velocity, Player.width, Player.height, false, false, gravDir);
            Vector2 through = Collision.TileCollision(Player.position, velocity, Player.width, Player.height, true, true, gravDir);
            if (solid == through)
                return;
            Player.position += through;
            Player.velocity = Vector2.Zero;
        }

        void TickDash()
        {
            // How far did vanilla really move us along the dash? In liquid it is less than the velocity; scale the velocity up for the
            // next tick so the dash covers the same ground (the longer liquid dash is then longer in ticks, not slower).
            float moved = Vector2.Dot(Player.position - prePosition, Direction);
            bool touching = BounceMath.TileNormal(Player, Direction, out Vector2 normal);
            if (!Player.wet)
                boost = 1f;
            else if (!touching && moved > 1f)
                boost = MathHelper.Clamp(boost * Settings.Speed / moved, 1f, 4f);

            if (aimPoint.HasValue)
            {
                TickTargetDash(touching, normal, moved);
                return;
            }

            if (touching)
            {
                float into = -Vector2.Dot(Direction, normal); // 1 = straight into the surface, 0 = along it
                if (into < Settings.GrazeCos)
                {
                    Vector2 slide = BounceMath.Slide(Direction, normal);
                    Direction = slide.LengthSquared() > 0.0001f ? Vector2.Normalize(slide) : Direction;
                }
                else if (Settings.BounceOnTile)
                {
                    Bounce(normal, true);
                    return;
                }
                else
                {
                    Finish(false);
                    return;
                }
            }

            if (--ticksLeft <= 0)
                Finish(true);
        }

        // A dash toward a point (the Katana's blade). It never just stops: the player is wider than the line that was aimed at the point, so
        // a corner is the usual obstacle and it is slid along (the heading is steered back toward the point every tick). The dash ends when
        // the player has arrived (ArriveDistance), when the ticks run out, or when he is pressed against a tile and has not moved for
        // StuckTicks, and it always ends with an impulse toward the point instead of a dead stop.
        void TickTargetDash(bool touching, Vector2 normal, float moved)
        {
            Vector2 toTarget = aimPoint.Value - Player.Center;
            float distance = toTarget.Length();
            Vector2 heading = distance > 0.01f ? toTarget / distance : Direction;

            if (Settings.ArriveDistance > 0f && distance <= Settings.ArriveDistance)
            {
                FinishWithImpulse();
                return;
            }

            // Past the target (a dash overshoots it by up to one step on its last tick): the dash is over. The heading must NOT be turned
            // toward the target now, it points backward, and that was what threw the player back the way he came.
            if (Vector2.Dot(toTarget, Direction) < 0f)
            {
                FinishWithImpulse();
                return;
            }

            if (touching)
            {
                Vector2 slide = BounceMath.Slide(heading, normal);
                Direction = slide.LengthSquared() > 0.0001f ? Vector2.Normalize(slide) : heading;
            }
            else
                Direction = heading;

            if (touching && moved < Settings.Speed * 0.1f)
                stuckTicks++;
            else
                stuckTicks = 0;
            if (Settings.StuckTicks > 0 && stuckTicks >= Settings.StuckTicks)
            {
                FinishWithImpulse();
                return;
            }

            if (--ticksLeft <= 0)
                FinishWithImpulse();
        }

        // Ends a target dash and leaves the player moving along the vector from his center to the target.
        void FinishWithImpulse()
        {
            // Toward the target while it is still ahead; when the player has flown past it (a dash overshoots by up to a step) the impulse
            // keeps the dash's own heading, so he never gets thrown back the way he came.
            Vector2 heading = Direction;
            if (aimPoint.HasValue)
            {
                Vector2 toTarget = aimPoint.Value - Player.Center;
                if (toTarget.LengthSquared() > 1f && Vector2.Dot(toTarget, Direction) > 0f)
                    heading = Vector2.Normalize(toTarget);
            }
            Vector2 impulse = heading * (Settings.Speed * Settings.ImpulseKeep);
            Finish(false);
            Player.velocity = impulse;
        }

        // Called by the weapon (an enemy hit) or by TickDash (a tile hit). Reflects the dash, freezes for the hit-stop, then flies back.
        public bool Bounce(Vector2 normal, bool isTile)
        {
            if (!Local || Phase != DashPhase.Dash || Bounced)
                return false;

            normal = normal.SafeNormalize(-Direction);
            Bounced = true;
            Vector2 outgoing = BounceMath.Reflect(Direction * Settings.Speed, normal);
            outgoing = BounceMath.PushAway(outgoing, normal, Settings.Speed * 0.5f);
            bounceVelocity = outgoing * Settings.BounceSpeed;

            if (Settings.HitStopTicks > 0)
            {
                Phase = DashPhase.HitStop;
                ticksLeft = Settings.HitStopTicks;
            }
            else
                StartBounceFlight();

            onBounce?.Invoke(Player.Center, normal, isTile);
            return true;
        }

        void StartBounceFlight()
        {
            if (Settings.BounceTicks <= 0)
            {
                Player.velocity = bounceVelocity;
                Finish(true);
                return;
            }
            Phase = DashPhase.Bounce;
            ticksLeft = Settings.BounceTicks;
        }

        // Ends the dash. keepSpeed = true leaves the player some of the dash's momentum (or the bounce's), false stops them dead.
        public void Finish(bool keepSpeed)
        {
            if (Phase == DashPhase.None)
                return;
            if (!keepSpeed)
                Player.velocity = Vector2.Zero;
            else if (Phase == DashPhase.Dash)
                Player.velocity = Direction * (Settings.Speed * 0.3f);
            else if (Phase == DashPhase.Bounce)
                Player.velocity = bounceVelocity;
            if (Settings.ImmuneTailTicks > 0 && !Player.dead)
                Immunity.Grant(Player, Settings.ImmuneTailTicks); // the grace period after the dash, however it ended
            Phase = DashPhase.None;
            onBounce = null;
            aimPoint = null;
            stuckTicks = 0;
            HoldFall();
        }

        // No fall damage from anything that happens during (or right after) a dash.
        void HoldFall()
        {
            Player.fallStart = (int)(Player.position.Y / 16f);
            Player.fallStart2 = Player.fallStart;
        }
    }
}