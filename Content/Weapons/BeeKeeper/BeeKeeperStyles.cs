using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
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

    /// <summary>
    /// The honey of the Bee Keeper for the shared metaball tool (Common/MetaballLayer): the drips that fall off an enemy the bees are working
    /// on, and the blob a returning bee carries (a one-tick body, so it follows the bee exactly). Drips sink, and are gone when they touch a
    /// tile or water. Every number reads BeeKeeperTuning live.
    /// </summary>
    public class BeeKeeperHoneyStyle : MetaballStyle
    {
        public override float Grid => BeeKeeperTuning.HoneyPixelSize;
        public override float Merge => BeeKeeperTuning.HoneyMerge;
        public override float OutlineWidth => BeeKeeperTuning.HoneyBlobOutline;
        public override int Bands => 4;
        public override float GlowStrength => BeeKeeperTuning.HoneyGlow;
        public override int MaxParticles => 200;
        public override float Rise => -BeeKeeperTuning.HoneyDripGravity;
        public override float Drag => 0.985f;

        public override float SizeOverLife(float life) => 1f - life * life * life;
        public override float HeatOverLife(float life) => BeeKeeperTuning.HoneyDripHeat * (1f - 0.5f * life);

        public override Color GetFill(float heat) => BeeKeeperTuning.HoneyFill(heat);
        public override Color GetOutline() => BeeKeeperTuning.HoneyDripOutlineColor;
        public override Color GetGlow() => BeeKeeperTuning.HoneyGlowColor;

        public override void Step(MetaballParticle particle)
        {
            base.Step(particle);
            Vector2 corner = particle.Position - new Vector2(1f, 1f);
            if (Collision.SolidCollision(corner, 2, 2) || Collision.WetCollision(corner, 2, 2))
                particle.Age = particle.Life; // lands: gone next tick
        }
    }
}