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
}