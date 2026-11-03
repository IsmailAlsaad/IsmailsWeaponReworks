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

        bool Local => Player.whoAmI == Main.myPlayer;

        public void Begin(Vector2 direction, DashSettings settings, Action<Vector2, Vector2, bool> bounceCallback)
        {
            if (!Local)
                return;
            Direction = direction.SafeNormalize(new Vector2(Player.direction, 0f));
            Settings = settings;
            onBounce = bounceCallback;
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
            Phase = DashPhase.None;
            onBounce = null;
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