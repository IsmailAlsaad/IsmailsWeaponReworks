using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// The lava blade and the flames that rise off it, for the shared metaball tool (Common/MetaballLayer). DrawInProjectile: the layer is
    /// drawn from the held blade's PreDraw, so the blade is in front of enemies instead of behind them. Every number reads VolcanoTuning live.
    /// </summary>
    public class VolcanoBlobStyle : MetaballStyle
    {
        public override float Grid => VolcanoTuning.PixelSize;
        public override float Merge => VolcanoTuning.BlobMerge;
        public override float OutlineWidth => VolcanoTuning.BlobOutline;
        public override int Bands => 5;
        public override float GlowStrength => VolcanoTuning.BlobGlow;
        public override int MaxParticles => 500;
        public override bool DrawInProjectile => true;
        public override float Rise => VolcanoTuning.FlameRise;
        public override float Drag => VolcanoTuning.FlameDrag;
        public override float Wobble => VolcanoTuning.FlameWobble;

        public override float SizeOverLife(float life) => 1f - life * life;
        public override float HeatOverLife(float life) => VolcanoTuning.FlameHeat * (1f - life);

        public override Color GetFill(float heat) => VolcanoTuning.LavaFill(heat);
        public override Color GetOutline() => VolcanoTuning.OutlineColor;
        public override Color GetGlow() => VolcanoTuning.GlowColor;
    }

    /// <summary>Lava that has left the blade: drips and spits. Heavy (they fall), they cool slowly and are gone when they touch a tile or water.</summary>
    public class VolcanoDripStyle : MetaballStyle
    {
        public override float Grid => VolcanoTuning.PixelSize;
        public override float Merge => VolcanoTuning.DripMerge;
        public override float OutlineWidth => VolcanoTuning.BlobOutline;
        public override int Bands => 5;
        public override float GlowStrength => VolcanoTuning.BlobGlow;
        public override int MaxParticles => 300;
        public override bool DrawInProjectile => true;   // drawn by VolcanoFx.DrawLayers, after the swing trail, so the trail is always behind it
        public override float Rise => -VolcanoTuning.DripGravity;
        public override float Drag => 0.985f;

        public override float SizeOverLife(float life) => 1f - life * life * life;
        public override float HeatOverLife(float life) => VolcanoTuning.DripHeat * (1f - 0.6f * life);

        public override Color GetFill(float heat) => VolcanoTuning.LavaFill(heat);
        public override Color GetOutline() => VolcanoTuning.OutlineColor;
        public override Color GetGlow() => VolcanoTuning.GlowColor;

        public override void Step(MetaballParticle particle)
        {
            base.Step(particle);
            Vector2 corner = particle.Position - new Vector2(1f, 1f);
            if (Collision.SolidCollision(corner, 2, 2) || Collision.WetCollision(corner, 2, 2))
                particle.Age = particle.Life; // lands: gone next tick
        }
    }

    /// <summary>
    /// The droplets of a lava explosion (the charged swing's blast, the eruption's shockwave): thrown out hard and braked quickly by drag, so
    /// they stop a short way out, with a little gravity; the big slow ones in the middle merge into one glob that shrinks away. Same look and
    /// same landing rule as the drips; only the movement and the merging differ.
    /// </summary>
    public class VolcanoBurstStyle : VolcanoDripStyle
    {
        public override float Merge => VolcanoTuning.BurstMerge;
        public override int MaxParticles => 400;
        public override float Rise => -VolcanoTuning.BurstGravity;
        public override float Drag => VolcanoTuning.BurstDrag;

        public override float SizeOverLife(float life) => 1f - life * life;
        public override float HeatOverLife(float life) => VolcanoTuning.BurstHeat * (1f - 0.75f * life);
    }

    /// <summary>
    /// The trail a Volcano swing leaves (a SweepTrail): a fan that starts as a thin line at the tip and widens down the blade; white-yellow
    /// on the tip edge, orange in the middle, deep red toward the hand. 'Heat' (set by the swing every tick) makes it brighter.
    /// </summary>
    public class VolcanoSweepTrailStyle : SweepTrailStyle
    {
        public float Heat;

        public override float Grid => VolcanoTuning.PixelSize;
        public override int LifeTicks => VolcanoTuning.TrailLifeTicks;
        public override int Rails => VolcanoTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => VolcanoTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * MathHelper.Lerp(VolcanoTuning.TrailOpacityLow, VolcanoTuning.TrailOpacityHigh, Heat);
            Color color = ThreeStop(across, VolcanoTuning.CoreColor, VolcanoTuning.OrangeColor, VolcanoTuning.DeepColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}