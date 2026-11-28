using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Boomerangs.CombatWrench
{
    /// <summary>
    /// All dust and sound for the Combat Wrench (vanilla dust and sounds only; Ismail's own sounds replace them later). Every machine
    /// calls these for the same event; all of it is visual, nothing here is synced.
    /// </summary>
    public static class CombatWrenchFx
    {
        static void Spark(Vector2 position, Vector2 velocity, float scale)
        {
            //Dust dust = Dust.NewDustPerfect(position, DustID.Adamantite, velocity, 80, default, scale);
            //dust.noGravity = true;
        }

        static void Chip(Vector2 position, Vector2 velocity, float scale)
        {
            Dust dust = Dust.NewDustPerfect(position, DustID.Silver, velocity, 0, default, scale);
            dust.noGravity = true;
        }

        // The throw: a swish and a few sparks flying out along the throw.
        public static void Throw(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.05f, PitchVariance = 0.1f }, position);
            Vector2 forward = direction.LengthSquared() > 0.01f ? Vector2.Normalize(direction) : Vector2.UnitX;
            for (int i = 0; i < CombatWrenchTuning.ThrowSparkCount; i++)
                Spark(position, forward * Main.rand.NextFloat(1.5f, 5f) + Main.rand.NextVector2Circular(1.2f, 1.2f), Main.rand.NextFloat(0.8f, 1.2f));
        }

        // Every tick of the flight: a glint and now and then a spark shed behind the wrench.
        public static void Flight(Vector2 position, Vector2 movement)
        {
            if (Main.dedServ)
                return;
            if (Main.rand.NextFloat() < CombatWrenchTuning.FlightSparkChance)
                Spark(position + Main.rand.NextVector2Circular(8f, 8f), -movement * 0.1f + Main.rand.NextVector2Circular(0.8f, 0.8f), Main.rand.NextFloat(0.7f, 1.1f));
            //if (Main.rand.NextBool(3))
            //{
            //    Dust glint = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(6f, 6f), DustID.Adamantite, Vector2.Zero, 100, default, 0.7f);
            //    glint.noGravity = true;
            //}
        }

        // The wrench hits a block: a clang, sparks and chips thrown away from the block ('normal' points away from it).
        public static void Impact(Vector2 position, Vector2 normal)
        {
            if (Main.dedServ)
                return;
            normal = normal.LengthSquared() < 0.01f ? new Vector2(0f, -1f) : Vector2.Normalize(normal);
            Vector2 along = new Vector2(-normal.Y, normal.X);
            SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.6f, PitchVariance = 0.15f }, position);
            for (int i = 0; i < CombatWrenchTuning.ImpactSparkCount; i++)
            {
                Vector2 velocity = along * Main.rand.NextFloat(-3f, 3f) + normal * Main.rand.NextFloat(0.8f, 3.5f);
                //if (i % 3 == 0)
                //    Chip(position, velocity * 0.7f, Main.rand.NextFloat(0.8f, 1.1f));
                //else
                //    Spark(position, velocity, Main.rand.NextFloat(0.8f, 1.3f));
                Chip(position, velocity * Main.rand.NextFloat(0.4f, 1f), Main.rand.NextFloat(0.8f, 1.7f));
            }
        }

        // The head clamps onto an enemy: a heavy clank and a spray of chips ('facing' is where the head points, into the enemy).
        public static void Snap(Vector2 position, Vector2 facing)
        {
            if (Main.dedServ)
                return;
            facing = facing.LengthSquared() < 0.01f ? Vector2.UnitX : Vector2.Normalize(facing);
            Vector2 back = -facing;
            Vector2 along = new Vector2(-facing.Y, facing.X);
            SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.9f, Pitch = -0.35f, PitchVariance = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Tink with { Volume = 0.6f, Pitch = 0.2f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < CombatWrenchTuning.SnapSparkCount; i++)
                Chip(position, back * Main.rand.NextFloat(1f, 4f) + along * Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(0.9f, 1.6f));
        }

        // The snap-back hit on an immovable enemy: a heavy clank, a camera punch and a spray of chips ('facing' is where the head points, into the enemy).
        public static void Strike(Vector2 position, Vector2 facing)
        {
            if (Main.dedServ)
                return;
            facing = facing.LengthSquared() < 0.01f ? Vector2.UnitX : Vector2.Normalize(facing);
            Vector2 back = -facing;
            Vector2 along = new Vector2(-facing.Y, facing.X);
            SoundEngine.PlaySound(SoundID.Tink with { Volume = 1f, Pitch = -0.5f, PitchVariance = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.6f, Pitch = -0.2f }, position);
            CameraShake.Punch(position, CombatWrenchTuning.StrikeShake, 10, "CombatWrenchStrike");
            for (int i = 0; i < CombatWrenchTuning.StrikeSparkCount; i++)
                Chip(position, back * Main.rand.NextFloat(1.5f, 5.5f) + along * Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(1f, 1.9f));
        }

        // The reel begins (the enemy starts sliding in). A placeholder sound until Ismail's reel clip exists (W7).
        public static void Reel(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.5f, Pitch = -0.4f, PitchVariance = 0.05f }, position);
        }

        // The wrench starts back to the hand.
        public static void Retract(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.4f, Pitch = 0.4f, PitchVariance = 0.1f }, position);
        }

        // Caught: a small pop and a few sparks.
        public static void Catch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.5f, Pitch = 0.7f }, position);
            for (int i = 0; i < CombatWrenchTuning.CatchSparkCount; i++)
                Spark(position, Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(0.7f, 1.1f));
        }
    }
}