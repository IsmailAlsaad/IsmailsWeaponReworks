using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The Enchanted Sword's hit count (0 .. HitsToDischarge - 1) lives here, on the owner's machine: true melee hits add to it, it is lost
    /// EnchantedSwordTuning.HitHoldTicks after the last hit, when the player swaps to another item and when they die; it is never saved. It
    /// also keeps the held blade alive: whenever the sword is held and no EnchantedSwordBladeProjectile exists, it spawns one. The other
    /// machines learn the count from the blade projectile (SendExtraAI), nothing here is synced.
    /// </summary>
    public class EnchantedSwordPlayer : ModPlayer
    {
        public int Hits;
        int sinceHit;

        public static Projectile Find(int owner, int type)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == owner && projectile.type == type)
                    return projectile;
            }
            return null;
        }

        // A counted hit. Returns true when it was the last one: the count starts over and the sword discharges.
        public static bool AddHit(Player player)
        {
            EnchantedSwordPlayer state = player.GetModPlayer<EnchantedSwordPlayer>();
            state.sinceHit = 0;
            state.Hits++;
            if (state.Hits >= EnchantedSwordTuning.HitsToDischarge)
            {
                state.Hits = 0;
                return true;
            }
            return false;
        }

        public override void OnRespawn()
        {
            Hits = 0;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (Player.dead || Player.HeldItem.type != ItemID.EnchantedSword)
            {
                Hits = 0;
                sinceHit = 0;
            }
            else if (Hits > 0)
            {
                sinceHit++;
                if (sinceHit > EnchantedSwordTuning.HitHoldTicks)
                    Hits = 0;
            }

            if (Player.dead || Player.noItems || Player.CCed || Player.HeldItem.type != ItemID.EnchantedSword)
                return;
            int type = ModContent.ProjectileType<EnchantedSwordBladeProjectile>();
            if (Find(Player.whoAmI, type) != null)
                return;
            Item item = Player.HeldItem;
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero, type,
                Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack), Player.whoAmI);
        }
    }
}