using System.IO;
using IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common.Net
{
    /// <summary>
    /// The mod's small net layer. Start opens a packet with its type byte; Handle (called from IsmailsWeaponReworks.HandlePacket, on the server and on the
    /// clients) reads the type byte and hands the rest to the feature that owns the packet. Every request helper runs the server step directly when
    /// netMode is SinglePlayer, so singleplayer and multiplayer share one code path.
    /// </summary>
    public static class IwrNet
    {
        public static ModPacket Start(IwrPacket type)
        {
            ModPacket packet = ModLoader.GetMod("IsmailsWeaponReworks").GetPacket();
            packet.Write((byte)type);
            return packet;
        }

        public static void Handle(BinaryReader reader, int whoAmI)
        {
            IwrPacket type = (IwrPacket)reader.ReadByte();
            switch (type)
            {
                case IwrPacket.CandyPickupRequest:
                    CandyPickup.ReadRequest(reader, whoAmI);
                    break;
                case IwrPacket.CandyPickupGrant:
                    CandyPickup.ReadGrant(reader);
                    break;
            }
        }
    }
}