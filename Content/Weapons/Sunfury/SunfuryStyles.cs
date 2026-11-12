using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.Sunfury
{
    /// <summary>
    /// The fire metaballs of the Sunfury (the same shared tool the Volcano's lava uses, Common/MetaballLayer): droplets and big slow globs that
    /// merge into one blob, thrown out and braked by drag, shrinking and cooling. Used by the charge-complete ring and the sun explosion.
    /// DrawInProjectile: drawn from the flail's and the blast's PreDraw (SunfuryFx.DrawLayers), in front of enemies. Every number reads SunfuryTuning live.
    /// </summary>
    public class SunfuryBurstStyle : MetaballStyle
    {
        public override float Grid => SunfuryTuning.PixelSize;
        public override float Merge => SunfuryTuning.FireMerge;
        public override float OutlineWidth => 0f;
        public override int Bands => 5;
        public override float GlowStrength => SunfuryTuning.FireGlow;
        public override int MaxParticles => 600;
        public override bool DrawInProjectile => true;
        public override float Rise => -SunfuryTuning.FireGravity;
        public override float Drag => SunfuryTuning.FireDrag;

        public override float SizeOverLife(float life) => 1f - life * life;
        public override float HeatOverLife(float life) => SunfuryTuning.FireHeat * (1f - 0.75f * life);

        public override Color GetFill(float heat) => SunfuryTuning.FireFill(heat);
        public override Color GetOutline() => SunfuryTuning.FireOutline;
        public override Color GetGlow() => SunfuryTuning.GlowColor(2);
    }
}
