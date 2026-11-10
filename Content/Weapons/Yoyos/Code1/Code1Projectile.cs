using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Code1
{
    /// <summary>
    /// The Code1's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and trick. The vanilla
    /// sprite is used until the yoyo's own art exists (its path is in Code1Tuning).
    /// </summary>
    public class Code1Projectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Code1;

        public override int YoyoItemType => ItemID.Code1;

        protected override YoyoTuning CreateTuning()
        {
            return new Code1Tuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new Code1Fx();
        }

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new Code1Trick(tuning);
        }
    }
}