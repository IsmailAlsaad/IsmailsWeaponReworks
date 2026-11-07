using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// How one dash behaves (a weapon's Tuning builds one): the speed in pixels per tick and how many ticks it lasts, whether a tile
    /// bounces the player or just stops them, how much speed a bounce keeps, the hit-stop (the player freezes for a few ticks on impact)
    /// and the bounce phase after it, optional immunity (ticks from the start; 0 = none) and the graze limit (a hit at a shallower angle
    /// than this, as the cosine of the angle between the direction and the surface normal, slides along the surface instead of bouncing).
    /// </summary>
    public readonly struct DashSettings
    {
        public readonly float Speed;
        public readonly int Ticks;
        public readonly bool BounceOnTile;
        public readonly float BounceSpeed;
        public readonly int HitStopTicks;
        public readonly int BounceTicks;
        public readonly int ImmuneTicks;
        public readonly float GrazeCos;
        public readonly float BounceDrag;
        public readonly int ImmuneTailTicks;   // extra immunity after the dash ends, however it ends (0 = none)
        public readonly float ArriveDistance;  // target dashes only: the dash is over once the player's center is this close to the target (0 = run the ticks out)
        public readonly int StuckTicks;        // target dashes only: pressed against a tile without moving this many ticks ends the dash (0 = never)
        public readonly bool PassPlatforms;    // a dash that heads down moves through platforms (DashPlayer.StepThroughPlatforms)
        public readonly float ImpulseKeep;     // target dashes only: a dash that ends leaves the player moving toward the target at Speed x this

        public DashSettings(float speed, int ticks, bool bounceOnTile, float bounceSpeed, int hitStopTicks, int bounceTicks, int immuneTicks = 0, float grazeCos = 0.3f, float bounceDrag = 0.88f, int immuneTailTicks = 0, float arriveDistance = 0f, int stuckTicks = 0, float impulseKeep = 0f, bool passPlatforms = false)
        {
            Speed = speed;
            Ticks = Math.Max(1, ticks);
            BounceOnTile = bounceOnTile;
            BounceSpeed = bounceSpeed;
            HitStopTicks = Math.Max(0, hitStopTicks);
            BounceTicks = Math.Max(0, bounceTicks);
            ImmuneTicks = Math.Max(0, immuneTicks);
            GrazeCos = grazeCos;
            BounceDrag = bounceDrag;
            ImmuneTailTicks = Math.Max(0, immuneTailTicks);
            ArriveDistance = Math.Max(0f, arriveDistance);
            StuckTicks = Math.Max(0, stuckTicks);
            ImpulseKeep = Math.Max(0f, impulseKeep);
            PassPlatforms = passPlatforms;
        }
    }

    /// <summary>
    /// The one way a weapon dashes the player (the Trident now, the Storm Spear next). All of it runs on the player's OWN client only
    /// (position and velocity belong to that client and vanilla syncs them), see DashPlayer. Visuals for the other machines come from
    /// the weapon's held projectile, which knows the phase from its own age.
    /// </summary>
    public static class PlayerDash
    {
        // Starts a dash toward 'direction' (a unit vector in the world). onBounce(position, normal, isTile) runs on the owner's client at
        // the moment of a bounce so the weapon can add its sparks, sound and camera shake. Does nothing on other players' machines.
        // 'target' (optional) makes it a dash TOWARD a point: a tile in the way is slid along instead of stopping the dash, and however the
        // dash ends the player keeps an impulse toward the point (see DashPlayer.TickTargetDash and the target fields of DashSettings).
        public static void Begin(Player player, Vector2 direction, DashSettings settings, Action<Vector2, Vector2, bool> onBounce = null, Vector2? target = null)
        {
            if (player.whoAmI != Main.myPlayer)
                return;
            player.GetModPlayer<DashPlayer>().Begin(direction, settings, onBounce, target);
        }

        // The tip hit an enemy: bounce off it (once per dash). Returns false when there was no dash, or it already bounced.
        public static bool Bounce(Player player, Vector2 normal)
        {
            if (player.whoAmI != Main.myPlayer)
                return false;
            return player.GetModPlayer<DashPlayer>().Bounce(normal, false);
        }

        // Stops the dash at once (the player keeps no speed).
        public static void End(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return;
            player.GetModPlayer<DashPlayer>().Finish(false);
        }

        public static bool IsDashing(Player player)
        {
            return player.GetModPlayer<DashPlayer>().Phase != DashPhase.None;
        }

        // Draws the player's vanilla afterimages this tick (call every tick the dash should leave them; works on every machine).
        public static void Afterimage(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
    }
}