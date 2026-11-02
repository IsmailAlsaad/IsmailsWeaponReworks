using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>
    /// The held Starfury. It draws the sword, poses the owner's arm and runs both uses:
    /// SWING DOWN / SWING UP (left click): a custom melee chop centered on the cursor (the middle of the slash points at it); consecutive
    /// swings alternate down, up, down ... and each one starts where the previous one ended, so a held button flows. An expanding sweep
    /// trail (SweepTrail) fills the area the blade crosses.
    /// CALL (right click): the arm lifts until the sword points straight up, a burst of star dust marks the moment, and on that tick the
    /// owner spawns a StarfuryStarProjectile far above, which falls in a straight line to a point in front of the player.
    /// MULTIPLAYER: the owner's client decides everything (which use it is, where the arm starts, the aim, the star spawn) and
    /// passes what the other machines need in the spawn packet: ai[0] = phase, ai[1] = start arm angle (a swing: relative to the aim;
    /// a call: facing-local radians), ai[2] = the aim angle captured at the click (radians, the player's own frame).
    /// Every machine then runs the same timers for the pose, sounds, dust and trail. Swing hits are detected by the owner's client
    /// like any projectile; the star is a normal spawned (and therefore synced) projectile. No custom packets.
    /// </summary>
    public class StarfurySwingProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Starfury;

        public enum Phase { SwingDown = 0, SwingUp = 1, Call = 2 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        float StartArm => Projectile.ai[1];
        float Aim => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        // A swing faces the side of its aim on every machine (so nobody's pose lags the synced direction); a call keeps the owner's direction.
        int Facing => IsCall ? (Owner.direction >= 0 ? 1 : -1) : (Math.Cos(Aim) >= 0.0 ? 1 : -1);
        bool IsCall => CurrentPhase == Phase.Call;
        bool IsUp => CurrentPhase == Phase.SwingUp;

        // Per-machine state: counters and visuals, never synced.
        int ticks;             // ticks this machine has run this projectile
        float timer;           // ticks since the use began
        float flash;           // 1 right after the star is called, fades to 0
        float armLocal;        // a swing: arm angle relative to the aim for a player facing right (positive = down); a call: facing-local arm angle
        float tiltRad;         // blade tilt relative to the arm
        Vector2 tipPosition;
        Vector2 lastTipPosition;
        SweepTrail trail;      // the fan behind the blade (null until a swing lays a point)
        int meleeStars;        // owner only: stars this swing has popped out of enemies (capped per swing)

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // ended by the AI, never by this countdown
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once per swing
            Projectile.ownerHitCheck = true;
        }

        // ---- helpers ----

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        // The arm and the blade as world angles (in the player's own frame, see ApplyPose). A swing is relative to its aim and mirrored by
        // the facing; the call keeps the old facing-local pose.
        float WorldArm(float arm)
        {
            return IsCall ? HeldSwordPose.ToWorld(Facing, arm) : Aim + Facing * arm;
        }

        float WorldBlade(float arm, float tilt)
        {
            return IsCall ? HeldSwordPose.ToWorld(Facing, arm + tilt) : Aim + Facing * (arm + tilt);
        }

        // The swing runs from one end of the arc to the other: down goes high to low, up goes low to high.
        float FromArm => Rad(IsUp ? StarfuryTuning.SwingLowDeg : StarfuryTuning.SwingHighDeg);
        float ToArm => Rad(IsUp ? StarfuryTuning.SwingHighDeg : StarfuryTuning.SwingLowDeg);
        float FromTilt => Rad(IsUp ? StarfuryTuning.TiltLowDeg : StarfuryTuning.TiltHighDeg);
        float ToTilt => Rad(IsUp ? StarfuryTuning.TiltHighDeg : StarfuryTuning.TiltLowDeg);

        // A swing that continues from the previous one (the arm already is where this one starts) needs no windup.
        int Windup => Math.Abs(StartArm - FromArm) < Rad(8f) ? 0 : StarfuryTuning.WindupTicks;

        int TotalTicks => IsCall ? StarfuryTuning.CallTicks : Windup + StarfuryTuning.SwingTicks + StarfuryTuning.RecoverTicks;

        float SwingProgress(float time)
        {
            float t = MathHelper.Clamp((time - Windup) / Math.Max(1, StarfuryTuning.SwingTicks), 0f, 1f);
            return MathHelper.SmoothStep(0f, 1f, t);
        }

        float SwingArmAt(float time)
        {
            int windup = Windup;
            if (time < windup)
            {
                float k = 1f - (float)Math.Pow(1f - time / Math.Max(1, windup), 2);
                return MathHelper.Lerp(StartArm, FromArm, k);
            }
            return MathHelper.Lerp(FromArm, ToArm, SwingProgress(time));
        }

        float SwingTiltAt(float time)
        {
            return MathHelper.Lerp(FromTilt, ToTilt, SwingProgress(time));
        }

        bool Damaging
        {
            get
            {
                if (IsCall)
                    return false;
                return timer >= Windup && timer <= Windup + StarfuryTuning.SwingTicks;
            }
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.Starfury)
            {
                Projectile.Kill();
                return;
            }

            if (timer >= TotalTicks)
            {
                owner.itemAnimation = 0; // the next use may start on the very next tick
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            if (ticks == 0 && IsOwner)
                owner.ChangeDir(IsCall ? (Main.MouseWorld.X >= owner.Center.X ? 1 : -1) : Facing);

            Projectile.scale = StarfuryTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            if (IsCall)
                UpdateCall();
            else
                UpdateSwing();

            owner.itemAnimation = 2; // keeps the item "in use" for as long as the sword is out
            owner.itemTime = 2;
            ApplyPose(owner);
            if (ticks == 0)
                lastTipPosition = tipPosition;

            if (IsCall)
            {
                CallEffects();
            }
            else
            {
                SwingEffects();
                if (IsOwner && Damaging)
                    TryBat();
            }

            flash = Math.Max(0f, flash - 1f / Math.Max(1, StarfuryTuning.FlashTicks));
            trail?.Tick();
            lastTipPosition = tipPosition;
            ticks++;
            timer++;
        }

        void UpdateSwing()
        {
            armLocal = SwingArmAt(timer);
            tiltRad = SwingTiltAt(timer);
        }

        void SwingEffects()
        {
            int windup = Windup;
            if ((int)timer == windup)
                StarfuryFx.PlaySwing(tipPosition, IsUp);

            if (timer >= windup && timer < windup + StarfuryTuning.SwingTicks)
            {
                // Three points per tick, so the pixel line follows a smooth arc instead of a few straight segments.
                for (int k = 0; k < 3; k++)
                    AddTrailSample(timer + k / 3f);
                StarfuryFx.SlashDust(Main.LocalPlayer.Center, tipPosition);
                Lighting.AddLight(tipPosition, 0.9f, 0.75f, 0.35f);
            }
        }

        void AddTrailSample(float time)
        {
            if (Main.dedServ)
                return;

            time -= 0.8f;
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm));
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            float length = StarfuryTuning.BladeLength * Projectile.scale * 0.9f;
            // The fan starts as a line at the tip and its inner edge slides down the blade toward the hand while the swing builds up.
            float expand = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(SwingProgress(time) / Math.Max(0.01f, StarfuryTuning.TrailExpandBy), 0f, 1f));
            float inner = MathHelper.Lerp(1f, StarfuryTuning.TrailInnerFraction, expand);
            trail ??= new SweepTrail(new StarfurySweepTrailStyle(), IsOwner);
            trail.Add(hand + direction * length, hand + direction * length * inner);
        }

        void UpdateCall()
        {
            // The arm lifts from where it was to the raised pose with a cubic ease-out and kicks up a little when the star is born.
            float lift = MathHelper.Clamp(timer / Math.Max(1, StarfuryTuning.CallLiftTicks), 0f, 1f);
            float eased = 1f - (float)Math.Pow(1f - lift, 3);
            armLocal = MathHelper.Lerp(StartArm, Rad(StarfuryTuning.CallArmDeg), eased) - Rad(StarfuryTuning.CallKickDeg) * flash;
            tiltRad = MathHelper.Lerp(0f, Rad(StarfuryTuning.CallTiltDeg), eased);
        }

        void CallEffects()
        {
            int t = (int)timer;
            if (t < StarfuryTuning.CallSpawnTick && t % 2 == 0)
                StarfuryFx.CallGather(tipPosition);

            if (t == StarfuryTuning.CallSpawnTick)
            {
                flash = 1f;
                StarfuryFx.CallBurst(tipPosition);
                StarfuryFx.PlayCall(tipPosition);
                if (IsOwner)
                    SpawnStar();
            }
            else if (t > StarfuryTuning.CallSpawnTick && Main.rand.NextBool(3))
            {
                StarfuryFx.BladeSparkle(Projectile.Center, tipPosition);
            }
            if (t >= StarfuryTuning.CallSpawnTick)
                Lighting.AddLight(tipPosition, 0.5f + 0.6f * flash, 0.4f + 0.5f * flash, 0.15f + 0.3f * flash);
        }

        // Owner only. The target is a point in front of the player, within sword reach (the cursor does not matter). The star starts far
        // above it (on the side the player does not face) and flies a straight line through it.
        void SpawnStar()
        {
            Vector2 target = Owner.MountedCenter + new Vector2(Facing * StarfuryTuning.CallFrontDistance, StarfuryTuning.CallTargetHeight);
            float side = -Facing * StarfuryTuning.SpawnSideOffset;
            float y = Math.Min(Main.screenPosition.Y - StarfuryTuning.SpawnAboveScreen, target.Y - StarfuryTuning.MinFallDistance);
            Vector2 spawn = new Vector2(target.X + side, Math.Max(y, 64f));
            Vector2 velocity = (target - spawn).SafeNormalize(Vector2.UnitY) * StarfuryTuning.CalledStarSpeed;
            float ticksToTarget = (target - spawn).Length() / StarfuryTuning.CalledStarSpeed;
            StarfuryStarProjectile.Spawn(Projectile.GetSource_FromThis(), spawn, velocity, 0, StarfuryStarProjectile.Mode.Straight, ticksToTarget,
                Projectile.damage, Projectile.knockBack, Projectile.owner);
        }

        // Owner only: a swing that touches a star that is falling (called or bounced, not already launched) bats it.
        void TryBat()
        {
            int starType = ModContent.ProjectileType<StarfuryStarProjectile>();
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * StarfuryTuning.BladeLength * Projectile.scale;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != starType)
                    continue;
                StarfuryStarProjectile star = (StarfuryStarProjectile)other.ModProjectile;
                if (!star.CanBeBatted)
                    continue;
                Rectangle box = other.Hitbox;
                box.Inflate((int)StarfuryTuning.BatPadding, (int)StarfuryTuning.BatPadding);
                float point = 0f;
                if (Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), start, end, 18f * Projectile.scale, ref point))
                    Bat(other, star);
            }
        }

        // Owner only. The hit star launches one way and a copy launches the other way (a fixed small spread, so the pair reads clearly);
        // when the player already has the maximum number of stars alive, there is no copy.
        void Bat(Projectile starProjectile, StarfuryStarProjectile star)
        {
            int generation = star.Generation + 1;
            Vector2 direction = BatDirection(starProjectile);
            float spread = Rad(StarfuryTuning.DoubleSpreadDeg);
            star.Launch(direction.RotatedBy(-spread), generation, Projectile.damage, Projectile.knockBack);
            if (StarfuryStarProjectile.CountOwned(Projectile.owner) < StarfuryTuning.MaxStars)
            {
                StarfuryStarProjectile.Spawn(Projectile.GetSource_FromThis(), starProjectile.Center, direction.RotatedBy(spread) * StarfuryTuning.LaunchSpeed(generation), generation,
                    StarfuryStarProjectile.Mode.Launched, 0f, Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            flash = 0.8f;
        }

        // Owner only: the closest enemy to the player (within BatTargetRange) that a star can be sent at, or -1.
        int ClosestEnemy(Projectile star)
        {
            int best = -1;
            float bestDistance = StarfuryTuning.BatTargetRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || !npc.CanBeChasedBy(star))
                    continue;
                float distance = Vector2.Distance(Owner.Center, npc.Center);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // A bat sends the star in a straight line at the enemy closest to the player; with none in range the old rule decides.
        Vector2 BatDirection(Projectile star)
        {
            int enemy = ClosestEnemy(star);
            if (enemy < 0)
                return LaunchDirection(star);
            return (Main.npc[enemy].Center - star.Center).SafeNormalize(LaunchDirection(star));
        }

        // The fallback when no enemy is in range: toward the cursor when the player faces the cursor's side; otherwise the star reflects off
        // the blade, by the angle it was hit at.
        Vector2 LaunchDirection(Projectile star)
        {
            Vector2 fallback = Vector2.UnitX * Facing;
            bool cursorOnFacingSide = (Main.MouseWorld.X >= Owner.Center.X ? 1 : -1) == Facing;
            if (cursorOnFacingSide)
                return (Main.MouseWorld - star.Center).SafeNormalize(fallback);

            Vector2 velocity = star.velocity;
            if (velocity.LengthSquared() < 0.25f)
                return (star.Center - Owner.Center).SafeNormalize(fallback); // a star hanging still: push it away from the player
            Vector2 blade = Projectile.rotation.ToRotationVector2();
            Vector2 normal = new Vector2(-blade.Y, blade.X);
            Vector2 reflected = velocity - 2f * Vector2.Dot(velocity, normal) * normal;
            return reflected.SafeNormalize(fallback);
        }

        // Moves the owner's arm and puts the projectile in the hand; the blade points along Projectile.rotation.
        void ApplyPose(Player owner)
        {
            float worldArm = WorldArm(armLocal);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, worldArm - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, worldArm);
            float blade = WorldBlade(armLocal, tiltRad);
            if (owner.gravDir == -1f)
                blade = -blade;
            Projectile.rotation = blade;
            Projectile.Center = hand;
            Projectile.spriteDirection = Facing;
            tipPosition = hand + blade.ToRotationVector2() * StarfuryTuning.BladeLength * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return Damaging ? (bool?)null : false;
        }

        // The blade is a line from the hand to the tip.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Damaging)
                return false;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * StarfuryTuning.BladeLength * Projectile.scale;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 18f * Projectile.scale, ref point);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            StarfuryFx.HitSpark(target.Center);
            if (IsOwner && meleeStars < StarfuryTuning.MeleeStarsPerSwing && StarfuryStarProjectile.CountOwned(Projectile.owner) < StarfuryTuning.MaxStars)
                SpawnMeleeStar(target);
        }

        // Owner only. A true blade hit pops a falling star out of the impact point (the closest spot of the blade to the enemy, kept inside
        // the enemy's hitbox). It is an ordinary star of generation 0 in the falling, parabolic state: batting, doubling and generations
        // work the same; it just cannot be batted for a few ticks, so this very swing does not bat it right away.
        void SpawnMeleeStar(NPC target)
        {
            Vector2 start = Projectile.Center;
            Vector2 blade = Projectile.rotation.ToRotationVector2() * StarfuryTuning.BladeLength * Projectile.scale;
            float t = blade.LengthSquared() > 0f ? MathHelper.Clamp(Vector2.Dot(target.Center - start, blade) / blade.LengthSquared(), 0f, 1f) : 0f;
            Rectangle box = target.Hitbox;
            Vector2 point = Vector2.Clamp(start + blade * t, box.TopLeft(), box.BottomRight());
            float angle = -MathHelper.PiOver2 + Main.rand.NextFloat(-1f, 1f) * Rad(StarfuryTuning.BounceSpreadDeg);
            Vector2 velocity = angle.ToRotationVector2() * StarfuryTuning.MeleeStarPopSpeed;
            int index = StarfuryStarProjectile.Spawn(Projectile.GetSource_FromThis(), point, velocity, 0, StarfuryStarProjectile.Mode.Parabolic, 0f,
                Projectile.damage, Projectile.knockBack, Projectile.owner);
            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is StarfuryStarProjectile star)
                star.StartGrace(target);
            meleeStars++;
            StarfuryFx.BounceBurst(point, 0);
        }

        // The trail outlives the swing and keeps fading on its own (DetachedTrails). The owner remembers how this use ended, so the
        // next swing continues from there and alternates.
        public override void OnKill(int timeLeft)
        {
            if (IsOwner)
            {
                StarfuryPlayer state = Owner.GetModPlayer<StarfuryPlayer>();
                state.LastPhase = (int)CurrentPhase;
                state.LastEndArm = armLocal;
                state.LastEndWorldArm = WorldArm(armLocal);
                state.LastEndTick = Main.GameUpdateCount;
            }
            DetachedTrails.Hand(trail);
            trail = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);

            Vector2 position = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;

            // Glow: steady while the sword is out, flaring when the star is called.
            float glow = 0.25f + flash;
            Main.EntitySpriteDraw(texture, position, null, StarfuryTuning.PinkColor * (0.5f * glow), rotation, origin, scale * (1.2f + 0.2f * flash), effects, 0);
            Main.EntitySpriteDraw(texture, position, null, StarfuryTuning.GoldColor * (0.6f * glow), rotation, origin, scale * (1.08f + 0.1f * flash), effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.35f + 0.5f * flash), rotation, origin, scale, effects, 0);
            if (flash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, StarfuryTuning.CoreColor * (0.8f * flash), rotation, origin, scale, effects, 0);
            return false;
        }
    }
}