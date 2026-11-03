using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Trident
{
    /// <summary>
    /// The held Trident: one strong thrust (a one-thrust chain) with the dash on top. At the first strike tick the owner's client starts
    /// the dash toward the aim (PlayerDash); a hit with the tip bounces the player off the enemy, a tile in the way bounces them in
    /// DashPlayer. The liquid mode (ai[0]'s mode = 1, set by the GlobalItem so every machine agrees) picks the longer chain.
    /// Every machine draws the pose, the streak, the dust and the player's afterimages from the projectile's age; the dash itself is
    /// the owner's, and vanilla syncs the player's position.
    /// </summary>
    public class TridentThrustProjectile : HeldSpearProjectile
    {
        bool Liquid => Mode == 1;

        protected override int ItemType => ItemID.Trident;
        protected override ThrustChain Chain => Liquid ? TridentTuning.LiquidChain : TridentTuning.Chain;
        protected override float Reach => TridentTuning.Reach;
        protected override ThrustPalette Palette => TridentTuning.Palette;
        protected override float SpearScale => TridentTuning.SpearScale;
        protected override float GripFraction => TridentTuning.GripFraction;
        protected override float HitWidth => TridentTuning.HitWidth;
        protected override int TrailLifeTicks => TridentTuning.TrailLifeTicks;
        protected override int TrailRails => TridentTuning.TrailRails;
        protected override float TrailOpacity => TridentTuning.TrailOpacity;
        protected override float PixelSize => TridentTuning.PixelSize;
        protected override float TrailTaper => TridentTuning.TrailTaper;

        protected override float ThrustPitch(int index)
        {
            return -0.35f;
        }

        // Every machine, the tick the spear shoots out: the owner starts the dash, everyone shows the burst.
        protected override void OnStrikeStart(int index)
        {
            TridentFx.DashStart(Owner.MountedCenter, Direction, Liquid);
            if (IsOwner)
                PlayerDash.Begin(Owner, Direction, Liquid ? TridentTuning.LiquidDash : TridentTuning.Dash, TridentFx.Bounce);
        }

        protected override void OnTipStep(Vector2 tip, Vector2 direction, float extension)
        {
            if (Owner.velocity.LengthSquared() < 16f)
                return;
            PlayerDash.Afterimage(Owner);
            TridentFx.DashBody(Owner.Center, direction, Liquid);
        }

        // Owner only (OnHitNPC): the tip struck an enemy, so the player bounces off it.
        protected override void OnTipHit(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsOwner)
                return;
            PlayerDash.Bounce(Owner, BounceMath.NpcNormal(Owner.Center, target, Direction));
        }
    }
}