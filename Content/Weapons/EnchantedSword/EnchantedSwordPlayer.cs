using IsmailsWeaponReworks.Common.Net;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The sword's charge. The count (hits so far) lives on the owner's machine only; what everyone needs is the outline STAGE (0 = none,
    /// 1 = after the first counted hit, 2 = after the second), which the owner sends to the others whenever it changes (a tiny packet,
    /// see WeaponNet) and again to a player who joins later. The third counted hit discharges: the owner spawns the ethereal sword (a
    /// normal synced projectile) and the charge drops to 0. The charge is never saved; it is lost when the sword is not held any more,
    /// on death, and after HitHoldTicks without a counted hit.
    /// </summary>
    public class EnchantedSwordPlayer : ModPlayer
    {
        public int Stage;           // 0..MaxStage, synced

        int hits;                   // owner only
        uint lastHitTick;           // owner only
        int swingSerial;            // owner only: grows by one for every new swing
        int countedSerial = -1;     // owner only: the swing that already counted a hit
        int lastItemAnimation;
        int shownStage;             // every machine: the stage the effects were last played for

        // Owner only (called from the GlobalItem's OnHitNPC, which only runs on the attacking player's own client). The first enemy a
        // swing touches adds one hit; the rest of that swing adds nothing.
        public void RegisterMeleeHit(Item item, NPC target)
        {
            if (countedSerial == swingSerial)
                return;
            countedSerial = swingSerial;
            hits++;
            lastHitTick = Main.GameUpdateCount;

            if (hits >= EnchantedSwordTuning.HitsToDischarge)
            {
                hits = 0;
                Stage = 0;
                Discharge(item, target);
            }
            else
            {
                Stage = Math.Min(hits, EnchantedSwordTuning.MaxStage);
            }
        }

        void Discharge(Item item, NPC target)
        {
            int damage = Math.Max(1, (int)(Player.GetWeaponDamage(item) * EnchantedSwordTuning.GhostDamageMultiplier));
            float knockback = Player.GetWeaponKnockback(item, item.knockBack) * EnchantedSwordTuning.GhostKnockbackMultiplier;

            // ai[0] = the enemy it attacks, ai[1] = the side it stands on (the sign: the side the player is on) and a random number
            // (the size, 1 to 2) that every machine turns into the same tilt and drift. Everything else follows from those two numbers.
            int side = Player.Center.X >= target.Center.X ? 1 : -1;
            float seed = side * (1f + Main.rand.NextFloat());
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), target.Center, Vector2.Zero, ModContent.ProjectileType<EtherealSwordProjectile>(),
                damage, knockback, Player.whoAmI, target.whoAmI, seed);
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                // A new swing starts when the animation counter jumps up (also when an auto-reuse swing follows the last one at once).
                if (Player.itemAnimation > lastItemAnimation)
                    swingSerial++;
                lastItemAnimation = Player.itemAnimation;

                bool holding = !Player.dead && Player.HeldItem.type == ItemID.EnchantedSword;
                bool expired = hits > 0 && Main.GameUpdateCount - lastHitTick > (uint)EnchantedSwordTuning.HitHoldTicks;
                if (!holding || expired)
                {
                    hits = 0;
                    Stage = 0;
                }
            }

            // Every machine plays the hit effects when the stage rises (the owner's own too, so there is one place for it).
            if (Stage > shownStage)
                EnchantedSwordFx.StageUp(Player, Stage);
            shownStage = Stage;
        }

        // ---- sync: only the stage ----

        void SendStage(int toWho, int fromWho)
        {
            ModPacket packet = WeaponNet.Begin(Mod, WeaponPacket.EnchantedSwordStage);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)Stage);
            packet.Send(toWho, fromWho);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SendStage(toWho, fromWho);
        }

        public override void CopyClientState(ModPlayer targetCopy)
        {
            ((EnchantedSwordPlayer)targetCopy).Stage = Stage;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            if (((EnchantedSwordPlayer)clientPlayer).Stage != Stage)
                SendStage(-1, Main.myPlayer);
        }

        // Called by WeaponNet. A client sends its own stage to the server, which passes it on to everyone else.
        public static void ReceiveStage(BinaryReader reader, int sender)
        {
            int index = reader.ReadByte();
            int stage = reader.ReadByte();
            if (Main.netMode == NetmodeID.Server)
                index = sender; // never trust the index in the packet
            if (index < 0 || index >= Main.maxPlayers || index == Main.myPlayer)
                return;
            EnchantedSwordPlayer other = Main.player[index].GetModPlayer<EnchantedSwordPlayer>();
            other.Stage = Math.Clamp(stage, 0, EnchantedSwordTuning.MaxStage);
            if (Main.netMode == NetmodeID.Server)
                other.SendStage(-1, sender);
        }
    }
}