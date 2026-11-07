using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The Night's Edge's look for the shared metaball tool (Common/MetaballLayer), a copy of the Dark Lance's look with its own tuning so
    /// the two weapons can be adjusted apart: every number reads NightsEdgeTuning live. Used for the free flames that leave the blade
    /// (a shared layer drawn behind, like the Dark Lance embers).
    /// </summary>
    public class NightsEdgeFlameStyle : MetaballStyle
    {
        public override float Grid => NightsEdgeTuning.FlamePixelSize;
        public override float Merge => NightsEdgeTuning.FlameMerge;
        public override float OutlineWidth => NightsEdgeTuning.FlameOutlineWidth;
        public override int Bands => NightsEdgeTuning.FlameBands;
        public override float GlowStrength => NightsEdgeTuning.FlameGlow;
        public override int MaxParticles => NightsEdgeTuning.FlameMaxParticles;
        public override float Rise => NightsEdgeTuning.FlameRise;
        public override float Drag => NightsEdgeTuning.FlameDrag;
        public override float Wobble => NightsEdgeTuning.FlameWobble;

        // a flame starts hot and cools to nothing
        public override float HeatOverLife(float life) => NightsEdgeTuning.FlameHeat * (1f - life);

        public override Color GetFill(float heat)
        {
            return heat < 0.5f
                ? Color.Lerp(NightsEdgeTuning.FlameColdColor, NightsEdgeTuning.FlameMidColor, heat * 2f)
                : Color.Lerp(NightsEdgeTuning.FlameMidColor, NightsEdgeTuning.FlameHotColor, (heat - 0.5f) * 2f);
        }

        public override Color GetOutline() => NightsEdgeTuning.FlameOutlineColor;

        public override Color GetGlow() => NightsEdgeTuning.FlameMidColor;
    }

    /// <summary>
    /// The same look for the layer that runs along the blade edge. It is drawn by the swing projectile's PreDraw (DrawInProjectile), after
    /// the sword, so the flames are in FRONT of the sprite; MetaballLayers.Of&lt;this&gt;(slot) gives every player a private layer, so each
    /// sword has its own clip (MetaballLayer.CellFilter).
    /// </summary>
    public class NightsEdgeEdgeFlameStyle : NightsEdgeFlameStyle
    {
        public override bool DrawInProjectile => true;
    }

    /// <summary>
    /// What a charged Night's Edge hands to the metaball layers every tick: a row of blobs along the blade edge (clipped to Ismail's edge
    /// mask by the layer's cell filter, with a heat wave running up the blade) and free flames that start at random pixels of the edge, take
    /// part of the blade's motion and rise. Visual only: nothing is synced, every machine does it for the swords it sees. Client only.
    /// </summary>
    public static class NightsEdgeFlames
    {
        // Ismail's mask: the same shape as the sword sprite, the edge in opaque pixels.
        public static string MaskPath => "IsmailsWeaponReworks/Content/Weapons/NightsEdge/NightsEdgeEdgeMask";

        // 0 until the last FlameGrowSteps steps before full charge, then rising to 1 at full charge. 'chargeShown' is the glow's 0..1.
        public static float Strength(float chargeShown, int maxLevel)
        {
            float steps = Math.Max(1f, Math.Min(maxLevel, NightsEdgeTuning.FlameGrowSteps));
            return MathHelper.Clamp((chargeShown * maxLevel - (maxLevel - steps)) / steps, 0f, 1f);
        }

        // 'now' and 'before' are where the sword sprite is this tick and was on the previous tick; 'strength' is Strength().
        public static void Update(MetaballLayer edge, MaskSampler mask, SpriteTransform now, SpriteTransform before, float strength)
        {
            if (Main.dedServ || strength <= 0.01f)
                return;

            float time = Main.GameUpdateCount;

            // The blobs along the edge. Their heat runs up the blade as a wave, so the posterized colors travel along the edge.
            Vector2[] anchors = mask.Anchors(Math.Max(2, NightsEdgeTuning.EdgeAnchors));
            for (int i = 0; i < anchors.Length; i++)
            {
                float along = anchors.Length > 1 ? i / (float)(anchors.Length - 1) : 0.5f;
                Vector2 point = now.TexelToWorld(mask.ToSprite(anchors[i], now.Size));
                point += new Vector2((float)Math.Sin(time * 0.23f + i * 1.9f), (float)Math.Cos(time * 0.19f + i * 2.3f)) * (NightsEdgeTuning.EdgeJitter * now.Scale);
                float wave = (float)Math.Sin(time * NightsEdgeTuning.EdgeWaveSpeed - along * MathHelper.TwoPi * NightsEdgeTuning.EdgeWaveLength);
                float heat = MathHelper.Clamp(NightsEdgeTuning.EdgeHeatBase + NightsEdgeTuning.EdgeHeatWave * wave, 0f, 1f);
                float radius = NightsEdgeTuning.EdgeRadius * now.Scale * strength * (1f + NightsEdgeTuning.EdgeRadiusPulse * (float)Math.Sin(time * 0.31f + i * 1.3f));
                edge.AddBlob(point, radius, heat);
            }

            // The free flames: they leave the edge, carry part of the blade's motion and rise.
            if (mask.OpaqueTexels.Length == 0)
                return;
            float expected = NightsEdgeTuning.FlameSpawnChance * strength;
            int count = (int)expected + (Main.rand.NextFloat() < expected - (int)expected ? 1 : 0);
            MetaballLayer free = MetaballLayers.Of<NightsEdgeFlameStyle>();
            for (int i = 0; i < count; i++)
            {
                Vector2 spriteTexel = mask.ToSprite(mask.OpaqueTexels[Main.rand.Next(mask.OpaqueTexels.Length)], now.Size);
                Vector2 point = now.TexelToWorld(spriteTexel);
                Vector2 carried = (point - before.TexelToWorld(spriteTexel)) * NightsEdgeTuning.FlameInherit;
                float cap = Math.Max(0.1f, NightsEdgeTuning.FlameInheritMax);
                if (carried.Length() > cap)
                    carried = Vector2.Normalize(carried) * cap;
                Vector2 velocity = carried + Main.rand.NextVector2Circular(1f, 1f) * NightsEdgeTuning.FlameKick;
                int life = Math.Max(6, (int)(NightsEdgeTuning.FlameLifeTicks * Main.rand.NextFloat(0.7f, 1.3f)));
                float size = NightsEdgeTuning.FlameSize * now.Scale * Main.rand.NextFloat(0.6f, 1.3f) * (0.5f + 0.5f * strength);
                free.Spawn(point, velocity, life, size);
            }
        }
    }

    /// <summary>
    /// The flames of ONE held sword (the swing's or the stance's): remembers where its sprite is and was, keeps this player's private edge
    /// layer clipped to the mask and feeds NightsEdgeFlames.Update every tick. The owning projectile creates it on first use (a field
    /// initializer would be shared by every copy of the projectile) and draws the edge layer in its PreDraw. Client only.
    /// </summary>
    public class NightsEdgeFlameRig
    {
        SpriteTransform sprite;
        SpriteTransform before;
        bool has;

        // Where the sword sprite is drawn right now, the way the projectiles draw it (no glow scale).
        public static SpriteTransform Transform(Projectile projectile, Texture2D texture, bool mirrored)
        {
            HeldSwordPose.Sprite(texture, projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects, mirrored);
            return new SpriteTransform
            {
                Position = projectile.Center + HeldSwordPose.GripShift(projectile.rotation, NightsEdgeTuning.GripOffset, projectile.scale),
                Rotation = projectile.rotation + rotationOffset,
                Origin = origin,
                Scale = projectile.scale,
                Effects = effects,
                Size = texture.Size()
            };
        }

        // strength 0..1 (NightsEdgeFlames.Strength for a swing). Does nothing until the sprite is loaded.
        public void Tick(Projectile projectile, bool mirrored, float strength, Vector2 tip)
        {
            if (Main.dedServ)
                return;
            Asset<Texture2D> art = TextureAssets.Projectile[projectile.type];
            if (!art.IsLoaded)
                return;

            Texture2D texture = art.Value;
            SpriteTransform now = Transform(projectile, texture, mirrored);
            before = has ? sprite : now;
            sprite = now;
            has = true;

            MaskSampler mask = MaskSampler.Load(NightsEdgeFlames.MaskPath, texture);
            MetaballLayer edge = MetaballLayers.Of<NightsEdgeEdgeFlameStyle>(projectile.owner);
            SpriteTransform clip = sprite;
            edge.CellFilter = cell => mask.Contains(clip, cell);
            NightsEdgeFlames.Update(edge, mask, sprite, before, strength);

            if (strength > 0.02f && NightsEdgeTuning.FlameLight > 0f)
            {
                float light = NightsEdgeTuning.FlameLight * strength;
                Lighting.AddLight(Vector2.Lerp(projectile.Center, tip, 0.6f), 0.55f * light, 0.2f * light, 0.95f * light);
            }
        }
    }
}