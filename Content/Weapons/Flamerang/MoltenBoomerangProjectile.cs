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
    /// alight. ARC (spawned sideways at the end of the throw's range): its velocity bends toward the player for a while, a hook-shaped
    /// path, then it RETURNS (homes back, accelerating) and is caught. A molten boomerang spawned by a contact explosion starts directly
    /// in RETURN.
    /// ai[0] = phase (0 arc, 1 return), ai[1] = turn direction (+1 or -1) for the arc, localAI[0] = age, localAI[1] = spin direction.
    /// MULTIPLAYER: spawned by the owner's client (so synced); the owner flips arc to return with netUpdate and applies the burning from
    /// the trail (NPC.AddBuff syncs itself). Every machine runs the same arc and draws its own copy of the trail from where its copy has been;
    /// other players' hit sparks come from RemoteHitTracker.
    /// </summary>
    public class MoltenBoomerangProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flamarang;

        static Asset<Texture2D> art;

        enum Phase { Arc = 0, Return = 1 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];

        Vector2 sweepStart;
        int ticks;
        PixelTrail trail;
        RemoteHitTracker hitTracker; // other machines only

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
                RemoteHitSparks(CurrentPhase); // needs last tick's sweepStart, so it runs first
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

            if (CurrentPhase == Phase.Arc)
            {
                // Bend the velocity toward the player a little every tick, then home straight back.
                Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[1] * FlamerangTuning.MoltenArcTurnRadians);
                if (isOwner && ticks >= FlamerangTuning.MoltenArcTicks)
                    BeginReturn();
            }
            else if (BoomerangFlight.StepReturn(Projectile, owner, FlamerangTuning.ReturnStartSpeed, FlamerangTuning.ReturnAcceleration, FlamerangTuning.ReturnMaxSpeed, FlamerangTuning.CatchDistance))
            {
                FlamerangFx.Catch(Projectile.Center); // every machine catches its own copy
                Projectile.Kill();
            }
        }

        // Owner only.
        void BeginReturn()
        {
            Projectile.ai[0] = (float)Phase.Return;
            Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length); // enemies can be hit again on the way back
            Projectile.netUpdate = true;
        }

        // Every machine: the trail outlives the boomerang and keeps fading (and burning, on the owner's machine) on its own.
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;
        }

        void RemoteHitSparks(Phase phase)
        {
            if (Main.dedServ || sweepStart == Vector2.Zero)
                return;
            hitTracker ??= new RemoteHitTracker();
            hitTracker.Update(Projectile, (int)phase, npc => FlamerangFx.HitSparks(npc.Center));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            FlamerangFx.HitSparks(target.Center);
            FlamerangBurn.Apply(target);
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = FlamerangTuning.FireColor;

            int length = ProjectileID.Sets.TrailCacheLength[Type];
            for (int i = length - 1; i >= 1; i--)
            {
                Vector2 old = Projectile.oldPos[i];
                if (old == Vector2.Zero)
                    continue;
                float fade = 1f - i / (float)length;
                Main.EntitySpriteDraw(texture, old + Projectile.Size * 0.5f - Main.screenPosition, null, glow * (0.6f * fade), Projectile.oldRot[i], origin, 0.7f + 0.3f * fade, SpriteEffects.None, 0);
            }

            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, position, null, glow * 0.55f, Projectile.rotation, origin, 1.4f, SpriteEffects.None, 0); // molten halo
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, FlamerangTuning.HotColor, 0.7f), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}