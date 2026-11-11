using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// The look of the honey trail a charged Around the World leaves: a pixel line (Common/PixelLine through PixelTrail) that is thick
    /// at the yoyo and thins to a point at its tail, amber glow outside and a bright golden core inside, with honey dripping off it. Every
    /// number reads the Hive-Five's tuning live. Visual only; the projectile hands the trail to DetachedTrails when it dies.
    /// </summary>
    public class HiveFiveTrailStyle : PixelTrailStyle
    {
        readonly HiveFiveTuning tuning;

        public HiveFiveTrailStyle(HiveFiveTuning tuning)
        {
            this.tuning = tuning;
        }

        public override float Grid => tuning.HoneyTrailGrid;
        public override int LifeTicks => tuning.HoneyTrailLifeTicks;
        public override int Layers => 2;
        public override float Jitter => tuning.HoneyTrailJitter;

        public override float GetWidth(int layer, float age)
        {
            float width = tuning.HoneyTrailWidth * (1f - 0.9f * age);
            return layer == 0 ? width : width * 0.5f;
        }

        public override Color GetColor(int layer, float age)
        {
            if (layer == 0)
                return tuning.HoneyTrailOuterColor * (0.55f * (1f - age));
            return tuning.HoneyTrailCoreColor * (0.85f * (1f - age));
        }

        // A drop of honey falls from a random point of the trail now and then (every machine, visual only).
        public override void Update(PixelTrail trail)
        {
            if (trail.Count > 0 && Main.rand.NextFloat() < tuning.HoneyTrailDripChance)
                Dust.NewDustPerfect(trail.PointAt(Main.rand.Next(trail.Count)), DustID.Honey, Main.rand.NextVector2Circular(0.6f, 0.6f), 100, default, Main.rand.NextFloat(0.7f, 1.0f));
        }
    }
}
