using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.BladeOfGrass
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

        // The button is released: the vines wither away.
        public static void Release(Vector2 position)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 4; i++)
                Leaf(position + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextVector2Circular(1.5f, 1.5f), 1f, DustID.Grass);
        }
    }
}