using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Valor
{
    /// <summary>
    /// The Valor's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and tricks. The vanilla
    /// sprite is used until the yoyo's own art exists (its path is in ValorTuning).
    /// </summary>
    public class ValorProjectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Valor;

        public override int YoyoItemType => ItemID.Valor;

        protected override YoyoTuning CreateTuning()
        {
            return new ValorTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new ValorFx();
        }

        // The left click: Pop the Clutch.
        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new ValorTrick(tuning);
        }

        // The right click: Around the World (3 turns).
        protected override YoyoTrick CreateRightTrick(YoyoTuning tuning)
        {
            return new ValorAroundTheWorldTrick(tuning);
        }
    }
}