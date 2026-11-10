using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// The Amazon's right click: the WalkTheDogTrick pattern from the Yoyos folder (the rolling, the hits, following the cursor's side and
    /// the retract rules are the Wooden Yoyo's, untouched), plus the barbed vines: while the yoyo rolls on the ground it plants a tiny barbed
    /// vine every VineIntervalTicks (never in the air, never closer than VineMinSpacingPx to the last one). Only the owner's client plants
    /// (AmazonBarbedVineProjectile.Plant spawns the vine with the normal projectile sync); every machine runs the same rolling.
    /// Hits during this trick do not add barbs: the barbs belong to the left click.
    /// </summary>
    public class AmazonWalkTheDogTrick : WalkTheDogTrick
    {
        int sinceVine;
        Vector2 lastVine;
        bool hasLast;

        AmazonTuning Tune => (AmazonTuning)T;

        public AmazonWalkTheDogTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            base.Begin(yoyo, authoritative);
            sinceVine = 0;
            hasLast = false;
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            base.Tick(yoyo);
            sinceVine++;
            if (!yoyo.IsOwner || !grounded || sinceVine < Tune.VineIntervalTicks)
                return;

            // The point on the surface under the yoyo (the floor, or the ceiling when gravity is flipped).
            int gravDir = yoyo.GravDir;
            Vector2 surface = new Vector2(yoyo.Projectile.Center.X, gravDir > 0 ? yoyo.Projectile.Bottom.Y : yoyo.Projectile.Top.Y);
            if (hasLast && Vector2.DistanceSquared(surface, lastVine) < Tune.VineMinSpacingPx * Tune.VineMinSpacingPx)
                return;

            sinceVine = 0;
            lastVine = surface;
            hasLast = true;
            AmazonBarbedVineProjectile.Plant(yoyo.Projectile, surface, gravDir, Tune);
        }
    }
}