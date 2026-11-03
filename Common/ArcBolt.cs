using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>The look of an ArcBolt: a wide additive glow with a thin bright core on top, both shrinking and fading over LifeTicks.</summary>
    public class ArcBoltStyle : PixelTrailStyle
    {
        readonly Color core;
        readonly Color glow;
        readonly int life;
        readonly float width;

        public ArcBoltStyle(Color core, Color glow, int lifeTicks, float width)
        {
            this.core = core;
            this.glow = glow;
            life = Math.Max(1, lifeTicks);
            this.width = width;
        }

        public override float Grid => 2f;
        public override int LifeTicks => life;
        public override int Layers => 2;

        public override float GetWidth(int layer, float age)
        {
            return (layer == 0 ? width : width * 0.4f) * (1f - age);
        }

        public override Color GetColor(int layer, float age)
        {
            float fade = 1f - age;
            // alpha 0 draws additively (the glow); the core is opaque
            return layer == 0 ? new Color(glow.R, glow.G, glow.B, 0) * (0.8f * fade) : core * fade;
        }
    }

    /// <summary>
    /// A jagged lightning line between two points, drawn with PixelLine through a PixelTrail that is handed to DetachedTrails at once, so
    /// it fades away on its own (nothing for the caller to keep or tick). Purely visual and client only: every machine that wants to
    /// show a bolt calls Spawn for the same event; nothing is synced. 'jag' is how far (in pixels) the bolt may wander off the straight
    /// line; 'forks' adds short side branches.
    /// </summary>
    public static class ArcBolt
    {
        public static void Spawn(Vector2 from, Vector2 to, ArcBoltStyle style, float jag, int forks = 0)
        {
            if (Main.dedServ)
                return;

            Vector2 line = to - from;
            float length = line.Length();
            if (length < 2f)
                return;

            Vector2 direction = line / length;
            Vector2 side = new Vector2(-direction.Y, direction.X);
            int segments = Math.Max(2, (int)(length / 12f));

            PixelTrail trail = new PixelTrail(style, false);
            trail.Add(from);
            for (int i = 1; i < segments; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)segments) + side * Main.rand.NextFloat(-jag, jag);
                trail.Add(point);
                if (forks > 0 && Main.rand.NextBool(3))
                {
                    forks--;
                    Vector2 end = point + (direction.RotatedByRandom(1.1f) * Main.rand.NextFloat(18f, 44f));
                    Spawn(point, end, style, jag * 0.6f);
                }
            }
            trail.Add(to);
            DetachedTrails.Hand(trail);
        }
    }
}