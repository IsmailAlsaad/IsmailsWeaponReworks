using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// The damage of a red candy's explosion: an invisible circle that lives a few ticks and hurts every enemy inside it once. Only the owner's client spawns it
    /// (CandyProjectile.Explode); the looks are CandyCaneSwordFx.RedExplosion, played by every machine from the candy itself. MULTIPLAYER: no custom packets.
    /// </summary>
    public class CandyBlastProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + Terraria.ID.ProjectileID.WoodenArrowFriendly;

        float Radius => CandyCaneSwordTuning.RedBlastRadius;

        public override void SetDefaults()
        {
            int size = (int)(CandyCaneSwordTuning.RedBlastRadius * 2f);
            Projectile.width = size;
            Projectile.height = size;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = CandyCaneSwordTuning.RedBlastTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
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

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}