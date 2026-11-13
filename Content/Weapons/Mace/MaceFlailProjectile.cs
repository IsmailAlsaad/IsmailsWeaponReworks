using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Mace
{
    /// <summary>
    /// The Mace's flail head: the shared flail loop (Common/Flail/FlailProjectile.cs) plus the Mace's own parts: its numbers and effects,
    /// and the tier 3 special: the regular elliptical throw (apex at the cursor, reach 8 to 20 tiles), only much faster. The first enemy the
    /// head hits makes it EXPLODE into MaceShrapnelProjectiles (the vanilla head fractured into a 3 x 3 grid of pieces, thrown out and falling under gravity, each piercing 3 enemies before it breaks); the head
    /// is gone and only the chain whips back to the hand (a held button then restarts tier 1). A throw that hits nothing just arrives home
    /// and the loop ends (or restarts if the button is held).
    /// MULTIPLAYER: the owner's client decides everything (the shared loop's rules). Where the head is during the special is a pure function
    /// of the aim (ai[1] / ai[2]) and the ticks since the special began, so every machine flies the same arc with no per-tick sync; only
    /// the owner's client spawns the shards (normal synced projectiles) and ends the special (the retract is synced through ai[0]).
    /// </summary>
    public class MaceFlailProjectile : FlailProjectile
    {
        // Vanilla art until a MaceFlailProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Mace;

        protected override int ItemType => ItemID.Mace;
        protected override FlailTuning Tuning => MaceTuning.Instance;
        protected override FlailFx Fx => MaceFx.Instance;
        protected override bool SweepsPath => CurrentPhase == Phase.Charge || CurrentPhase == Phase.Special;  // the special's head is far faster than it is wide
        protected override bool HeadHidden => CurrentPhase == Phase.Retract;   // after the special the head is gone (exploded) or already at the hand

        static MaceTuning Mace => MaceTuning.Instance;

        float arcTicks;

        // ---- the special ----

        protected override void BeginSpecial(Player owner)
        {
            // The whole arc (out and back) is the perimeter of the regular throw's ellipse; it is flown at SpecialSpeed.
            float a = AimReach * 0.5f;
            float b = a * Tuning.EllipseWidthRatio;
            float perimeter = MathHelper.Pi * (3f * (a + b) - (float)Math.Sqrt((3f * a + b) * (a + 3f * b)));
            arcTicks = Math.Max(Mace.SpecialMinTicks, perimeter / Math.Max(1f, Mace.SpecialSpeed));
        }

        protected override void UpdateSpecial(Player owner)
        {
            float u = MathHelper.Clamp((phaseTimer + 1) / Math.Max(1f, arcTicks), 0f, 1f);
            Vector2 oldCenter = Projectile.Center;
            Projectile.Center = EllipsePoint(u * MathHelper.TwoPi, owner.MountedCenter);
            trail?.Add(Projectile.Center);
            Fx.FlightDust(Projectile.Center, Projectile.Center - oldCenter, 3);
            Lighting.AddLight(Projectile.Center, 0.7f, 0.75f, 0.9f);

            if (u >= 1f)
            {
                Projectile.Center = owner.MountedCenter;  // the whole arc flown without hitting anything: the head is home
                if (IsOwner)
                    EndSpecial();
            }
        }

        // ---- hits ----

        // Owner's client: the first enemy the special's head hits makes it explode into shrapnel; the head is gone and the chain whips back.
        protected override void OnFlailHit(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CurrentPhase != Phase.Special)
                return;

            int pieces = Math.Max(1, Mace.ShardColumns) * Math.Max(1, Mace.ShardRows);
            int damage = Math.Max(1, (int)(lockedDamage * Mace.ShardDamageMultiplier));
            float knockback = Projectile.knockBack * Mace.ShardKnockbackMultiplier;
            float gravDir = Owner.gravDir >= 0f ? 1f : -1f;
            float start = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < pieces; i++)
            {
                float angle = start + MathHelper.TwoPi * i / pieces + Main.rand.NextFloat(-0.2f, 0.2f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(Mace.ShardLaunchSpeedMin, Mace.ShardLaunchSpeedMax) + new Vector2(0f, -Mace.ShardLaunchUp * gravDir);
                // ai[0] = the enemy it exploded on (+1; its pieces leave it alone for a moment), ai[1] = which piece of the head this is, piece 0 plays the explosion effect.
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<MaceShrapnelProjectile>(),
                    damage, knockback, Projectile.owner, target.whoAmI + 1, i);
            }
            EndSpecial();
        }
    }
}