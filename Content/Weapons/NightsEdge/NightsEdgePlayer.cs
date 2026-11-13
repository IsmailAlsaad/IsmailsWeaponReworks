using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The Night's Edge's charge (0 to 1) lives here, on the owner's machine: true melee hits fill it (the first enemy each swing hits counts).
    /// THE CHARGE FADES, a full one too: the timer since the last counted hit runs on this machine; after FadeGraceTicks without a counted hit the
    /// charge drains to 0 over FadeDrainTicks. It is lost when the player swaps to another item or dies (the Light's Bane's rule) and never saved.
    /// It also keeps the held blade alive: whenever the sword is held and no NightsEdgeBladeProjectile exists, it spawns one. The other machines
    /// learn the charge from the blade projectile (SendExtraAI), nothing here is synced.
    /// </summary>
    public class NightsEdgePlayer : ModPlayer
    {
        public float Charge;
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

        // A counted hit: adds charge and restarts the fade timer.
        public static void AddCharge(Player player, float amount)
        {
            NightsEdgePlayer state = player.GetModPlayer<NightsEdgePlayer>();
            state.Charge = MathHelper.Clamp(state.Charge + amount, 0f, 1f);
            state.sinceHit = 0;
        }

        public override void OnRespawn()
        {
            Charge = 0f;
            sinceHit = 0;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (Player.dead || Player.HeldItem.type != ItemID.NightsEdge)
            {
                Charge = 0f;
                sinceHit = 0;
                return;
            }

            sinceHit++;
            if (Charge > 0f && sinceHit > NightsEdgeTuning.FadeGraceTicks)
                Charge = Math.Max(0f, Charge - 1f / Math.Max(1, NightsEdgeTuning.FadeDrainTicks));

            if (Player.noItems || Player.CCed)
                return;
            int type = ModContent.ProjectileType<NightsEdgeBladeProjectile>();
            if (Find(Player.whoAmI, type) != null)
                return;
            Item item = Player.HeldItem;
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero, type,
                Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack), Player.whoAmI);
        }
    }
}