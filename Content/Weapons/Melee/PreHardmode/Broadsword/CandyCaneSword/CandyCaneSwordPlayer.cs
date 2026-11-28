using System.IO;
using IsmailsWeaponReworks.Common.Net;
using IsmailsWeaponReworks.Content.Buffs;
using IsmailsWeaponReworks.Common.Rework;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
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

        // The Grape buff's purple dye (see GrapeBuff): while it runs, EVERY dye slot of this player is the vanilla Purple Dye, armor, accessories and all the
        // misc slots (pet, light pet, hook, minecart, mount) alike, whatever dyes he really wears. No item is touched: Player.UpdateDyes recomputes all of
        // these from the real dye slots every tick, so the real dyes are back the tick the buff ends. This runs for every player on every machine and
        // vanilla syncs the buff list, so everybody sees everybody purple. (cYorai is copied from cPet right after this hook.)
        public override void UpdateDyes()
        {
            if (Main.dedServ || !Player.HasBuff(ModContent.BuffType<GrapeBuff>()))
                return;
            int dye = GameShaders.Armor.GetShaderIdFromItemId(ItemID.PurpleDye);
            Player.cHead = dye;
            Player.cBody = dye;
            Player.cLegs = dye;
            Player.cHandOn = dye;
            Player.cHandOff = dye;
            Player.cBack = dye;
            Player.cFront = dye;
            Player.cShoe = dye;
            Player.cWaist = dye;
            Player.cShield = dye;
            Player.cShieldFallback = dye;
            Player.cNeck = dye;
            Player.cFace = dye;
            Player.cFaceHead = dye;
            Player.cBeard = dye;
            Player.cBalloon = dye;
            Player.cBalloonFront = dye;
            Player.cWings = dye;
            Player.cCarpet = dye;
            Player.cFloatingTube = dye;
            Player.cBackpack = dye;
            Player.cTail = dye;
            Player.cFlameWaker = dye;
            Player.cPortableStool = dye;
            Player.cMinion = dye;
            Player.cPet = dye;
            Player.cLight = dye;
            Player.cMinecart = dye;
            Player.cMount = dye;
            Player.cGrapple = dye;
        }

        // Grape also cleanses: every debuff the player already has is removed (the buff makes him immune to NEW ones, see GrapeBuff.Update). Only the owner's
        // client does it; vanilla syncs the buff list from there. Backwards, because DelBuff shifts the later buffs down.
        public override void PostUpdateBuffs()
        {
            if (Player.whoAmI != Main.myPlayer || !Player.HasBuff(ModContent.BuffType<GrapeBuff>()))
                return;
            for (int i = Player.buffType.Length - 1; i >= 0; i--)
            {
                int type = Player.buffType[i];
                if (type > 0 && Main.debuff[type])
                    Player.DelBuff(i);
            }
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