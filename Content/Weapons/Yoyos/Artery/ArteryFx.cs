using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The dust and sounds of the Artery (vanilla only): blood chips on a hit, the lasso catching an enemy and the loop closing.
    /// The defaults are in YoyoFx. Every machine calls these for the same event; all of it is visual, nothing here is synced.
    /// </summary>
    public class ArteryFx : YoyoFx
    {
        protected override int ChipDust => DustID.Blood;

        public override void Throw(Vector2 position, int side)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.3f, PitchVariance = 0.1f }, position);
        }

        // The string touched an enemy and the yoyo starts to swing around it: a sharp whip and a few drops.
        public virtual void LoopCatch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = 0.75f, PitchVariance = 0.05f }, position);
            for (int i = 0; i < 6; i++)
                Chip(position, Main.rand.NextVector2Circular(2.5f, 2.5f), Main.rand.NextFloat(0.8f, 1.2f));
        }

        // The loop closed on its enemy (patch A2 adds the blood explosion on top of this): a wet snap and a burst of drops.
        public virtual void LoopClose(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.6f, Pitch = -0.2f, PitchVariance = 0.1f }, position);
            for (int i = 0; i < 14; i++)
                Chip(position, Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(0.9f, 1.4f));
        }
    }
}