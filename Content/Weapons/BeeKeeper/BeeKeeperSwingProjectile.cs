using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BeeKeeper
{
    /// <summary>
    /// The held Bee Keeper. It draws the sword, poses the owner's arm and runs the left click swing, which is the Starfury's: SWING DOWN /
    /// SWING UP, a custom melee chop centered on the cursor (the middle of the slash points at it): a slow heavy anticipation, a fast strike
    /// that decelerates, a short recover. Consecutive swings alternate down, up, down ... and each one starts where the previous one ended,
    /// so a held button flows. An expanding sweep trail (SweepTrail) fills the area the blade crosses. The strike hits every enemy once.
    /// MULTIPLAYER: the owner's client decides everything (which swing it is, where the arm starts, the aim) and passes what the other
    /// machines need in the spawn packet: ai[0] = phase (0 down, 1 up), ai[1] = start arm angle (relative to the aim), ai[2] = the aim angle
    /// captured at the click (radians, the player's own frame). Every machine then runs the same timers for the pose, sounds, dust and trail.
    /// Hits are detected by the owner's client like any projectile. No custom packets.
    /// </summary>
    public class BeeKeeperSwingProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.BeeKeeper;

        public enum Phase { SwingDown = 0, SwingUp = 1 }

        bool IsUp => Projectile.ai[0] >= 0.5f;
        float StartArm => Projectile.ai[1];
        float Aim => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        // A swing faces the side of its aim on every machine (so nobody's pose lags the synced direction).
        int Facing => Math.Cos(Aim) >= 0.0 ? 1 : -1;

        // Per-machine state: counters and visuals, never synced.
        int ticks;             // ticks this machine has run this projectile
        float timer;           // ticks since the swing began
        float armLocal;        // arm angle relative to the aim for a player facing right (positive = down)
        float tiltRad;         // blade tilt relative to the arm
        Vector2 tipPosition;
        Vector2 lastOwnerCenter; // where the owner was on the previous tick: the trail interpolates the owner's movement from it
        Vector2 prevCenter;      // the blade's hand position and angle on the previous tick: the hit test sweeps from them
        float prevRotation;
        SweepTrail trail;      // the fan behind the blade (null until a swing lays a point)

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

        // The arm and the blade as world angles (in the player's own frame, see ApplyPose): relative to the aim and mirrored by the facing.
        float WorldArm(float arm)
        {
            return Aim + Facing * arm;
        }

        float WorldBlade(float arm, float tilt)
        {
            return Aim + Facing * (arm + tilt);
        }

        // The swing runs from one end of the arc to the other: down goes high to low, up goes low to high.
        float FromArm => Rad(IsUp ? BeeKeeperTuning.SwingLowDeg : BeeKeeperTuning.SwingHighDeg);
        float ToArm => Rad(IsUp ? BeeKeeperTuning.SwingHighDeg : BeeKeeperTuning.SwingLowDeg);
        float FromTilt => Rad(IsUp ? BeeKeeperTuning.TiltLowDeg : BeeKeeperTuning.TiltHighDeg);
        float ToTilt => Rad(IsUp ? BeeKeeperTuning.TiltHighDeg : BeeKeeperTuning.TiltLowDeg);

        // The swing is anticipation, strike, recover (see BeeKeeperTuning). A swing that continues from the previous one starts its
        // anticipation from where the arm is; a fresh combo starts it from the rest pose.
        int StrikeStart => BeeKeeperTuning.AnticipateTicks;
        int StrikeEnd => StrikeStart + BeeKeeperTuning.SwingTicks;
        int TotalTicks => StrikeEnd + BeeKeeperTuning.RecoverTicks;

        // +1 when the arm angle grows during the swing (down), -1 when it shrinks (up).
        float Travel => IsUp ? -1f : 1f;

        // 0 until the strike starts, then eased from 0 to 1: fastest right after the anticipation, decelerating until the strike ends.
        float SwingProgress(float time)
        {
            float t = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, BeeKeeperTuning.SwingTicks), 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, Math.Max(1f, BeeKeeperTuning.SwingEasePower));
        }

        float SwingArmAt(float time)
        {
            float back = FromArm - Travel * Rad(BeeKeeperTuning.AnticipateDeg);   // pulled back, away from where the strike goes
            float over = ToArm + Travel * Rad(BeeKeeperTuning.OvershootDeg);      // carried a little past the end of the arc
            if (time < StrikeStart)
                return MathHelper.Lerp(StartArm, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(back, over, SwingProgress(time));
            return MathHelper.Lerp(over, ToArm, MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(1, BeeKeeperTuning.RecoverTicks)));
        }

        float SwingTiltAt(float time)
        {
            float back = FromTilt - Travel * Rad(BeeKeeperTuning.AnticipateTiltDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(FromTilt, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)));
            return MathHelper.Lerp(back, ToTilt, SwingProgress(time));
        }

        bool Damaging => timer >= StrikeStart && timer <= StrikeEnd;

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.BeeKeeper)
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
                owner.ChangeDir(Facing);

            Projectile.scale = BeeKeeperTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            prevCenter = Projectile.Center; // the previous tick's pose (the first tick fixes it below)
            prevRotation = Projectile.rotation;
            if (ticks == 0)
                lastOwnerCenter = owner.Center;

            armLocal = SwingArmAt(timer);
            tiltRad = SwingTiltAt(timer);

            owner.itemAnimation = 2; // keeps the item "in use" for as long as the sword is out
            owner.itemTime = 2;
            ApplyPose(owner);
            if (ticks == 0)
            {
                prevCenter = Projectile.Center;
                prevRotation = Projectile.rotation;
            }

            SwingEffects();

            trail?.Tick();
            lastOwnerCenter = owner.Center;
            ticks++;
            timer++;
        }

        void SwingEffects()
        {
            if ((int)timer == StrikeStart)
                BeeKeeperFx.PlaySwing(tipPosition, IsUp);

            if (timer >= StrikeStart && timer < StrikeEnd)
            {
                // Many points per tick, laid between the previous tick's pose and this one, so the pixel line follows a smooth arc.
                int samples = Math.Max(1, BeeKeeperTuning.TrailSamplesPerTick);
                for (int k = 0; k < samples; k++)
                {
                    float fraction = (k + 1f) / samples;
                    AddTrailSample(timer - 1f + fraction, fraction);
                }
                BeeKeeperFx.SlashDust(Owner.Center, tipPosition);
                Lighting.AddLight(tipPosition, 0.9f, 0.6f, 0.15f);
            }
        }

        // 'fraction' is how far between the previous tick and this one the sample is (0..1): the owner is placed where they were at that
        // moment, so the trail stays one connected line while the player moves during the swing.
        void AddTrailSample(float time, float fraction)
        {
            if (Main.dedServ)
                return;

            time -= BeeKeeperTuning.TrailLagTicks;
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm)) + shift;
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            float length = BeeKeeperTuning.BladeLength * Projectile.scale * 0.9f;
            // The fan starts as a line at the tip and its inner edge slides down the blade toward the hand while the swing builds up.
            float expand = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(SwingProgress(time) / Math.Max(0.01f, BeeKeeperTuning.TrailExpandBy), 0f, 1f));
            float outer = BeeKeeperTuning.TrailOuter;
            float inner = MathHelper.Lerp(outer, BeeKeeperTuning.TrailInner, expand);
            trail ??= new SweepTrail(new BeeKeeperSweepTrailStyle(), IsOwner);
            // 'along' = where in the strike this sample is; the style (TailFirst) makes the tail live shorter, so it fades out first.
            float along = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, BeeKeeperTuning.SwingTicks), 0f, 1f);
            trail.Add(hand + direction * length * outer, hand + direction * length * inner, along);
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
            tipPosition = hand + blade.ToRotationVector2() * BeeKeeperTuning.BladeLength * Projectile.scale;
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
            return BladeTouches(targetHitbox, BeeKeeperTuning.HitWidth * Projectile.scale);
        }

        // The strike is fast, so the test sweeps: it checks the line at a few poses between the previous tick and this one, and a thin
        // enemy cannot slip through between two ticks.
        bool BladeTouches(Rectangle box, float width)
        {
            int steps = Math.Max(1, BeeKeeperTuning.SweepHitSteps);
            float length = BeeKeeperTuning.BladeLength * BeeKeeperTuning.HitLengthScale * Projectile.scale;
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

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
        }

        // Owner's client only. A true melee hit of the swing: sparks, a sound, a small camera punch, and bees sent at the enemy (the more
        // hits on it, the more bees). Stings and clouds never count as a hit, only this. The bees' stings are a fraction of this swing's damage.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BeeKeeperFx.HitSpark(target.Center);
            BeeKeeperFx.PlayHit(target.Center);
            CameraShake.Punch(target.Center, BeeKeeperTuning.HitShake, 8);
            Owner.GetModPlayer<BeeKeeperPlayer>().RecruitBees(target, Projectile.damage);
        }

        // The trail outlives the swing and keeps fading on its own (DetachedTrails). The owner remembers how this swing ended, so the
        // next one continues from there and alternates.
        public override void OnKill(int timeLeft)
        {
            if (IsOwner)
            {
                BeeKeeperPlayer state = Owner.GetModPlayer<BeeKeeperPlayer>();
                state.LastPhase = IsUp ? (int)Phase.SwingUp : (int)Phase.SwingDown;
                state.LastEndArm = armLocal;
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

            // A faint honey glow behind the sword while it strikes, then the sword itself.
            float glow = Damaging ? 0.5f : 0.2f;
            Main.EntitySpriteDraw(texture, position, null, BeeKeeperTuning.HoneyColor * (0.5f * glow), rotation, origin, scale * 1.12f, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.35f), rotation, origin, scale, effects, 0);
            return false;
        }
    }
}