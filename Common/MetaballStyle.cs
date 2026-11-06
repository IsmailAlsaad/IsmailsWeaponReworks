using Microsoft.Xna.Framework;
using System;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>One round particle of a MetaballLayer. Everything is plain data; the layer's style moves and shrinks it.</summary>
    public class MetaballParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public int Age;
        public int Life;
        public float Size;     // radius in px at birth
        public float Wobble;   // random phase for the sideways wander
    }

    /// <summary>
    /// How a MetaballLayer looks and behaves. A weapon subclasses this once (see the Dark Lance's DarkLanceBlobStyle) and every number reads
    /// the weapon's Tuning live, so hot reload works. The tool: round things (particles and one-frame blobs) each add a smooth round field
    /// to the world pixel grid, the fields of overlapping things add up, and every grid cell above a threshold is a filled square. So
    /// things that come close grow a bridge and become ONE blob with ONE outline, and the colors blend by temperature ('heat', 0 = cold
    /// to 1 = hot). Pixels, hard color bands, no shader. Use it whenever an effect should be "cool custom particles that merge".
    /// </summary>
    public abstract class MetaballStyle
    {
        public virtual float Grid => 2f;            // world px per pixel (match the weapon's other pixel art)
        public virtual float Merge => 2.2f;         // how far a thing's field reaches, in its radii: bigger = joins from further away, bridges more
        public virtual float OutlineWidth => 0.2f;  // thickness of the merged outline (field units: 0.1 thin, 0.4 thick)
        public virtual int Bands => 5;              // hard color bands of the fill
        public virtual float GlowStrength => 0.3f;  // faint additive glow around the blobs (0 = none)
        public virtual int MaxParticles => 400;     // the oldest are dropped first
        public virtual bool DrawInProjectile => false; // true: MetaballLayers does not draw this layer after the tiles; a held weapon draws it from its projectile's PreDraw (MetaballLayer.DrawInBatch), so it is in FRONT of enemies

        // movement of particles (px per tick)
        public virtual float Rise => 0f;            // upward push per tick (negative = sinks)
        public virtual float Drag => 0.97f;         // velocity kept per tick
        public virtual float Wobble => 0f;          // sideways wander per tick

        // 'life' runs from 0 (born) to 1 (gone)
        public virtual float SizeOverLife(float life) => 1f - life * life;
        public virtual float HeatOverLife(float life) => 1f - life;

        public abstract Color GetFill(float heat);   // heat 0..1, already posterized into bands
        public abstract Color GetOutline();
        public virtual Color GetGlow() => GetFill(0.5f);

        /// <summary>One tick of motion for one particle. Override for something else than rise, drag and wobble (gravity wells, swirls...).</summary>
        public virtual void Step(MetaballParticle particle)
        {
            particle.Velocity.Y -= Rise;
            particle.Velocity.X += (float)Math.Sin(particle.Age * 0.25f + particle.Wobble) * Wobble;
            particle.Velocity *= Drag;
            particle.Position += particle.Velocity;
        }
    }
}