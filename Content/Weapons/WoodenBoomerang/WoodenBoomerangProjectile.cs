using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.WoodenBoomerang
{
    /// <summary>
    /// The thrown Wooden Boomerang: out to a fixed range (or until a block), then back to the player in a smooth force-based arc. The first
    /// enemy (or, on the way out, the first block) it hits breaks it in half: it becomes two WoodenBoomerangShardProjectiles that linger and hurt.
    /// ai[0] = phase (0 outbound, 1 returning, 2 broken), ai[1] = range, ai[2] = distance flown (every machine adds its own, only the
    /// owner decides the turn). localAI[1] = spin direction, localAI[2] = age in ticks.
    /// MULTIPLAYER: the owner's client decides the throw, the turn, a block hit and the break (OnHitNPC only runs there). The break flips
    /// ai[0] to Broken (synced through netUpdate), spawns the two shards (normal spawned projectiles, synced by tModLoader) and every
    /// machine removes its own copy on its next tick. Other machines run the same flight; the break shows through the shards.
    /// </summary>
    public class WoodenBoomerangProjectile : ModProjectile
    {
        // Vanilla art until a WoodenBoomerangProjectile.png exists next to this file (the shards are cut from the same art).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenBoomerang;

        static Asset<Texture2D> art;

        enum Phase { Outbound = 0, Returning = 1, Broken = 2 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];

        Vector2 sweepStart; // center at the start of this tick, for swept hit tests

        public static Texture2D Art()
        {
            return art?.Value ?? TextureAssets.Projectile[ModContent.ProjectileType<WoodenBoomerangProjectile>()].Value;
        }

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/WoodenBoomerang/WoodenBoomerangProjectile", out art);
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
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1; // the break (flagged and synced), not the pierce count, ends it
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        // A broken boomerang is only waiting for every machine to remove its copy.
        public override bool? CanDamage()
        {
            return CurrentPhase == Phase.Broken ? false : (bool?)null;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || CurrentPhase == Phase.Broken)
            {
                Projectile.Kill(); // broken: the shards already exist; every machine removes its own copy
                return;
            }
            bool isOwner = Projectile.owner == Main.myPlayer;
            Phase phase = CurrentPhase;

            sweepStart = Projectile.Center;
            Projectile.tileCollide = phase == Phase.Outbound || WoodenBoomerangTuning.BreakOnTilesWhileReturning; // by default only the way out meets blocks
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 60);

            if (++Projectile.localAI[2] > WoodenBoomerangTuning.FlightMaxTicks)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = Projectile.velocity.X >= 0f ? 1f : -1f;
            Projectile.rotation += WoodenBoomerangTuning.SpinSpeed * Projectile.localAI[1];
            if (Main.rand.NextBool(4))
                WoodenBoomerangFx.Trail(Projectile.Center, Projectile.velocity, 0.9f);

            if (phase == Phase.Outbound)
            {
                if (BoomerangFlight.StepOutbound(Projectile, Projectile.ai[1], WoodenBoomerangTuning.ThrowSpeed, WoodenBoomerangTuning.BrakeDistance, WoodenBoomerangTuning.BrakeMinSpeed) && isOwner)
                    BeginReturn();
            }
            else if (BoomerangFlight.StepForceReturn(Projectile, owner, WoodenBoomerangTuning.ReturnAcceleration, WoodenBoomerangTuning.ReturnMaxSpeed, WoodenBoomerangTuning.ReturnDrag, WoodenBoomerangTuning.CatchDistance))
            {
                WoodenBoomerangFx.Catch(Projectile.Center); // every machine catches its own copy
                Projectile.Kill();
            }
        }

        // Owner only.
        void BeginReturn()
        {
            Projectile.ai[0] = (float)Phase.Returning;
            Projectile.netUpdate = true;
        }

        // A block breaks it, like an enemy does. The owner's client decides; the shards fly back away from the block.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer && CurrentPhase != Phase.Broken)
                Break(-oldVelocity);
            return false; // never die on a tile: the break is flagged and synced
        }

        // Owner's client only. It must not be moved or killed here: the break is flagged (synced) and every machine removes its own copy.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
                Break(Projectile.velocity);
        }

        // Owner's client only: spawn the two shards, one to each side of 'direction', and flag the boomerang as broken.
        void Break(Vector2 direction)
        {
            if (CurrentPhase == Phase.Broken)
                return;

            direction = direction.SafeNormalize(Vector2.UnitX);
            int damage = Math.Max(1, (int)(Projectile.damage * WoodenBoomerangTuning.ShardDamageMultiplier));
            int shardType = ModContent.ProjectileType<WoodenBoomerangShardProjectile>();
            for (int half = 0; half < 2; half++)
            {
                float side = half == 0 ? -1f : 1f;
                Vector2 velocity = direction.RotatedBy(side * WoodenBoomerangTuning.ShardSpreadAngle) * WoodenBoomerangTuning.ShardSpeed + new Vector2(0f, -WoodenBoomerangTuning.ShardHop);
                // ai[0]: which half (0 = left, 1 = right), ai[1]: spin direction
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, shardType, damage, Projectile.knockBack, Projectile.owner, half, side);
            }

            Projectile.ai[0] = (float)Phase.Broken;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }

        // Swept: the whole line it moved along this tick, so a fast boomerang cannot skip over an enemy.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 half = Projectile.Size * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft() - half, targetHitbox.Size() + Projectile.Size, sweepStart, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (CurrentPhase == Phase.Broken)
                return false;

            Texture2D texture = Art();
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = WoodenBoomerangTuning.GlowColor;

            int length = ProjectileID.Sets.TrailCacheLength[Type];
            for (int i = length - 1; i >= 1; i--)
            {
                Vector2 old = Projectile.oldPos[i];
                if (old == Vector2.Zero)
                    continue;
                float fade = 1f - i / (float)length;
                Main.EntitySpriteDraw(texture, old + Projectile.Size * 0.5f - Main.screenPosition, null, glow * (0.35f * fade), Projectile.oldRot[i], origin, 0.7f + 0.3f * fade, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}