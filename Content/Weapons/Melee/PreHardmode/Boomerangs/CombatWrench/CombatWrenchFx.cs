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

        // W5: while the wrench shakes after the hit, a tiny chip now and then jumps off its head ('strength' is the shake's fading envelope, 0..1).
        public static void Rattle(Vector2 position, float strength)
        {
            if (Main.dedServ || Main.rand.NextFloat() >= CombatWrenchTuning.RattleSparkChance * strength)
                return;
            Chip(position, Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(0.6f, 1.1f));
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

        // The reel begins. A movable enemy gets Ismail's WrenchReel clip: it plays while the enemy is pulled in, follows the enemy, and the
        // caller fades it out (FadeOut on the returned handle) when the wrench is picked up. An immovable enemy (or a missing clip) keeps the
        // vanilla placeholder and returns null.
        public static ReelSound Reel(Vector2 position, bool movable)
        {
            if (Main.dedServ)
                return null;
            string path = "IsmailsWeaponReworks/Assets/Sounds/WrenchReel";
            if (!movable || !Terraria.ModLoader.ModContent.HasAsset(path))
            {
                SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.5f, Pitch = -0.4f, PitchVariance = 0.05f }, position);
                return null;
            }
            ReelSound handle = new ReelSound(position);
            SoundEngine.PlaySound(new SoundStyle(path) { Volume = CombatWrenchTuning.ReelSoundVolume, PitchVariance = CombatWrenchTuning.ReelSoundPitchVariance, MaxInstances = 4 }, position, handle.Update);
            return handle;
        }

        /// <summary>The playing reel clip. The wrench moves Position with the enemy and calls FadeOut; the sound fades itself and stops, even if the wrench is gone by then.</summary>
        public sealed class ReelSound
        {
            public Vector2 Position;
            int fadeTotal;
            int fadeLeft = -1;   // -1 = not fading

            public ReelSound(Vector2 position)
            {
                Position = position;
            }

            public void FadeOut()
            {
                if (fadeLeft >= 0)
                    return;
                fadeTotal = (int)MathHelper.Max(1f, CombatWrenchTuning.ReelSoundFadeTicks);
                fadeLeft = fadeTotal;
            }

            // The sound engine calls this every tick; false ends the sound.
            public bool Update(ActiveSound sound)
            {
                sound.Position = Position;
                if (fadeLeft < 0)
                    return true;
                fadeLeft--;
                if (fadeLeft <= 0)
                    return false;
                sound.Volume = fadeLeft / (float)fadeTotal;
                return true;
            }
        }

        // The strike begins: a heavy whoosh ('direction' is where the hammer is going).
        public static void Swing(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.55f, PitchVariance = 0.05f }, position);
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.45f, Pitch = -0.7f }, position);
        }

        // The heavy hit on the reeled-in enemy: a thump, a camera punch, chips and smoke thrown the way the enemy is flung ('direction' points away
        // from the player). Ismail's WrenchHeavyHit clip plays when the file exists; vanilla stand-ins until then.
        public static void HeavyHit(Vector2 position, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            direction = direction.LengthSquared() < 0.01f ? Vector2.UnitX : Vector2.Normalize(direction);
            Vector2 along = new Vector2(-direction.Y, direction.X);

            string path = $"IsmailsWeaponReworks/Assets/Sounds/MetalBonk/MetalBonk{Main.rand.Next(1,3)}";
            if (Terraria.ModLoader.ModContent.HasAsset(path))
                SoundEngine.PlaySound(new SoundStyle(path) { Volume = CombatWrenchTuning.HitSoundVolume , PitchVariance = 0.2f}, position);
            else
            {
                SoundEngine.PlaySound(SoundID.Tink with { Volume = 1f, Pitch = -0.6f, PitchVariance = 0.05f }, position);
                SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.8f, Pitch = -0.3f }, position);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.35f, Pitch = -0.5f }, position);
            }

            CameraShake.Punch(position, CombatWrenchTuning.HitShake, 14, "CombatWrenchHit");
            for (int i = 0; i < CombatWrenchTuning.HitSparkCount; i++)
                Chip(position, direction * Main.rand.NextFloat(2f, 9f) + along * Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(1f, 2f));
            for (int i = 0; i < CombatWrenchTuning.HitSmokeCount; i++)
            {
                Dust smoke = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(6f, 6f), DustID.Smoke, direction * Main.rand.NextFloat(0.5f, 3f) + Main.rand.NextVector2Circular(1.5f, 1.5f), 120, default, Main.rand.NextFloat(1f, 1.6f));
                smoke.noGravity = true;
            }
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