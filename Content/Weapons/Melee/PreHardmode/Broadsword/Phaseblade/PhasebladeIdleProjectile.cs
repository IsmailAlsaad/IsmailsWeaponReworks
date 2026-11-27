using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.Phaseblade
{
    /// <summary>The Phaseblade held out while nothing is swinging (HeldIdleSwordProjectile): the ready pose is where its own swing starts, the look is its steady glow.</summary>
    public class PhasebladeIdleProjectile : HeldIdleSwordProjectile
    {
        public override bool Holds(int itemType) { return PhasebladeTuning.IsPhaseblade(itemType); }
        protected override int SwingType => ModContent.ProjectileType<PhasebladeSwingProjectile>();
        protected override float ReadyArmDeg => PhasebladeTuning.RestArmDeg;
        protected override float ReadyTiltDeg => PhasebladeTuning.TiltHighDeg;
        protected override float SwordScale => PhasebladeTuning.SwordScale;

        // A planted or flying-back blade is not in the hand.
        protected override bool Hidden(Player owner)
        {
            int itemType = owner.HeldItem.type;
            return PhasebladePlantedProjectile.Has(owner, itemType) || PhasebladePlantedProjectile.IsReturning(owner, itemType);
        }

        protected override void DrawBlade(Texture2D texture, Vector2 position, float rotation, Vector2 origin, float scale, SpriteEffects effects, Color lightColor)
        {
            Color color = PhasebladeTuning.ColorOf(Main.player[Projectile.owner].HeldItem.type);
            // The steady glow, drawn around the CENTER of the blade sprite like the swing's, so the look does not change when the swing starts.
            Vector2 hiltToCenter = new Vector2(Projectile.spriteDirection > 0 ? texture.Width * 0.5f : -texture.Width * 0.5f, -texture.Height * 0.5f);
            Vector2 centerPosition = position + (hiltToCenter * scale).RotatedBy(rotation);
            Vector2 centerOrigin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, centerPosition, null, color * (0.5f * 0.3f), rotation, centerOrigin, scale * 1.25f, effects, 0);
            Main.EntitySpriteDraw(texture, centerPosition, null, color * (0.6f * 0.3f), rotation, centerOrigin, scale * 1.1f, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.6f), rotation, origin, scale, effects, 0);
        }
    }
}