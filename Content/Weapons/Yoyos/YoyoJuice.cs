using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The yoyos' afterimages, ready to use by any yoyo (the Wooden Yoyo does not use them, Code 1 does). A YoyoProjectile owns one; a
    /// trick switches it on with YoyoTrick.Afterimages. It lays one afterimage of the yoyo's sprite per tick while the yoyo is fast and
    /// out, on top of the shared AfterimageTrail (each afterimage fades on its own, and Release lets the rest fade after the projectile
    /// is gone). Tune it in YoyoTuning (AfterimageTicks, AfterimageMinScale, AfterimageAlpha, AfterimageMinSpeed). Visual only, never synced.
    /// </summary>
    public class YoyoAfterimages
    {
        AfterimageTrail trail;

        // Once per tick (PostAI). lay = this tick may leave an afterimage; speed = how far the yoyo moved this tick.
        public void Tick(Texture2D texture, bool lay, Vector2 center, float rotation, float speed)
        {
            if (Main.dedServ)
                return;
            trail ??= AfterimageTrail.Sprite(texture, YoyoTuning.AfterimageTicks, YoyoTuning.AfterimageMinScale);
            trail.Tick();
            if (lay && speed > YoyoTuning.AfterimageMinSpeed)
                trail.Add(center, rotation, 1f, Lighting.GetColor((int)(center.X / 16f), (int)(center.Y / 16f)) * YoyoTuning.AfterimageAlpha);
        }

        // PreDraw, before the yoyo's body.
        public void Draw()
        {
            trail?.Draw();
        }

        // OnKill: the leftover afterimages keep fading by themselves.
        public void Release()
        {
            if (trail == null)
                return;
            DetachedTrails.Hand(trail);
            trail = null;
        }
    }

    /// <summary>
    /// The yoyos' hit shake: one subtle camera punch, through the shared CameraShake. A trick asks for it with YoyoTrick.HitShake
    /// (0 = none). Every machine could call it for the same event; the owner's client does, for its own hits.
    /// </summary>
    public static class YoyoShake
    {
        public static void Hit(Vector2 center, float strength)
        {
            if (strength <= 0f)
                return;
            CameraShake.Punch(center, strength, 6, "YoyoHit");
        }
    }
}