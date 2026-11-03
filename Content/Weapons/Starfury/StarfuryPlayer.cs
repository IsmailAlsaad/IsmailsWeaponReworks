using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>
    /// The little bit of state the Starfury keeps between uses, on the owner's machine only (nothing here is synced: the owner decides
    /// and passes what the other machines need through the swing projectile's spawn values).
    /// </summary>
    public class StarfuryPlayer : ModPlayer
    {
        public int LastPhase = -1;       // StarfurySwingProjectile.Phase of the previous use, -1 = none yet
        public float LastEndArm;         // where the arm was when the previous use ended (a swing: relative to its aim; a call: facing-local)
        public uint LastEndTick;         // Main.GameUpdateCount when it ended
        public uint LastCallTick;        // when the last right click happened (the call cooldown counts from it)
        public float LastEndWorldArm;    // the same end pose as a world angle (a call starts from it, whatever the aim was)

        // The sword's charge: blade hits so far (0..StarfuryTuning.MaxHits), owner's machine only. Other machines learn it from the swing
        // and call projectiles (it travels in their ai[0]), which is the only time the sword is visible. It is spent by a tossed star
        // and lost when the player swaps to another item or dies; it is never saved.
        public int ChargeHits;

        // Owner only. Adds one hit; false when the sword is already fully charged.
        public bool AddHit()
        {
            if (ChargeHits >= StarfuryTuning.MaxHits)
                return false;
            ChargeHits++;
            return true;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || ChargeHits <= 0)
                return;
            if (Player.dead || Player.HeldItem.type != ItemID.Starfury)
                ChargeHits = 0;
        }
    }
}