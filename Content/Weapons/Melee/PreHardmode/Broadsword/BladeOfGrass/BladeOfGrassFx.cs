using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>All dust and sound for the Blade of Grass (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class BladeOfGrassFx
    {
        static Dust Leaf(Vector2 position, Vector2 velocity, float scale, int type)
        {
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 0, default, scale);
            dust.noGravity = true;
            return dust;
        }

        // A leaf or a spark off the shaking blade; stronger charge = more of them.
        public static void ChargeLeaf(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;
            Leaf(position, Main.rand.NextVector2Circular(1.2f, 1.2f) * (0.5f + charge), 0.9f + 0.4f * charge, Main.rand.NextBool(3) ? DustID.JungleGrass : DustID.Grass);
        }

        // A vine locks onto an enemy.
        public static void Lock(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.5f, Pitch = 0.6f }, position);
            for (int i = 0; i < 5; i++)
                Leaf(position, Main.rand.NextVector2Circular(2f, 2f), 1f, DustID.GreenTorch);
        }

        // A section breaks off the vine and sticks to the enemy.
        public static void BreakOff(Vector2 position, float lengthFraction)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.9f, Pitch = -0.2f }, position);
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.6f, Pitch = -0.1f }, position);
            int count = 8 + (int)(lengthFraction * 14f);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(1.5f, 5f);
                Leaf(position + Main.rand.NextVector2Circular(10f, 10f), velocity, Main.rand.NextFloat(1f, 1.6f), Main.rand.NextBool(3) ? DustID.JungleGrass : DustID.Grass);
            }
        }

        // The spiny section pricks the enemy (every damage tick, on every machine).
        public static void SpineTick(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 2; i++)
                Leaf(position + Main.rand.NextVector2Circular(14f, 14f), Main.rand.NextVector2Circular(1.2f, 1.2f), Main.rand.NextFloat(0.8f, 1.2f), DustID.Grass);
        }

        // A damage tick that really hit (owner only).
        public static void SpineHit(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.25f, Pitch = 0.4f }, position);
            Leaf(position, Main.rand.NextVector2Circular(2f, 2f), 1.1f, DustID.GreenTorch);
        }

        // The coil unwinds and is gone.
        public static void Unwind(Vector2 center, int width, int height)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 10; i++)
                Leaf(center + new Vector2(Main.rand.NextFloat(-0.5f, 0.5f) * width, Main.rand.NextFloat(-0.5f, 0.5f) * height), Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextFloat(0.9f, 1.4f), DustID.Grass);
        }

        // The swing of the blade (every machine); the vines make it a little more rustly the longer they are.
        public static void PlaySwing(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.1f + 0.4f * charge, PitchVariance = 0.1f }, position);
        }

        // Leaves off the tip while the blade cuts.
        public static void SwingDust(Vector2 tip, Vector2 motion, float charge)
        {
            if (Main.dedServ)
                return;
            int count = 1 + (int)(charge * 2.5f);
            for (int i = 0; i < count; i++)
                Leaf(tip + Main.rand.NextVector2Circular(5f, 5f), motion * Main.rand.NextFloat(0.1f, 0.3f) + Main.rand.NextVector2Circular(1.2f, 1.2f), Main.rand.NextFloat(0.9f, 1.5f),
                    Main.rand.NextBool(3) ? DustID.JungleGrass : DustID.Grass);
        }

        // A true melee hit: a burst of leaves and a hit sound, bigger with longer vines.
        public static void HitSpark(Vector2 position, float charge)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.4f + 0.15f * charge, Pitch = 0.2f + 0.3f * charge, PitchVariance = 0.15f }, position);
            int count = 6 + (int)(charge * 8f);
            for (int i = 0; i < count; i++)
                Leaf(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(2f, 5f + 3f * charge), Main.rand.NextFloat(1.1f, 1.8f), i % 3 == 0 ? DustID.JungleGrass : DustID.Grass);
        }

        // The hit that made the vines longer: a rustle and a few leaves shoot off the tip.
        public static void ChargeHit(Vector2 tip)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.45f, Pitch = 0.3f }, tip);
            for (int i = 0; i < 6; i++)
                Leaf(tip, Main.rand.NextVector2Circular(3f, 3f), 1.2f, DustID.GreenTorch);
        }

        // The sword leaves the hand (every machine, at the start of the drop).
        public static void PlantStart(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.5f }, position);
            for (int i = 0; i < 6; i++)
                Leaf(position + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextVector2Circular(1.5f, 1.5f), 1.1f, DustID.Grass);
        }

        // The sword lands in the ground.
        public static void PlantImpact(Vector2 ground)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.9f, Pitch = -0.3f }, ground);
            SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.8f, Pitch = -0.2f }, ground);
            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4.5f, -1f));
                Dust dirt = Dust.NewDustPerfect(ground + new Vector2(Main.rand.NextFloat(-8f, 8f), 0f), DustID.Dirt, velocity, 0, default, Main.rand.NextFloat(1f, 1.5f));
                dirt.noGravity = false;
            }
            for (int i = 0; i < 10; i++)
                Leaf(ground + new Vector2(Main.rand.NextFloat(-10f, 10f), -4f), new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4f, -1f)), Main.rand.NextFloat(1f, 1.6f), Main.rand.NextBool(3) ? DustID.JungleGrass : DustID.Grass);
        }

        // The planted vines pop out of the ground (the Amazon's vine bud look: a little dirt and a few leaves).
        public static void Emerge(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.5f, Pitch = 0f, PitchVariance = 0.15f }, position);
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(-2.6f, -0.6f));
                Dust.NewDustPerfect(position + new Vector2(Main.rand.NextFloat(-8f, 8f), 0f), DustID.Dirt, velocity, 0, default, Main.rand.NextFloat(0.8f, 1.2f));
            }
            for (int i = 0; i < 5; i++)
                Leaf(position + new Vector2(Main.rand.NextFloat(-6f, 6f), -6f), new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1.5f, -0.5f)), Main.rand.NextFloat(0.8f, 1.2f), DustID.Grass);
        }

        // A leaf drifts off the planted vines' root.
        public static void PlantedLeaf(Vector2 position)
        {
            if (Main.dedServ)
                return;
            Leaf(position + Main.rand.NextVector2Circular(6f, 6f), new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1f, -0.2f)), Main.rand.NextFloat(0.8f, 1.2f), Main.rand.NextBool(3) ? DustID.JungleGrass : DustID.Grass);
        }


        // A planted set starts to wither (it is being replaced).
        public static void Wither(Vector2 center)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Grass with { Volume = 0.6f, Pitch = -0.4f }, center);
            for (int i = 0; i < 12; i++)
                Leaf(center + Main.rand.NextVector2Circular(14f, 30f), Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.NextFloat(1f, 1.4f), DustID.Grass);
        }

        // The sword was put away: the vines wither away.
        public static void Release(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 4; i++)
                Leaf(position + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextVector2Circular(1.5f, 1.5f), 1f, DustID.Grass);
        }
    }
}