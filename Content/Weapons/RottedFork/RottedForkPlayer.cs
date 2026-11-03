using IsmailsWeaponReworks.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.RottedFork
{
    /// <summary>
    /// Holding right click throws the Rotted Fork again and again. The first throw is the normal item use; when that javelin ends while
    /// the right button is still held, AutoThrow is armed (by the javelin's OnKill) and this throws the next one as soon as the cooldown
    /// is over, and so on until the button is released or the item is swapped. Done here instead of relying on vanilla's autoReuse, which
    /// is not guaranteed to repeat a right click.
    /// MULTIPLAYER: owner's client only (the throw is the same synced projectile spawn as the first one); nothing here is synced.
    /// </summary>
    public class RottedForkPlayer : ModPlayer
    {
        public bool AutoThrow;

        public override void PreUpdate()
        {
            if (!AutoThrow || Player.whoAmI != Main.myPlayer)
                return;

            Item item = Player.HeldItem;
            if (!Player.active || Player.dead || item.type != ItemID.TheRottedFork || !Player.controlUseTile)
            {
                AutoThrow = false; // released, swapped away or dead: stop
                return;
            }

            if (Player.noItems || Player.CCed || Player.mouseInterface || Player.itemAnimation > 0)
                return;
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<RottedForkSpinProjectile>()] > 0 || Player.ownedProjectileCounts[ModContent.ProjectileType<RottedForkJavelinProjectile>()] > 0)
                return;
            if (Player.GetModPlayer<ThrustPlayer>().OnCooldown(ItemID.TheRottedFork))
                return;

            item.useStyle = ItemUseStyleID.Swing; // the throw keeps the swing pose (the spin leaves the Shoot style behind)
            RottedForkGlobalItem.Throw(Player, Player.GetSource_ItemUse(item), Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack));
            Player.itemAnimation = item.useAnimation; // the vanilla swing pose plays for the throw, like the first one
            Player.itemAnimationMax = item.useAnimation;
            Player.itemTime = item.useAnimation;
        }
    }
}