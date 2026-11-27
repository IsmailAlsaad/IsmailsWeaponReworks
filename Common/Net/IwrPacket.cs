namespace IsmailsWeaponReworks.Common.Net
{
    /// <summary>Every custom packet of the mod. The first byte of every packet is one of these; add new ones at the END so the numbers never shift.</summary>
    public enum IwrPacket : byte
    {
        CandyPickupRequest, // client to server: "I touched this candy" (slot, owner, identity)
        CandyPickupGrant    // server to every client: "this candy went to this player" (picker, slot, owner, identity, kind)
    }
}