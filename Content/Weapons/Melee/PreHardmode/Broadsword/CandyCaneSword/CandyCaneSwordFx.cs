using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// Every dust and sound of the Candy Cane Sword (vanilla dust and sounds only), so a later juice pass edits one file. Every machine calls these
    /// for the same event; all of it is visual or audio, nothing here is synced.
    /// </summary>
    public static class CandyCaneSwordFx
    {
        // Ismail's sheet: 5 columns x 8 rows of 40 x 40 cells (no extension). Until it exists a colored square is drawn instead.
        // Ismail's sounds: CandyBreak1-3, CandyPickup and CandyExplode (no extension).
        public static string SoundFolder => "IsmailsWeaponReworks/Assets/Sounds/Candy/";

        public static string SheetPath => "IsmailsWeaponReworks/Content/Weapons/Melee/PreHardmode/Broadsword/CandyCaneSword/Candy";

        // ---- sounds ----

        public static void PlaySwing(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = CandyCaneSwordTuning.SwingSoundVolume, Pitch = 0.1f, PitchVariance = 0.1f }, position);
        }

        // ---- dust ----

        // A few sparks in the sword's color (and white) where the blade touched the enemy.
        public static void HitSpark(Vector2 position, CandyKind kind)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < CandyCaneSwordTuning.HitSparkCount; i++)
            {
                int type = Main.rand.NextBool() ? CandyKinds.DustOf(kind) : DustID.WhiteTorch;
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(6f, 6f), type, Main.rand.NextVector2Circular(2.5f, 2.5f), 0, default, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        // 'count' dust of the candy's own color thrown out of 'position'.
        public static void Burst(Vector2 position, CandyKind kind, int count, float speed, float scale)
        {
            if (Main.dedServ)
                return;
            int type = CandyKinds.DustOf(kind);
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(4f, 4f), type, Main.rand.NextVector2Circular(speed, speed), 0, default, scale * Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        // A candy appears.
        public static void Spawn(Vector2 position, CandyKind kind)
        {
            if (Main.dedServ)
                return;
            Burst(position, kind, CandyCaneSwordTuning.SpawnDustCount, 2f, 0.9f);
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = CandyCaneSwordTuning.SpawnSoundVolume, Pitch = 0.7f, PitchVariance = 0.2f, MaxInstances = 2 }, position);
        }

        // A candy runs out of time (or the oldest is pushed out).
        public static void Break(Vector2 position, CandyKind kind)
        {
            if (Main.dedServ)
                return;
            Burst(position, kind, CandyCaneSwordTuning.BreakDustCount, 3f, 1f);
            SoundEngine.PlaySound(new SoundStyle(SoundFolder + "CandyBreak", 1, 3) { Volume = CandyCaneSwordTuning.BreakSoundVolume, PitchVariance = 0.15f, MaxInstances = 3 }, position);
        }

        // A green or yellow candy is picked up (every machine sees the candy vanish).
        public static void Pickup(Vector2 position, CandyKind kind)
        {
            if (Main.dedServ)
                return;
            Burst(position, kind, CandyCaneSwordTuning.PickupDustCount, 3.5f, 1.1f);
            SoundEngine.PlaySound(new SoundStyle(SoundFolder + "CandyPickup") { Volume = CandyCaneSwordTuning.PickupSoundVolume, Pitch = kind == CandyKind.Yellow ? 0.2f : 0f, PitchVariance = 0.1f, MaxInstances = 2 }, position);
        }

        // A red candy goes off.
        public static void RedExplosion(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < CandyCaneSwordTuning.BlastDustCount; i++)
            {
                int type = i % 3 == 0 ? DustID.WhiteTorch : DustID.RedTorch;
                Dust dust = Dust.NewDustPerfect(position, type, Main.rand.NextVector2Circular(7f, 7f), 0, default, Main.rand.NextFloat(1.1f, 1.8f));
                dust.noGravity = true;
            }
            for (int i = 0; i < CandyCaneSwordTuning.BlastSmokeCount; i++)
            {
                Dust smoke = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), DustID.Smoke, Main.rand.NextVector2Circular(2f, 2f), 120, default, Main.rand.NextFloat(0.8f, 1.2f));
                smoke.noGravity = true;
            }
            SoundEngine.PlaySound(new SoundStyle(SoundFolder + "CandyExplode") { Volume = CandyCaneSwordTuning.BlastSoundVolume, PitchVariance = 0.1f, MaxInstances = 3 }, position);
            CameraShake.Punch(position, CandyCaneSwordTuning.RedBlastShake, 10);
        }

        // A blue candy sticks to an enemy or a block.
        public static void BlueStick(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Burst(position, CandyKind.Blue, 6, 2.5f, 1f);
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = CandyCaneSwordTuning.StickSoundVolume, Pitch = -0.2f, PitchVariance = 0.15f, MaxInstances = 3 }, position);
        }

        // A blue candy's tick on the enemy it hangs on.
        public static void BlueTick(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Burst(position, CandyKind.Blue, 3, 1.5f, 0.8f);
        }

        // An enemy dies to the sword: a poof of every candy color where it stood (the owner's machine only; the candies show their own spawn dust everywhere).
        public static void KillBurst(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < CandyCaneSwordTuning.KillBurstDustCount; i++)
            {
                CandyKind kind = (CandyKind)(i % 4); // red, green, blue, yellow
                Dust dust = Dust.NewDustPerfect(position, CandyKinds.DustOf(kind), Main.rand.NextVector2Circular(5f, 5f) + new Vector2(0f, -2f), 0, default, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
            CameraShake.Punch(position, CandyCaneSwordTuning.KillBurstShake, 8);
        }
    }
}