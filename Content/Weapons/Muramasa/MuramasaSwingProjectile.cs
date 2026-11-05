using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Muramasa
{
    /// <summary>
    /// The held Muramasa while it swings (patch 1): a custom melee chop centered on the cursor (the middle of the slash points at it): a short
    /// anticipation, a fast strike that decelerates, a short recover. It starts from the exact pose the sheathing stance was in when the button
    /// was pressed, so there is no jump, and it swings DOWN when the arm is above the aim line and UP when it is below it (so a sword that
    /// is sheathed at the hip is drawn upward). An expanding sweep trail (SweepTrail) fills the area the blade crosses. When it ends, the
    /// owner starts a new MuramasaStanceProjectile from the pose the swing ended in (the sword begins sheathing again).
    /// MULTIPLAYER: the owner's client decides everything (the click, the aim, the start pose) and passes what the other machines need in
    /// the spawn packet: ai[0] = the arm's start angle (facing-local radians), ai[1] = the blade tilt's start angle, ai[2] = the aim
    /// (radians, the player's own frame). Every machine then runs the same timers for the pose, sound, dust and trail. Hits are detected
    /// by the owner's client like any projectile. No custom packets.
    /// </summary>
    public class MuramasaSwingProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Muramasa;

        float StartTilt => Projectile.ai[1];
        float Aim => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        // A swing faces the side of its aim on every machine (so nobody's pose lags the synced direction).
        int Facing => Math.Cos(Aim) >= 0.0 ? 1 : -1;

        // The arm's start angle relative to the aim, mirrored by the facing (positive = below the aim line).
        float StartArm => MathHelper.WrapAngle(HeldSwordPose.ToWorld(Facing, Projectile.ai[0]) - Aim) * Facing;
        bool IsUp => StartArm > 0f;

        // Per-machine state: counters and visuals, never synced.
        int ticks;             // ticks this machine has run this projectile
        float timer;           // ticks since the swing began
        float flash;           // 1 at the start of the strike, fades to 0
        float armLocal;        // arm angle relative to the aim for a player facing right (positive = down)
        float tiltRad;         // blade tilt relative to the arm
        Vector2 tipPosition;
        Vector2 lastOwnerCenter; // where the owner was on the previous tick: the trail interpolates the owner's movement from it
        Vector2 prevCenter;      // the blade's hand position and angle on the previous tick: the hit test sweeps from them
        float prevRotation;
        SweepTrail trail;      // the fan behind the blade (null until the strike lays a point)

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

        // The arm and the blade as world angles: relative to the aim and mirrored by the facing.
        float WorldArm(float arm)
        {
            return Aim + Facing * arm;
        }

        float WorldBlade(float arm, float tilt)
        {
            return Aim + Facing * (arm + tilt);
        }

        // The swing runs from one end of the arc to the other: down goes high to low, up goes low to high.
        float FromArm => Rad(IsUp ? MuramasaTuning.SwingLowDeg : MuramasaTuning.SwingHighDeg);
        float ToArm => Rad(IsUp ? MuramasaTuning.SwingHighDeg : MuramasaTuning.SwingLowDeg);
        float FromTilt => Rad(IsUp ? MuramasaTuning.TiltLowDeg : MuramasaTuning.TiltHighDeg);
        float ToTilt => Rad(IsUp ? MuramasaTuning.TiltHighDeg : MuramasaTuning.TiltLowDeg);

        int StrikeStart => MuramasaTuning.AnticipateTicks;
        int StrikeEnd => StrikeStart + MuramasaTuning.SwingTicks;
        int TotalTicks => StrikeEnd + MuramasaTuning.RecoverTicks;

        // +1 when the arm angle grows during the swing (down), -1 when it shrinks (up).
        float Travel => IsUp ? -1f : 1f;

        // 0 until the strike starts, then eased from 0 to 1: fastest right after the anticipation, decelerating until the strike ends.
        float SwingProgress(float time)
        {
            float t = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, MuramasaTuning.SwingTicks), 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, Math.Max(1f, MuramasaTuning.SwingEasePower));
        }

        float SwingArmAt(float time)
        {
            float back = FromArm - Travel * Rad(MuramasaTuning.AnticipateDeg);   // pulled back, away from where the strike goes
            float over = ToArm + Travel * Rad(MuramasaTuning.OvershootDeg);      // carried a little past the end of the arc
            if (time < StrikeStart)
                return MathHelper.Lerp(StartArm, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(back, over, SwingProgress(time));
            return MathHelper.Lerp(over, ToArm, MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(1, MuramasaTuning.RecoverTicks)));
        }

        float SwingTiltAt(float time)
        {
            float back = FromTilt - Travel * Rad(MuramasaTuning.AnticipateTiltDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(StartTilt, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)));
            return MathHelper.Lerp(back, ToTilt, SwingProgress(time));
        }

        bool Damaging => timer >= StrikeStart && timer <= StrikeEnd;

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.Muramasa)
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

            if (IsOwner)
                owner.ChangeDir(Facing); // every tick, so nothing (movement keys) can turn the player away from the swing

            Projectile.scale = MuramasaTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

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

            flash = Math.Max(0f, flash - 1f / Math.Max(1, MuramasaTuning.FlashTicks));
            trail?.Tick();
            lastOwnerCenter = owner.Center;
            ticks++;
            timer++;
        }

        void SwingEffects()
        {
            if ((int)timer == StrikeStart)
            {
                flash = 1f;
                MuramasaFx.PlaySwing(tipPosition, IsUp);
            }

            if (timer >= StrikeStart && timer < StrikeEnd)
            {
                // Many points per tick, laid between the previous tick's pose and this one, so the pixel line follows a smooth arc.
                int samples = Math.Max(1, MuramasaTuning.TrailSamplesPerTick);
                for (int k = 0; k < samples; k++)
                {
                    float fraction = (k + 1f) / samples;
                    AddTrailSample(timer - 1f + fraction, fraction);
                }
                MuramasaFx.SlashDust(Main.LocalPlayer.Center, tipPosition);
                Lighting.AddLight(tipPosition, 0.3f, 0.55f, 1f);
            }
        }

        // 'fraction' is how far between the previous tick and this one the sample is (0..1): the owner is placed where they were at that
        // moment, so the trail stays one connected line while the player moves during the swing.
        void AddTrailSample(float time, float fraction)
        {
            if (Main.dedServ)
                return;

            time -= MuramasaTuning.TrailLagTicks;
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm)) + shift;
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            float length = MuramasaTuning.BladeLength * Projectile.scale * MuramasaTuning.TrailLengthScale;
            // The fan starts as a line at the tip and its inner edge slides down the blade toward the hand while the swing builds up.
            float expand = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(SwingProgress(time) / Math.Max(0.01f, MuramasaTuning.TrailExpandBy), 0f, 1f));
            float inner = MathHelper.Lerp(1f, MuramasaTuning.TrailInner, expand);
            trail ??= new SweepTrail(new MuramasaSweepTrailStyle(), IsOwner);
            // 'along' = where in the strike this sample is; the style (TailFirst) makes the tail live shorter, so it fades out first.
            float along = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, MuramasaTuning.SwingTicks), 0f, 1f);
            trail.Add(hand + direction * length, hand + direction * length * inner, along);
        }

        // The blade is a line from the hand to the tip. The strike is fast, so the test sweeps: it checks the line at a few poses between the
        // previous tick and this one, and a thin enemy cannot slip through between two ticks.
        bool BladeTouches(Rectangle box, float width)
        {
            int steps = Math.Max(1, MuramasaTuning.SweepHitSteps);
            float length = MuramasaTuning.BladeLength * Projectile.scale;
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
            tipPosition = hand + blade.ToRotationVector2() * MuramasaTuning.BladeLength * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return Damaging ? (bool?)null : false;
        }

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
            MuramasaFx.HitSpark(target.Center);
            CameraShake.Punch(target.Center, MuramasaTuning.HitShake, 8);
        }

        // The trail outlives the swing and keeps fading on its own (DetachedTrails). The owner starts the sheathing again from the pose
        // the swing ended in (converted to the facing-local angles the stance uses).
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;

            if (!IsOwner)
                return;
            Player owner = Owner;
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.Muramasa)
                return;
            float endArmLocal = HeldSwordPose.ToWorld(owner.direction >= 0 ? 1 : -1, WorldArm(armLocal));
            MuramasaStanceProjectile.Spawn(Projectile.GetSource_FromThis(), owner, endArmLocal, tiltRad);
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

            // Glow: steady while the sword is out, flaring at the strike.
            float glow = 0.25f + flash;
            Main.EntitySpriteDraw(texture, position, null, MuramasaTuning.DeepColor * (0.5f * glow), rotation, origin, scale * (1.2f + 0.2f * flash), effects, 0);
            Main.EntitySpriteDraw(texture, position, null, MuramasaTuning.BlueColor * (0.6f * glow), rotation, origin, scale * (1.08f + 0.1f * flash), effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.35f + 0.5f * flash), rotation, origin, scale, effects, 0);
            if (flash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, MuramasaTuning.CoreColor * (0.8f * flash), rotation, origin, scale, effects, 0);
            DrawBladeDebug();
            return false;
        }

        // MuramasaTuning.ShowBladeDebug: a magenta square on the tip the hit test uses and a green square where the trail's outer edge is
        // laid for this tick, so BladeLength, TrailLengthScale and TrailLagTicks can be matched to the sprite by eye.
        void DrawBladeDebug()
        {
            if (!MuramasaTuning.ShowBladeDebug)
                return;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.EntitySpriteDraw(pixel, tipPosition - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.Magenta, 0f, new Vector2(0.5f), 5f, SpriteEffects.None, 0);

            float time = timer - 1f - MuramasaTuning.TrailLagTicks; // timer has already been advanced for this tick
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm));
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            Vector2 edge = hand + direction * MuramasaTuning.BladeLength * Projectile.scale * MuramasaTuning.TrailLengthScale;
            Main.EntitySpriteDraw(pixel, edge - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.LimeGreen, 0f, new Vector2(0.5f), 5f, SpriteEffects.None, 0);
        }
    }
}