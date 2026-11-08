using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// The Light's Bane's charge (0 to 1) lives here, on the owner's machine: true melee hits fill it, it cools on its own while it is not
    /// full (a FULL charge stays), and it is lost when the player swaps to another item or dies (the Starfury's rule); it is never saved.
    /// It also keeps the held blade alive: whenever the sword is held and no LightsBaneBladeProjectile exists, it spawns one. The other
    /// machines learn the charge from the blade projectile (SendExtraAI), nothing here is synced.
    /// </summary>
    public class LightsBanePlayer : ModPlayer
    {
        public float Charge;
        public int ImbueTicks;   // ticks of imbue left (the owner's machine only; the imbue projectile shows it to everyone)
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

        public static void AddCharge(Player player, float amount)
        {
            LightsBanePlayer state = player.GetModPlayer<LightsBanePlayer>();
            state.Charge = MathHelper.Clamp(state.Charge + amount, 0f, 1f);
            state.sinceHit = 0;
        }

        public override void OnRespawn()
        {
            Charge = 0f;
            ImbueTicks = 0;
        }

        // The imbue's speed boost: the owner's run speeds are scaled (vanilla syncs the position, so nobody else needs to know), easing out over the
        // last ImbueFadeTicks. It works in the air too, and it belongs to the player: swapping the sword away does not end it.
        public override void PostUpdateRunSpeeds()
        {
            if (Player.whoAmI != Main.myPlayer || ImbueTicks <= 0)
                return;
            float ease = MathHelper.Clamp(ImbueTicks / (float)Math.Max(1, LightsBaneTuning.ImbueFadeTicks), 0f, 1f);
            float speed = MathHelper.Lerp(1f, LightsBaneTuning.ImbueSpeedMultiplier, ease);
            float accel = MathHelper.Lerp(1f, LightsBaneTuning.ImbueAccelMultiplier, ease);
            Player.maxRunSpeed *= speed;
            Player.accRunSpeed *= speed;
            Player.runAcceleration *= accel;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (Player.dead)
                ImbueTicks = 0;
            else if (ImbueTicks > 0)
                ImbueTicks--;

            if (Player.dead || Player.HeldItem.type != ItemID.LightsBane)
            {
                Charge = 0f;
                sinceHit = 0;
            }
            else
            {
                sinceHit++;
                if (Charge > 0f && Charge < LightsBaneTuning.ChargedAt && sinceHit > LightsBaneTuning.ChargeHoldTicks)
                    Charge = Math.Max(0f, Charge - LightsBaneTuning.ChargeDecayPerTick);
            }

            if (Player.dead || Player.noItems || Player.CCed || Player.HeldItem.type != ItemID.LightsBane)
                return;
            int type = ModContent.ProjectileType<LightsBaneBladeProjectile>();
            if (Find(Player.whoAmI, type) != null)
                return;
            Item item = Player.HeldItem;
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero, type,
                Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack), Player.whoAmI);
        }
    }
}