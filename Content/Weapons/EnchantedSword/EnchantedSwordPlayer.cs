using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The Enchanted Sword's charge (0 .. HitsToDischarge, HitsToDischarge = FULL) lives here, on the owner's machine: thrust hits add to it,
    /// a partial charge is lost EnchantedSwordTuning.HitHoldTicks after the last hit (a FULL one never decays), both are lost when the player
    /// swaps to another item and when they die, and the throw spends it (Spend); it is never saved. It
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

        // A counted hit. Returns true when it made the sword FULL; at full further hits add nothing (the charge waits for the throw).
        public static bool AddHit(Player player)
        {
            EnchantedSwordPlayer state = player.GetModPlayer<EnchantedSwordPlayer>();
            int full = Math.Max(1, EnchantedSwordTuning.HitsToDischarge);
            if (state.Hits >= full)
                return false;
            state.sinceHit = 0;
            state.Hits++;
            return state.Hits >= full;
        }

        public static bool IsFull(Player player)
        {
            return player.GetModPlayer<EnchantedSwordPlayer>().Hits >= Math.Max(1, EnchantedSwordTuning.HitsToDischarge);
        }

        // The throw spends the whole charge.
        public static void Spend(Player player)
        {
            EnchantedSwordPlayer state = player.GetModPlayer<EnchantedSwordPlayer>();
            state.Hits = 0;
            state.sinceHit = 0;
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
            else if (Hits > 0 && Hits < Math.Max(1, EnchantedSwordTuning.HitsToDischarge)) // a FULL sword keeps its charge until the throw
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