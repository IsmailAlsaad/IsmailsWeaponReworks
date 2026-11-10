using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// The dust and sounds of the Amazon (vanilla only): the barbs, their release and the latched vines. The defaults are in YoyoFx.
    /// Every machine calls these for the same event; all of it is visual, nothing here is synced.
    /// </summary>
    public class AmazonFx : YoyoFx
    {
        protected override int ChipDust => DustID.JungleGrass;

        public override void Throw(Vector2 position, int side)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.3f, PitchVariance = 0.1f }, position);
        }

        void Leaf(Vector2 position, Vector2 velocity, float scale, int type)
        {
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 0, default, scale);
            dust.noGravity = true;
        }

        // A hit made the yoyo more barbed (stage 1 or 2): a prickly rustle that gets higher and a few leaves, more with every stage.
        public virtual void Barb(Vector2 position, int stage)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.45f, Pitch = 0.1f + 0.25f * stage, PitchVariance = 0.05f }, position);
            int count = 4 + 3 * stage;
            for (int i = 0; i < count; i++)
                Leaf(position, Main.rand.NextVector2Circular(2.5f + stage, 2.5f + stage), Main.rand.NextFloat(0.8f, 1.2f), DustID.Grass);
        }

        // The barbs are released onto the enemy: a lashing sound and a burst of leaves.
        public virtual void Release(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.8f, Pitch = -0.2f, PitchVariance = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.5f, Pitch = 0.2f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < 18; i++)
                Leaf(position, Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(0.9f, 1.4f), DustID.Grass);
            for (int i = 0; i < 8; i++)
                Leaf(position, Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(1f, 1.3f), DustID.GreenTorch);
        }

        // A latched vine pricks (every damage tick, on every machine): a couple of leaves.
        public virtual void LatchTick(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 2; i++)
                Leaf(position + Main.rand.NextVector2Circular(14f, 14f), Main.rand.NextVector2Circular(1.2f, 1.2f), Main.rand.NextFloat(0.8f, 1.2f), DustID.Grass);
        }

        // A damage tick that really hit (owner only).
        public virtual void LatchHit(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.25f, Pitch = 0.4f }, position);
            Leaf(position, Main.rand.NextVector2Circular(2f, 2f), 1.1f, DustID.GreenTorch);
        }

        // The vines unwind and are gone.
        public virtual void Unwind(Vector2 center, int width, int height)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Leaf(center + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f) * width, Main.rand.NextFloat(-0.5f, 0.5f) * height), Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextFloat(0.9f, 1.4f), DustID.Grass);
        }
    }
}