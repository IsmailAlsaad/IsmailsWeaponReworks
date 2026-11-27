using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.OreSwords
{
    /// <summary>The sounds of the ore broadswords (vanilla sounds and Ismail's own files). Every machine calls these for the same event; all of it is audio, nothing here is synced.</summary>
    public static class OreSwordFx
    {
        // A hit on an enemy: one of the MetalSword1..N files (Assets/Sounds/MetalSword/), picked at random. It plays on top of the enemy's own hit sound.
        // A missing file is skipped (no crash), so the sword works before the sounds are added.
        public static void PlayHit(Vector2 position)
        {
            if (Main.dedServ)
                return;

            string path = "IsmailsWeaponReworks/Assets/Sounds/MetalSword/MetalSword";
            if (!ModContent.HasAsset(path + "1"))
                return;

            SoundEngine.PlaySound(new SoundStyle(path, 1, System.Math.Max(1, OreSwordTuning.HitSoundCount))
            {
                Volume = OreSwordTuning.HitSoundVolume,
                Pitch = OreSwordTuning.HitSoundPitch,
                PitchVariance = OreSwordTuning.HitSoundPitchVariance,
                MaxInstances = OreSwordTuning.HitSoundMaxInstances
            }, position);
        }
    }
}