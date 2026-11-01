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
    /// The Trimarang itself. OUTBOUND: flies toward the cursor direction and slows so that it stops exactly TrimarangTuning.HoldRange
    /// away (or at the first block). HOLD: sits there spinning faster and faster up to a cap, shredding everything in reach, for as
    /// long as the owner holds the button. RETURNING: starts the moment the button is released (or the item is swapped, or the player
    /// gets too far away) and ends with the catch. Every 5th hit it deals sends out a TrimarangEtherealProjectile.
    /// ai[0] = phase, ai[1] = range, ai[2] = distance flown (every machine adds its own); localAI[2] = age, localAI[1] = sound counter.
    /// MULTIPLAYER: the owner's client decides everything (stop, wall, release, hit counting, ethereal spawns) and syncs the phase with
    /// netUpdate. The spin speed is a pure function of the projectile's age, so every machine spins it the same without syncing it.
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
        RemoteHitTracker hitTracker; // other machines only

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
            Projectile.tileCollide = true;
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
            Projectile.tileCollide = CurrentPhase == Phase.Outbound;
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
                    Projectile.velocity = Vector2.Zero;
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
            float speed = Math.Max(TrimarangTuning.MinThrowSpeed, TrimarangTuning.ThrowSpeed * remaining / Math.Max(1f, range));
            speed = Math.Min(speed, remaining); // never overshoots the stop point
            Projectile.velocity = direction * speed;
            Projectile.ai[2] += speed;
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

        // A block stops it where it is; it then holds there like it would at the full range.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer && CurrentPhase == Phase.Outbound)
                EnterHold();
            return false;
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

        // The ethereal form never touches the enemy that triggered it (ai[0]); ai[1] = target (-1 none, -2 dying); ai[2] = spin speed.
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
            int damage = Math.Max(1, (int)(Projectile.damage * TrimarangTuning.EtherealDamageMultiplier));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, direction * TrimarangTuning.EtherealLaunchSpeed, type, damage, Projectile.knockBack, Projectile.owner, trigger.whoAmI, -1f, SpinSpeed);
        }

        // Swept circle: the whole path it moved along this tick, widened by the blade radius.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = TrimarangTuning.HitRadius;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft() - new Vector2(radius), targetHitbox.Size() + new Vector2(radius * 2f), sweepStart, Projectile.Center);
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = TrimarangTuning.MainColor;
            float spinT = SpinT;

            int length = ProjectileID.Sets.TrailCacheLength[Type];
            for (int i = length - 1; i >= 1; i--)
            {
                Vector2 old = Projectile.oldPos[i];
                if (old == Vector2.Zero)
                    continue;
                float fade = 1f - i / (float)length;
                Main.EntitySpriteDraw(texture, old + Projectile.Size * 0.5f - Main.screenPosition, null, glow * (0.4f * fade), Projectile.oldRot[i], origin, 0.8f + 0.2f * fade, SpriteEffects.None, 0);
            }

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