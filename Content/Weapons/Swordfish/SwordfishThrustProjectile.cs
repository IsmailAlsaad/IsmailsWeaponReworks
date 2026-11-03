using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Swordfish
{
    /// <summary>
    /// The held Swordfish: a burst of quick thrusts like the Spear's, or, when it was started in liquid (ai[0]'s mode = 1, set by the
    /// GlobalItem on the owner's client so every machine agrees), ENRAGED: an endless chain that goes on for as long as the button is
    /// held and the player is still in liquid. The owner decides at the start of each thrust whether another one follows; the chain's end
    /// age travels in ai[1] (see HeldSpearProjectile), so the rage needs no extra sync.
    /// </summary>
    public class SwordfishThrustProjectile : HeldSpearProjectile
    {
        bool Enraged => Mode == 1;

        protected override int ItemType => ItemID.Swordfish;
        protected override ThrustChain Chain => Enraged ? SwordfishTuning.RageChain : SwordfishTuning.Chain;
        protected override float Reach => SwordfishTuning.Reach;
        protected override ThrustPalette Palette => Enraged ? SwordfishTuning.RagePalette : SwordfishTuning.Palette;
        protected override float SpearScale => SwordfishTuning.SpearScale;
        protected override float GripFraction => SwordfishTuning.GripFraction;
        protected override float HitWidth => SwordfishTuning.HitWidth;
        protected override int TrailLifeTicks => SwordfishTuning.TrailLifeTicks;
        protected override int TrailRails => SwordfishTuning.TrailRails;
        protected override float TrailOpacity => SwordfishTuning.TrailOpacity;
        protected override float PixelSize => SwordfishTuning.PixelSize;

        // Owner only: keep going while the button is held and the player is still in liquid.
        protected override bool ShouldContinue(int index)
        {
            return Owner.controlUseItem && LiquidState.Touching(Owner);
        }

        protected override float ThrustPitch(int index)
        {
            return base.ThrustPitch(index) + (Enraged ? 0.2f : 0f);
        }

        protected override void OnThrustStart(int index)
        {
            if (Enraged && index == 0)
                SwordfishFx.RageBurst(Owner.MountedCenter);
        }

        protected override void OnTipStep(Vector2 tip, Vector2 direction, float extension)
        {
            if (Enraged)
                SwordfishFx.RageTip(tip, direction);
        }
    }
}