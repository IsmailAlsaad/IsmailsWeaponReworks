using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.Starfury
{
    /// <summary>The Starfury held out while nothing is swinging (HeldIdleSwordProjectile): the ready pose is where its own swing starts, the look is its steady glow.</summary>
    public class StarfuryIdleProjectile : HeldIdleSwordProjectile
    {
        public override bool Holds(int itemType) { return itemType == ItemID.Starfury; }
        protected override int SwingType => ModContent.ProjectileType<StarfurySwingProjectile>();
        protected override float ReadyArmDeg => StarfuryTuning.RestArmDeg;
        protected override float ReadyTiltDeg => StarfuryTuning.TiltHighDeg;
        protected override float SwordScale => StarfuryTuning.SwordScale;

        protected override void DrawBlade(Texture2D texture, Vector2 position, float rotation, Vector2 origin, float scale, SpriteEffects effects, Color lightColor)
        {
            Main.EntitySpriteDraw(texture, position, null, StarfuryTuning.PinkColor * (0.5f * 0.25f), rotation, origin, scale * 1.2f, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, StarfuryTuning.GoldColor * (0.6f * 0.25f), rotation, origin, scale * 1.08f, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.35f), rotation, origin, scale, effects, 0);
        }
    }
}