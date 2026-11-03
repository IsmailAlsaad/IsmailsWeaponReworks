using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.StormSpear
{
    /// <summary>
    /// The held Storm Spear: the Trident's one-thrust dash without the bounce. At the first strike tick the owner's client starts the
    /// dash (PlayerDash: a tile stops it, the player is immune for its whole length); the tip pierces every enemy once and Electrifies it.
    /// Every machine lays the lightning behind the player's body from the player's synced position (ArcBolt), so the trail needs no packets.
    /// </summary>
    public class StormSpearThrustProjectile : HeldSpearProjectile
    {
        Vector2 lastBody;
        bool hasLastBody;

        protected override int ItemType => ItemID.ThunderSpear;
        protected override ThrustChain Chain => StormSpearTuning.Chain;
        protected override float Reach => StormSpearTuning.Reach;
        protected override ThrustPalette Palette => StormSpearTuning.Palette;
        protected override float SpearScale => StormSpearTuning.SpearScale;
        protected override float GripFraction => StormSpearTuning.GripFraction;
        protected override float HitWidth => StormSpearTuning.HitWidth;
        protected override int TrailLifeTicks => StormSpearTuning.TrailLifeTicks;
        protected override int TrailRails => StormSpearTuning.TrailRails;
        protected override float TrailOpacity => StormSpearTuning.TrailOpacity;
        protected override float PixelSize => StormSpearTuning.PixelSize;

        protected override float ThrustPitch(int index)
        {
            return 0.2f;
        }

        protected override void OnStrikeStart(int index)
        {
            lastBody = Owner.Center;
            hasLastBody = true;
            StormSpearFx.DashStart(Owner.Center, Direction);
            if (IsOwner)
                PlayerDash.Begin(Owner, Direction, StormSpearTuning.Dash);
        }

        protected override void OnTipStep(Vector2 tip, Vector2 direction, float extension)
        {
            Vector2 body = Owner.Center;
            if (hasLastBody && Vector2.DistanceSquared(lastBody, body) > 16f)
            {
                PlayerDash.Afterimage(Owner);
                StormSpearFx.BodyTrail(lastBody, body, direction);
            }
            lastBody = body;
            hasLastBody = true;
        }

        // Owner only (OnHitNPC): every enemy the tip touches is Electrified (the buff is synced by NPC.AddBuff).
        protected override void OnTipHit(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsOwner)
                return;
            //target.AddBuff(BuffID.ShadowFlame, StormSpearTuning.ElectrifiedTicks);
            StormSpearFx.Zap(Tip, target.Center, true);//target.HasBuff(BuffID.Electrified));
        }
    }
}