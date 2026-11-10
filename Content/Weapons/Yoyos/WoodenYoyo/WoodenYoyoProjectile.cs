using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.WoodenYoyo
{
    /// <summary>
    /// The WoodenYoyo's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and trick. The vanilla
    /// sprite is used until the yoyo's own art exists (its path is in WoodenYoyoTuning).
    /// </summary>
    public class WoodenYoyoProjectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodYoyo;

        public override int YoyoItemType => ItemID.WoodYoyo;

        protected override YoyoTuning CreateTuning()
        {
            return new WoodenYoyoTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new WoodenYoyoFx();
        }

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new WoodenYoyoTrick(tuning);
        }
    }
}