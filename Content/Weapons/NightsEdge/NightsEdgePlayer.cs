using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The Night's Edge's state on the owner's machine (nothing here is synced: the owner decides and the swing projectile carries what the
    /// other machines need in its spawn values). It remembers how the last swing ended (so the next one continues from there and
    /// alternates) and how many consecutive swings that hit an enemy the sword has done (Charge, 0..ChargeSwings). The charge is lost when the player swaps
    /// to another item or dies; between swings the owner's NightsEdgeStanceProjectile holds the sword out. Never saved.
    /// </summary>
    public class NightsEdgePlayer : ModPlayer
    {
        public int LastPhase = -1;       // NightsEdgeSwingProjectile.Phase of the previous swing, -1 = none yet
        public float LastEndArm;         // where the arm was when the previous swing ended (relative to its aim)
        public uint LastEndTick;         // Main.GameUpdateCount when it ended
        public float LastEndWorldArm;    // the arm and the blade of that swing's last pose as WORLD angles (the stance eases on from them)
        public float LastEndWorldBlade;
        public int Charge;               // consecutive swings that hit an enemy so far, 0..NightsEdgeTuning.ChargeSwings

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;
            if (Player.dead || Player.HeldItem.type != ItemID.NightsEdge)
            {
                Charge = 0;
                LastPhase = -1;
                return;
            }

            // Holding the sword: whenever neither a swing nor a stance exists, the sword is held out in its idle stance (a normal projectile).
            if (Player.noItems || Player.CCed)
                return;
            if (Find(Player.whoAmI, ModContent.ProjectileType<NightsEdgeSwingProjectile>()) != null || Find(Player.whoAmI, ModContent.ProjectileType<NightsEdgeStanceProjectile>()) != null)
                return;

            int facing = Player.direction >= 0 ? 1 : -1;
            bool afterSwing = LastPhase >= 0 && Main.GameUpdateCount - LastEndTick <= 6;
            float startArm = afterSwing ? LastEndWorldArm : NightsEdgeStanceProjectile.IdleWorldArm(facing);
            float startBlade = afterSwing ? LastEndWorldBlade : NightsEdgeStanceProjectile.IdleWorldBlade(facing);

            // A fully charged sword stays charged for a while after the last swing: the stance shows it for what is left.
            int chargeLeft = 0;
            if (LastPhase >= 0 && Charge >= Math.Max(1, NightsEdgeTuning.ChargeSwings))
                chargeLeft = Math.Max(0, NightsEdgeTuning.ChargedLingerTicks - (int)(Main.GameUpdateCount - LastEndTick));
            NightsEdgeStanceProjectile.Spawn(Player.GetSource_ItemUse(Player.HeldItem), Player, startArm, startBlade, chargeLeft);
        }

        // The owner's projectile of this type, or null.
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
    }
}