using System.IO;
using IsmailsWeaponReworks.Common.Net;
using IsmailsWeaponReworks.Common.Rework;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// Keeps the held blade alive: whenever the Candy Cane Sword is held (and its rework is on) and no CandyCaneSwordBladeProjectile exists, the owner's
    /// client spawns one. The blade removes itself when another item is held. Nothing here is synced: the projectile is a normal owner-spawned projectile.
    /// </summary>
    public class CandyCaneSwordPlayer : ModPlayer
    {
        /// <summary>
        /// The color of the candy the next lucky hit spawns, and so of the sword (its sprite, trail and sparks). The owner's client rolls it (at the start, and
        /// again after every candy it spawns); every other machine mirrors it through the CandyNext packet (SyncPlayer / SendClientChanges, so a joining player
        /// gets it too).
        /// </summary>
        public CandyKind NextCandy;

        bool rolled;

        public override void CopyClientState(ModPlayer targetCopy)
        {
            ((CandyCaneSwordPlayer)targetCopy).NextCandy = NextCandy;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            if (((CandyCaneSwordPlayer)clientPlayer).NextCandy != NextCandy)
                SyncPlayer(-1, Main.myPlayer, false);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = IwrNet.Start(IwrPacket.CandyNext);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)NextCandy);
            packet.Send(toWho, fromWho);
        }

        // The server (and every client) got a player's next candy. The server never trusts the player number in the packet and passes it on.
        public static void ReadNext(BinaryReader reader, int sender)
        {
            int player = reader.ReadByte();
            int kind = System.Math.Clamp((int)reader.ReadByte(), 0, 3);
            if (Main.netMode == NetmodeID.Server)
                player = sender;
            if (player < 0 || player >= Main.maxPlayers)
                return;

            CandyCaneSwordPlayer state = Main.player[player].GetModPlayer<CandyCaneSwordPlayer>();
            state.NextCandy = (CandyKind)kind;
            if (Main.netMode == NetmodeID.Server)
                state.SyncPlayer(-1, sender, false);
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI == Main.myPlayer && !rolled)
            {
                rolled = true;
                NextCandy = CandyKinds.Pick(Player);
            }
            if (Player.whoAmI != Main.myPlayer || Player.dead || Player.noItems || Player.CCed)
                return;
            Item item = Player.HeldItem;
            if (item.type != ItemID.CandyCaneSword || !ReworkToggle.Holds(Player, item.type))
                return;

            int type = ModContent.ProjectileType<CandyCaneSwordBladeProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == type)
                    return;
            }
            Projectile.NewProjectile(Player.GetSource_ItemUse(item), Player.MountedCenter, Vector2.Zero, type,
                Player.GetWeaponDamage(item), Player.GetWeaponKnockback(item, item.knockBack), Player.whoAmI);
        }
    }
}