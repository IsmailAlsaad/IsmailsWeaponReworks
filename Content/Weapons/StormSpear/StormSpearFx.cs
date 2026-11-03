using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.StormSpear
{
    /// <summary>The Storm Spear's own lightning and dust (the shared thrust effects live in Common/ThrustFx). Vanilla dust only.</summary>
    public static class StormSpearFx
    {
        static ArcBoltStyle Style()
        {
            return new ArcBoltStyle(StormSpearTuning.BoltCore, StormSpearTuning.BoltGlow, StormSpearTuning.BoltLifeTicks, StormSpearTuning.BoltWidth);
        }

        // The dash begins (every machine): a crackling ring around the player.
        public static void DashStart(Vector2 center, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            ThrustPalette palette = StormSpearTuning.Palette;
            for (int i = 0; i < 16; i++)
            {
                Vector2 ring = (MathHelper.TwoPi * i / 16f).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(center + ring * 12f, i % 2 == 0 ? palette.DustA : palette.DustB, ring * 3f - direction * 1.5f, 100, default, Main.rand.NextFloat(1.1f, 1.7f));
                dust.noGravity = true;
            }
            for (int i = 0; i < 3; i++)
                ArcBolt.Spawn(center, center + Main.rand.NextVector2CircularEdge(46f, 46f), Style(), StormSpearTuning.BoltJag * 0.7f);
        }

        // Every tick of the dash: a bolt along the stretch the body just flew, side branches, and electric dust that fades on its own.
        public static void BodyTrail(Vector2 from, Vector2 to, Vector2 direction)
        {
            if (Main.dedServ)
                return;
            ThrustPalette palette = StormSpearTuning.Palette;
            ArcBolt.Spawn(from, to, Style(), StormSpearTuning.BoltJag, 1);
            for (int i = 0; i < 1; i++)
            {
                Vector2 position = Vector2.Lerp(from, to, Main.rand.NextFloat()) + Main.rand.NextVector2Circular(10f, 14f);
                Dust dust = Dust.NewDustPerfect(position, i == 0 ? palette.DustB : palette.DustA, Main.rand.NextVector2Circular(1.5f, 1.5f) - direction * 0.8f, 100, default, Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = true;
            }
        }

        // The tip hit an enemy (owner's screen): a bolt from the tip into the enemy and a burst of sparks.
        public static void Zap(Vector2 tip, Vector2 target, bool applied)
        {
            if (Main.dedServ)
                return;
            ThrustPalette palette = StormSpearTuning.Palette;
            if (!applied)
            {
                // The enemy is immune to Electrified: a grey puff, no bolt.
                for (int i = 0; i < 8; i++)
                {
                    Dust puff = Dust.NewDustPerfect(target, DustID.Smoke, Main.rand.NextVector2Circular(2f, 2f), 150, default, 1.2f);
                    puff.noGravity = true;
                }
                return;
            }
            ArcBolt.Spawn(tip, target, Style(), StormSpearTuning.BoltJag, 2);
            for (int i = 0; i < 1; i++)
            {
                Dust dust = Dust.NewDustPerfect(target, palette.DustA, Main.rand.NextVector2Circular(5f, 5f), 100, default, Main.rand.NextFloat(1.2f, 1.8f));
                dust.noGravity = true;
            }
            SoundStyle electricEel = new SoundStyle("IsmailsWeaponReworks/Content/Sounds/ElectricEel");
            SoundEngine.PlaySound(electricEel with { PitchVariance = 0.15f});
        }
    }
}