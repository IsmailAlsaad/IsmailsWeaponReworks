using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Meatball
{
    /// <summary>
    /// The blood metaballs of the Meatball (the same shared tool the Sunfury's fire and the Volcano's lava use, Common/MetaballLayer): droplets
    /// and big slow globs that merge into one blob, thrown out and braked by drag, falling like blood, shrinking and darkening. Used by the
    /// tear-off, the splat, the heartbeat and the explosion. DrawInProjectile: drawn from the flail's and the ball's PreDraw
    /// (MeatballFx.DrawLayers), in front of enemies. Every number reads MeatballTuning live.
    /// </summary>
    public class MeatballBloodStyle : MetaballStyle
    {
        static MeatballTuning Tune => MeatballTuning.Instance;

        public override float Grid => Tune.PixelSize;
        public override float Merge => Tune.BloodMerge;
        public override float OutlineWidth => 0f;
        public override int Bands => 5;
        public override float GlowStrength => Tune.BloodGlow;
        public override int MaxParticles => 600;
        public override bool DrawInProjectile => true;
        public override float Rise => -Tune.BloodGravity;   // negative = it sinks
        public override float Drag => Tune.BloodDrag;

        public override float SizeOverLife(float life) => 1f - life * life;
        public override float HeatOverLife(float life) => Tune.BloodHeat * (1f - 0.6f * life);

        public override Color GetFill(float heat) => Tune.BloodFill(heat);
        public override Color GetOutline() => new Color(0, 0, 0, 0);
        public override Color GetGlow() => Tune.GlowColor(2);
    }
}