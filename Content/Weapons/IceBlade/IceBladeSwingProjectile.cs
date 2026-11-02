using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.IceBlade
{
    /// <summary>
    /// The held Ice Blade. It draws the blade, poses the owner's arm and runs the whole mechanic:
    /// CHARGE (button held): the arm lifts with a cubic ease-out, the blade flashes on every click (1 to 3 = tiers, 4 = freeze).
    /// TAP (released before click 1): a plain melee chop, no wave. SLASH (released on tier 1 to 3): the very same chop, plus a
    /// freezing trail behind the blade and an IceBladeWaveProjectile sent toward the cursor. FROZEN (click 4 reached while still holding): the
    /// owner gets the vanilla Frozen buff, which blocks every item for a few seconds.
    /// MULTIPLAYER: the owner's client decides (release, tier, aim, freeze, wave spawn) and syncs the decision through
    /// ai[0] phase, ai[1] tier, ai[2] aim angle (world radians) with netUpdate. Every machine runs the same timers from its own
    /// counter for the arm pose, click sounds, flash, dust and trail, so nothing but those three values is sent.
    /// Hits are detected by the owner's client like any projectile; the wave is a normal spawned (and therefore synced) projectile.
    /// </summary>
    public class IceBladeSwingProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.IceBlade;

        enum Phase { Charge = 0, Slash = 1, Tap = 2, Frozen = 3 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        int ReleasedTier => (int)Projectile.ai[1];
        float AimAngle => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int Facing => Owner.direction >= 0 ? 1 : -1;

        // Per-machine state: counters and visuals, never synced.
        int lastPhase = -1;
        int ticks;             // ticks this machine has run this projectile
        float timer;           // ticks inside the current phase
        float flash;           // 1 right after a click, fades to 0
        float opacity = 1f;
        bool holding = true;   // false once the blade is gone and only the trail is left
        float armLocal;        // arm direction in radians for a player facing right (positive = down)
        float tiltRad;         // blade tilt relative to the arm
        float lastChargeArm;
        Vector2 tipPosition;
        Vector2 lastTipPosition;
        PixelTrail trail;      // the freezing trail along the blade tip (null until a charged release swings)

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

        float ToWorld(float localRadians)
        {
            return HeldSwordPose.ToWorld(Facing, localRadians);
        }

        Vector2 ComputeHand(float arm)
        {
            return HeldSwordPose.Hand(Owner, Facing, arm);
        }

        Vector2 BladeDirection(float arm, float tilt)
        {
            return HeldSwordPose.BladeDirection(Owner, Facing, arm, tilt);
        }

        static int ChargeTier(int t)
        {
            int tier = 0;
            for (int click = 1; click <= 3; click++)
            {
                if (t >= IceBladeTuning.ClickTicks(click))
                    tier = click;
            }
            return tier;
        }

        bool Damaging
        {
            get
            {
                if (!holding)
                    return false;
                switch (CurrentPhase)
                {
                    case Phase.Slash:
                    case Phase.Tap:
                        return timer >= IceBladeTuning.TapWindupTicks && timer <= IceBladeTuning.TapWindupTicks + IceBladeTuning.TapSwingTicks;
                    default:
                        return false;
                }
            }
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;
            ticks++;
            Phase phase = CurrentPhase;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun, or a freeze that is not ours).
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            if (holding && (owner.noItems || owner.HeldItem.type != ItemID.IceBlade || (phase != Phase.Frozen && owner.CCed)))
            {
                Projectile.Kill();
                return;
            }

            if ((int)phase != lastPhase)
            {
                lastPhase = (int)phase;
                EnterPhase(phase);
            }

            if (holding)
                Projectile.scale = IceBladeTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            lastTipPosition = tipPosition;
            switch (phase)
            {
                case Phase.Charge:
                    UpdateCharge(owner);
                    break;
                case Phase.Slash:
                case Phase.Tap:
                    UpdateSwing(owner);
                    break;
                default:
                    UpdateFrozen(owner);
                    break;
            }
            if (!Projectile.active)
                return;

            flash = Math.Max(0f, flash - 1f / Math.Max(1, IceBladeTuning.ClickFlashTicks));
            trail?.Tick();

            if (holding)
            {
                owner.itemAnimation = 2; // keeps the item "in use" for as long as the blade is out
                owner.itemTime = 2;
                ApplyPose(owner);
            }
            timer++;
        }

        void EnterPhase(Phase phase)
        {
            timer = 0f;
            switch (phase)
            {
                case Phase.Slash:
                    trail = null;
                    break;
                case Phase.Frozen:
                    IceBladeFx.FreezeShatter(Projectile.Center);
                    IceBladeFx.PlayFreeze(Projectile.Center);
                    if (IsOwner)
                        Owner.AddBuff(BuffID.Frozen, IceBladeTuning.FreezeDebuffTicks); // the lockout: vanilla Frozen blocks every item
                    break;
            }
        }

        void UpdateCharge(Player owner)
        {
            int t = (int)timer;

            if (IsOwner)
                owner.ChangeDir(Main.MouseWorld.X >= owner.Center.X ? 1 : -1);

            for (int click = 1; click <= 4; click++)
            {
                if (t == IceBladeTuning.ClickTicks(click))
                    OnClick(click);
            }

            // Arm: lifts from low to high with a cubic ease-out, blade held at a fixed tilt.
            float lift = MathHelper.Clamp(timer / Math.Max(1, IceBladeTuning.LiftTicks), 0f, 1f);
            float eased = 1f - (float)Math.Pow(1f - lift, 3);
            armLocal = MathHelper.Lerp(Rad(IceBladeTuning.ArmStartDeg), Rad(IceBladeTuning.ArmEndDeg), eased);
            float danger = 0f; // 0 to 1 between the shake start and the freeze click
            if (t >= IceBladeTuning.ShakeStartTicks)
            {
                danger = MathHelper.Clamp((t - IceBladeTuning.ShakeStartTicks) / (float)Math.Max(1, IceBladeTuning.FreezeClickTicks - IceBladeTuning.ShakeStartTicks), 0f, 1f);
                armLocal += (float)Math.Sin(timer * 1.9f) * Rad(IceBladeTuning.ShakeDeg) * (0.3f + 0.7f * danger);
            }
            lastChargeArm = armLocal;
            tiltRad = Rad(IceBladeTuning.BladeTiltDeg);

            int tier = ChargeTier(t);
            if (t % 3 == 0)
                IceBladeFx.ChargeGather(tipPosition, tier);
            if (Main.rand.NextBool(4))
                IceBladeFx.BladeSparkle(Projectile.Center, tipPosition, tier);
            if (t >= IceBladeTuning.Click3Ticks)
                IceBladeFx.FrostOnHand(Projectile.Center, danger);
            Lighting.AddLight(tipPosition, 0.12f + 0.1f * tier + 0.5f * flash, 0.3f + 0.12f * tier + 0.5f * flash, 0.5f + 0.15f * tier + 0.5f * flash);

            if (!IsOwner)
                return;
            if (!owner.channel)
            {
                Release(t);
            }
            else if (t >= IceBladeTuning.FreezeClickTicks + IceBladeTuning.ReleaseGraceTicks)
            {
                Projectile.ai[0] = (float)Phase.Frozen;
                Projectile.netUpdate = true;
            }
        }

        void OnClick(int click)
        {
            flash = 1f;
            if (click <= 3)
            {
                IceBladeFx.PlayClick(tipPosition, click);
                IceBladeFx.ClickBurst(tipPosition, click);
            }
            else
            {
                IceBladeFx.PlayFreezeClick(tipPosition);
                IceBladeFx.FreezeClickBurst(tipPosition);
            }
        }

        // Owner only: the button was released. The tier is the number of clicks already heard (0 = tap).
        void Release(int t)
        {
            int tier = ChargeTier(t);
            Projectile.ai[0] = tier > 0 ? (float)Phase.Slash : (float)Phase.Tap;
            Projectile.ai[1] = tier;
            Projectile.ai[2] = (Main.MouseWorld - Owner.MountedCenter).ToRotation();
            Projectile.netUpdate = true;
        }

        // The release swing IS the tap swing: windup (from wherever the charge arm was), chop, recover. Both read this one timeline.
        float SwingProgress(float time)
        {
            float t = MathHelper.Clamp((time - IceBladeTuning.TapWindupTicks) / Math.Max(1, IceBladeTuning.TapSwingTicks), 0f, 1f);
            return MathHelper.SmoothStep(0f, 1f, t);
        }

        float SwingArmAt(float time)
        {
            int windup = IceBladeTuning.TapWindupTicks;
            if (time < windup)
            {
                float k = 1f - (float)Math.Pow(1f - time / Math.Max(1, windup), 2);
                return MathHelper.Lerp(lastChargeArm, Rad(IceBladeTuning.TapStartDeg), k);
            }
            return MathHelper.Lerp(Rad(IceBladeTuning.TapStartDeg), Rad(IceBladeTuning.TapEndDeg), SwingProgress(time));
        }

        float SwingTiltAt(float time)
        {
            return MathHelper.Lerp(Rad(IceBladeTuning.BladeTiltDeg), 0f, SwingProgress(time));
        }

        void UpdateSwing(Player owner)
        {
            int windup = IceBladeTuning.TapWindupTicks;
            int swing = IceBladeTuning.TapSwingTicks;
            int recover = IceBladeTuning.TapRecoverTicks;
            int t = (int)timer;
            bool released = CurrentPhase == Phase.Slash; // a charged release: the same swing plus trail and wave
            int tier = released ? ReleasedTier : 0;

            if (timer < windup + swing + recover)
            {
                armLocal = SwingArmAt(timer);
                tiltRad = SwingTiltAt(timer);
                if (timer > windup + swing)
                    opacity = 1f - (timer - windup - swing) / Math.Max(1, recover);

                if (t == windup)
                {
                    if (released)
                        IceBladeFx.PlaySlash(tipPosition, tier);
                    else
                        IceBladeFx.PlayTapSwing(tipPosition);
                }

                if (released && timer >= windup && timer < windup + swing)
                {
                    // Three points per tick, so the pixel line follows a smooth arc instead of a few straight segments.
                    for (int k = 0; k < 3; k++)
                        AddTrailSample(timer + k / 3f, tier);
                    IceBladeFx.SlashDust(lastTipPosition, tipPosition, tier);
                    Lighting.AddLight(tipPosition, 0.4f + 0.15f * tier, 0.8f + 0.1f * tier, 1.1f);
                }

                if (released && t == IceBladeTuning.SlashWaveTick)
                {
                    Vector2 direction = AimAngle.ToRotationVector2();
                    IceBladeFx.WaveBorn(WaveSpawnPoint(direction), direction, tier);
                    if (IsOwner)
                        FireWave(tier, direction);
                }
            }
            else
            {
                holding = false; // blade gone; after a charged release the projectile stays only to finish drawing the trail
                opacity = 0f;
            }

            int end = windup + swing + (released ? IceBladeTuning.TrailLifeTicks : recover);
            if (t >= end)
                Projectile.Kill();
        }

        void AddTrailSample(float time, int tier)
        {
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 hand = ComputeHand(arm);
            Vector2 direction = BladeDirection(arm, tilt);
            float length = IceBladeTuning.BladeLength * Projectile.scale;
            trail ??= new PixelTrail(new IceBladeTrailStyle(tier), IsOwner);
            trail.Add(hand + direction * length); // the blade's edge: the tip
        }

        // Where the wave starts: in front of the player, in the middle of the area the blade sweeps (a fraction of the blade's reach along the aim).
        Vector2 WaveSpawnPoint(Vector2 direction)
        {
            return Owner.MountedCenter + direction * IceBladeTuning.BladeLength * Projectile.scale * IceBladeTuning.WaveSpawnFraction;
        }

        // Owner only. The wave is a normal projectile, so tModLoader syncs it to everyone else.
        void FireWave(int tier, Vector2 direction)
        {
            int damage = Math.Max(1, (int)(Projectile.damage * IceBladeTuning.WaveDamageMultiplier(tier)));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), WaveSpawnPoint(direction), direction * IceBladeTuning.WaveSpeed(tier),
                ModContent.ProjectileType<IceBladeWaveProjectile>(), damage, Projectile.knockBack * IceBladeTuning.WaveKnockbackMultiplier(tier), Projectile.owner, tier);
        }

        void UpdateFrozen(Player owner)
        {
            armLocal = lastChargeArm + (float)Math.Sin(timer * 2.3f) * Rad(IceBladeTuning.ShakeDeg);
            tiltRad = Rad(IceBladeTuning.BladeTiltDeg);
            IceBladeFx.FrostOnHand(Projectile.Center, 1f);
            if (timer >= IceBladeTuning.FrozenAnimTicks)
                Projectile.Kill();
        }

        // Moves the owner's arm and puts the projectile in the hand; the blade points along Projectile.rotation.
        void ApplyPose(Player owner)
        {
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, ToWorld(armLocal) - MathHelper.PiOver2);
            Vector2 hand = ComputeHand(armLocal);
            float blade = ToWorld(armLocal + tiltRad);
            if (owner.gravDir == -1f)
                blade = -blade;
            Projectile.rotation = blade;
            Projectile.Center = hand;
            Projectile.spriteDirection = Facing;
            tipPosition = hand + blade.ToRotationVector2() * IceBladeTuning.BladeLength * Projectile.scale;
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
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * IceBladeTuning.BladeLength * Projectile.scale;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 18f * Projectile.scale, ref point);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            IceFx.Burst(target.Center, 6, 3f, 1f);
        }

        // The trail outlives the swing and keeps fading on its own (DetachedTrails).
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            DrawTrail();
            if (!holding || opacity <= 0f)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);

            Vector2 position = Projectile.Center - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;

            // Glow: steady and tier-based while charging, flaring on every click.
            float steady = CurrentPhase == Phase.Charge ? 0.1f * ChargeTier((int)timer) : 0.2f;
            float glow = steady + flash;
            if (glow > 0.01f)
            {
                Main.EntitySpriteDraw(texture, position, null, IceBladeTuning.HaloColor * (0.6f * glow * opacity), rotation, origin, scale * (1.2f + 0.2f * flash), effects, 0);
                Main.EntitySpriteDraw(texture, position, null, IceBladeTuning.MidColor * (0.6f * glow * opacity), rotation, origin, scale * (1.08f + 0.1f * flash), effects, 0);
            }
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.35f + 0.5f * flash) * opacity, rotation, origin, scale, effects, 0);
            if (flash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, IceBladeTuning.CoreColor * (0.8f * flash * opacity), rotation, origin, scale, effects, 0);
            return false;
        }

        // The freezing trail behind the blade tip: a PixelTrail (Common/PixelTrail.cs), drawn by the shared pixel line renderer.
        void DrawTrail()
        {
            trail?.Draw();
        }
    }
}