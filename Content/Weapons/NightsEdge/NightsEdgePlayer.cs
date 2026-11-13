using IsmailsWeaponReworks.Content.Buffs;
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

        // Ticks of Shadow Armor left: the time left on NightsEdgeShadowArmorBuff (0 without it). The buff is the single source of truth, so right-clicking
        // its icon cancels the armor, death clears it, and every machine sees it (vanilla syncs the buff list).
        public int ShadowTicks
        {
            get
            {
                int index = Player.FindBuffIndex(ModContent.BuffType<NightsEdgeShadowArmorBuff>());
                return index >= 0 ? Player.buffTime[index] : 0;
            }
        }
        bool armorLook;  // every machine: the armor's ignition burst has played for this buff
        bool hadArmor;   // owner's machine: the buff was on last tick
        int dropTimer;   // owner's machine: ticks since the last droplet

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
            hadArmor = false;
        }

        public override void PostUpdate()
        {
            // Every machine: the Shadow Armor's look (the buff list is synced, so everyone sees it on everyone): an ignition burst once, then dark flames and a light.
            int shadowLeft = Player.dead ? 0 : ShadowTicks;
            if (shadowLeft > 0)
            {
                if (!armorLook)
                    NightsEdgeFx.ArmorIgnite(Player);
                armorLook = true;
                NightsEdgeFx.ArmorLook(Player, MathHelper.Clamp(shadowLeft / (float)Math.Max(1, NightsEdgeTuning.ShadowArmorFadeTicks), 0f, 1f));
            }
            else
                armorLook = false;

            if (Player.whoAmI != Main.myPlayer)
                return;

            // The buff drives the whole armor, whoever gave it: the sword stays fully charged and its fade is paused for as long as it lasts, and the charge is
            // lost when it ends (the Light's Bane's rule); the owner spits a dark droplet every DropIntervalTicks (spawned projectiles, so tModLoader syncs them).
            bool armored = shadowLeft > 0;
            if (armored)
            {
                Charge = 1f;
                sinceHit = 0;
                hadArmor = true;
                if (++dropTimer >= NightsEdgeTuning.DropIntervalTicks)
                {
                    dropTimer = 0;
                    NightsEdgeDropProjectile.Throw(Player, 1);
                }
            }
            else
            {
                dropTimer = 0;
                if (hadArmor)
                {
                    hadArmor = false;
                    Charge = 0f;
                    sinceHit = 0;
                }
            }

            if (Player.dead || Player.HeldItem.type != ItemID.NightsEdge)
            {
                if (!armored)
                {
                    Charge = 0f;
                    sinceHit = 0;
                }
                return;
            }

            if (!armored)
            {
                sinceHit++;
                if (Charge > 0f && sinceHit > NightsEdgeTuning.FadeGraceTicks)
                    Charge = Math.Max(0f, Charge - 1f / Math.Max(1, NightsEdgeTuning.FadeDrainTicks));
            }

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