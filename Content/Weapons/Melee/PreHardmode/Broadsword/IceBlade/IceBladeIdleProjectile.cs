using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.IceBlade
{
    /// <summary>The IceBlade held out while nothing is swinging (HeldIdleSwordProjectile): the ready pose is where its own swing starts, the look is its steady glow.</summary>
    public class IceBladeIdleProjectile : HeldIdleSwordProjectile
    {
        public override bool Holds(int itemType) { return itemType == ItemID.IceBlade; }
        protected override int SwingType => ModContent.ProjectileType<IceBladeSwingProjectile>();
        protected override float ReadyArmDeg => IceBladeTuning.ArmStartDeg;
        protected override float ReadyTiltDeg => IceBladeTuning.BladeTiltDeg;
        protected override float SwordScale => IceBladeTuning.SwordScale;
        protected override int ReturnDelayTicks => 7;  // ticks the sword stays in its post-strike pose (60 = 1 second)
        protected override float IdleEase => 0.06f;     // how softly it settles into the ready pose afterwards (0.15 = default)

        protected override bool SwingOwnsSword(Projectile swing)
        {
            IceBladeSwingProjectile ice = swing.ModProjectile as IceBladeSwingProjectile;
            return ice == null || ice.HoldsSword;
        }

        protected override void DrawBlade(Texture2D texture, Vector2 position, float rotation, Vector2 origin, float scale, SpriteEffects effects, Color lightColor)
        {
            Main.EntitySpriteDraw(texture, position, null, IceBladeTuning.HaloColor * (0.6f * 0.2f), rotation, origin, scale * 1.2f, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, IceBladeTuning.MidColor * (0.6f * 0.2f), rotation, origin, scale * 1.08f, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.35f), rotation, origin, scale, effects, 0);
        }
    }
}