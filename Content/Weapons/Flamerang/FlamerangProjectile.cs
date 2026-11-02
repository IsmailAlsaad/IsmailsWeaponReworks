using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Flamerang
{
    /// <summary>
    /// The thrown Flamerang. It flies at the cursor exactly like the Ice Boomerang (fixed range, braking gradually at the end) and
    /// pierces everything on the way. It never comes back as itself:
    /// - at the end of its range it spins down (SETTLING), then explodes in fire and sends two MoltenBoomerangProjectiles sideways,
    ///   which arc around and return to the player;
    /// - on the first enemy or tile it touches it explodes at once, throws ObsidianShardProjectiles that fall and linger, and
    ///   is replaced by one molten boomerang that returns straight to the player.
    /// ai[0] = phase (0 outbound, 1 settling, 2 spent), ai[1] = throw range in pixels, ai[2] = distance flown (every machine adds
    /// its own). localAI[0] = settle timer, localAI[1] = spin direction, localAI[2] = ticks since it left the outbound phase.
    /// MULTIPLAYER: the owner's client decides everything (stop, contact, explosion and every spawn) and flips ai[0] with netUpdate;
    /// the blast, the shards and the molten boomerangs are normal spawned projectiles, so tModLoader syncs them. Other machines run
    /// the same flight and the visuals; their hit sparks come from RemoteHitTracker.
    /// </summary>
    public class FlamerangProjectile : ModProjectile
    {
        // Vanilla art until a FlamerangProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flamarang;

        static Asset<Texture2D> art;

        enum Phase { Outbound = 0, Settling = 1, Spent = 2 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];

        Vector2 sweepStart;
        Vector2 flightDirection = Vector2.UnitX; // last flight direction, for the sideways arcs
        bool exploded;                           // owner only: the contact explosion already happened
        RemoteHitTracker hitTracker;             // other machines only

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Flamerang/FlamerangProjectile", out art);
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
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1; // infinite pierce
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600; // safety net only; the throw range ends it long before
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        // Settling is just a visual; the outbound flight hurts.
        public override bool? CanDamage()
        {
            return CurrentPhase != Phase.Outbound ? false : (bool?)null;
        }

        // Swept hit test, so a fast boomerang cannot skip over an enemy between two ticks.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (CurrentPhase != Phase.Outbound)
                return false;
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
            Phase phase = CurrentPhase;

            if (phase == Phase.Spent)
            {
                Projectile.Kill(); // the explosion already happened; every machine removes its own copy
                return;
            }

            if (!isOwner)
                RemoteHitSparks(phase); // needs last tick's sweepStart, so it runs first
            sweepStart = Projectile.Center;
            Projectile.tileCollide = phase == Phase.Outbound;

            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = Projectile.velocity.X >= 0f ? 1f : -1f;
            float spinDirection = Projectile.localAI[1];
            Lighting.AddLight(Projectile.Center, 0.9f, 0.5f, 0.15f);

            if (phase != Phase.Outbound)
            {
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 60);
                if (++Projectile.localAI[2] > FlamerangTuning.FlightMaxTicks)
                {
                    Projectile.Kill();
                    return;
                }
            }

            if (phase == Phase.Outbound)
                AiOutbound(isOwner, spinDirection);
            else
                AiSettling(isOwner, spinDirection);
        }

        // Same flight as the Ice Boomerang's outbound phase.
        void AiOutbound(bool isOwner, float spinDirection)
        {
            Projectile.rotation += FlamerangTuning.SpinSpeed * spinDirection;
            FlamerangFx.FlightDust(Projectile.Center, Projectile.velocity, 0.5f);

            float speed = Projectile.velocity.Length();
            if (speed >= 0.01f)
                flightDirection = Projectile.velocity / speed;
            float remaining = Projectile.ai[1] - Projectile.ai[2];

            // Gradual stop and distance flown: shared with the other fixed-range boomerangs (BoomerangFlight.StepOutbound).
            if (BoomerangFlight.StepOutbound(Projectile, Projectile.ai[1], FlamerangTuning.ThrowSpeed, FlamerangTuning.BrakeDistance, FlamerangTuning.BrakeMinSpeed) && isOwner)
            {
                if (speed >= 0.01f)
                    Projectile.Center += flightDirection * Math.Max(0f, remaining); // stop exactly at the end of the range
                BeginSettle();
            }
        }

        // Owner only.
        void BeginSettle()
        {
            Projectile.ai[0] = (float)Phase.Settling;
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0] = 0f;
            Projectile.netUpdate = true;
        }

        void AiSettling(bool isOwner, float spinDirection)
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float t = MathHelper.Clamp(Projectile.localAI[0] / FlamerangTuning.SettleTicks, 0f, 1f);
            Projectile.rotation += FlamerangTuning.SpinSpeed * (1f - t) * spinDirection;
            FlamerangFx.Gather(Projectile.Center, 34f * (1f - t) + 8f);

            if (Projectile.localAI[0] >= FlamerangTuning.SettleTicks)
            {
                if (isOwner)
                    ExplodeAtRangeEnd();
                Projectile.Kill(); // every machine ends its own copy on the same timeline
            }
        }

        // ---- the two explosions (owner only) ----

        void SpawnBlast(Vector2 center)
        {
            int damage = Math.Max(1, (int)(Projectile.damage * FlamerangTuning.BlastDamageMultiplier));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero, ModContent.ProjectileType<FlamerangBlastProjectile>(), damage, Projectile.knockBack, Projectile.owner);
        }

        // Nothing was hit: fire explosion, then two molten boomerangs fly off sideways and arc back to the player.
        void ExplodeAtRangeEnd()
        {
            Vector2 center = Projectile.Center;
            SpawnBlast(center);

            Vector2 sideways = new Vector2(-flightDirection.Y, flightDirection.X);
            int damage = Math.Max(1, (int)(Projectile.damage * FlamerangTuning.MoltenDamageMultiplier));
            int moltenType = ModContent.ProjectileType<MoltenBoomerangProjectile>();
            foreach (float side in new[] { 1f, -1f })
            {
                Vector2 velocity = sideways * side * FlamerangTuning.MoltenArcSpeed; // the sideways impulse; the molten boomerang's own force bends it back
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, velocity, moltenType, damage, Projectile.knockBack, Projectile.owner);
            }
        }

        // An enemy or a tile was touched: fire explosion with falling obsidian shards, and one molten boomerang flies straight back.
        void ExplodeOnContact(Vector2 contact)
        {
            if (exploded)
                return;
            exploded = true;
            SpawnBlast(contact);

            ObsidianShardProjectile.MakeRoom(Projectile.owner, FlamerangTuning.ShardCount); // at most ShardMaxAlive per player: the oldest break
            int shardType = ModContent.ProjectileType<ObsidianShardProjectile>();
            int shardDamage = Math.Max(1, (int)(Projectile.damage * FlamerangTuning.ShardDamageMultiplier));
            float spread = MathHelper.ToRadians(FlamerangTuning.ShardSpreadDeg);
            for (int i = 0; i < FlamerangTuning.ShardCount; i++)
            {
                float angle = -MathHelper.PiOver2 + Main.rand.NextFloat(-spread, spread); // fanned out around straight up
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(FlamerangTuning.ShardMinSpeed, FlamerangTuning.ShardMaxSpeed);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), contact, velocity, shardType, shardDamage, Projectile.knockBack * 0.3f, Projectile.owner);
            }

            Player owner = Main.player[Projectile.owner];
            Vector2 toOwner = owner.Center - Projectile.Center;
            Vector2 back = toOwner.LengthSquared() > 1f ? Vector2.Normalize(toOwner) : Vector2.UnitX;
            int moltenDamage = Math.Max(1, (int)(Projectile.damage * FlamerangTuning.MoltenDamageMultiplier));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, back * FlamerangTuning.ReturnStartSpeed, ModContent.ProjectileType<MoltenBoomerangProjectile>(), moltenDamage, Projectile.knockBack, Projectile.owner);

            Projectile.ai[0] = (float)Phase.Spent; // every machine removes its copy next tick
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer && CurrentPhase == Phase.Outbound)
            {
                Vector2 back = oldVelocity.LengthSquared() > 0.01f ? Vector2.Normalize(oldVelocity) : flightDirection;
                ExplodeOnContact(Projectile.Center - back * 10f); // slightly in front of the surface
            }
            return false; // never dies on a tile by itself
        }

        // ---- hits ----

        void RemoteHitSparks(Phase phase)
        {
            if (Main.dedServ || phase != Phase.Outbound || sweepStart == Vector2.Zero)
                return;
            hitTracker ??= new RemoteHitTracker();
            hitTracker.Update(Projectile, 0, npc => FlamerangFx.HitSparks(npc.Center));
        }

        // Only runs on the owner's client. It pierces, but the first enemy it touches makes it explode.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner != Main.myPlayer || CurrentPhase != Phase.Outbound)
                return;
            Rectangle box = target.Hitbox;
            Vector2 center = Projectile.Center;
            Vector2 contact = new Vector2(MathHelper.Clamp(center.X, box.Left, box.Right), MathHelper.Clamp(center.Y, box.Top, box.Bottom));
            FlamerangFx.HitSparks(contact);
            ExplodeOnContact(contact);
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            if (CurrentPhase == Phase.Spent)
                return false;
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float settleT = CurrentPhase == Phase.Settling ? MathHelper.Clamp(Projectile.localAI[0] / FlamerangTuning.SettleTicks, 0f, 1f) : 0f;
            float scale = MathHelper.Lerp(1f, 1.25f, settleT); // swells with heat before it bursts
            Color glow = Color.Lerp(FlamerangTuning.FireColor, FlamerangTuning.HotColor, settleT);

            int length = ProjectileID.Sets.TrailCacheLength[Type];
            for (int i = length - 1; i >= 1; i--)
            {
                Vector2 old = Projectile.oldPos[i];
                if (old == Vector2.Zero)
                    continue;
                float fade = 1f - i / (float)length;
                Main.EntitySpriteDraw(texture, old + Projectile.Size * 0.5f - Main.screenPosition, null, glow * (0.55f * fade), Projectile.oldRot[i], origin, scale * (0.6f + 0.4f * fade), SpriteEffects.None, 0);
            }

            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, position, null, glow * 0.5f, Projectile.rotation, origin, scale * 1.3f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.6f), Projectile.rotation, origin, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}