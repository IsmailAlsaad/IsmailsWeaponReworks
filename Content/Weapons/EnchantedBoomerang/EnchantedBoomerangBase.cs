using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedBoomerang
{
    /// <summary>
    /// Flight code shared by the normal and the ethereal boomerang: out to a fixed range (or until a block), then back to the player,
    /// piercing every enemy on the way. A normal and an ethereal one that touch each other burst into stardust and bounce apart.
    /// ai[0] = phase, ai[1] = range, ai[2] = distance flown (every machine adds its own, only the owner decides the turn).
    /// MULTIPLAYER: the owner's client decides everything (turn, wall hit, collision, bounce, burst) and syncs it with ai[0] +
    /// velocity + netUpdate; both boomerangs belong to the same player, so one client sees both and no cross-client agreement is
    /// needed. Other machines simulate the same motion and visuals; their hit sparks come from RemoteHitTracker.
    /// </summary>
    public abstract class EnchantedBoomerangBase : ModProjectile
    {
        // The vanilla boomerang sprite for both (the ethereal one is drawn see-through). Drop your own PNGs next to this file, named
        // after the projectile class (EnchantedBoomerangProjectile.png / EtherealBoomerangProjectile.png), and they are used instead.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.EnchantedBoomerang;

        protected abstract bool Ethereal { get; }

        // Bounced flies exactly like Returning (the force-based return); it only marks that this one already bounced off its partner,
        // so every machine can play the bounce sparks once when the value arrives.
        enum Phase { Outbound = 0, Returning = 1, Bounced = 2 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];

        static Asset<Texture2D> normalArt;
        static Asset<Texture2D> etherealArt;

        // Per-machine state.
        Vector2 sweepStart;       // center at the start of this tick, for swept hit tests
        int lastPhase = -1;
        int phaseTimer;
        bool bounced;             // owner only: this one already bounced once in this throw
        RemoteHitTracker hitTracker; // other machines only

        public override void Load()
        {
            if (Main.dedServ)
                return;
            string path = "IsmailsWeaponReworks/Content/Weapons/EnchantedBoomerang/" + GetType().Name;
            if (Ethereal)
                ModContent.RequestIfExists<Texture2D>(path, out etherealArt);
            else
                ModContent.RequestIfExists<Texture2D>(path, out normalArt);
        }

        public override void Unload()
        {
            normalArt = null;
            etherealArt = null;
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
            Projectile.localNPCHitCooldown = EnchantedBoomerangTuning.HitCooldownTicks;
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

            // Both of these need last tick's start, so they run before sweepStart is overwritten.
            if (isOwner)
                TryCollideWithPartner();
            else
                RemoteHitSparks(CurrentPhase);

            Phase phase = CurrentPhase; // the pair test may have just started a bounce
            sweepStart = Projectile.Center;
            Projectile.tileCollide = phase == Phase.Outbound; // only the way out stops at blocks
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 60);

            if ((int)phase != lastPhase)
            {
                lastPhase = (int)phase;
                phaseTimer = 0;
                if (phase == Phase.Bounced)
                    EnchantedBoomerangFx.BounceSparks(Projectile.Center);
            }

            if (++Projectile.localAI[2] > EnchantedBoomerangTuning.FlightMaxTicks)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = (Projectile.velocity.X >= 0f ? 1f : -1f) * (Ethereal ? -1f : 1f); // spin direction
            Projectile.rotation += EnchantedBoomerangTuning.SpinSpeed * Projectile.localAI[1];

            Color glow = Ethereal ? EnchantedBoomerangTuning.EtherealColor : EnchantedBoomerangTuning.NormalColor;
            Lighting.AddLight(Projectile.Center, glow.R / 255f * 0.6f, glow.G / 255f * 0.6f, glow.B / 255f * 0.6f);
            EnchantedBoomerangFx.FlightDust(Projectile.Center, Projectile.velocity, Ethereal);

            switch (phase)
            {
                case Phase.Outbound:
                    AiOutbound(isOwner);
                    break;
                default: // Returning and Bounced
                    AiReturning(owner);
                    break;
            }
            phaseTimer++;
        }

        void AiOutbound(bool isOwner)
        {
            float range = Projectile.ai[1];
            float remaining = range - Projectile.ai[2];
            float speed = Projectile.velocity.Length();
            if (speed < 0.01f)
            {
                if (isOwner)
                    BeginReturn();
                return;
            }
            if (remaining < EnchantedBoomerangTuning.BrakeDistance)
            {
                float braked = Math.Max(EnchantedBoomerangTuning.BrakeMinSpeed, EnchantedBoomerangTuning.ThrowSpeed * Math.Max(0f, remaining) / EnchantedBoomerangTuning.BrakeDistance);
                if (speed > braked)
                {
                    Projectile.velocity *= braked / speed;
                    speed = braked;
                }
            }
            Projectile.ai[2] += speed; // every machine adds the same distance; only the owner decides when it is enough
            if (isOwner && Projectile.ai[2] >= range)
                BeginReturn();
        }

        void AiReturning(Player owner)
        {
            // Force-based: the pull toward the player is added to the velocity the boomerang already has (the braked throw, or the
            // sideways bounce impulse), so it swings round in one smooth arc instead of homing in a straight line.
            if (BoomerangFlight.StepForceReturn(Projectile, owner, EnchantedBoomerangTuning.ReturnAcceleration, EnchantedBoomerangTuning.ReturnMaxSpeed, EnchantedBoomerangTuning.ReturnDrag, EnchantedBoomerangTuning.CatchDistance))
            {
                EnchantedBoomerangFx.Catch(Projectile.Center, Ethereal); // every machine catches its own copy
                Projectile.Kill();
            }
        }

        // Owner only.
        void BeginReturn()
        {
            Projectile.ai[0] = (float)Phase.Returning;
            Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length); // enemies can be hit again on the way back
            Projectile.netUpdate = true;
        }

        // A block ends the way out; both kinds then turn back.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer && CurrentPhase == Phase.Outbound)
                BeginReturn();
            return false;
        }

        // ---- the two kinds meeting ----

        // Owner only, and only the normal boomerang runs it, so each pair is tested once. Relative motion over the last tick is a
        // line segment; the closest it gets to zero is the closest the two boomerangs came, so a fast pair cannot tunnel.
        void TryCollideWithPartner()
        {
            if (Ethereal || bounced || CurrentPhase == Phase.Bounced || sweepStart == Vector2.Zero)
                return;
            int partnerType = ModContent.ProjectileType<EtherealBoomerangProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.type != partnerType || other.owner != Projectile.owner)
                    continue;
                EnchantedBoomerangBase partner = other.ModProjectile as EnchantedBoomerangBase;
                if (partner == null || partner.bounced || partner.CurrentPhase == Phase.Bounced || partner.sweepStart == Vector2.Zero)
                    continue;

                Vector2 from = sweepStart - partner.sweepStart;
                Vector2 to = Projectile.Center - other.Center;
                Vector2 relative = to - from;
                float length2 = relative.LengthSquared();
                float t = length2 < 0.0001f ? 0f : MathHelper.Clamp(-Vector2.Dot(from, relative) / length2, 0f, 1f);
                if ((from + relative * t).Length() > 2f * EnchantedBoomerangTuning.CollideRadius)
                    continue;

                Vector2 contact = (Vector2.Lerp(sweepStart, Projectile.Center, t) + Vector2.Lerp(partner.sweepStart, other.Center, t)) * 0.5f;
                Vector2 apart = Projectile.Center - other.Center;
                BounceAway(apart);
                partner.BounceAway(-apart);
                int damage = Math.Max(1, (int)(Projectile.damage * EnchantedBoomerangTuning.BurstDamageMultiplier));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), contact, Vector2.Zero, ModContent.ProjectileType<StardustBurstProjectile>(), damage, Projectile.knockBack, Projectile.owner);
                return;
            }
        }

        // Owner only: a sideways IMPULSE, 90 degrees to the travel direction on the side that leads away from the other boomerang. There
        // is no separate bounce phase: the boomerang goes straight into the force-based return, and the pull toward the player bends
        // that impulse into one smooth arc.
        void BounceAway(Vector2 awayFromOther)
        {
            Vector2 direction = Projectile.velocity;
            if (direction.LengthSquared() < 0.01f)
                direction = Projectile.Center - sweepStart;
            direction = direction.LengthSquared() < 0.01f ? Vector2.UnitX : Vector2.Normalize(direction);
            Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
            if (Vector2.Dot(perpendicular, awayFromOther) < 0f)
                perpendicular = -perpendicular;

            Projectile.velocity = perpendicular * EnchantedBoomerangTuning.BounceSpeed;
            Projectile.ai[0] = (float)Phase.Bounced;
            Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length);
            bounced = true;
            Projectile.netUpdate = true;
        }

        // ---- hits ----

        void RemoteHitSparks(Phase phase)
        {
            if (Main.dedServ || sweepStart == Vector2.Zero)
                return;
            hitTracker ??= new RemoteHitTracker();
            hitTracker.Update(Projectile, (int)phase, npc => EnchantedBoomerangFx.HitSparks(npc.Center, Ethereal));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            EnchantedBoomerangFx.HitSparks(target.Center, Ethereal);
        }

        // Swept: the whole line it moved along this tick, so fast boomerangs cannot skip over an enemy.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 half = Projectile.Size * 0.5f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft() - half, targetHitbox.Size() + Projectile.Size, sweepStart, Projectile.Center);
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Ethereal ? etherealArt : normalArt)?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = Ethereal ? EnchantedBoomerangTuning.EtherealColor : EnchantedBoomerangTuning.NormalColor;

            // Afterimages.
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
            Color body = Ethereal ? Color.White * EnchantedBoomerangTuning.EtherealOpacity : Color.Lerp(lightColor, Color.White, 0.6f);
            Main.EntitySpriteDraw(texture, position, null, body, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}