using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using IsmailsWeaponReworks.Common;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.IceBoomerang
{
    /// <summary>All dust and sound for the Ice Boomerang (vanilla dust and sounds only), so a later juice pass edits one file.</summary>
    public static class IceBoomerangFx
    {
        // The dust code itself lives in Common/IceFx.cs (shared with the Ice Blade); these keep the boomerang's call sites unchanged.
        public static void Burst(Vector2 center, int count, float speed, float scale)
        {
            IceFx.Burst(center, count, speed, scale);
        }

        public static void Ring(Vector2 center, int count, float radius, float outwardSpeed)
        {
            IceFx.Ring(center, count, radius, outwardSpeed);
        }

        // Frost gathering toward a point (used while the boomerang settles).
        public static void Gather(Vector2 center, float radius)
        {
            IceFx.Gather(center, radius);
        }

        public static void PlayFormed(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.8f, PitchVariance = 0.2f }, position);
        }

        // The pitch rises with every crystal the boomerang has bounced off.
        public static void PlayBlast(Vector2 position, int level)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.85f, Pitch = MathHelper.Min(0.9f, -0.3f + 0.15f * level) }, position);
        }

        public static void PlayCatch(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.6f, Pitch = 0.5f }, position);
        }

        public static void PlayShatter(Vector2 position)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f, PitchVariance = 0.25f }, position);
        }
    }
}