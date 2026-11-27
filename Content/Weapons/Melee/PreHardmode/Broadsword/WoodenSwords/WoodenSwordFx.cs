using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords
{
    /// <summary>
    /// Every dust and sound of the wooden sword reworks (vanilla dust and sounds only), so a later juice pass edits one file. Every machine
    /// calls these for the same event; all of it is visual, nothing here is synced.
    /// </summary>
    public static class WoodenSwordFx
    {
        // ---- sounds ----

        public static void PlaySwing(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = 0.05f, PitchVariance = 0.1f }, position);
        }

        // A splinter lodges in an enemy.
        public static void PlayStick(Vector2 position)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(new SoundStyle("IsmailsWeaponReworks/Assets/Sounds/WoodSplit/WoodSplit", 1, 4) { Volume = 0.4f, PitchVariance = 0.1f }, position);
        }

        // ---- dust ----

        public static void HitSpark(Vector2 position)
        {
            if (Main.dedServ)
                return;
            //for (int i = 0; i < 3; i++)
            //{
            //    Dust dust = Dust.NewDustPerfect(position, DustID.WoodFurniture, Main.rand.NextVector2Circular(2.5f, 2.5f), 0, default, Main.rand.NextFloat(0.8f, 1.2f));
            //    dust.noGravity = true;
            //}
        }

        // A few chips of the splinter's wood (column = the wood) thrown from 'position'.
        public static void Chips(Vector2 position, int column, int count, float speed)
        {
            if (Main.dedServ)
                return;
            int type = WoodenSwordWoods.DustOf(column);
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(3f, 3f), type, Main.rand.NextVector2Circular(speed, speed), 0, default, Main.rand.NextFloat(0.6f, 1f));
                dust.noGravity = false;
            }
        }

        // The end of a splinter's life: it breaks into a burst of its wood's dust (a little bigger and faster than the lodging chips, with a small pop upwards).
        public static void Break(Vector2 position, int column, int count, float speed)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.5f, Pitch = 0.05f, PitchVariance = 0.1f , MaxInstances = 0}, position);
            int type = WoodenSwordWoods.DustOf(column);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(speed, speed) + new Vector2(0f, -0.6f);
                Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(3f, 3f), type, velocity, 0, default, Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = false;
            }
        }
    }
}