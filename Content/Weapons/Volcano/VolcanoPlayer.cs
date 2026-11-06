using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// The Volcano's heat (0 to 1) lives here, on the owner's machine, so it survives swapping items for a moment and cools on its own.
    /// It also keeps the held blade alive: whenever the sword is held and no VolcanoBladeProjectile exists, it spawns one. The other machines
    /// learn the heat from the blade projectile (SendExtraAI), nothing here is synced.
    /// </summary>
    public class VolcanoPlayer : ModPlayer
    {
        public float Heat;
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

        public static void AddHeat(Player player, float amount)
        {
            VolcanoPlayer state = player.GetModPlayer<VolcanoPlayer>();
            state.Heat = MathHelper.Clamp(state.Heat + amount, 0f, 1f);
            state.sinceHit = 0;
        }

        public override void OnRespawn()
        {
            Heat = 0f;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            sinceHit++;
            if (Heat > 0f && sinceHit > VolcanoTuning.HeatHoldTicks)
                Heat = Math.Max(0f, Heat - VolcanoTuning.HeatDecayPerTick);

            if (Player.dead || Player.noItems || Player.CCed || Player.HeldItem.type != ItemID.FieryGreatsword)
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