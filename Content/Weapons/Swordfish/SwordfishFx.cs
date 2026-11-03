using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Swordfish
{
    /// <summary>The Swordfish's own dust and sound (the shared thrust effects live in Common/ThrustFx); only the rage needs more.</summary>
    public static class SwordfishFx
    {
        // The enraged chain begins: a burst of red fire around the hand and a low roar.
        public static void RageBurst(Vector2 position)
        {
            if (Main.dedServ)
                return;
            //SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = -0.3f }, position);
            for (int i = 0; i < 14; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / 14f).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(position + direction * 10f, i % 3 == 0 ? DustID.Water : DustID.WaterCandle, direction * Main.rand.NextFloat(2f, 4.5f), 100, default, Main.rand.NextFloat(1.2f, 1.7f));
                dust.noGravity = true;
            }
        }

        // Fire shed at the tip of an enraged thrust.
        public static void RageTip(Vector2 tip, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextBool(3) ? DustID.Water : DustID.WaterCandle, -direction * Main.rand.NextFloat(0.5f, 2.5f) + Main.rand.NextVector2Circular(1f, 1f), 100, default, Main.rand.NextFloat(1.1f, 1.6f));
            dust.noGravity = true;
        }
    }
}