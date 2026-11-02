using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Flamerang
{
    /// <summary>
    /// The molten boomerang the Flamerang turns into. It pierces everything and leaves a trail of fire behind it that sets enemies
    /// alight. It is spawned with an impulse (sideways at the end of the throw's range, or straight at the player by a contact explosion)
    /// and from then on a force pulls it toward the player's CURRENT position every tick (BoomerangFlight.StepForceReturn), so it flies a
    /// smooth arc that bends to wherever the player is now, and it is caught when it arrives.
    /// localAI[1] = spin direction. ai[] is unused.
    /// MULTIPLAYER: spawned by the owner's client (so synced); every machine runs the same force-based flight from the synced velocity,
    /// the owner applies the burning from the trail (NPC.AddBuff syncs itself). Every machine draws its own copy of the trail from where its copy has been;
    /// other players' hit sparks come from RemoteHitTracker.
    /// </summary>
    public class MoltenBoomerangProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flamarang;

        static Asset<Texture2D> art;

        Vector2 sweepStart;
        int ticks;
        PixelTrail trail;
        RemoteHitTracker hitTracker; // other machines only
        AfterimageTrail afterimages; // visual only, never synced

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Flamerang/MoltenBoomerangProjectile", out art);
        }

        public override void Unload()
        {
            art = null;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1; // infinite pierce
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = FlamerangTuning.MoltenHitCooldownTicks;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 half = Projectile.Size * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft() - half, targetHitbox.Size() + Projectile.Size, sweepStart, Projectile.Center);
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            bool isOwner = Projectile.owner == Main.myPlayer;
            trail ??= MoltenTrailStyle.Create(isOwner);

            if (!isOwner)
                RemoteHitSparks(); // needs last tick's sweepStart, so it runs first
            sweepStart = Projectile.Center;

            if (ticks == 0)
            {
                Projectile.localAI[1] = Projectile.velocity.X >= 0f ? 1f : -1f;
                FlamerangFx.MoltenBorn(Projectile.Center, Projectile.velocity.LengthSquared() > 0.01f ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX);
            }
            ticks++;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 60);
            if (ticks > FlamerangTuning.FlightMaxTicks)
            {
                Projectile.Kill();
                return;
            }

            Projectile.rotation += FlamerangTuning.SpinSpeed * Projectile.localAI[1];
            Lighting.AddLight(Projectile.Center, 1f, 0.55f, 0.15f);
            FlamerangFx.FlightDust(Projectile.Center, Projectile.velocity, 1f);

            // The trail: where this copy has been, recently (a PixelTrail). Every machine keeps its own for drawing and embers; only the
            // owner's burns enemies (MoltenTrailStyle.Update). When this projectile dies the trail is handed to DetachedTrails (OnKill).
            trail.Add(Projectile.Center);
            trail.Tick();

            // Pulled toward the player's current position by a force (the spawn velocity is the impulse), so it flies a smooth arc.
            float AdjustedAcceleration = MathHelper.Lerp(FlamerangTuning.ReturnAcceleration, FlamerangTuning.ReturnAcceleration * 30f, (float)ticks / (float)(FlamerangTuning.FlightMaxTicks * 0.1f));
            if (BoomerangFlight.StepForceReturn(Projectile, owner, AdjustedAcceleration, FlamerangTuning.ReturnMaxSpeed, FlamerangTuning.ReturnDrag, FlamerangTuning.CatchDistance))
            {
                FlamerangFx.Catch(Projectile.Center); // every machine catches its own copy
                Projectile.Kill();
            }
        }

        // Every machine: the trail outlives the boomerang and keeps fading (and burning, on the owner's machine) on its own.
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;
            DetachedTrails.Hand(afterimages);
            afterimages = null;
        }

        void RemoteHitSparks()
        {
            if (Main.dedServ || sweepStart == Vector2.Zero)
                return;
            hitTracker ??= new RemoteHitTracker();
            hitTracker.Update(Projectile, 0, npc => FlamerangFx.HitSparks(npc.Center));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            FlamerangFx.HitSparks(target.Center);
            FlamerangBurn.Apply(target);
        }

        // One afterimage per tick (visual only, never synced). Each carries its own lifetime: OnKill hands the trail to DetachedTrails,
        // so when this projectile dies the leftover afterimages fade out on their own instead of vanishing with it.
        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active)
                return;
            afterimages ??= AfterimageTrail.Sprite(art?.Value ?? TextureAssets.Projectile[Type].Value, 8, 0.7f);
            afterimages.Tick();
            afterimages.Add(Projectile.Center, Projectile.rotation, 1f, FlamerangTuning.FireColor * 0.6f);
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = FlamerangTuning.FireColor;

            afterimages?.Draw();

            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, position, null, glow * 0.55f, Projectile.rotation, origin, 1.4f, SpriteEffects.None, 0); // molten halo
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, FlamerangTuning.HotColor, 0.7f), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}