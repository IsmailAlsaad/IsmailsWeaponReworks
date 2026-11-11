using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Phaseblade
{
    /// <summary>
    /// The held Phaseblade (any color). One projectile runs ONE swing of the combo; the next click continues it:
    /// SWING DOWN (1): a custom melee chop centered on the cursor: a short pull back, a fast strike that decelerates, a short recover.
    /// SPIN DOWN (2): starts where swing 1 ended and the arm keeps rotating the same way, over the head (the LOOP, no damage), then strikes
    /// down like swing 1, but the blade also spins about the hand for SpinTurns whole turns while the arm rotates.
    /// SPIN UP (3): swings up from the low end with the same spinning blade. After it the combo starts over with swing 1.
    /// An expanding sweep trail (SweepTrail) fills the area the blade crosses. The hit test samples the blade's pose between two ticks from
    /// the pose functions themselves (not by turning the last angle toward the new one), so a fast spin cannot skip an enemy.
    /// MULTIPLAYER: the owner's client decides everything (which swing it is, where the arm starts, the aim) and passes what the other machines
    /// need in the spawn packet: ai[0] = phase, ai[1] = start arm angle (relative to the aim), ai[2] = the aim angle captured at the click
    /// (radians, the player's own frame). Every machine then runs the same timers for the pose, sounds, dust and trail (the blade's color is read
    /// from the held item, which every machine knows). Hits are detected by the owner's client like any projectile; RemoteHitTracker gives
    /// the other machines their hit sparks. No custom packets.
    /// </summary>
    public class PhasebladeSwingProjectile : ModProjectile
    {
        // The projectile's own sprite is only a placeholder: the swing draws the held item's sprite (the color of the Phaseblade in the hand).
        public override string Texture => "Terraria/Images/Item_" + ItemID.BluePhaseblade;

        public enum Phase { SwingDown = 0, SpinDown = 1, SpinUp = 2 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        float StartArm => Projectile.ai[1];
        float Aim => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        // A swing faces the side of its aim on every machine (so nobody's pose lags the synced direction).
        int Facing => Math.Cos(Aim) >= 0.0 ? 1 : -1;
        bool IsUp => CurrentPhase == Phase.SpinUp;
        bool Spins => CurrentPhase != Phase.SwingDown;
        bool Loops => CurrentPhase == Phase.SpinDown;
        Color BladeColor => PhasebladeTuning.ColorOf(Owner.HeldItem.type);

        // Per-machine state: counters and visuals, never synced.
        int ticks;             // ticks this machine has run this projectile
        float timer;           // ticks since the swing began
        float poseTime;        // the swing time of the pose that was applied on this tick (the hit test samples back from it)
        float flash;           // 1 right when the strike starts, fades to 0
        float armLocal;        // arm angle relative to the aim for a player facing right (positive = down)
        float tiltRad;         // blade tilt relative to the arm (includes the spin)
        Vector2 tipPosition;
        Vector2 lastTipPosition;
        Vector2 lastOwnerCenter; // where the owner was on the previous tick: the trail interpolates the owner's movement from it
        Vector2 hitOwnerFrom;    // the owner's position at the start and end of this tick: the hit test interpolates between them
        Vector2 hitOwnerTo;
        SweepTrail trail;      // the fan behind the blade (null until a swing lays a point)
        readonly RemoteHitTracker remoteHits = new RemoteHitTracker();

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

        // The arm and the blade as world angles (in the player's own frame, see ApplyPose); a swing is relative to its aim and mirrored by the facing.
        float WorldArm(float arm)
        {
            return Aim + Facing * arm;
        }

        float WorldBlade(float arm, float tilt)
        {
            return Aim + Facing * (arm + tilt);
        }

        // Where the pull back of a down swing ends (just above the top of the arc).
        float BaseBack => Rad(PhasebladeTuning.SwingHighDeg) - Rad(PhasebladeTuning.AnticipateDeg);

        // Swing 2's windup is a loop: the arm keeps rotating from where it is, round, until it reaches the pulled back pose again. The target is
        // the pulled back pose plus whole turns, so it is at least MinLoopDeg ahead of the start. Every machine computes the same from ai[1].
        float LoopBack
        {
            get
            {
                float back = BaseBack;
                float minimum = StartArm + Rad(PhasebladeTuning.MinLoopDeg);
                if (back < minimum)
                    back += MathHelper.TwoPi * (float)Math.Ceiling((minimum - back) / MathHelper.TwoPi);
                return back;
            }
        }

        // The whole arc of swing 2 is moved by the same number of turns, so it strikes down exactly where swing 1 does.
        float Wrap => Loops ? LoopBack - BaseBack : 0f;

        // The swing runs from one end of the arc to the other: down goes high to low, up goes low to high.
        float FromArm => Rad(IsUp ? PhasebladeTuning.SwingLowDeg : PhasebladeTuning.SwingHighDeg) + Wrap;
        float ToArm => Rad(IsUp ? PhasebladeTuning.SwingHighDeg : PhasebladeTuning.SwingLowDeg) + Wrap;
        float FromTilt => Rad(IsUp ? PhasebladeTuning.TiltLowDeg : PhasebladeTuning.TiltHighDeg);
        float ToTilt => Rad(IsUp ? PhasebladeTuning.TiltHighDeg : PhasebladeTuning.TiltLowDeg);

        int WindupTicks => Loops ? PhasebladeTuning.LoopTicks : PhasebladeTuning.AnticipateTicks;
        int StrikeTicks => Spins ? PhasebladeTuning.SpinSwingTicks : PhasebladeTuning.SwingTicks;
        int StrikeStart => WindupTicks;
        int StrikeEnd => StrikeStart + StrikeTicks;
        int TotalTicks => StrikeEnd + PhasebladeTuning.RecoverTicks;

        // +1 when the arm angle grows during the swing (down), -1 when it shrinks (up).
        float Travel => IsUp ? -1f : 1f;

        // 0 until the strike starts, then eased from 0 to 1: fastest right after the windup, decelerating until the strike ends.
        float SwingProgress(float time)
        {
            float t = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, StrikeTicks), 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, Math.Max(1f, PhasebladeTuning.SwingEasePower));
        }

        float SwingArmAt(float time)
        {
            float back = FromArm - Travel * Rad(PhasebladeTuning.AnticipateDeg);   // pulled back, away from where the strike goes
            float over = ToArm + Travel * Rad(PhasebladeTuning.OvershootDeg);      // carried a little past the end of the arc
            if (time < StrikeStart)
                return MathHelper.Lerp(StartArm, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(back, over, SwingProgress(time));
            return MathHelper.Lerp(over, ToArm, MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(1, PhasebladeTuning.RecoverTicks)));
        }

        // The wrist, plus the spin: whole turns about the hand, in the direction the arm travels, riding the same easing as the strike.
        float SwingTiltAt(float time)
        {
            float back = FromTilt - Travel * Rad(PhasebladeTuning.AnticipateTiltDeg);
            float tilt = time < StrikeStart
                ? MathHelper.Lerp(FromTilt, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)))
                : MathHelper.Lerp(back, ToTilt, SwingProgress(time));
            if (Spins)
                tilt += Travel * MathHelper.TwoPi * PhasebladeTuning.SpinTurns * SwingProgress(time);
            return tilt;
        }

        bool Damaging => poseTime >= StrikeStart && poseTime <= StrikeEnd;

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || !PhasebladeTuning.IsPhaseblade(owner.HeldItem.type))
            {
                Projectile.Kill();
                return;
            }

            if (timer >= TotalTicks)
            {
                owner.itemAnimation = 0; // the next swing may start on the very next tick
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            if (ticks == 0 && IsOwner)
                owner.ChangeDir(Facing);

            Projectile.scale = PhasebladeTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            if (ticks == 0)
                lastOwnerCenter = owner.Center;
            hitOwnerFrom = lastOwnerCenter;
            hitOwnerTo = owner.Center;
            poseTime = timer;

            armLocal = SwingArmAt(timer);
            tiltRad = SwingTiltAt(timer);

            owner.itemAnimation = 2; // keeps the item "in use" for as long as the blade is out
            owner.itemTime = 2;
            ApplyPose(owner);
            if (ticks == 0)
                lastTipPosition = tipPosition;

            Effects();
            if (!IsOwner && Damaging)
                remoteHits.Update(Projectile, 0, npc => PhasebladeFx.HitSpark(npc.Center, BladeColor));

            flash = Math.Max(0f, flash - 1f / Math.Max(1, PhasebladeTuning.FlashTicks));
            trail?.Tick();
            lastTipPosition = tipPosition;
            lastOwnerCenter = owner.Center;
            ticks++;
            timer++;
        }

        void Effects()
        {
            Color color = BladeColor;
            if (ticks == 0 && Loops)
                PhasebladeFx.PlayLoop(tipPosition);

            int strike = StrikeStart;
            if ((int)timer == strike)
            {
                flash = 1f;
                PhasebladeFx.PlaySwing(tipPosition, IsUp, Spins);
            }

            if (timer >= strike && timer < StrikeEnd)
            {
                // Many points per tick, laid between the previous tick's pose and this one, so the pixel line follows a smooth arc.
                int samples = Math.Max(1, PhasebladeTuning.TrailSamplesPerTick);
                for (int k = 0; k < samples; k++)
                {
                    float fraction = (k + 1f) / samples;
                    AddTrailSample(timer - 1f + fraction, fraction);
                }
                PhasebladeFx.SlashDust(Projectile.Center, tipPosition, color);
                if (Spins)
                    PhasebladeFx.SpinDust(tipPosition, tipPosition - lastTipPosition, color);
            }

            if (timer >= strike && timer <= StrikeEnd)
                Lighting.AddLight(tipPosition, color.ToVector3() * (0.7f + 0.4f * flash));
        }

        // 'fraction' is how far between the previous tick and this one the sample is (0..1): the owner is placed where they were at that
        // moment, so the trail stays one connected line while the player moves during the swing.
        void AddTrailSample(float time, float fraction)
        {
            if (Main.dedServ)
                return;

            time -= PhasebladeTuning.TrailLagTicks;
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm)) + shift;
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            float length = PhasebladeTuning.BladeLength * Projectile.scale;
            float outer = PhasebladeTuning.TrailOuter;
            // The fan starts as a line at the outer edge and its inner edge slides down the blade toward the hand while the swing builds up.
            float inner = PixelFan.Expand(SwingProgress(time), PhasebladeTuning.TrailExpandBy, PhasebladeTuning.TrailInner, outer);
            trail ??= new SweepTrail(new PhasebladeSweepTrailStyle(BladeColor), IsOwner);
            // 'along' = where in the strike this sample is; the style (TailFirst) makes the tail live shorter, so it fades out first.
            float along = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, StrikeTicks), 0f, 1f);
            trail.AddBlade(hand, direction, length, outer, inner, along);
        }

        // The blade is a line from the hand to the tip. The strike (and above all the spin) is fast, so the test checks the line at a few poses
        // between the previous tick and this one, each taken from the pose functions at that moment, and a thin enemy cannot slip through.
        bool BladeTouches(Rectangle box, float width)
        {
            int steps = Math.Max(1, PhasebladeTuning.SweepHitSteps);
            float length = PhasebladeTuning.BladeLength * Projectile.scale;
            for (int i = 1; i <= steps; i++)
            {
                float fraction = i / (float)steps;
                float time = poseTime - 1f + fraction;
                float arm = SwingArmAt(time);
                float tilt = SwingTiltAt(time);
                Vector2 shift = Vector2.Lerp(hitOwnerFrom, hitOwnerTo, fraction) - Owner.Center;
                Vector2 start = HeldSwordPose.HandWorld(Owner, WorldArm(arm)) + shift;
                Vector2 end = start + HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt)) * length;
                float point = 0f;
                if (Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), start, end, width, ref point))
                    return true;
            }
            return false;
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
            tipPosition = hand + blade.ToRotationVector2() * PhasebladeTuning.BladeLength * Projectile.scale;
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
            return BladeTouches(targetHitbox, PhasebladeTuning.HitWidth * Projectile.scale);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            PhasebladeFx.HitSpark(target.Center, BladeColor);
            flash = Math.Max(flash, 0.6f);
        }

        // The trail outlives the swing and keeps fading on its own (DetachedTrails). The owner remembers how this swing ended, so the next
        // click continues the combo from there (the arm angle is wrapped to -180..180 degrees: it is the same pose, and the next swing's
        // loop is computed from it).
        public override void OnKill(int timeLeft)
        {
            if (IsOwner)
            {
                PhasebladePlayer state = Owner.GetModPlayer<PhasebladePlayer>();
                state.LastPhase = (int)CurrentPhase;
                state.LastEndArm = MathHelper.WrapAngle(armLocal);
                state.LastEndTick = Main.GameUpdateCount;
            }
            DetachedTrails.Hand(trail);
            trail = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            int itemType = Owner.HeldItem.type;
            if (!PhasebladeTuning.IsPhaseblade(itemType))
                return false;
            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);

            Vector2 position = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;
            Color color = BladeColor;

            // Glow: steady while the blade is out, flaring on the strike and on a hit. It is drawn around the CENTER of the blade sprite (not
            // the hilt), so it swells outward evenly instead of stretching toward the tip.
            float glow = 0.3f + flash;
            Vector2 hiltToCenter = new Vector2(Projectile.spriteDirection > 0 ? texture.Width * 0.5f : -texture.Width * 0.5f, -texture.Height * 0.5f);
            Vector2 centerPosition = position + (hiltToCenter * scale).RotatedBy(rotation);
            Vector2 centerOrigin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, centerPosition, null, color * (0.5f * glow), rotation, centerOrigin, scale * (1.25f + 0.15f * flash), effects, 0);
            Main.EntitySpriteDraw(texture, centerPosition, null, color * (0.6f * glow), rotation, centerOrigin, scale * (1.1f + 0.08f * flash), effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.6f + 0.3f * flash), rotation, origin, scale, effects, 0);
            if (flash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, PhasebladeTuning.CoreOf(color) * (0.6f * flash), rotation, origin, scale, effects, 0);
            return false;
        }
    }
}