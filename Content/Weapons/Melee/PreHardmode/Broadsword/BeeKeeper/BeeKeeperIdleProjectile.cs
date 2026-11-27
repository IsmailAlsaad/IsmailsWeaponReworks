using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BeeKeeper
{
    /// <summary>The BeeKeeper held out while nothing is swinging (HeldIdleSwordProjectile): the ready pose is where its own swing starts, the look is its steady glow.</summary>
    public class BeeKeeperIdleProjectile : HeldIdleSwordProjectile
    {
        public override bool Holds(int itemType) { return itemType == ItemID.BeeKeeper; }
        protected override int SwingType => ModContent.ProjectileType<BeeKeeperSwingProjectile>();
        protected override float ReadyArmDeg => BeeKeeperTuning.RestArmDeg;
        protected override float ReadyTiltDeg => BeeKeeperTuning.TiltHighDeg;
        protected override float SwordScale => BeeKeeperTuning.SwordScale;

        protected override void DrawBlade(Texture2D texture, Vector2 position, float rotation, Vector2 origin, float scale, SpriteEffects effects, Color lightColor)
        {
            Main.EntitySpriteDraw(texture, position, null, BeeKeeperTuning.HoneyColor * (0.5f * 0.2f), rotation, origin, scale * 1.12f, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.35f), rotation, origin, scale, effects, 0);
        }
    }
}