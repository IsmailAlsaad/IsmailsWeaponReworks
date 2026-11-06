using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// The poison gas of a dying bee, for the shared metaball tool (Common/MetaballLayer): round puffs that drift, swell and shrink away and
    /// merge into one sickly green cloud with one outline. Every number reads BeeKeeperTuning live.
    /// </summary>
    public class BeeKeeperCloudStyle : MetaballStyle
    {
        public override float Grid => BeeKeeperTuning.PixelSize;
        public override float Merge => BeeKeeperTuning.CloudMerge;
        public override float OutlineWidth => BeeKeeperTuning.CloudOutline;
        public override int Bands => 4;
        public override float GlowStrength => BeeKeeperTuning.CloudGlow;
        public override int MaxParticles => 500;
        public override float Rise => BeeKeeperTuning.CloudRise;
        public override float Drag => BeeKeeperTuning.CloudDrag;
        public override float Wobble => BeeKeeperTuning.CloudWobble;

        public override float SizeOverLife(float life) => (float)Math.Sqrt(Math.Max(0.0, Math.Sin(MathHelper.Clamp(life, 0f, 1f) * Math.PI)));
        public override float HeatOverLife(float life) => BeeKeeperTuning.CloudHeat * (1f - 0.5f * life);

        public override Color GetFill(float heat) => BeeKeeperTuning.CloudFill(heat);
        public override Color GetOutline() => BeeKeeperTuning.CloudOutlineColor;
        public override Color GetGlow() => BeeKeeperTuning.CloudGlowColor;
    }

    /// <summary>
    /// The trail a Bee Keeper swing leaves (a SweepTrail): a fan that starts as a thin line at the tip and widens down the blade; pale honey
    /// on the tip edge, amber in the middle, deep orange toward the hand, fading from its tail.
    /// </summary>
    public class BeeKeeperSweepTrailStyle : SweepTrailStyle
    {
        public override float Grid => BeeKeeperTuning.PixelSize;
        public override int LifeTicks => BeeKeeperTuning.TrailLifeTicks;
        public override int Rails => BeeKeeperTuning.TrailRails;

        public override SweepFade Fade => SweepFade.TailFirst;
        public override float TailLifeScale => BeeKeeperTuning.TailLifeScale;

        public override Color GetColor(float across, float age)
        {
            float fade = Falloff(age) * BeeKeeperTuning.TrailOpacity;
            Color color = ThreeStop(across, BeeKeeperTuning.CoreColor, BeeKeeperTuning.HoneyColor, BeeKeeperTuning.DeepColor);
            return color * (fade * (1f - 0.6f * across));
        }
    }
}