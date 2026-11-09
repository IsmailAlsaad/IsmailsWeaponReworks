using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Content.Weapons.EnchantedBoomerang;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The ethereal beam a full Enchanted Sword throws on a right click (patch 3): the vanilla Enchanted Beam sprite, stretched to
    /// EnchantedSwordTuning.GhostBladeLength, flying straight out of the tip. It stops at the first enemy it touches (one real hit of
    /// BeamDamageMultiplier) and the ghost sword (EtherealSwordProjectile) is conjured there; a wall or the end of its range just fizzles
    /// (the charge was spent at the throw and is NOT refunded). Projectile.Center is the beam's TIP; the hilt trails one blade length behind it.
    /// MULTIPLAYER: the owner's client spawns it (EtherealBeamProjectile.Spawn, a normal synced projectile with a straight-line flight, so
    /// nothing is synced per tick), decides the hit (OnHitNPC only runs there) and spawns the ghost; every machine plays the same trail,
    /// dust and end effects from its own copy. Damage is the owner's alone. No custom packets.
    /// </summary>
    public class EtherealBeamProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.EnchantedBeam;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        int ticks;
        bool hitDone;               // the first enemy has been hit: the ghost is conjured (only once)
        Vector2 lastMiddle;
        PixelTrail trail;
        Texture2D texture;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;          // it stops at the first enemy
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;         // set from the range on the first tick
            Projectile.usesLocalNPCImmunity = true; // another weapon's hit on the enemy must not make the beam pass through it
            Projectile.localNPCHitCooldown = -1;
        }

        // The owner's client throws it from the sword's tip.
        public static void Spawn(Player owner, Vector2 tip, Vector2 direction, int damage, float knockback, int crit, IEntitySource source)
        {
            Vector2 velocity = direction.SafeNormalize(Vector2.UnitX) * EnchantedSwordTuning.BeamSpeed;
            int index = Projectile.NewProjectile(source, tip, velocity, ModContent.ProjectileType<EtherealBeamProjectile>(), damage, knockback, owner.whoAmI);
            if (index >= 0 && index < Main.maxProjectiles)
                Main.projectile[index].CritChance = crit;
        }

        Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 Middle => Projectile.Center - Direction * (EnchantedSwordTuning.GhostBladeLength * 0.5f);

        public override void AI()
        {
            if (ticks == 0)
            {
                float speed = Math.Max(1f, Projectile.velocity.Length());
                Projectile.timeLeft = Math.Max(1, (int)Math.Ceiling(EnchantedSwordTuning.BeamRangeTiles * 16f / speed)); // the range
                lastMiddle = Middle;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (!Main.dedServ)
            {
                if (texture == null)
                {
                    Main.instance.LoadProjectile(ProjectileID.EnchantedBeam);
                    texture = TextureAssets.Projectile[ProjectileID.EnchantedBeam].Value;
                }

                // The trail is laid along the middle of the beam, with several points per tick so the pixel line stays smooth at this speed.
                trail ??= new PixelTrail(new EtherealBeamTrailStyle(), IsOwner);
                Vector2 middle = Middle;
                int samples = Math.Max(1, EnchantedSwordTuning.BeamTrailSamples);
                for (int k = 0; k < samples; k++)
                    trail.Add(Vector2.Lerp(lastMiddle, middle, (k + 1f) / samples));
                trail.Tick();
                lastMiddle = middle;

                if (Main.rand.NextFloat() < EnchantedSwordTuning.BeamDustChance)
                    EnchantedSwordFx.BeamDust(Projectile.Center, Projectile.velocity);
                Lighting.AddLight(Projectile.Center, 0.3f, 0.5f, 0.8f);
            }

            ticks++;
        }

        // ---- hits ----

        // The tip's path since the last step (a fast beam must not slip through a thin enemy).
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 end = Projectile.Center;
            return SegmentHit.Overlaps(targetHitbox, end - Projectile.velocity, end, EnchantedSwordTuning.BeamHitWidth);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = Projectile.velocity.X >= 0f ? 1 : -1;
            modifiers.SourceDamage *= EnchantedSwordTuning.BeamDamageMultiplier;
            modifiers.Knockback *= EnchantedSwordTuning.BeamKnockbackMultiplier;
        }

        // Owner's client only: the first enemy the beam touches takes the ghost. The ghost starts exactly where the beam is (its tip here, the same
        // direction of flight) and keeps flying through the enemy, spinning.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            EnchantedSwordFx.HitSpark(target.Center, 2f);
            if (hitDone)
                return;
            hitDone = true;
            CameraShake.Punch(target.Center, EnchantedSwordTuning.HitShake, 8);
            EtherealSwordProjectile.Spawn(Owner, target, Projectile.Center, Projectile.damage, Projectile.knockBack, Projectile.GetSource_FromThis(), (-Projectile.velocity).ToRotation());
        }

        // A wall: it fizzles (every machine simulates its own copy of the flight).
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            EnchantedSwordFx.BeamEnd(Projectile.Center, true);
            return true;
        }

        // The trail outlives the beam and keeps fading on its own (DetachedTrails). The end of the range (timeLeft 0) fizzles softly; an enemy
        // or a wall ends it with the puff only (the ghost and the hit spark make the rest).
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;
            EnchantedSwordFx.BeamEnd(Projectile.Center, timeLeft <= 0);

            // No enemy was hit (the ghost takes over then): a wall or the end of the range makes the sword explode into the Enchanted Boomerang's
            // stardust burst. The owner spawns it (a normal synced projectile, every machine plays the burst from its first tick).
            if (IsOwner && !hitDone)
            {
                int damage = Math.Max(1, (int)(Projectile.damage * EnchantedSwordTuning.BeamExplosionDamageMultiplier));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<StardustBurstProjectile>(), damage, Projectile.knockBack, Projectile.owner);
            }
        }

        // ---- drawing ----

        // The point of the sprite that sits on the hilt and that the sprite turns around (the same knobs as the ghost sword, so one fix does both).
        static Vector2 Pivot(Texture2D art)
        {
            return new Vector2(art.Width * EnchantedSwordTuning.GhostSpritePivotX, art.Height * EnchantedSwordTuning.GhostSpritePivotY);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ticks == 0 || texture == null)
                return false;

            trail?.Draw();

            float length = EnchantedSwordTuning.GhostBladeLength;
            Vector2 direction = Direction;
            Vector2 position = Projectile.Center - direction * length - Main.screenPosition; // the hilt
            float rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(EnchantedSwordTuning.GhostSpriteOffsetDeg);
            float scale = length / Math.Max(1f, texture.Height);
            float opacity = EnchantedSwordTuning.GhostOpacity;
            Vector2 origin = Pivot(texture);
            // The same look as the ghost sword: a soft blue glow grown from the middle of the sprite, then the sprite itself (additive).
            Vector2 middle = position + new Vector2((0.5f - EnchantedSwordTuning.GhostSpritePivotX) * texture.Width, (0.5f - EnchantedSwordTuning.GhostSpritePivotY) * texture.Height).RotatedBy(rotation) * scale;
            Vector2 glowOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            Main.EntitySpriteDraw(texture, middle, null, EnchantedSwordTuning.BlueColor * (0.5f * opacity), rotation, glowOrigin, scale * EnchantedSwordTuning.GhostGlowScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, new Color(255, 255, 255, 0) * opacity, rotation, origin, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
