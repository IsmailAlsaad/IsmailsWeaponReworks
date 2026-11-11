using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.HiveFive
{
    /// <summary>
    /// The dust and sounds of the Hive-Five (vanilla only). The defaults are in YoyoFx; override a method here for a different feel.
    /// </summary>
    public class HiveFiveFx : YoyoFx
    {
        // Honey chips on a hit.
        protected override int ChipDust => DustID.Honey;

        // A thin trail of honey drops thrown off the spinning yoyo, more the faster it goes.
        public override void SpinTrail(Vector2 position, Vector2 velocity, float speed01)
        {
            if (Main.dedServ || speed01 < 0.25f || !Main.rand.NextBool(2))
                return;
            Dust dust = Dust.NewDustPerfect(position, DustID.Honey, -velocity * 0.03f + Main.rand.NextVector2Circular(0.8f, 0.8f), 100, default, Main.rand.NextFloat(0.7f, 1.1f));
            dust.noGravity = true;
        }
    }
}
