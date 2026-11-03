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
    /// SWING DOWN / SWING UP (left click): a custom melee chop centered on the cursor (the middle of the slash points at it): a slow heavy
    /// anticipation, a fast strike that decelerates, a short recover. Consecutive swings alternate down, up, down ... and each one starts
    /// where the previous one ended, so a held button flows. An expanding sweep trail (SweepTrail) fills the area the blade crosses. A true
    /// blade hit (the first one of each swing) charges the sword: the glow and the trail get stronger and the next toss is a higher tier.
    /// CALL (right click): the arm lifts until the sword points straight up, a burst of star dust marks the moment, and on that tick the
    /// owner spawns a StarfuryStarProjectile at the blade tip, which hops on a small parabola to a point in front of the player and spends the charge.
    /// MULTIPLAYER: the owner's client decides everything (which use it is, where the arm starts, the aim, the star spawn) and
    /// passes what the other machines need in the spawn packet: ai[0] = phase + 10 * charge hits (the owner raises the hits mid-swing
    /// when a blade hit charges the sword, with netUpdate), ai[1] = start arm angle (a swing: relative to the aim;
    /// a call: facing-local radians), ai[2] = the aim angle captured at the click (radians, the player's own frame).
    /// Every machine then runs the same timers for the pose, sounds, dust and trail. Swing hits are detected by the owner's client
    /// like any projectile; the star is a normal spawned (and therefore synced) projectile. No custom packets.
    /// </summary>
    public class StarfurySwingProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Starfury;

        public enum Phase { SwingDown = 0, SwingUp = 1, Call = 2 }

        Phase CurrentPhase => (Phase)((int)Projectile.ai[0] % 10);
        int Charge => (int)Projectile.ai[0] / 10; // the sword's charge hits
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
        Vector2 lastOwnerCenter; // where the owner was on the previous tick: the trail interpolates the owner's movement from it
        Vector2 prevCenter;      // the blade's hand position and angle on the previous tick: the hit test sweeps from them
        float prevRotation;
        SweepTrail trail;      // the fan behind the blade (null until a swing lays a point)
        bool hitCounted;       // owner only: this swing has already charged the sword (only the first hit of a swing counts)
        int chargeAtStart;     // the charge when this use began: the trail's strength for the whole swing
        int lastCharge;        // per machine: lets every machine notice a hit the owner added to the charge
        float chargeShown;     // 0..1, the glow's strength: follows the charge smoothly and drains away when the star is tossed
        bool discharged;       // the call has spent the charge (the glow drains)

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

        // The swing is anticipation, strike, recover (see StarfuryTuning). A swing that continues from the previous one starts its anticipation
        // from where the arm is; a fresh combo starts it from the rest pose.
        int StrikeStart => StarfuryTuning.AnticipateTicks;
        int StrikeEnd => StrikeStart + StarfuryTuning.SwingTicks;

        int TotalTicks => IsCall ? StarfuryTuning.CallTicks : StrikeEnd + StarfuryTuning.RecoverTicks;

        // +1 when the arm angle grows during the swing (down), -1 when it shrinks (up).
        float Travel => IsUp ? -1f : 1f;

        // 0 until the strike starts, then eased from 0 to 1: fastest right after the anticipation, decelerating until the strike ends.
        float SwingProgress(float time)
        {
            float t = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, StarfuryTuning.SwingTicks), 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, Math.Max(1f, StarfuryTuning.SwingEasePower));
        }

        float SwingArmAt(float time)
        {
            float back = FromArm - Travel * Rad(StarfuryTuning.AnticipateDeg);   // pulled back, away from where the strike goes
            float over = ToArm + Travel * Rad(StarfuryTuning.OvershootDeg);      // carried a little past the end of the arc
            if (time < StrikeStart)
                return MathHelper.Lerp(StartArm, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(back, over, SwingProgress(time));
            return MathHelper.Lerp(over, ToArm, MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(1, StarfuryTuning.RecoverTicks)));
        }

        float SwingTiltAt(float time)
        {
            float back = FromTilt - Travel * Rad(StarfuryTuning.AnticipateTiltDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(FromTilt, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)));
            return MathHelper.Lerp(back, ToTilt, SwingProgress(time));
        }

        bool Damaging
        {
            get
            {
                if (IsCall)
                    return false;
                return timer >= StrikeStart && timer <= StrikeEnd;
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

            prevCenter = Projectile.Center; // the previous tick's pose (the first tick fixes it below)
            prevRotation = Projectile.rotation;
            if (ticks == 0)
                lastOwnerCenter = owner.Center;

            if (IsCall)
                UpdateCall();
            else
                UpdateSwing();

            owner.itemAnimation = 2; // keeps the item "in use" for as long as the sword is out
            owner.itemTime = 2;
            ApplyPose(owner);
            if (ticks == 0)
            {
                lastTipPosition = tipPosition;
                prevCenter = Projectile.Center;
                prevRotation = Projectile.rotation;
            }

            UpdateCharge();

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
            lastOwnerCenter = owner.Center;
            ticks++;
            timer++;
        }

        float ChargeFractionAtStart => MathHelper.Clamp(chargeAtStart / (float)Math.Max(1, StarfuryTuning.MaxHits), 0f, 1f);

        // Every machine: smooths the glow and notices a hit (or a whole level) the owner added to the charge through ai[0].
        void UpdateCharge()
        {
            int charge = Charge;
            if (ticks == 0)
            {
                chargeAtStart = charge;
                lastCharge = charge;
                chargeShown = charge / (float)Math.Max(1, StarfuryTuning.MaxHits);
            }
            else if (charge > lastCharge)
            {
                int level = StarfuryTuning.LevelOf(charge);
                flash = Math.Max(flash, 0.5f);
                StarfuryFx.ChargeHit(tipPosition, level);
                if (level > StarfuryTuning.LevelOf(lastCharge))
                {
                    flash = 1f;
                    StarfuryFx.ChargeUp(tipPosition, level);
                    StarfuryFx.PlayChargeUp(tipPosition, level);
                }
                lastCharge = charge;
            }

            float target = discharged ? 0f : charge / (float)Math.Max(1, StarfuryTuning.MaxHits);
            chargeShown = MathHelper.Lerp(chargeShown, target, discharged ? 0.2f : 0.35f);
            if (chargeShown > 0.02f && Main.rand.NextFloat() < 0.15f + 0.5f * chargeShown)
                StarfuryFx.ChargeSparkle(Projectile.Center, tipPosition, StarfuryTuning.LevelOf(charge));
        }

        void UpdateSwing()
        {
            armLocal = SwingArmAt(timer);
            tiltRad = SwingTiltAt(timer);
        }

        void SwingEffects()
        {
            int strike = StrikeStart;
            if ((int)timer == strike)
                StarfuryFx.PlaySwing(tipPosition, IsUp);

            if (timer >= strike && timer < StrikeEnd)
            {
                // Many points per tick, laid between the previous tick's pose and this one, so the pixel line follows a smooth arc.
                int samples = Math.Max(1, StarfuryTuning.TrailSamplesPerTick);
                for (int k = 0; k < samples; k++)
                {
                    float fraction = (k + 1f) / samples;
                    AddTrailSample(timer - 1f + fraction, fraction);
                }
                StarfuryFx.SlashDust(Main.LocalPlayer.Center, tipPosition);
                Lighting.AddLight(tipPosition, 0.9f, 0.75f, 0.35f);
            }
        }

        // 'fraction' is how far between the previous tick and this one the sample is (0..1): the owner is placed where they were at that
        // moment, so the trail stays one connected line while the player moves during the swing.
        void AddTrailSample(float time, float fraction)
        {
            if (Main.dedServ)
                return;

            time -= 0.8f;
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm)) + shift;
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            float length = StarfuryTuning.BladeLength * Projectile.scale * 0.9f;
            // The fan starts as a line at the tip and its inner edge slides down the blade toward the hand while the swing builds up.
            float expand = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(SwingProgress(time) / Math.Max(0.01f, StarfuryTuning.TrailExpandBy), 0f, 1f));
            float charge = ChargeFractionAtStart;
            float inner = MathHelper.Lerp(1f, StarfuryTuning.TrailInnerAt(charge), expand);
            trail ??= new SweepTrail(new StarfurySweepTrailStyle(charge), IsOwner);
            // The tail of the swing lives shorter than its tip, so the trailing end fades out first and the fade travels up to the tip.
            float along = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, StarfuryTuning.SwingTicks), 0f, 1f);
            trail.Add(hand + direction * length, hand + direction * length * inner, MathHelper.Lerp(StarfuryTuning.TailLifeScale, 1f, along));
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
                if (Charge > 0)
                {
                    discharged = true;
                    StarfuryFx.ChargeRelease(tipPosition, StarfuryTuning.LevelOf(Charge));
                }
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

        // Owner only. The star is born at the blade tip (the sword points straight up) and hops on a small parabola to a point just in front
        // of the player, on the side they face (StarfuryStarProjectile.SolveToss); after that it keeps falling. Its generation is the sword's
        // charge level, and tossing it spends the whole charge.
        void SpawnStar()
        {
            StarfuryPlayer state = Owner.GetModPlayer<StarfuryPlayer>();
            int level = StarfuryTuning.LevelOf(state.ChargeHits);
            state.ChargeHits = 0;
            Vector2 velocity = StarfuryStarProjectile.SolveToss(tipPosition, Owner, Facing);
            StarfuryStarProjectile.Spawn(Projectile.GetSource_FromThis(), tipPosition, velocity, level, StarfuryStarProjectile.Mode.Toss,
                StarfuryTuning.CallBlockGraceTicks, Projectile.damage, Projectile.knockBack, Projectile.owner);
        }

        // The blade is a line from the hand to the tip. The strike is fast, so the test sweeps: it checks the line at a few poses between the
        // previous tick and this one, and a thin enemy or a star cannot slip through between two ticks.
        bool BladeTouches(Rectangle box, float width)
        {
            int steps = Math.Max(1, StarfuryTuning.SweepHitSteps);
            float length = StarfuryTuning.BladeLength * Projectile.scale;
            float deltaAngle = MathHelper.WrapAngle(Projectile.rotation - prevRotation);
            for (int i = 1; i <= steps; i++)
            {
                float fraction = i / (float)steps;
                Vector2 start = Vector2.Lerp(prevCenter, Projectile.Center, fraction);
                Vector2 end = start + (prevRotation + deltaAngle * fraction).ToRotationVector2() * length;
                float point = 0f;
                if (Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), start, end, width, ref point))
                    return true;
            }
            return false;
        }

        // Owner only: a swing that touches a star that is falling (called or bounced, not already launched) bats it.
        void TryBat()
        {
            int starType = ModContent.ProjectileType<StarfuryStarProjectile>();
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
                if (BladeTouches(box, 18f * Projectile.scale))
                    Bat(other, star);
            }
        }

        // Owner only. The hit star launches toward the bat direction as the next generation (one star: no copy).
        void Bat(Projectile starProjectile, StarfuryStarProjectile star)
        {
            int generation = star.Generation + 1;
            Vector2 direction = BatDirection(starProjectile);
            star.Launch(direction, generation, Projectile.damage, Projectile.knockBack);
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
            //return LaunchDirection(star);
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
            return BladeTouches(targetHitbox, 18f * Projectile.scale);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            StarfuryFx.HitSpark(target.Center);
            // Owner only: the first enemy the blade hits in a swing adds one hit to the sword's charge. The raised value reaches the other
            // machines through ai[0] (they notice it in UpdateCharge and play the same effect).
            if (IsOwner && !hitCounted)
            {
                hitCounted = true;
                StarfuryPlayer state = Owner.GetModPlayer<StarfuryPlayer>();
                if (state.AddHit())
                {
                    Projectile.ai[0] = (float)(int)CurrentPhase + 10f * state.ChargeHits;
                    Projectile.netUpdate = true;
                }
            }
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

            // The charge: a halo in the color of the star the sword will toss, stronger with every hit and pulsing; it drains away when the star is tossed.
            if (chargeShown > 0.02f)
            {
                Color chargeColor = StarfuryTuning.GenerationColor(StarfuryTuning.LevelOf(Charge));
                float pulse = 0.8f + 0.2f * (float)Math.Sin(ticks * 0.4f);
                Main.EntitySpriteDraw(texture, position, null, chargeColor * (0.6f * chargeShown * pulse), rotation, origin, scale * (1.35f + 0.15f * chargeShown), effects, 0);
                Main.EntitySpriteDraw(texture, position, null, chargeColor * (0.5f * chargeShown * pulse), rotation, origin, scale * 1.15f, effects, 0);
            }

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