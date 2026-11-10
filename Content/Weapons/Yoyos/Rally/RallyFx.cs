using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Rally
{
    /// <summary>
    /// The dust and sounds of the Rally (vanilla only): the cracks, the break of the shell and the shards. The defaults are in YoyoFx.
    /// Every machine calls these for the same event; all of it is visual, nothing here is synced.
    /// </summary>
    public class RallyFx : YoyoFx
    {
        protected override int ChipDust => DustID.Stone;

        public override void Throw(Vector2 position, int side)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.4f, PitchVariance = 0.1f }, position);
        }

        // A hit cracked the yoyo (stage 1 or 2): a sharp tick that gets higher and a few chips, more with every stage.
        public virtual void Crack(Vector2 position, int stage)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.6f, Pitch = 0.1f + 0.25f * stage, PitchVariance = 0.05f }, position);
            int count = 4 + 3 * stage;
            for (int i = 0; i < count; i++)
                Chip(position, Main.rand.NextVector2Circular(2.5f + stage, 2.5f + stage), Main.rand.NextFloat(0.7f, 1.1f));
        }

        // The shell breaks: a crunch, a burst of chips and a few bright sparks.
        public virtual void Break(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.8f, Pitch = -0.1f, PitchVariance = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.7f, Pitch = 0.7f }, position);
            for (int i = 0; i < 24; i++)
                Chip(position, Main.rand.NextVector2Circular(5.5f, 5.5f), Main.rand.NextFloat(0.9f, 1.5f));
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(position, DustID.WhiteTorch, Main.rand.NextVector2Circular(4f, 4f), 0, default, Main.rand.NextFloat(1f, 1.4f));
                dust.noGravity = true;
            }
        }

        // A shard flies fast: a few chips behind it.
        public virtual void ShardTrail(Vector2 position, Vector2 velocity)
        {
            if (Main.dedServ)
                return;
            Chip(position, velocity * -0.1f + Main.rand.NextVector2Circular(0.8f, 0.8f), Main.rand.NextFloat(0.6f, 0.9f));
        }

        // A shard bounces off a tile: a soft tick and two chips.
        public virtual void ShardBounce(Vector2 position, float impact)
        {
            if (Main.dedServ)
                return;
            float strength = MathHelper.Clamp(impact / 8f, 0.3f, 1f);
            SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.25f * strength + 0.1f, Pitch = 0.6f, PitchVariance = 0.15f }, position);
            for (int i = 0; i < 2; i++)
                Chip(position, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -Main.rand.NextFloat(0.5f, 1.5f)), Main.rand.NextFloat(0.6f, 0.9f));
        }

        // A shard hits an enemy.
        public virtual void ShardHit(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.35f, Pitch = 0.5f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < 5; i++)
                Chip(position, Main.rand.NextVector2Circular(3f, 3f), Main.rand.NextFloat(0.7f, 1f));
        }

        // A shard is used up (its pierces are gone, or its time ran out): it crumbles.
        public virtual void ShardBreak(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.35f, Pitch = 0.5f, PitchVariance = 0.15f }, position);
            for (int i = 0; i < 7; i++)
                Chip(position, Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(0.7f, 1.1f));
        }
    }
}