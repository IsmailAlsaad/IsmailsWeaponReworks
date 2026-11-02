using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Trimarang
{
    /// <summary>
    /// The Trimarang itself. OUTBOUND: flies toward the cursor direction, weakly steering toward the cursor, and slows so that it stops
    /// exactly TrimarangTuning.HoldRange of flight path away. It passes through blocks. HOLD: spins there faster and faster up to a cap,
    /// shredding everything in reach, slowly drifting toward the cursor (never farther than the range from the player), for as
    /// long as the owner holds the button. RETURNING: starts the moment the button is released (or the item is swapped, or the player
    /// gets too far away) and ends with the catch. Every 5th hit it deals sends out a TrimarangEtherealProjectile.
    /// ai[0] = phase, ai[1] = range, ai[2] = distance flown (every machine adds its own); localAI[2] = age, localAI[1] = sound counter.
    /// MULTIPLAYER: the owner's client decides everything (stop, release, steering, hit counting, ethereal spawns) and syncs the phase
    /// with netUpdate. Main.MouseWorld exists only on the owner's client, so the owner steers and sends position and velocity through a
    /// throttled netUpdate (TrimarangTuning.SteerSyncTicks); the other machines follow the synced velocity in between. The spin speed
    /// is a pure function of the projectile's age, so every machine spins it the same without syncing it.
    /// Other players' hit sparks come from RemoteHitTracker.
    /// </summary>
    public class TrimarangProjectile : ModProjectile
    {
        // The vanilla sprite. A PNG named TrimarangProjectile.png next to this file replaces it automatically.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Trimarang;

        enum Phase { Outbound = 0, Hold = 1, Returning = 2 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        float Age => Projectile.localAI[2];
        float SpinT => MathHelper.Clamp(Age / Math.Max(1, TrimarangTuning.SpinRampTicks), 0f, 1f);
        float SpinSpeed => MathHelper.Lerp(TrimarangTuning.SpinStart, TrimarangTuning.SpinCap, SpinT * SpinT);

        static Asset<Texture2D> art;

        Vector2 sweepStart;
        int hitCount;              // owner only: hits dealt by this boomerang (the ethereal forms do not count)
        int etherealsSpawned;      // owner only: fans the ethereal forms out around the boomerang
        Vector2 lastSyncedVelocity;  // owner only: the velocity the last steering sync carried
        RemoteHitTracker hitTracker; // other machines only
        AfterimageTrail afterimages; // visual only, never synced

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Trimarang/TrimarangProjectile", out art);
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
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // passes through blocks in every phase
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = TrimarangTuning.HitCooldownSlow;
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!isOwner)
                RemoteHitSparks(CurrentPhase); // needs last tick's sweepStart, so it runs first
            sweepStart = Projectile.Center;

            Projectile.localAI[2]++;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 60);
            if (Age > TrimarangTuning.FlightMaxTicks)
            {
                Projectile.Kill();
                return;
            }

            // Spin: slow at first, faster and faster up to the cap. It also hits the same enemy more often the faster it spins.
            Projectile.rotation += SpinSpeed;
            Projectile.localNPCHitCooldown = (int)Math.Round(MathHelper.Lerp(TrimarangTuning.HitCooldownSlow, TrimarangTuning.HitCooldownFast, SpinT));
            Projectile.localAI[1]++;
            TrimarangFx.Spinning(Projectile.Center, SpinT, (int)Projectile.localAI[1]);
            Lighting.AddLight(Projectile.Center, 0.5f, 0.3f, 0.8f);

            // The owner decides when it comes back: the button was released, the item is no longer held, or the player is too far away.
            if (isOwner && CurrentPhase != Phase.Returning)
            {
                bool tooFar = Vector2.Distance(owner.Center, Projectile.Center) > TrimarangTuning.LeashDistance;
                if (!owner.controlUseItem || owner.HeldItem.type != ItemID.Trimarang || tooFar)
                    BeginReturn();
            }

            switch (CurrentPhase)
            {
                case Phase.Outbound:
                    AiOutbound(isOwner);
                    break;
                case Phase.Hold:
                    AiHold(owner, isOwner);
                    break;
                default:
                    if (BoomerangFlight.StepReturn(Projectile, owner, TrimarangTuning.ReturnStartSpeed, TrimarangTuning.ReturnAcceleration, TrimarangTuning.ReturnMaxSpeed, TrimarangTuning.CatchDistance))
                    {
                        TrimarangFx.Catch(Projectile.Center); // every machine catches its own copy
                        Projectile.Kill();
                    }
                    break;
            }
        }

        // The speed is proportional to the distance still to go, so it glides to a stop exactly at the range.
        void AiOutbound(bool isOwner)
        {
            float range = Projectile.ai[1];
            float remaining = Math.Max(0f, range - Projectile.ai[2]);
            Vector2 direction = Projectile.velocity.LengthSquared() > 0.0001f ? Vector2.Normalize(Projectile.velocity) : Vector2.UnitX;
            if (isOwner)
                direction = SteerTowardMouse(direction); // only the owner has the cursor; the distance logic below is unchanged
            float speed = Math.Max(TrimarangTuning.MinThrowSpeed, TrimarangTuning.ThrowSpeed * remaining / Math.Max(1f, range));
            speed = Math.Min(speed, remaining); // never overshoots the stop point
            Projectile.velocity = direction * speed;
            Projectile.ai[2] += speed;
            if (isOwner)
                SyncSteering();
            if (isOwner && remaining - speed <= TrimarangTuning.StopSnapDistance)
                EnterHold();
        }

        // Owner only.
        void EnterHold()
        {
            Projectile.ai[0] = (float)Phase.Hold;
            Projectile.velocity = Vector2.Zero;
            Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length);
            Projectile.netUpdate = true;
        }

        // Owner only.
        void BeginReturn()
        {
            Projectile.ai[0] = (float)Phase.Returning;
            Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length); // enemies can be hit again on the way back
            Projectile.netUpdate = true;
        }

        // Owner only. Bends the flight direction toward the cursor by at most SteerTurn radians per tick: weak on purpose, the throw
        // still mostly goes where it was thrown. Only bends the direction, so the stop distance is untouched.
        Vector2 SteerTowardMouse(Vector2 direction)
        {
            Vector2 toMouse = Main.MouseWorld - Projectile.Center;
            if (toMouse.LengthSquared() < 16f * 16f)
                return direction; // on top of the cursor: nothing to steer toward (and no jitter)
            float current = direction.ToRotation();
            float delta = MathHelper.WrapAngle(toMouse.ToRotation() - current);
            return (current + MathHelper.Clamp(delta, -TrimarangTuning.SteerTurn, TrimarangTuning.SteerTurn)).ToRotationVector2();
        }

        // HOLD: the owner lets it drift slowly toward the cursor and never farther than the range from the player (the outward part of
        // the motion is dropped, so at the range it slides along the circle). The other machines keep the last synced velocity.
        void AiHold(Player owner, bool isOwner)
        {
            if (!isOwner)
                return;
            Vector2 desired = Vector2.Zero;
            Vector2 toMouse = Main.MouseWorld - Projectile.Center;
            if (toMouse.Length() > TrimarangTuning.HoldSteerDeadzone)
                desired = Vector2.Normalize(toMouse) * TrimarangTuning.HoldSteerSpeed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, TrimarangTuning.HoldSteerSmoothing);

            Vector2 fromOwner = Projectile.Center - owner.Center;
            float distance = fromOwner.Length();
            if (distance >= Projectile.ai[1] - TrimarangTuning.StopSnapDistance && distance > 0.01f)
            {
                Vector2 outward = fromOwner / distance;
                float along = Vector2.Dot(Projectile.velocity, outward);
                if (along > 0f)
                    Projectile.velocity -= outward * along;
            }
            if (Projectile.velocity.LengthSquared() < 0.0025f)
                Projectile.velocity = Vector2.Zero;
            SyncSteering();
        }

        // Owner only. Throttled: the owner is the only one who knows where the cursor is, so it sends position and velocity every few
        // ticks (only when the velocity changed) and the other machines follow that motion in between.
        void SyncSteering()
        {
            if (Age % TrimarangTuning.SteerSyncTicks != 0f || Projectile.velocity == lastSyncedVelocity)
                return;
            lastSyncedVelocity = Projectile.velocity;
            Projectile.netUpdate = true;
        }

        // ---- hits ----

        void RemoteHitSparks(Phase phase)
        {
            if (Main.dedServ || sweepStart == Vector2.Zero)
                return;
            hitTracker ??= new RemoteHitTracker();
            hitTracker.Update(Projectile, (int)phase, npc => TrimarangFx.HitSparks(npc.Center));
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (CurrentPhase == Phase.Hold)
                modifiers.Knockback *= TrimarangTuning.HoldKnockbackMultiplier;
        }

        // Only runs on the owner's client, which is exactly who counts the hits and spawns the ethereal forms.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            TrimarangFx.HitSparks(target.Center);
            hitCount++;
            if (hitCount % TrimarangTuning.HitsPerEthereal == 0)
                SpawnEthereal(target);
        }

        // The ethereal form never touches the enemy that triggered it (ai[0]); ai[1] = target NPC (-1 none, -2 dying); ai[2] = spin speed.
        void SpawnEthereal(NPC trigger)
        {
            int type = ModContent.ProjectileType<TrimarangEtherealProjectile>();

            int alive = 0;
            Projectile oldest = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.type != type || other.owner != Projectile.owner || other.ai[1] == -2f)
                    continue;
                alive++;
                if (oldest == null || other.timeLeft < oldest.timeLeft)
                    oldest = other;
            }
            if (alive >= TrimarangTuning.EtherealMaxAlive && oldest != null)
                (oldest.ModProjectile as TrimarangEtherealProjectile)?.BeginDying();

            float angle = MathHelper.ToRadians(TrimarangTuning.GoldenAngleDeg) * etherealsSpawned++;
            Vector2 direction = angle.ToRotationVector2();
            int firstTarget = TrimarangEtherealProjectile.FindTarget(Projectile.Center, trigger.whoAmI, Projectile); // -1: it arcs back to the player
            int damage = Math.Max(1, (int)(Projectile.damage * TrimarangTuning.EtherealDamageMultiplier));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, direction * TrimarangTuning.EtherealLaunchSpeed, type, damage, Projectile.knockBack, Projectile.owner, trigger.whoAmI, firstTarget, SpinSpeed);
        }

        // Swept circle: the whole path it moved along this tick, widened by the blade radius.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = TrimarangTuning.HitRadius;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft() - new Vector2(radius), targetHitbox.Size() + new Vector2(radius * 2f), sweepStart, Projectile.Center);
        }

        // One afterimage per tick (visual only, never synced). Each carries its own lifetime: OnKill hands the trail to DetachedTrails,
        // so when this projectile dies the leftover afterimages fade out on their own instead of vanishing with it.
        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active)
                return;
            afterimages ??= AfterimageTrail.Sprite(art?.Value ?? TextureAssets.Projectile[Type].Value, 8, 0.8f);
            afterimages.Tick();
            afterimages.Add(Projectile.Center, Projectile.rotation, 1f, TrimarangTuning.MainColor * 0.4f);
        }

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(afterimages);
            afterimages = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = TrimarangTuning.MainColor;
            float spinT = SpinT;

            afterimages?.Draw();

            Vector2 position = Projectile.Center - Main.screenPosition;
            // Motion blur: ghost copies a little behind in the spin, stronger the faster it goes.
            for (int k = 1; k <= 3; k++)
                Main.EntitySpriteDraw(texture, position, null, glow * (0.45f * spinT / k), Projectile.rotation - k * SpinSpeed * 0.45f, origin, 1.05f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, glow * (0.25f + 0.3f * spinT), Projectile.rotation, origin, 1.3f, SpriteEffects.None, 0); // halo
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.6f), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}