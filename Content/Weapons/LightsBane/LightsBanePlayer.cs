using IsmailsWeaponReworks.Content.Buffs;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
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
        // Ticks of imbue left: the time left on LightsBaneImbueBuff (0 without it). The buff is the single source of truth, so right-clicking its
        // icon cancels the imbue, death clears it, and every machine sees it (vanilla syncs the buff list).
        public int ImbueTicks
        {
            get
            {
                int index = Player.FindBuffIndex(ModContent.BuffType<LightsBaneImbueBuff>());
                return index >= 0 ? Player.buffTime[index] : 0;
            }
        }
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

        // The imbue's look (visual only, set by LightsBaneImbueProjectile every tick on EVERY machine, so everyone sees it on everyone): true while
        // the player should be drawn in the Phase dye. It is reset every tick, so it is false the moment the imbue projectile is gone.
        public bool ImbueLook;

        public override void ResetEffects()
        {
            ImbueLook = false;
        }

        // While ImbueLook is on, every dye slot of the DRAWN player is overridden with the vanilla Phase dye (armor and accessories alike, whatever
        // dyes they really wear; no item is given or consumed). This runs on the drawn copy only, so nothing about the player's real dyes changes.
        // The imbue's afterimages are drawn through the same path, so they are Phase-dyed too while the look is on.
        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (!ImbueLook)
                return;
            int dye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.PhaseDye);
            drawInfo.cHead = dye;
            drawInfo.cBody = dye;
            drawInfo.cLegs = dye;
            drawInfo.cHandOn = dye;
            drawInfo.cHandOff = dye;
            drawInfo.cBack = dye;
            drawInfo.cFront = dye;
            drawInfo.cShoe = dye;
            drawInfo.cWaist = dye;
            drawInfo.cShield = dye;
            drawInfo.cNeck = dye;
            drawInfo.cFace = dye;
            drawInfo.cBalloon = dye;
            drawInfo.cBalloonFront = dye;
            drawInfo.cWings = dye;
            drawInfo.cCarpet = dye;
            drawInfo.cFloatingTube = dye;
            drawInfo.cBackpack = dye;
            drawInfo.cTail = dye;
        }

        public override void OnRespawn()
        {
            Charge = 0f;
        }

        // The imbue's speed boost: the owner's run speeds are scaled (vanilla syncs the position, so nobody else needs to know), easing out over the
        // last ImbueFadeTicks. It works in the air too, and it belongs to the player: swapping the sword away does not end it.
        public override void PostUpdateRunSpeeds()
        {
            int left = ImbueTicks;
            if (Player.whoAmI != Main.myPlayer || left <= 0)
                return;
            float ease = MathHelper.Clamp(left / (float)Math.Max(1, LightsBaneTuning.ImbueFadeTicks), 0f, 1f);
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