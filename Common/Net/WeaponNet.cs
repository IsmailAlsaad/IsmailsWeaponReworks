using IsmailsWeaponReworks.Content.Weapons.EnchantedSword;
using System.IO;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common.Net
{
    /// <summary>
    /// The mod's small net layer: one packet enum and one place that routes an incoming packet to the weapon that sent it. Mod.HandlePacket
    /// calls Handle. A weapon writes its packet with Begin (which writes the type byte first) and reads the rest itself. Added for the
    /// Enchanted Sword's outline stage (the first and so far only packet). Nothing here needs a SinglePlayer shortcut: packets are only
    /// ever sent from the ModPlayer sync hooks, which tModLoader only calls in multiplayer.
    /// </summary>
    public enum WeaponPacket : byte
    {
        EnchantedSwordStage
    }

    public static class WeaponNet
    {
        public static ModPacket Begin(Mod mod, WeaponPacket type)
        {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)type);
            return packet;
        }

        public static void Handle(BinaryReader reader, int whoAmI)
        {
            WeaponPacket type = (WeaponPacket)reader.ReadByte();
            switch (type)
            {
                case WeaponPacket.EnchantedSwordStage:
                    EnchantedSwordPlayer.ReceiveStage(reader, whoAmI);
                    break;
            }
        }
    }
}