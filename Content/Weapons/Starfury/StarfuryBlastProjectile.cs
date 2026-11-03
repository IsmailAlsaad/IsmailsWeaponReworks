using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>
    /// The explosion of the 3rd-bat star: a round area that hurts every enemy in it once, for a few ticks. Spawned by the owner at the
    /// spot the star hit (so tModLoader syncs it), then every machine plays the same sparks, sound, light and camera shake on its first
    /// tick. The owner's client detects the hits. No custom packets.
    /// </summary>
    public class StarfuryBlastProjectile : ModProjectile
    {
        // Only the vanilla star sprite, stretched into a flash (no art needed).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Starfury;

        int ticks;

        public override void SetDefaults()
        {
            Projectile.width = StarfuryTuning.BlastSize;
            Projectile.height = StarfuryTuning.BlastSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = StarfuryTuning.BlastTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        public override void AI()
        {
            if (ticks == 0)
            {
                StarfuryFx.BlastBurst(Projectile.Center);
                StarfuryFx.PlayBlast(Projectile.Center);
                CameraShake.Punch(Projectile.Center, StarfuryTuning.BlastShake, 20, "StarfuryBlast");
            }
            float fade = Projectile.timeLeft / (float)System.Math.Max(1, StarfuryTuning.BlastTicks);
            Lighting.AddLight(Projectile.Center, StarfuryTuning.GenerationColor(StarfuryTuning.MaxBats).ToVector3() * 2.5f * fade);
            ticks++;
        }

        // A circle, not the square hitbox.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float radius = StarfuryTuning.BlastSize * 0.5f;
            return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float progress = 1f - Projectile.timeLeft / (float)System.Math.Max(1, StarfuryTuning.BlastTicks);
            float grow = 1f - (1f - progress) * (1f - progress); // ease-out: the flash opens fast and settles
            float diameter = StarfuryTuning.BlastSize * MathHelper.Lerp(0.4f, 1.1f, grow);
            float scale = diameter / texture.Width;
            float alpha = 1f - progress;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color hot = StarfuryTuning.GenerationColor(StarfuryTuning.MaxBats);
            Main.EntitySpriteDraw(texture, position, null, hot * (0.7f * alpha), progress * 2f, origin, scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, StarfuryTuning.CoreColor * (0.9f * alpha), -progress * 3f, origin, scale * 0.6f, SpriteEffects.None, 0);
            return false;
        }
    }
}