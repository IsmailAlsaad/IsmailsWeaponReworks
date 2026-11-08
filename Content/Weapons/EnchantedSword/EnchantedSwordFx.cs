using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// Every dust and sound of the Enchanted Sword rework (vanilla dust and sounds only), so a later juice pass edits one file. Every
    /// machine calls these for the same event; all of it is visual, nothing here is synced.
    /// </summary>
    public static class EnchantedSwordFx
    {
        static void Mote(Vector2 position, Vector2 velocity, float scale, int type)
        {
            Dust dust = Dust.NewDustPerfect(position, type, velocity, 100, default, scale);
            dust.noGravity = true;
        }

        static int BlueDust(int i)
        {
            return i % 3 == 0 ? DustID.IceTorch : DustID.BlueTorch;
        }

        // ---- the player's blade ----

        // A counted hit: the outline gets stronger. A rising chime and a ring of sparks around the hand.
        public static void StageUp(Player player, int stage)
        {
            if (Main.dedServ)
                return;
            Vector2 at = player.Center + new Vector2(player.direction * 26f, -6f);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.55f, Pitch = -0.2f + 0.35f * stage, PitchVariance = 0.05f }, at);
            int count = 6 + 6 * stage;
            for (int i = 0; i < count; i++)
                Mote(at, (MathHelper.TwoPi * i / count).ToRotationVector2() * (2.5f + stage), 1.1f + 0.2f * stage, BlueDust(i));
        }

        // Ethereal flecks along the swinging blade while it carries an outline.
        public static void SwingDust(Rectangle hitbox, int stage)
        {
            if (Main.dedServ || !Main.rand.NextBool(System.Math.Max(1, 4 - stage)))
                return;
            Mote(Main.rand.NextVector2FromRectangle(hitbox), Main.rand.NextVector2Circular(0.8f, 0.8f), 0.9f + 0.3f * stage, BlueDust(Main.rand.Next(3)));
        }

        // ---- the ethereal sword ----

        // The 3rd hit discharges: a flash, a ring and a chime where the ghost appears.
        public static void Discharge(Vector2 center)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = 0.45f }, center);
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.45f, Pitch = 0.4f }, center);
            for (int i = 0; i < 22; i++)
                Mote(center, (MathHelper.TwoPi * i / 22f).ToRotationVector2() * 5f, 1.5f, BlueDust(i));
            for (int i = 0; i < 12; i++)
                Mote(center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 4f), Main.rand.NextFloat(1.1f, 1.7f), DustID.MagicMirror);
        }

        // kind: 0 = up, 1 = down, 2 = thrust. The whoosh of a swing that starts to strike.
        public static void PlaySwing(Vector2 position, int kind)
        {
            if (Main.dedServ)
                return;
            float pitch = kind == 0 ? 0.3f : kind == 1 ? 0.1f : 0.55f;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = pitch, PitchVariance = 0.08f }, position);
            if (kind == 2)
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.3f, Pitch = 0.5f }, position);
        }

        // The ghost's blade hits an enemy.
        public static void GhostHit(Vector2 position, bool thrust)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.45f, Pitch = thrust ? 0.6f : 0.35f, PitchVariance = 0.1f }, position);
            int count = thrust ? 14 : 8;
            for (int i = 0; i < count; i++)
                Mote(position, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(2f, thrust ? 6f : 4.5f), Main.rand.NextFloat(1.1f, 1.7f), BlueDust(i));
        }

        // A wisp shed by the ghost's blade and hand.
        public static void Wisp(Vector2 position, Vector2 drift)
        {
            if (Main.dedServ)
                return;
            Mote(position, drift + Main.rand.NextVector2Circular(0.6f, 0.6f), Main.rand.NextFloat(0.8f, 1.3f), BlueDust(Main.rand.Next(3)));
        }

        // The ghost has swung its last and fades away.
        public static void Vanish(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.3f, Pitch = 0.6f }, position);
            for (int i = 0; i < 14; i++)
                Mote(position, Main.rand.NextVector2Circular(2.5f, 2.5f) + new Vector2(0f, -0.8f), Main.rand.NextFloat(1f, 1.5f), BlueDust(i));
        }
    }
}