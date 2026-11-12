using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Phaseblade
{
    /// <summary>All dust and sound for the Phaseblades (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class PhasebladeFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position, bool up, bool spin)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = up ? 0.2f : 0f, PitchVariance = 0.1f }, position);
            //if (spin)
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.45f, Pitch = 0.35f, PitchVariance = 0.2f }, position);
        }

        public static void PlayLoop(Vector2 position)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.45f, Pitch = -0.2f, PitchVariance = 0.1f }, position);
        }

        // ---- dust ----

        static void Spark(Vector2 position, Vector2 velocity, float scale, Color color)
        {
            Dust dust = Dust.NewDustPerfect(position, DustID.TintableDustLighted, velocity, 0, new Color(color.R, color.G, color.B), scale);
            dust.noGravity = true;
        }

        // A few sparks along the outer half of the blade while it strikes.
        public static void SlashDust(Vector2 hand, Vector2 tip, Color color)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 3; i++)
                Spark(Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.5f, 1f)), Main.rand.NextVector2Circular(1.2f, 1.2f), 1.1f, color);
        }

        // Extra sparks thrown off the tip of a spinning blade.
        public static void SpinDust(Vector2 tip, Vector2 tipVelocity, Color color)
        {
            if (Main.dedServ)
                return;
            Spark(tip, -tipVelocity * 0.08f + Main.rand.NextVector2Circular(1f, 1f), 1.2f, color);
        }

        public static void PlayPlantLift(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.5f, Pitch = 0.1f, PitchVariance = 0.1f }, position);
        }

        // The sound the tile makes when it is hit (dirt, stone, wood, glass, a modded tile's own, see TileSound), taken from the tile the blade
        // went into; the low blade hum is added on top. A plain clink only when the game's own tile sound cannot be played.
        public static void PlayPlant(Vector2 ground)
        {
            if (Main.dedServ)
                return;
            if (!TileSound.PlayHit((int)(ground.X / 16f), (int)(ground.Y / 16f)))
                SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.9f, Pitch = -0.4f }, ground);
            SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.6f, Pitch = -0.3f }, ground);
        }

        // The blade lands: a fan of sparks thrown up and sideways from the ground, and a short camera punch.
        public static void PlantImpact(Vector2 ground, Color color)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 16; i++)
            {
                float angle = -MathHelper.PiOver2 + Main.rand.NextFloat(-1.3f, 1.3f);
                Spark(ground, angle.ToRotationVector2() * Main.rand.NextFloat(1.5f, 5f), 1.3f, color);
            }
            CameraShake.Punch(ground, PhasebladeTuning.PlantShake, 10);
        }

        // An ember drifting up from the planted blade.
        public static void PlantEmber(Vector2 position, Color color)
        {
            if (Main.dedServ)
                return;
            Spark(position + Main.rand.NextVector2Circular(5f, 3f), new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(0.8f, 2f)), 0.9f, color);
        }

        // The blade tears out of the ground on its way back.
        public static void PullOut(Vector2 position, Color color)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Spark(position + Main.rand.NextVector2Circular(6f, 20f), Main.rand.NextVector2Circular(2.5f, 2.5f) + new Vector2(0f, -1.5f), 1.2f, color);
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.55f, Pitch = 0.25f, PitchVariance = 0.1f }, position);
        }

        // The blade slaps back into the hand.
        public static void Catch(Vector2 position, Color color)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Spark(position, Main.rand.NextVector2Circular(3f, 3f), 1.2f, color);
            SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.5f, Pitch = 0.5f, PitchVariance = 0.1f }, position);
        }

        // The blade goes without being caught (its owner died or went away, or it fell out of the world).
        public static void Retract(Vector2 position, Color color)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Spark(position + Main.rand.NextVector2Circular(6f, 20f), Main.rand.NextVector2Circular(2.5f, 2.5f), 1.2f, color);
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.5f, Pitch = -0.1f, PitchVariance = 0.1f }, position);
        }

        public static void HitSpark(Vector2 position, Color color)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 8; i++)
                Spark(position, Main.rand.NextVector2Circular(3f, 3f), 1.2f, color);
        }
    }
}