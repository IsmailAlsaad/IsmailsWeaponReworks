using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The dark flame explosion of a fully charged swing: every enemy the blade hits bursts, and the round area hurts everything in it once
    /// for a few ticks and sets it on Shadowflame. Spawned by the owner at the spot the blade hit (so tModLoader syncs it); every machine plays
    /// the same burst and sound on its first tick. The owner's client detects the hits. No custom packets.
    /// </summary>
    public class NightsEdgeBlastProjectile : ModProjectile
    {
        // Never drawn (metaballs and dust only): any vanilla projectile texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Fireball;

        int ticks;

        float Radius => NightsEdgeTuning.BlastSize * 0.5f;

        public override void SetDefaults()
        {
            Projectile.width = NightsEdgeTuning.BlastSize;
            Projectile.height = NightsEdgeTuning.BlastSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = NightsEdgeTuning.BlastTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        public override void AI()
        {
            if (ticks == 0)
            {
                NightsEdgeFx.Blast(Projectile.Center, Radius);
                CameraShake.Punch(Projectile.Center, NightsEdgeTuning.BlastShake, 12, "NightsEdgeBlast");
            }
            float fade = Projectile.timeLeft / (float)System.Math.Max(1, NightsEdgeTuning.BlastTicks);
            Lighting.AddLight(Projectile.Center, 0.9f * fade, 0.3f * fade, 1.6f * fade);
            ticks++;
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float radius = Radius;
            return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
        }

        // Owner's client only (NPC.AddBuff syncs).
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, NightsEdgeTuning.BlastShadowflameTicks);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}