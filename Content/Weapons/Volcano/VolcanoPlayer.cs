using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// The Volcano's heat (0 to 1) lives here, on the owner's machine, so it survives swapping items for a moment and cools on its own.
    /// It also keeps the held blade alive: whenever the sword is held and no VolcanoBladeProjectile exists, it spawns one (not while a thrown sword exists, and not for RegrowTicks after it). The other machines
    /// learn the heat from the blade projectile (SendExtraAI), nothing here is synced.
    /// </summary>
    public class VolcanoPlayer : ModPlayer
    {
        public float Heat;
        int sinceHit;
        int regrow;   // owner: ticks the hand stays empty after a thrown sword is gone

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

        public static void AddHeat(Player player, float amount)
        {
            VolcanoPlayer state = player.GetModPlayer<VolcanoPlayer>();
            state.Heat = MathHelper.Clamp(state.Heat + amount, 0f, 1f);
            state.sinceHit = 0;
        }

        // The thrown sword ended: the blade starts cold.
        public static void ResetHeat(Player player)
        {
            VolcanoPlayer state = player.GetModPlayer<VolcanoPlayer>();
            state.Heat = 0f;
            state.sinceHit = 0;
        }

        public override void OnRespawn()
        {
            Heat = 0f;
            regrow = 0;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            // While the sword is thrown (flying, stuck, or the handle bouncing home) the hand is empty: the heat holds, and a new blade forms
            // RegrowTicks after the handle is caught (0 = at once, cold: the eruption reset the heat).
            bool thrown = Find(Player.whoAmI, ModContent.ProjectileType<VolcanoThrownProjectile>()) != null;
            if (thrown)
            {
                regrow = VolcanoTuning.RegrowTicks;
                sinceHit = 0;
            }
            else if (regrow > 0)
                regrow--;

            sinceHit++;
            if (Heat > 0f && sinceHit > VolcanoTuning.HeatHoldTicks)
                Heat = Math.Max(0f, Heat - VolcanoTuning.HeatDecayPerTick);

            if (Player.dead || Player.noItems || Player.CCed || Player.HeldItem.type != ItemID.FieryGreatsword)
                return;
            if (regrow > 0)
                return;
            int type = ModContent.ProjectileType<VolcanoBladeProjectile>();
            if (Find(Player.whoAmI, type) != null)
                return;
            Item item = Player.HeldItem;
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero, type,
                Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack), Player.whoAmI);
        }
    }
}