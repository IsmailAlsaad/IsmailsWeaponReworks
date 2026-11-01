using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Flamerang
{
    /// <summary>
    /// The look and the effect of the molten boomerang's trail: a pixel line that cools from white-yellow through orange to dark red
    /// and shrinks as it ages. Because it is a PixelTrail it is handed to DetachedTrails when the boomerang dies, so it keeps fading,
    /// and keeps setting enemies on fire (weaker as it fades), after the boomerang has been caught.
    /// </summary>
    public class MoltenTrailStyle : PixelTrailStyle
    {
        public override float Grid => FlamerangTuning.PixelSize;
        public override int LifeTicks => FlamerangTuning.TrailLifeTicks;
        public override int Layers => 2;
        public override float Jitter => FlamerangTuning.TrailJitter;

        public override float GetWidth(int layer, float age)
        {
            float width = FlamerangTuning.TrailWidth * (1f - 0.8f * age);
            return layer == 0 ? width : width * 0.5f;
        }

        public override Color GetColor(int layer, float age)
        {
            if (layer == 0)
                return Color.Lerp(FlamerangTuning.FireColor, FlamerangTuning.EmberColor, age) * (0.55f * (1f - age));
            return Color.Lerp(FlamerangTuning.HotColor, FlamerangTuning.FireColor, age) * (0.8f * (1f - age));
        }

        public override void Update(PixelTrail trail)
        {
            if (trail.Count > 0)
                FlamerangFx.TrailEmber(trail.PointAt(Main.rand.Next(trail.Count))); // visual: every machine
            if (trail.IsOwner && trail.Clock % System.Math.Max(1, FlamerangTuning.BurnCheckInterval) == 0)
                BurnAlong(trail);
        }

        // Owner only: every enemy touching a point of the trail is set on fire. Older points reach less far, so the burn fades with the trail.
        static void BurnAlong(PixelTrail trail)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.CanBeChasedBy())
                    continue;

                bool touching = false;
                for (int p = trail.Count - 1; p >= 0 && !touching; p -= 2)
                {
                    int reach = (int)(FlamerangTuning.TrailRadius * (1f - FlamerangTuning.TrailBurnFade * trail.AgeAt(p)));
                    Rectangle box = npc.Hitbox;
                    box.Inflate(reach, reach);
                    touching = box.Contains(trail.PointAt(p).ToPoint());
                }
                if (touching)
                    FlamerangBurn.Apply(npc); // vanilla syncs this to the server unless quiet
            }
        }

        public static PixelTrail Create(bool isOwner) => new PixelTrail(new MoltenTrailStyle(), isOwner);
    }
}