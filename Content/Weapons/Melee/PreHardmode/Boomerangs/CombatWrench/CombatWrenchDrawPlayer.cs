using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Boomerangs.CombatWrench
{
    /// <summary>
    /// While the thrown wrench is out, the player is drawn as if holding nothing, so the vanilla Combat Wrench that sits on the
    /// player's back disappears until it is caught. Visual only: every machine does it for every player, nothing is synced.
    /// </summary>
    public class CombatWrenchDrawPlayer : ModPlayer
    {
        readonly Item nothing = new Item();

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (Player.HeldItem.type == ItemID.CombatWrench && Player.ownedProjectileCounts[ModContent.ProjectileType<CombatWrenchProjectile>()] > 0)
                drawInfo.heldItem = nothing;
        }
    }
}