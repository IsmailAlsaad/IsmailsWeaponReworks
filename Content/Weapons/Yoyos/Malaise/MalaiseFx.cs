using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Malaise
{
    /// <summary>
    /// The dust and sounds of the Malaise (vanilla only), all corruption: purple shadowflame wisps instead of chips. The defaults are in
    /// YoyoFx; the loop moments come from LoopYoyoFx. Every machine calls these for the same event; all of it is visual, nothing here is synced.
    /// </summary>
    public class MalaiseFx : LoopYoyoFx
    {
        // The chips that fly off a hit: shadowflame and purple fire, mixed.
        protected override int ChipDust => Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.PurpleTorch;

        // A floating wisp (no gravity), the Malaise's main particle.
        void Wisp(Vector2 position, Vector2 velocity, float scale)
        {
            Dust dust = Dust.NewDustPerfect(position, ChipDust, velocity, 0, default, scale);
            dust.noGravity = true;
        }

        public override void Throw(Vector2 position, int side)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.1f, PitchVariance = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = -0.4f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < 4; i++)
                Wisp(position, Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.NextFloat(0.9f, 1.3f));
        }

        // The string touched an enemy and the yoyo starts to swing around it: a whip, a hiss and a ring of wisps.
        public override void LoopCatch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = 0.6f, PitchVariance = 0.05f }, position);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = -0.5f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < 10; i++)
                Wisp(position, (MathHelper.TwoPi * i / 10f).ToRotationVector2() * Main.rand.NextFloat(1.5f, 3.5f), Main.rand.NextFloat(1f, 1.5f));
        }

        // One shred hit: wisps lift off the noose and the enemy (the hit's own sound comes with the strike).
        public override void LoopShred(Vector2 center, float noose)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < 5; i++)
            {
                Vector2 direction = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
                Wisp(center + direction * noose * Main.rand.NextFloat(0.5f, 1f), direction * Main.rand.NextFloat(0.8f, 2.2f) + new Vector2(0f, -0.4f), Main.rand.NextFloat(0.9f, 1.4f));
            }
        }

        // The loop is let go: a dark pulse, a ring of shadowflame and a small camera punch.
        public override void LoopClose(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = -0.6f, PitchVariance = 0.1f }, position);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 1f, Pitch = -0.3f, PitchVariance = 0.1f }, position);
            int ring = 24;
            for (int i = 0; i < ring; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / ring + Main.rand.NextFloat(-0.08f, 0.08f)).ToRotationVector2();
                Wisp(position + direction * 6f, direction * Main.rand.NextFloat(3.5f, 6f), Main.rand.NextFloat(1.3f, 1.8f));
            }
            for (int i = 0; i < 14; i++)
                Wisp(position, Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(1f, 1.6f));
            CameraShake.Punch(position, 2.5f, 12, "MalaiseClose");
        }

        // Rock the Baby: the triangle is complete. A pulse of wisps along the three sides and a ring at the pointy top (the hand).
        public void RockTriangle(Vector2 hand, Vector2 a, Vector2 b)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.8f, Pitch = 0.2f, PitchVariance = 0.05f }, hand);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, Pitch = -0.4f, PitchVariance = 0.05f }, hand);
            Vector2[] corners = { hand, a, b };
            for (int side = 0; side < 3; side++)
            {
                Vector2 from = corners[side];
                Vector2 to = corners[(side + 1) % 3];
                for (int i = 0; i < 8; i++)
                    Wisp(Vector2.Lerp(from, to, (i + Main.rand.NextFloat()) / 8f), Main.rand.NextVector2Circular(0.8f, 0.8f), Main.rand.NextFloat(0.9f, 1.3f));
            }
            for (int i = 0; i < 8; i++)
                Wisp(hand, (MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(1.5f, 2.5f), Main.rand.NextFloat(1f, 1.4f));
        }

        // Rock the Baby: the yoyo reached one end of its swing (speed01 = how far the speed-up has come, 0 to 1): a thump that rises in
        // pitch, a burst of wisps and a small camera punch that grows with the speed.
        public void RockSwingEnd(Vector2 position, float speed01)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.35f, Pitch = -0.5f + 0.7f * speed01, PitchVariance = 0.05f }, position);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.3f, Pitch = -0.2f + 0.6f * speed01, PitchVariance = 0.05f }, position);
            for (int i = 0; i < 8; i++)
                Wisp(position, Main.rand.NextVector2Circular(2.8f, 2.8f), Main.rand.NextFloat(0.9f, 1.4f));
            CameraShake.Punch(position, 0.8f + 1.2f * speed01, 8, "MalaiseRock");
        }

        // Rock the Baby: a wisp or two left behind the swinging yoyo.
        public void RockSwingTrail(Vector2 position, Vector2 velocity, float speed01)
        {
            if (Main.dedServ)
                return;
            Wisp(position, -velocity * 0.12f + Main.rand.NextVector2Circular(0.6f, 0.6f), Main.rand.NextFloat(0.8f, 1.2f) + 0.3f * speed01);
        }

        // Rock the Baby is let go: the dark ring of the loop's close at A, a plain ring at B (one sound and one punch).
        public void RockRelease(Vector2 a, Vector2 b, bool hasB)
        {
            LoopClose(a);
            if (hasB && !Main.dedServ)
            {
                int ring = 16;
                for (int i = 0; i < ring; i++)
                    Wisp(b, (MathHelper.TwoPi * i / ring).ToRotationVector2() * Main.rand.NextFloat(3f, 5f), Main.rand.NextFloat(1.2f, 1.6f));
            }
        }
    }
}