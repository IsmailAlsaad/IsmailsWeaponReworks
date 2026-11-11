using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using IsmailsWeaponReworks.Common;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The dust and sounds of the Artery (vanilla only): blood chips on a hit, the lasso catching an enemy and the loop closing.
    /// The defaults are in YoyoFx. Every machine calls these for the same event; all of it is visual, nothing here is synced.
    /// </summary>
    public class ArteryFx : YoyoFx
    {
        protected override int ChipDust => DustID.Blood;

        // Set by the trick when a loop starts (ArteryTuning.ExplosionRadiusPx and ExplosionShake), because the close plays on every machine
        // and the fx has no tuning of its own.
        public float ExplosionRadius = 90f;
        public float ExplosionShake = 5f;

        // One shred hit: a few drops fly off the noose and off the enemy (the hit's own sound comes with the strike).
        public virtual void LoopShred(Vector2 center, float noose)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 5; i++)
            {
                Vector2 direction = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                Chip(center + direction * noose * Main.rand.NextFloat(0.5f, 1f), direction * Main.rand.NextFloat(1.2f, 3.2f), Main.rand.NextFloat(0.8f, 1.3f));
            }
        }

        // The blood explosion: a ring of blood that flies out to the radius, a heavy burst in the middle, a thump and a camera punch.
        protected virtual void Explode(Vector2 position)
        {
            float radius = MathHelper.Max(16f, ExplosionRadius);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.2f, PitchVariance = 0.1f }, position);
            int ring = 36;
            for (int i = 0; i < ring; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / ring + Main.rand.NextFloat(-0.08f, 0.08f)).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(position + direction * radius * 0.15f, ChipDust, direction * radius * Main.rand.NextFloat(0.07f, 0.1f), 0, default, 1.6f);
                dust.noGravity = true;
            }
            for (int i = 0; i < 30; i++)
                Chip(position, Main.rand.NextVector2Circular(radius * 0.12f, radius * 0.12f), Main.rand.NextFloat(1f, 1.9f));
            if (ExplosionShake > 0f)
                CameraShake.Punch(position, ExplosionShake, 16, "ArteryExplosion");
        }

        public override void Throw(Vector2 position, int side)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.3f, PitchVariance = 0.1f }, position);
        }

        // The string touched an enemy and the yoyo starts to swing around it: a sharp whip and a few drops.
        public virtual void LoopCatch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = 0.75f, PitchVariance = 0.05f }, position);
            for (int i = 0; i < 6; i++)
                Chip(position, Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(0.8f, 1.2f));
        }

        // The loop closed on its enemy (patch A2 adds the blood explosion on top of this): a wet snap and a burst of drops.
        public virtual void LoopClose(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.6f, Pitch = -0.2f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < 14; i++)
                Chip(position, Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(0.9f, 1.4f));
            Explode(position);
        }
    }
}