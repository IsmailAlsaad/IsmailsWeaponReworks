using System.IO;
using IsmailsWeaponReworks.Common.Net;
using IsmailsWeaponReworks.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// Any player can pick up a green or yellow candy, and a candy can only be picked up once, so the SERVER decides:
    /// 1. The player's own client sees him touch the candy and calls Request (singleplayer runs the server step directly).
    /// 2. The server (Resolve) checks that the candy exists, is still free, can help that player and is near him, claims it, and tells every client (grant packet).
    /// 3. Every machine (Grant) plays the pickup and removes the candy; only the PICKER'S machine applies the effect to his own player (Apply):
    ///    green heals (Player.Heal), yellow adds Sugar Rush time (Player.AddBuff). Vanilla syncs the life and the buff list of a player from his own client.
    /// A candy is named by its slot, its owner and its identity, so a recycled slot is never taken by mistake.
    /// </summary>
    public static class CandyPickup
    {
        // Local player's client.
        public static void Request(Projectile candy)
        {
            int index = candy.whoAmI;
            int owner = candy.owner;
            int identity = candy.identity;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Resolve(Main.myPlayer, index, owner, identity);
                return;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            ModPacket packet = IwrNet.Start(IwrPacket.CandyPickupRequest);
            packet.Write((short)index);
            packet.Write((byte)owner);
            packet.Write((short)identity);
            packet.Send();
        }

        // Server: a client's request arrived (sender = the picker).
        public static void ReadRequest(BinaryReader reader, int sender)
        {
            short index = reader.ReadInt16();
            byte owner = reader.ReadByte();
            short identity = reader.ReadInt16();
            if (Main.netMode == NetmodeID.Server)
                Resolve(sender, index, owner, identity);
        }

        // Server (or singleplayer): the one place that says yes or no.
        static void Resolve(int picker, int index, int owner, int identity)
        {
            Projectile projectile = Find(index, owner, identity);
            if (projectile == null || !(projectile.ModProjectile is CandyProjectile candy))
                return;
            if (picker < 0 || picker >= Main.maxPlayers)
                return;
            Player player = Main.player[picker];
            if (!candy.CanBeClaimedBy(player))
                return;
            float reach = CandyCaneSwordTuning.PickupRange + CandyCaneSwordTuning.MagnetRange + CandyCaneSwordTuning.ServerReachSlack;
            if (Vector2.Distance(player.Center, projectile.Center) > reach)
                return;

            candy.Claimed = true;
            CandyKind kind = candy.Kind;
            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = IwrNet.Start(IwrPacket.CandyPickupGrant);
                packet.Write((byte)picker);
                packet.Write((short)index);
                packet.Write((byte)owner);
                packet.Write((short)identity);
                packet.Write((byte)kind);
                packet.Send();
            }
            Grant(picker, index, owner, identity, kind);
        }

        // Clients: the server gave a candy to a player.
        public static void ReadGrant(BinaryReader reader)
        {
            byte picker = reader.ReadByte();
            short index = reader.ReadInt16();
            byte owner = reader.ReadByte();
            short identity = reader.ReadInt16();
            CandyKind kind = (CandyKind)reader.ReadByte();
            Grant(picker, index, owner, identity, kind);
        }

        // Every machine (the server and singleplayer too): the candy is gone; the picker's own machine gets the effect.
        static void Grant(int picker, int index, int owner, int identity, CandyKind kind)
        {
            Projectile projectile = Find(index, owner, identity);
            if (projectile != null)
            {
                if (projectile.ModProjectile is CandyProjectile candy)
                    candy.Claimed = true;
                CandyCaneSwordFx.Pickup(projectile.Center, kind);
                projectile.Kill();
            }

            if (Main.dedServ || picker != Main.myPlayer)
                return;
            Apply(Main.LocalPlayer, kind);
        }

        static Projectile Find(int index, int owner, int identity)
        {
            if (index < 0 || index >= Main.maxProjectiles)
                return null;
            Projectile projectile = Main.projectile[index];
            if (!projectile.active || projectile.type != ModContent.ProjectileType<CandyProjectile>() || projectile.owner != owner || projectile.identity != identity)
                return null;
            return projectile;
        }

        // The picker's own machine only.
        static void Apply(Player player, CandyKind kind)
        {
            if (kind == CandyKind.Green)
            {
                int heal = System.Math.Min(CandyCaneSwordTuning.HealAmount, player.statLifeMax2 - player.statLife);
                if (heal > 0)
                    player.Heal(heal);
            }
            else if (kind == CandyKind.Yellow)
            {
                int type = ModContent.BuffType<SugarRushBuff>();
                int index = player.FindBuffIndex(type);
                int current = index >= 0 ? player.buffTime[index] : 0;
                player.AddBuff(type, System.Math.Min(CandyCaneSwordTuning.SugarRushMaxTicks, current + CandyCaneSwordTuning.SugarRushTicksPerCandy));
            }

            // The purple seed (see CandyCaneSwordPurple): a green or a yellow candy ALSO gives Grape, refreshed to GrapeTicks (it never stacks past that).
            if (CandyCaneSwordPurple.Active && (kind == CandyKind.Green || kind == CandyKind.Yellow))
                player.AddBuff(ModContent.BuffType<GrapeBuff>(), CandyCaneSwordTuning.GrapeTicks);
        }
    }
}