using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Shroomerang
{
    /// <summary>
    /// The thrown Shroomerang: out to a fixed range (or until a block), then back to the player in a smooth force-based arc, piercing
    /// every enemy on the way. While it flies it drops glowing mushrooms (ShroomerangMushroomProjectile).
    /// ai[0] = phase (0 outbound, 1 returning), ai[1] = range, ai[2] = distance flown (every machine adds its own, only the owner
    /// decides the turn). localAI[1] = spin direction, localAI[2] = age in ticks.
    /// MULTIPLAYER: the owner's client decides the throw, the turn, a block hit and every mushroom it spawns (mushrooms are normal
    /// spawned projectiles, synced by tModLoader); the phase flips through netUpdate. Other machines run the same flight and visuals;
    /// their hit sparks come from RemoteHitTracker.
    /// </summary>
    public class ShroomerangProjectile : ModProjectile
    {
        // Vanilla art until a ShroomerangProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Shroomerang;

        static Asset<Texture2D> art;

        enum Phase { Outbound = 0, Returning = 1 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];

        Vector2 sweepStart;          // center at the start of this tick, for swept hit tests
        RemoteHitTracker hitTracker; // other machines only

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Shroomerang/ShroomerangProjectile", out art);
        }

        public override void Unload()
        {
            art = null;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1; // infinite pierce
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = ShroomerangTuning.HitCooldownTicks;
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
            Phase phase = CurrentPhase;

            if (!isOwner)
                RemoteHitSparks(phase); // needs last tick's sweepStart, so it runs first
            sweepStart = Projectile.Center;
            Projectile.tileCollide = phase == Phase.Outbound; // only the way out stops at blocks
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 60);

            if (++Projectile.localAI[2] > ShroomerangTuning.FlightMaxTicks)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = Projectile.velocity.X >= 0f ? 1f : -1f;
            Projectile.rotation += ShroomerangTuning.SpinSpeed * Projectile.localAI[1];

            Lighting.AddLight(Projectile.Center, 0.15f, 0.4f, 0.9f); // it glows blue, like the vanilla one
            ShroomerangFx.FlightDust(Projectile.Center, Projectile.velocity);

            if (phase == Phase.Outbound)
            {
                if (BoomerangFlight.StepOutbound(Projectile, Projectile.ai[1], ShroomerangTuning.ThrowSpeed, ShroomerangTuning.BrakeDistance, ShroomerangTuning.BrakeMinSpeed) && isOwner)
                    BeginReturn();
            }
            else if (BoomerangFlight.StepForceReturn(Projectile, owner, ShroomerangTuning.ReturnAcceleration, ShroomerangTuning.ReturnMaxSpeed, ShroomerangTuning.ReturnDrag, ShroomerangTuning.CatchDistance))
            {
                ShroomerangFx.Catch(Projectile.Center); // every machine catches its own copy
                Projectile.Kill();
                return;
            }

            if (isOwner)
                AiMushrooms();
        }

        // Owner only.
        void BeginReturn()
        {
            Projectile.ai[0] = (float)Phase.Returning;
            //Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length); // enemies can be hit again on the way back
            Projectile.netUpdate = true;
        }

        // Owner's client only: a new mushroom every SpawnIntervalTicks, in both directions of the flight.
        void AiMushrooms()
        {
            int age = (int)Projectile.localAI[2];
            if (age < ShroomerangTuning.FirstSpawnDelayTicks || (age - ShroomerangTuning.FirstSpawnDelayTicks) % ShroomerangTuning.SpawnIntervalTicks != 0)
                return;

            ShroomerangMushroomProjectile.EvictForNewMushroom(Projectile.owner);
            Vector2 velocity = Projectile.velocity * ShroomerangTuning.InheritVelocity + Main.rand.NextVector2Circular(ShroomerangTuning.ScatterSpeed, ShroomerangTuning.ScatterSpeed);
            int damage = Math.Max(1, (int)(Projectile.damage * ShroomerangTuning.BurstDamageMultiplier));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<ShroomerangMushroomProjectile>(),
                damage, Projectile.knockBack, Projectile.owner, 0f, Main.rand.NextBool() ? 1f : -1f);
        }

        // A block ends the way out and turns it back.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer && CurrentPhase == Phase.Outbound)
            {
                ShroomerangFx.SporePuff(Projectile.Center, 8, 3f, 1.1f);
                BeginReturn();
            }
            return false; // never die on a tile
        }

        void RemoteHitSparks(Phase phase)
        {
            if (Main.dedServ || sweepStart == Vector2.Zero)
                return;
            hitTracker ??= new RemoteHitTracker();
            hitTracker.Update(Projectile, (int)phase, npc => ShroomerangFx.HitSparks(npc.Center));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BeginReturn();
            ShroomerangFx.HitSparks(target.Center);
        }

        // Swept: the whole line it moved along this tick, so a fast boomerang cannot skip over an enemy.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 half = Projectile.Size * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft() - half, targetHitbox.Size() + Projectile.Size, sweepStart, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = ShroomerangTuning.GlowColor;

            int length = ProjectileID.Sets.TrailCacheLength[Type];
            for (int i = length - 1; i >= 1; i--)
            {
                Vector2 old = Projectile.oldPos[i];
                if (old == Vector2.Zero)
                    continue;
                float fade = 1f - i / (float)length;
                Main.EntitySpriteDraw(texture, old + Projectile.Size * 0.5f - Main.screenPosition, null, glow * (0.5f * fade), Projectile.oldRot[i], origin, 0.7f + 0.3f * fade, SpriteEffects.None, 0);
            }

            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, position, null, glow * 0.45f, Projectile.rotation, origin, 1.35f, SpriteEffects.None, 0); // halo
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.6f), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}