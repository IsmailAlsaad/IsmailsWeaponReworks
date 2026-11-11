using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Malaise
{
    /// <summary>
    /// The Malaise's held yoyo projectile: the machinery is in YoyoProjectile and the loop's synced data in LoopYoyoProjectile, this only
    /// names the yoyo's item, tuning, fx and trick. The vanilla sprite is used until the yoyo's own art exists (Tuning.TexturePath).
    /// </summary>
    public class MalaiseProjectile : LoopYoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CorruptYoyo;

        public override int YoyoItemType => ItemID.CorruptYoyo;

        protected override YoyoTuning CreateTuning()
        {
            return new MalaiseTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new MalaiseFx();
        }

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new MalaiseTrick(tuning);
        }
    }
}