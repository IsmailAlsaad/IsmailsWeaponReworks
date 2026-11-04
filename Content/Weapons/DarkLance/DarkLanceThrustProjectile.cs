using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// The Dark Lance's THRUST (left click, every other click): one strong thrust toward the cursor, built on the shared held-spear core.
    /// Every orb the last sweep gathered makes it bigger (scale, reach and hit width, DarkLanceTuning.SizePerOrb) and stronger (the damage
    /// is set by the GlobalItem with DamagePerOrb); the gathered orbs show as a dark blob on the tip. With all MaxOrbs gathered the first enemy
    /// the tip hits triggers an evil explosion (DarkLanceBlastProjectile).
    /// SPAWN VALUES: ai[0] = seed (0..9999) + 10000 * gathered orbs (HeldSpearProjectile's Mode), ai[1] = 0, ai[2] = the aim in the player's own frame.
    /// MULTIPLAYER: see HeldSpearProjectile (the owner decides click, aim and hits; every machine mirrors the thrust from the projectile's age).
    /// The gathered count rides in the spawn, so every machine draws the same size. The explosion is a normal spawned projectile made by the owner.
    /// </summary>
    public class DarkLanceThrustProjectile : HeldSpearProjectile
    {
        int Gathered => Math.Min(Mode, DarkLanceTuning.MaxOrbs);
        float Size => 1f + DarkLanceTuning.SizePerOrb * Gathered;
        bool exploded; // owner only

        protected override int ItemType => ItemID.DarkLance;
        protected override ThrustChain Chain => DarkLanceTuning.ThrustBurst;
        protected override float Reach => DarkLanceTuning.ThrustReach * Size;
        protected override ThrustPalette Palette => DarkLanceTuning.Palette;
        protected override float SpearScale => DarkLanceTuning.ThrustScale * Size;
        protected override float GripFraction => DarkLanceTuning.ThrustGrip;
        protected override float HitWidth => DarkLanceTuning.ThrustHitWidth * Size;
        protected override bool RetargetEachThrust => false;
        protected override int TrailLifeTicks => DarkLanceTuning.ThrustTrailLifeTicks;
        protected override int TrailRails => DarkLanceTuning.ThrustTrailRails;
        protected override float TrailOpacity => DarkLanceTuning.ThrustTrailOpacity;
        protected override float PixelSize => DarkLanceTuning.PixelSize;
        protected override float TrailTaper => 0.6f;

        protected override float ThrustPitch(int index)
        {
            return base.ThrustPitch(index) - 0.04f * Gathered;
        }

        public override void AI()
        {
            base.AI();
            if (!Projectile.active || Main.dedServ)
                return;

            Owner.GetModPlayer<DarkLancePlayer>().LanceTip = Tip; // every machine
            DarkLanceEmbers.TipCharge(Tip, Gathered, Age / 60f);   // the gathered orbs, a living blob on the tip
        }

        protected override void OnStrikeStart(int index)
        {
            DarkLanceFx.ThrustStart(Tip, Gathered);
            if (IsOwner)
                CameraShake.Punch(Tip, 0.6f + 0.25f * Gathered, 10, "DarkLanceThrust");
        }

        protected override void OnTipStep(Vector2 tip, Vector2 direction, float extension)
        {
            if (Main.rand.NextBool(2))
                DarkLanceEmbers.Shed(tip - direction * 10f, -direction * 1.2f);
        }

        // Owner only (OnHitNPC runs on the owner's client): all orbs gathered = the first enemy hit blows up, once.
        protected override void OnTipHit(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (exploded || Gathered < DarkLanceTuning.MaxOrbs)
                return;
            exploded = true;
            int blastDamage = Math.Max(1, (int)(Projectile.damage * DarkLanceTuning.BlastDamageMultiplier));
            Vector2 point = SegmentHit.ClosestPoint(Butt, Tip, target.Center);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), point, Vector2.Zero, ModContent.ProjectileType<DarkLanceBlastProjectile>(), blastDamage, Projectile.knockBack, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool result = base.PreDraw(ref lightColor);
            float fill = Gathered / (float)Math.Max(1, DarkLanceTuning.MaxOrbs);
            float radius = DarkLanceTuning.TipGlowMinRadius + (DarkLanceTuning.TipGlowMaxRadius - DarkLanceTuning.TipGlowMinRadius) * (0.2f + 0.8f * fill) * Math.Max(0.3f, Extension);
            DarkLanceFx.DrawGlow(Tip, radius, DarkLanceTuning.TipGlowStrength * (0.5f + 0.5f * fill));
            return result;
        }
    }
}