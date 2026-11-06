using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.ThornChakram
{
    /// <summary>
    /// The thrown Thorn Chakram: out to a fixed range (or until a block), then back to the player in a smooth force-based arc, spitting a
    /// ring of 8 stingers from its outer circle every BurstIntervalTicks. The first enemy it hits makes it latch on: it sticks to the enemy,
    /// spins faster and faster and shreds it, and when the latch ends (or the enemy dies) it explodes into 8 piercing stingers.
    /// ai[0] = phase (0 outbound, 1 returning, 2 latched, 3 spent).
    /// ai[1] = outbound/returning: range in pixels; latched: index of the enemy it sits on, plus (distance of the contact point from the enemy's center) / 1024 as the fraction.
    /// ai[2] = outbound/returning: distance flown (every machine adds its own, only the owner decides the turn); latched: angle of the contact point seen from the enemy's center.
    /// localAI[1] = spin direction, localAI[2] = age in ticks. The latch timer is the field latchTicks.
    /// MULTIPLAYER: the owner's client decides the throw, the turn, a block hit, every stinger spawn (normal spawned projectiles, synced by
    /// tModLoader), the latch (OnHitNPC only runs there) and the explosion. The phase, the enemy and the side are synced through netUpdate;
    /// every machine then follows the enemy on its own copy and runs the same spin, dust and sound. The explosion flags the chakram Spent
    /// (synced) and every machine shows it and removes its own copy next tick. Other machines' hit sparks come from RemoteHitTracker.
    /// </summary>
    public class ThornChakramProjectile : ModProjectile
    {
        // Vanilla art until a ThornChakramProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ThornChakram;

        static Asset<Texture2D> art;

        enum Phase { Outbound = 0, Returning = 1, Latched = 2, Spent = 3 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        bool InFlight => CurrentPhase == Phase.Outbound || CurrentPhase == Phase.Returning;

        Vector2 sweepStart;          // center at the start of this tick, for swept hit tests
        RemoteHitTracker hitTracker; // other machines only
        int latchTicks;              // ticks since the latch began on this machine (the owner's copy decides when it ends)
        int spentTicks;              // ticks since this machine saw the explosion (times the second ring)
        AfterimageTrail afterimages; // visual only, never synced
        bool latchEffectPlayed;      // visual only

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/ThornChakram/ThornChakramProjectile", out art);
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
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = ThornChakramTuning.HitCooldownTicks;
        }

        // A spent chakram is only waiting for every machine to remove its copy.
        public override bool? CanDamage()
        {
            return CurrentPhase == Phase.Spent ? false : (bool?)null;
        }

        // While it shreds, every hit is a small one and does not knock the enemy off the chakram.
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (CurrentPhase == Phase.Latched)
            {
                modifiers.SourceDamage *= ThornChakramTuning.ShredDamageMultiplier;
                modifiers.DisableKnockback();
            }
        }

        // Swept while it flies, so a fast chakram cannot skip over an enemy; while latched the plain hitbox is used.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (CurrentPhase == Phase.Latched)
                return null;
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
                // The first ring was spawned by Explode. Every machine counts its own ticks: the owner spawns the second ring (turned half
                // a step) when the delay is up, everyone shows the explosion again, and then every machine removes its own copy.
                if (spentTicks == 0)
                    ExplosionVisuals();
                if (spentTicks == ThornChakramTuning.ExplosionSecondDelayTicks)
                {
                    if (isOwner)
                        SpawnStingers(ThornChakramTuning.ExplosionCount, 0f, ThornChakramTuning.ExplosionSpeed, ThornChakramTuning.ExplosionStingerLifeTicks,
                            ThornChakramTuning.ExplosionDamageMultiplier, true, (int)Projectile.ai[1], MathHelper.Pi / Math.Max(1, ThornChakramTuning.ExplosionCount));
                    ExplosionVisuals();
                }
                if (++spentTicks > ThornChakramTuning.ExplosionSecondDelayTicks)
                    Projectile.Kill(); // every machine removes its own copy
                return;
            }

            if (!isOwner)
                RemoteHitSparks(phase); // needs last tick's sweepStart, so it runs first
            sweepStart = Projectile.Center;
            Projectile.tileCollide = phase == Phase.Outbound; // only the way out stops at blocks
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 60);

            if (++Projectile.localAI[2] > ThornChakramTuning.FlightMaxTicks)
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = Projectile.velocity.X >= 0f ? 1f : -1f;

            if (phase == Phase.Latched)
            {
                AiLatched(isOwner);
                return;
            }

            Projectile.rotation += ThornChakramTuning.SpinSpeed * Projectile.localAI[1];
            Lighting.AddLight(Projectile.Center, 0.1f, 0.35f, 0.1f);
            if (Main.rand.NextBool(3))
                ThornChakramFx.Trail(Projectile.Center, Projectile.velocity, 1f);

            if (phase == Phase.Outbound)
            {
                if (BoomerangFlight.StepOutbound(Projectile, Projectile.ai[1], ThornChakramTuning.ThrowSpeed, ThornChakramTuning.BrakeDistance, ThornChakramTuning.BrakeMinSpeed) && isOwner)
                    BeginReturn();
            }
            else if (BoomerangFlight.StepForceReturn(Projectile, owner, ThornChakramTuning.ReturnAcceleration, ThornChakramTuning.ReturnMaxSpeed, ThornChakramTuning.ReturnDrag, ThornChakramTuning.CatchDistance))
            {
                ThornChakramFx.Catch(Projectile.Center); // every machine catches its own copy
                Projectile.Kill();
                return;
            }

            if (isOwner)
                AiBursts();
        }

        // Owner only.
        void BeginReturn()
        {
            Projectile.ai[0] = (float)Phase.Returning;
            Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length); // enemies can be hit again on the way back
            Projectile.netUpdate = true;
        }

        // Latched: it sits on the enemy (on the side it came from), spinning faster and faster, shredding it every ShredIntervalTicks.
        // When the time is up, or the enemy is gone, the owner's client makes it explode.
        void AiLatched(bool isOwner)
        {
            Projectile.tileCollide = false;
            Projectile.velocity = Vector2.Zero;

            latchTicks++;
            float t = MathHelper.Clamp(latchTicks / (float)ThornChakramTuning.LatchTicks, 0f, 1f);
            Projectile.localNPCHitCooldown = (int)MathHelper.Lerp(ThornChakramTuning.ShredIntervalTicks, ThornChakramTuning.ShredIntervalTicks * 3f,t);
            Projectile.rotation += ThornChakramTuning.SpinSpeed * MathHelper.Lerp(ThornChakramTuning.LatchSpinMaxMultiplier, 0f, t) * Projectile.localAI[1];
            Lighting.AddLight(Projectile.Center, 0.15f + 0.2f * t, 0.45f + 0.2f * t, 0.15f);

            if (!latchEffectPlayed)
            {
                latchEffectPlayed = true;
                ThornChakramFx.PlayLatch(Projectile.Center);
                ThornChakramFx.Puff(Projectile.Center, 10, 3.5f, 1.2f);
            }

            int index = (int)Projectile.ai[1];
            NPC npc = index >= 0 && index < Main.maxNPCs ? Main.npc[index] : null;
            if (npc == null || !npc.active)
            {
                if (isOwner)
                    Explode(); // the enemy died (or vanished): no reason to wait
                return;
            }

            Projectile.Center = npc.Center + LatchOffsetVector();

            if (latchTicks % ThornChakramTuning.ShredIntervalTicks == 0)
            {
                ThornChakramFx.Shred(Projectile.Center, 3 + (int)(4f * t));
                ThornChakramFx.PlayShred(Projectile.Center, t);
            }

            if (isOwner && latchTicks >= ThornChakramTuning.LatchTicks)
                Explode();
        }

        // Where it sits on the enemy, relative to the enemy's center: the point where it touched it (see OnHitNPC for the encoding).
        Vector2 LatchOffsetVector()
        {
            float length = (Projectile.ai[1] - (int)Projectile.ai[1]) * 1024f;
            return Projectile.ai[2].ToRotationVector2() * length;
        }

        // Owner's client: a ring of stingers around the circle, a new one every BurstIntervalTicks while it flies. They start on the
        // outer circle and fly straight outward; the ring turns with the chakram, so the stingers leave from where its blades are.
        void AiBursts()
        {
            int age = (int)Projectile.localAI[2];
            if (age < ThornChakramTuning.BurstFirstDelayTicks || (age - ThornChakramTuning.BurstFirstDelayTicks) % ThornChakramTuning.BurstIntervalTicks != 0)
                return;
            SpawnStingers(ThornChakramTuning.BurstCount, ThornChakramTuning.BurstRadius, ThornChakramTuning.BurstSpeed,
                ThornChakramTuning.BurstStingerLifeTicks, ThornChakramTuning.BurstDamageMultiplier, false);
        }

        void SpawnStingers(int count, float radius, float speed, int lifeTicks, float damageMultiplier, bool piercing, int ignoreNpc = -1, float angleOffset = 0f)
        {
            int damage = Math.Max(1, (int)(Projectile.damage * damageMultiplier));
            int type = ModContent.ProjectileType<ThornChakramStingerProjectile>();
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (Projectile.rotation + angleOffset + MathHelper.TwoPi * i / count).ToRotationVector2();
                // ai[0]: 1 = piercing, ai[1]: life in ticks, ai[2]: 2 * (index of the one enemy it must not hurt + 1) + 1 if this one plays the burst sound (one per ring)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + direction * radius, direction * speed, type, damage, Projectile.knockBack,
                    Projectile.owner, piercing ? 1f : 0f, lifeTicks, (i == 0 ? 1f : 0f) + 2f * (ignoreNpc + 1));
            }
        }

        // Owner's client: the latch is over. The chakram is flagged Spent (synced) instead of being killed on the spot, so every machine
        // shows the explosion and removes its own copy next tick.
        void Explode()
        {
            if (CurrentPhase == Phase.Spent)
                return;
            SpawnStingers(ThornChakramTuning.ExplosionCount, 0f, ThornChakramTuning.ExplosionSpeed,
                ThornChakramTuning.ExplosionStingerLifeTicks, ThornChakramTuning.ExplosionDamageMultiplier, true, (int)Projectile.ai[1]); // never hurts the enemy it just shredded
            Projectile.ai[0] = (float)Phase.Spent;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }

        void ExplosionVisuals()
        {
            ThornChakramFx.PlayExplosion(Projectile.Center);
            ThornChakramFx.Puff(Projectile.Center, 28, 6f, 1.5f);
            ThornChakramFx.Ring(Projectile.Center, 24, 12f, 6f);
            ThornChakramFx.Ring(Projectile.Center, 16, 30f, 3f);
        }

        // A block ends the way out and turns it back.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer && CurrentPhase == Phase.Outbound)
            {
                ThornChakramFx.Puff(Projectile.Center, 6, 3f, 1f);
                BeginReturn();
            }
            return false; // never die on a tile
        }

        // Owner's client only. The first enemy it hits in flight makes it latch on. It must not be moved or killed here: the new phase
        // is flagged (synced) and every machine then follows the enemy itself.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            if (InFlight)
            {
                // It latches on where it touched the enemy. The offset from the enemy's center is synced as ai[1] = enemy index + length / 1024
                // (the fraction) and ai[2] = angle, so every machine puts the chakram at the same spot of the enemy.
                Vector2 offset = Contact.EntryPoint(sweepStart, Projectile.Center, target.Hitbox) - target.Center;
                float length = Math.Min(offset.Length(), 1000f);
                Projectile.ai[0] = (float)Phase.Latched;
                Projectile.ai[1] = target.whoAmI + length / 1024f;
                Projectile.ai[2] = length > 0.01f ? offset.ToRotation() : 0f;
                Projectile.localNPCHitCooldown = ThornChakramTuning.ShredIntervalTicks;
                Projectile.localNPCImmunity[target.whoAmI] = ThornChakramTuning.ShredIntervalTicks;
                Projectile.netUpdate = true;
            }
            ThornChakramFx.Shred(target.Center, 2);
        }

        void RemoteHitSparks(Phase phase)
        {
            if (Main.dedServ || sweepStart == Vector2.Zero)
                return;
            hitTracker ??= new RemoteHitTracker();
            // While latched the segment id changes every shred interval, so each shred sparks once, like the owner's hits.
            int segment = phase == Phase.Latched ? 10 + latchTicks / Math.Max(1, ThornChakramTuning.ShredIntervalTicks) : (int)phase;
            hitTracker.Update(Projectile, segment, npc => ThornChakramFx.Shred(npc.Center, 2));
        }

        // One afterimage per tick while it flies (visual only, never synced). OnKill hands the trail to DetachedTrails, so the
        // leftovers fade out on their own when the chakram is caught, explodes or dies.
        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active)
                return;
            afterimages ??= AfterimageTrail.Sprite(art?.Value ?? TextureAssets.Projectile[Type].Value, ThornChakramTuning.AfterimageTicks, 0.7f);
            afterimages.Tick();
            if (InFlight)
                afterimages.Add(Projectile.Center, Projectile.rotation, 1f, ThornChakramTuning.GlowColor * 0.45f);
        }

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(afterimages);
            afterimages = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (CurrentPhase == Phase.Spent)
                return false;

            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = ThornChakramTuning.GlowColor;
            bool latched = CurrentPhase == Phase.Latched;
            float t = latched ? MathHelper.Clamp(latchTicks / (float)ThornChakramTuning.LatchTicks, 0f, 1f) : 0f;

            afterimages?.Draw();

            // Spin blur: faint copies trailing behind in rotation, longer the faster it spins.
            float spinStep = ThornChakramTuning.SpinSpeed * MathHelper.Lerp(1f, ThornChakramTuning.LatchSpinMaxMultiplier, t) * Projectile.localAI[1];
            Vector2 position = Projectile.Center - Main.screenPosition;
            for (int i = 3; i >= 1; i--)
                Main.EntitySpriteDraw(texture, position, null, glow * (0.18f + 0.1f * t), Projectile.rotation - spinStep * i * 0.6f, origin, 1f, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(texture, position, null, glow * (0.4f + 0.3f * t), Projectile.rotation, origin, latched ? 1.2f + 0.2f * t : 1.15f, SpriteEffects.None, 0); // halo
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.5f), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}