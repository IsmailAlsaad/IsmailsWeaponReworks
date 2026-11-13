using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The dark flame colors for the shared metaball tool (Common/MetaballLayer): deep violet at heat 0, purple in the middle, pale lilac at
    /// full heat (the same purples as the Dark Lance). Every number of the subclasses reads NightsEdgeTuning live. All three layers are
    /// DrawInProjectile: the held blade draws them from its PreDraw (NightsEdgeFx.DrawLayers), so they sit in front of enemies.
    /// </summary>
    public abstract class NightsEdgeBlobStyle : MetaballStyle
    {
        public override float Grid => NightsEdgeTuning.PixelSize;
        public override int Bands => 5;
        public override bool DrawInProjectile => true;

        static Color Opaque(Color color) => new Color(color.R, color.G, color.B);

        public override Color GetFill(float heat)
        {
            Color deep = Opaque(NightsEdgeTuning.DeepColor);
            Color mid = Opaque(NightsEdgeTuning.PurpleColor);
            Color core = Opaque(NightsEdgeTuning.CoreColor);
            return heat < 0.5f ? Color.Lerp(deep, mid, heat * 2f) : Color.Lerp(mid, core, (heat - 0.5f) * 2f);
        }

        public override Color GetOutline() => Color.Lerp(Opaque(NightsEdgeTuning.DeepColor), Color.Black, 0.45f);
        public override Color GetGlow() => NightsEdgeTuning.PurpleColor;
    }

    /// <summary>The metaballs that run along the blade's cutting edge (fed every tick from the mask PNG): small, tight, they follow the sprite exactly.</summary>
    public class NightsEdgeEdgeStyle : NightsEdgeBlobStyle
    {
        public override float Merge => NightsEdgeTuning.EdgeMerge;
        public override float OutlineWidth => NightsEdgeTuning.EdgeOutline;
        public override float GlowStrength => NightsEdgeTuning.EdgeGlow;
        public override int MaxParticles => 50;
    }

    /// <summary>The dark flames that rise off the blade's edge (particles with a life of their own) and the embers of the explosion.</summary>
    public class NightsEdgeFlameStyle : NightsEdgeBlobStyle
    {
        public override float Merge => NightsEdgeTuning.FlameMerge;
        public override float OutlineWidth => NightsEdgeTuning.FlameOutline;
        public override float GlowStrength => NightsEdgeTuning.FlameGlow;
        public override int MaxParticles => 500;
        public override float Rise => NightsEdgeTuning.FlameRise;
        public override float Drag => NightsEdgeTuning.FlameDrag;
        public override float Wobble => NightsEdgeTuning.FlameWobble;

        public override float SizeOverLife(float life) => 1f - life * life;
        public override float HeatOverLife(float life) => NightsEdgeTuning.FlameHeat * (1f - life);
    }

    /// <summary>
    /// The droplets of the dark flame explosion: thrown out hard and braked quickly by drag, so they stop a short way out, with a little
    /// gravity; the big slow ones in the middle merge into one glob that shrinks away.
    /// </summary>
    public class NightsEdgeBurstStyle : NightsEdgeBlobStyle
    {
        public override float Merge => NightsEdgeTuning.BurstMerge;
        public override float OutlineWidth => NightsEdgeTuning.FlameOutline;
        public override float GlowStrength => NightsEdgeTuning.FlameGlow;
        public override int MaxParticles => 400;
        public override float Rise => -NightsEdgeTuning.BurstGravity;
        public override float Drag => NightsEdgeTuning.BurstDrag;

        public override float SizeOverLife(float life) => 1f - life * life;
        public override float HeatOverLife(float life) => NightsEdgeTuning.BurstHeat * (1f - 0.75f * life);
    }
}