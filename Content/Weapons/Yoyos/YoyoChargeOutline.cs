using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// The numbers of the charge outline (a yoyo that glows while it is charged: the Hive-Five's golden one, the Cascade's red one).
    /// A yoyo's tuning implements this and gives its own colors. Properties, so hot reload can tweak them.
    /// </summary>
    public interface IChargeOutlineTuning
    {
        float OutlineWidth { get; }       // pixels, at full strength
        int OutlinePulseTicks { get; }    // one pulse
        float OutlineMin { get; }         // the pulse swings between these strengths
        float OutlineMax { get; }
        float OutlineEase { get; }        // how fast it fades in and out (0..1 per tick)
        float GlowScale { get; }          // the soft glow copy behind the outline is this much bigger than the yoyo
        float GlowAlpha { get; }          // its opacity
        Color OutlineColor { get; }       // alpha 0 = additive
        Color OutlineCoreColor { get; }   // the brighter inner ring
    }

    /// <summary>
    /// The charge outline of a yoyo (extracted from the Hive-Five when the Cascade needed it too): Shown eases toward 1 while the yoyo is
    /// charged (or holding at max speed) and back to 0 otherwise; Draw puts a soft glow copy of the sprite behind it, then the sprite
    /// repeated in eight directions around it in the outline color and four times closer in the core color, pulsing (PixelOutline.Pulse).
    /// Shown is also what the yoyos blend their string color with. Visual only: every machine runs and draws its own, from synced state.
    /// </summary>
    public class YoyoChargeOutline
    {
        public float Shown;   // 0..1

        public void Tick(bool want, IChargeOutlineTuning tuning)
        {
            Shown += ((want ? 1f : 0f) - Shown) * MathHelper.Clamp(tuning.OutlineEase, 0.01f, 1f);
        }

        public void Draw(Texture2D texture, Vector2 center, float rotation, IChargeOutlineTuning tuning)
        {
            if (Shown < 0.02f || Main.dedServ)
                return;
            Vector2 position = center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float strength = Shown * PixelOutline.Pulse((int)Main.GameUpdateCount, tuning.OutlinePulseTicks, tuning.OutlineMin, tuning.OutlineMax);

            Main.EntitySpriteDraw(texture, position, null, tuning.OutlineColor * (tuning.GlowAlpha * strength), rotation, origin, tuning.GlowScale, SpriteEffects.None, 0);
            for (int i = 0; i < 8; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / 8f).ToRotationVector2();
                Main.EntitySpriteDraw(texture, position + direction * tuning.OutlineWidth, null, tuning.OutlineColor * (0.9f * strength), rotation, origin, 1f, SpriteEffects.None, 0);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 direction = (MathHelper.PiOver2 * i).ToRotationVector2();
                Main.EntitySpriteDraw(texture, position + direction * (tuning.OutlineWidth * 0.5f), null, tuning.OutlineCoreColor * (0.5f * strength), rotation, origin, 1f, SpriteEffects.None, 0);
            }
        }
    }
}
