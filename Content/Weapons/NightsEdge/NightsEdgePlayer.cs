using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The Night's Edge's state on the owner's machine (nothing here is synced: the owner decides and the swing projectile carries what the
    /// other machines need in its spawn values). It remembers how the last swing ended (so the next one continues from there and
    /// alternates) and how many consecutive swings the sword has done (Charge, 0..ChargeSwings). The charge is lost when the player swaps
    /// to another item or dies; between swings the sword is not drawn, so nobody else needs to see it. Never saved.
    /// </summary>
    public class NightsEdgePlayer : ModPlayer
    {
        public int LastPhase = -1;       // NightsEdgeSwingProjectile.Phase of the previous swing, -1 = none yet
        public float LastEndArm;         // where the arm was when the previous swing ended (relative to its aim)
        public uint LastEndTick;         // Main.GameUpdateCount when it ended
        public int Charge;               // consecutive swings so far, 0..NightsEdgeTuning.ChargeSwings

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (Player.dead || Player.HeldItem.type != ItemID.NightsEdge)
            {
                Charge = 0;
                LastPhase = -1;
            }
        }
    }
}