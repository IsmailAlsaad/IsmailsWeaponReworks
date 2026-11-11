using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The Artery's held yoyo projectile: the machinery is in YoyoProjectile and the loop's synced data in LoopYoyoProjectile, this only names
    /// the yoyo's item, tuning, fx and trick. The vanilla sprite is used until the yoyo's own art exists (Tuning.TexturePath).
    /// </summary>
    public class ArteryProjectile : LoopYoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrimsonYoyo;

        public override int YoyoItemType => ItemID.CrimsonYoyo;

        protected override YoyoTuning CreateTuning()
        {
            return new ArteryTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new ArteryFx();
        }

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new ArteryTrick(tuning);
        }
    }
}