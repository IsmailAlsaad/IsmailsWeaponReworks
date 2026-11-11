using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// The Hive-Five's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and trick. The vanilla
    /// sprite is used until the yoyo's own art exists (its path is in HiveFiveTuning).
    /// </summary>
    public class HiveFiveProjectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.HiveFive;

        public override int YoyoItemType => ItemID.HiveFive;

        protected override YoyoTuning CreateTuning()
        {
            return new HiveFiveTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new HiveFiveFx();
        }

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new HiveFiveTrick(tuning);
        }
    }
}
