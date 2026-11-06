using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// The held Volcano (patch 1): ONE projectile that lives for as long as the sword is held (VolcanoPlayer spawns it). It poses the arm,
    /// draws the handle sprite and feeds the procedural lava blade to the metaball layers (VolcanoBlade). Holding the button swings it:
    /// a chop centered on the cursor (anticipation, a fast strike that decelerates, a recover), repeating while the button stays down; between
    /// swings the sword rests in a ready pose with the flames rising. The strike damages (once per enemy per swing) and every true melee hit
    /// adds heat (VolcanoPlayer.Heat): the blade grows hotter and more violent, up to fully charged. At full heat a right click throws the whole sword (patch 2, VolcanoThrownProjectile) and every enemy the blade hits bursts into a small lava explosion (VolcanoBlastProjectile).
    /// MULTIPLAYER: the owner's client decides everything (the click, the aim, the speed, the hits, the heat). What the other machines need
    /// travels in SendExtraAI: the heat (a byte), a swing serial, the swing's aim and speed; they start the same swing when the serial
    /// changes and run the same timers for pose, trail, sound and sparks. Hits and damage are the owner's alone (RemoteHitTracker gives the
    /// others their hit sparks). The lava itself is visual only, each machine feeds its own metaball layers. No custom packets.
    /// </summary>
    public class VolcanoBladeProjectile : ModProjectile
    {
        // The vanilla item sprite is the stand-in handle until VolcanoHandle.png exists.
        public override string Texture => "Terraria/Images/Item_" + ItemID.FieryGreatsword;

        static Asset<Texture2D> handleArt;
        public static Asset<Texture2D> HandleArt => handleArt; // the thrown sword draws the same handle

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // ---- synced (SendExtraAI / ReceiveExtraAI) ----
        int swingSerial;          // changes with every swing the owner starts
        float swingAim;           // the swing's aim (radians, the player's own frame)
        float swingSpeed = 1f;    // the owner's melee speed at the click
        float heatTarget;         // the real heat 0..1

        // ---- per machine: counters and visuals, never synced ----
        bool pendingSwing;        // a swing the owner started that this machine has not begun yet
        float pendingAim;
        float pendingSpeed = 1f;
        float lastSentHeat = -1f;
        int ticks;
        bool swinging;
        float timer;              // ticks since the swing began
        int facing = 1;           // the side the sword is on (the swing's side while swinging, else the player's)
        float aim;                // the swing's aim
        float timeScale = 1f;     // 1 / melee speed
        float startArmRel;        // the swing's start pose: arm relative to the aim (mirrored by the facing, positive = below) ...
        float startTilt;          // ... and the blade tilt
        float armRel;             // the current swing pose
        float tiltRel;
        float armLocal = MathHelper.ToRadians(VolcanoTuning.ReadyArmDeg); // the resting pose, facing-local (positive = down)
        float tiltRad = MathHelper.ToRadians(VolcanoTuning.ReadyTiltDeg);
        float heatShown;          // the heat that is drawn: eases toward the real one
        bool wasCharged;
        bool strikeStarted;
        bool rightWasDown;        // owner: the right button on the previous tick (a throw needs a fresh press)
        float flash;
        Vector2 lastOwnerCenter;
        Vector2 prevCenter;       // the hand and angle on the previous tick: the hit test sweeps from them
        float prevRotation;
        Vector2 prevTip;
        Vector2 tipMotion;        // how far the tip moved this tick, as handed to the flames and drips (zero for a moment after the player turns)
        int noInherit;            // ticks left in which the movement is ignored
        int lastFacing = 1;
        float swingHeat;          // the heat at the click (owner only): decides this swing's damage
        bool swingGaveHeat;       // owner: this swing already added its heat (a swing counts at most one hit toward the charge)
        Vector2 jointPosition;
        Vector2 tipPosition;
        SweepTrail trail;
        VolcanoSweepTrailStyle trailStyle;
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
            Projectile.timeLeft = 10000; // refilled every tick: the blade lives as long as the sword is held
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once per swing (the owner clears it when a swing starts)
            Projectile.ownerHitCheck = true;
        }

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Volcano/VolcanoHandle", out handleArt);
        }

        public override void Unload()
        {
            handleArt = null;
        }

        // ---- helpers ----

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        float TotalLength => (VolcanoTuning.HandleLength + VolcanoTuning.BladeLength) * Projectile.scale * 0.8f;

        bool IsUp => startArmRel > 0f;
        float Travel => IsUp ? -1f : 1f;

        float FromArm => Rad(IsUp ? VolcanoTuning.SwingLowDeg : VolcanoTuning.SwingHighDeg);
        float ToArm => Rad(IsUp ? VolcanoTuning.SwingHighDeg : VolcanoTuning.SwingLowDeg);
        float FromTilt => Rad(IsUp ? VolcanoTuning.TiltLowDeg : VolcanoTuning.TiltHighDeg);
        float ToTilt => Rad(IsUp ? VolcanoTuning.TiltHighDeg : VolcanoTuning.TiltLowDeg);

        float StrikeStart => VolcanoTuning.AnticipateTicks * timeScale;
        float StrikeEnd => StrikeStart + VolcanoTuning.SwingTicks * timeScale;
        float TotalTicks => StrikeEnd + VolcanoTuning.RecoverTicks * timeScale;

        bool Damaging => swinging && timer >= StrikeStart && timer <= StrikeEnd;

        // The arm and the blade as world angles: relative to the aim and mirrored by the facing.
        float WorldArm(float arm)
        {
            return aim + facing * arm;
        }

        float WorldBlade(float arm, float tilt)
        {
            return aim + facing * (arm + tilt);
        }

        // 0 until the strike starts, then eased from 0 to 1: fastest right after the anticipation, decelerating until the strike ends.
        float SwingProgress(float time)
        {
            float t = MathHelper.Clamp((time - StrikeStart) / Math.Max(0.01f, VolcanoTuning.SwingTicks * timeScale), 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, Math.Max(1f, VolcanoTuning.SwingEasePower));
        }

        float SwingArmAt(float time)
        {
            float back = FromArm - Travel * Rad(VolcanoTuning.AnticipateDeg);
            float over = ToArm + Travel * Rad(VolcanoTuning.OvershootDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(startArmRel, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(0.01f, StrikeStart)));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(back, over, SwingProgress(time));
            return MathHelper.Lerp(over, ToArm, MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(0.01f, VolcanoTuning.RecoverTicks * timeScale)));
        }

        float SwingTiltAt(float time)
        {
            float back = FromTilt - Travel * Rad(VolcanoTuning.AnticipateTiltDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(startTilt, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(0.01f, StrikeStart)));
            return MathHelper.Lerp(back, ToTilt, SwingProgress(time));
        }

        // ---- sync ----

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)MathHelper.Clamp((float)Math.Round(heatTarget * 255f), 0f, 255f));
            writer.Write((byte)(swingSerial & 255));
            writer.Write(swingAim);
            writer.Write(swingSpeed);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            heatTarget = reader.ReadByte() / 255f;
            int serial = reader.ReadByte();
            float readAim = reader.ReadSingle();
            float readSpeed = reader.ReadSingle();
            if (serial != swingSerial)
            {
                swingSerial = serial;
                pendingSwing = true;
                pendingAim = readAim;
                pendingSpeed = readSpeed;
            }
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.FieryGreatsword)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 10000;
            Projectile.scale = VolcanoTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            if (ticks == 0)
            {
                lastOwnerCenter = owner.Center;
                facing = owner.direction >= 0 ? 1 : -1;
                if (IsOwner)
                    heatTarget = owner.GetModPlayer<VolcanoPlayer>().Heat;
                heatShown = heatTarget;
                wasCharged = heatShown >= VolcanoTuning.ChargedAt;
                if (IsOwner)
                    rightWasDown = owner.controlUseTile; // a button already held when the blade forms is not a fresh press (no second throw)
            }

            if (IsOwner)
            {
                // The projectile carries the item's current damage (buffs and reforges change it).
                Item item = owner.HeldItem;
                Projectile.damage = owner.GetWeaponDamage(item);
                Projectile.knockBack = owner.GetWeaponKnockback(item, item.knockBack);
                Projectile.CritChance = owner.GetWeaponCrit(item);

                heatTarget = owner.GetModPlayer<VolcanoPlayer>().Heat;
                if (Math.Abs(heatTarget - lastSentHeat) >= VolcanoTuning.HeatSyncStep || (heatTarget != lastSentHeat && (heatTarget <= 0f || heatTarget >= 1f)))
                {
                    lastSentHeat = heatTarget;
                    Projectile.netUpdate = true;
                }
            }
            heatShown += (heatTarget - heatShown) * MathHelper.Clamp(VolcanoTuning.HeatShowEase, 0.01f, 1f);
            if (Math.Abs(heatTarget - heatShown) < 0.002f)
                heatShown = heatTarget;

            // Right click at full heat: the whole sword is thrown. A fresh press only, and not while the click is used on a chest, a door or the
            // inventory. Below full heat it does nothing.
            if (IsOwner)
            {
                bool rightDown = owner.controlUseTile && !owner.mouseInterface && !owner.tileInteractionHappened;
                bool pressed = rightDown && !rightWasDown;
                rightWasDown = rightDown;
                if (pressed && heatTarget >= VolcanoTuning.ChargedAt)
                {
                    Throw(owner);
                    return;
                }
            }

            // A swing starts: the owner's click (the button is held), or the owner's swing reaching this machine.
            if (!swinging)
            {
                if (IsOwner && owner.controlUseItem && !owner.mouseInterface)
                    BeginSwing(HeldAim.Capture(owner), MathHelper.Clamp(owner.GetTotalAttackSpeed(DamageClass.Melee) / MathHelper.Lerp(1f, VolcanoTuning.HeavyTimeScale, heatTarget), VolcanoTuning.MinSpeed, VolcanoTuning.MaxSpeed)); // the speed that is synced already includes the heaviness
                else if (pendingSwing)
                {
                    pendingSwing = false;
                    BeginSwing(pendingAim, MathHelper.Clamp(pendingSpeed, VolcanoTuning.MinSpeed, VolcanoTuning.MaxSpeed));
                }
            }

            prevCenter = Projectile.Center; // the previous tick's pose (the first tick fixes it below)
            prevRotation = Projectile.rotation;
            prevTip = tipPosition;

            float worldArm;
            float worldBlade;
            if (swinging)
            {
                if (IsOwner)
                    owner.ChangeDir(facing); // every tick, so nothing (movement keys) can turn the player away from the swing
                armRel = SwingArmAt(timer);
                tiltRel = SwingTiltAt(timer);
                worldArm = WorldArm(armRel);
                worldBlade = WorldBlade(armRel, tiltRel);
                owner.itemAnimation = 2; // keeps the item "in use" for as long as the swing lasts
                owner.itemTime = 2;
            }
            else
            {
                // The ready pose: the arm eases into it after a swing and bobs a little, so the sword is never dead still.
                facing = owner.direction >= 0 ? 1 : -1;
                float bob = (float)Math.Sin(MathHelper.TwoPi * ticks / Math.Max(1, VolcanoTuning.IdleBobTicks)) * Rad(VolcanoTuning.IdleBobDeg);
                float ease = MathHelper.Clamp(VolcanoTuning.IdleEase, 0.01f, 1f);
                float readyArm = Rad(MathHelper.Lerp(VolcanoTuning.ReadyArmDeg, VolcanoTuning.ChargedReadyArmDeg, heatShown));
                float readyTilt = Rad(MathHelper.Lerp(VolcanoTuning.ReadyTiltDeg, VolcanoTuning.ChargedReadyTiltDeg, heatShown));
                armLocal += MathHelper.WrapAngle(readyArm + bob - armLocal) * ease;
                tiltRad += (readyTilt - tiltRad) * ease;
                worldArm = HeldSwordPose.ToWorld(facing, armLocal);
                worldBlade = HeldSwordPose.ToWorld(facing, armLocal + tiltRad);
            }

            ApplyPose(owner, worldArm, worldBlade);
            if (ticks == 0)
            {
                prevCenter = Projectile.Center;
                prevRotation = Projectile.rotation;
                prevTip = tipPosition;
            }

            // The sword jumps from one side of the player to the other when they turn: that is no real movement, so for a moment the flames and
            // drips do not inherit the tip's velocity (they flew far to the sides).
            if (ticks > 0 && facing != lastFacing)
                noInherit = VolcanoTuning.TurnNoInheritTicks;
            lastFacing = facing;
            tipMotion = noInherit > 0 ? Vector2.Zero : tipPosition - prevTip;
            if (noInherit > 0)
                noInherit--;

            if (!Main.dedServ)
            {
                Vector2 direction = Projectile.rotation.ToRotationVector2();
                VolcanoBlade.Feed(jointPosition, direction, VolcanoTuning.BladeLength * Projectile.scale, Projectile.scale, heatShown, ticks / 60f, Projectile.owner, tipMotion);

                bool charged = heatShown >= VolcanoTuning.ChargedAt;
                if (charged && !wasCharged)
                    VolcanoFx.Charged(jointPosition, tipPosition);
                if (charged || heatShown < 0.85f)
                    wasCharged = charged;
            }

            if (swinging)
            {
                SwingEffects();
                if (!IsOwner && Damaging)
                    remoteHits.Update(Projectile, swingSerial, npc => VolcanoFx.HitSpark(npc.Center, heatShown));

                flash = Math.Max(0f, flash - 1f / Math.Max(1, VolcanoTuning.FlashTicks));
                trail?.Tick();
                timer++;
                if (timer >= TotalTicks)
                    EndSwing(owner);
            }
            else
            {
                flash = Math.Max(0f, flash - 1f / Math.Max(1, VolcanoTuning.FlashTicks));
            }

            lastOwnerCenter = owner.Center;
            ticks++;
        }

        // Starts a swing from the pose the sword is in right now (no jump). The owner also tells everyone else and clears the hit memory.
        void BeginSwing(float swingAimAngle, float speed)
        {
            swinging = true;
            timer = 0f;
            strikeStarted = false;
            aim = swingAimAngle;
            facing = HeldAim.Facing(aim);
            timeScale = 1f / speed;
            startArmRel = MathHelper.WrapAngle(HeldSwordPose.ToWorld(facing, armLocal) - aim) * facing;
            startTilt = tiltRad;
            armRel = startArmRel;
            tiltRel = startTilt;
            DetachedTrails.Hand(trail);
            trail = null;

            if (IsOwner)
            {
                swingSerial = (swingSerial + 1) & 255;
                swingHeat = heatTarget;
                swingGaveHeat = false;
                swingAim = aim;
                swingSpeed = speed;
                Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length); // every enemy can be hit again by this swing
                Projectile.netUpdate = true;
                Owner.ChangeDir(facing);
            }
        }

        // The swing is over: the sword keeps the pose it ended in (turned back to the facing-local angles the ready pose uses) and eases
        // into the ready pose; if the button is still down the next swing starts on the very next tick.
        void EndSwing(Player owner)
        {
            armLocal = HeldSwordPose.ToWorld(facing, WorldArm(armRel));
            tiltRad = tiltRel;
            swinging = false;
            owner.itemAnimation = 0;
            owner.itemTime = 0;
            DetachedTrails.Hand(trail);
            trail = null;
        }

        void SwingEffects()
        {
            if (!strikeStarted && timer >= StrikeStart)
            {
                strikeStarted = true;
                flash = 1f;
                VolcanoFx.PlaySwing(tipPosition, heatShown);
            }

            if (timer >= StrikeStart && timer < StrikeEnd)
            {
                // Many points per tick, laid between the previous tick's pose and this one, so the pixel line follows a smooth arc.
                int samples = Math.Max(1, VolcanoTuning.TrailSamplesPerTick);
                for (int k = 0; k < samples; k++)
                {
                    float fraction = (k + 1f) / samples;
                    AddTrailSample(timer - 1f + fraction, fraction);
                }
                if (trailStyle != null)
                    trailStyle.Heat = heatShown;
                VolcanoFx.SwingEmbers(tipPosition, tipMotion, heatShown);
                Lighting.AddLight(tipPosition, 1f, 0.5f, 0.12f);
            }
        }

        // 'fraction' is how far between the previous tick and this one the sample is (0..1): the owner is placed where they were at that
        // moment, so the trail stays one connected line while the player moves during the swing.
        void AddTrailSample(float time, float fraction)
        {
            if (Main.dedServ)
                return;

            time -= VolcanoTuning.TrailLagTicks;
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm)) + shift;
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            float length = TotalLength * VolcanoTuning.TrailLengthScale;
            // The fan starts as a line at the tip and its inner edge slides down the blade toward the hand while the swing builds up.
            float expand = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(SwingProgress(time) / Math.Max(0.01f, VolcanoTuning.TrailExpandBy), 0f, 1f));
            float inner = MathHelper.Lerp(1f, VolcanoTuning.TrailInner, expand);
            if (trail == null)
            {
                trailStyle = new VolcanoSweepTrailStyle();
                trail = new SweepTrail(trailStyle, IsOwner);
            }
            float along = MathHelper.Clamp((time - StrikeStart) / Math.Max(0.01f, VolcanoTuning.SwingTicks * timeScale), 0f, 1f);
            trail.Add(hand + direction * length, hand + direction * length * inner, along);
        }

        // The blade is a line from the hand to the tip. The strike is fast, so the test sweeps: it checks the line at a few poses between the
        // previous tick and this one, and a thin enemy cannot slip through between two ticks.
        bool BladeTouches(Rectangle box, float width)
        {
            int steps = Math.Max(1, VolcanoTuning.SweepHitSteps);
            float deltaAngle = MathHelper.WrapAngle(Projectile.rotation - prevRotation);
            for (int i = 1; i <= steps; i++)
            {
                float fraction = i / (float)steps;
                Vector2 start = Vector2.Lerp(prevCenter, Projectile.Center, fraction);
                Vector2 end = start + (prevRotation + deltaAngle * fraction).ToRotationVector2() * TotalLength;
                float point = 0f;
                if (Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), start, end, width, ref point))
                    return true;
            }
            return false;
        }

        // Moves the owner's arm and puts the projectile in the hand; the blade points along Projectile.rotation.
        void ApplyPose(Player owner, float worldArm, float worldBlade)
        {
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, worldArm - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, worldArm);
            float blade = worldBlade;
            if (owner.gravDir == -1f)
                blade = -blade;
            Projectile.rotation = blade;
            Projectile.Center = hand;
            Projectile.spriteDirection = facing;
            Vector2 direction = blade.ToRotationVector2();
            jointPosition = hand + direction * (VolcanoTuning.HandleLength * Projectile.scale);
            tipPosition = jointPosition + direction * (VolcanoTuning.BladeLength * Projectile.scale);
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
            return BladeTouches(targetHitbox, VolcanoTuning.HitWidth * Projectile.scale);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
            modifiers.SourceDamage *= MathHelper.Lerp(1f, VolcanoTuning.HeavyDamageMultiplier, swingHeat);
            modifiers.Knockback *= MathHelper.Lerp(1f, VolcanoTuning.HeavyKnockbackMultiplier, swingHeat);
        }

        // Owner's client only. A true melee hit of the swing: the vanilla burn, sparks, a camera punch, and heat for the blade.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, VolcanoTuning.OnFireTicks + (int)(VolcanoTuning.OnFireHeatBonusTicks * heatShown));
            VolcanoFx.HitSpark(target.Center, heatShown);
            CameraShake.Punch(target.Center, VolcanoTuning.HitShake * (0.6f + heatShown), 8);
            if (Owner.GetModPlayer<VolcanoPlayer>().Heat >= VolcanoTuning.ChargedAt)
                SpawnBlast(target); // already fully charged before this hit
            if (!swingGaveHeat)
            {
                swingGaveHeat = true; // only the first hit of a swing charges the blade
                VolcanoPlayer.AddHeat(Owner, VolcanoTuning.HeatPerHit);
                Projectile.netUpdate = true; // the other machines hear the new heat through SendExtraAI
            }
        }

        // Owner's client. The whole sword leaves the hand, pointed at the cursor, tip first (VolcanoThrownProjectile), and this held blade ends.
        // VolcanoPlayer keeps the hand empty while the thrown sword exists and for RegrowTicks after it.
        void Throw(Player owner)
        {
            float aim = HeldAim.Capture(owner);
            owner.ChangeDir(HeldAim.Facing(aim));
            Vector2 hand = HeldSwordPose.HandWorld(owner, aim);
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(owner, aim);
            Vector2 tip = hand + direction * ((VolcanoTuning.HandleLength + VolcanoTuning.BladeLength) * Projectile.scale);
            int type = ModContent.ProjectileType<VolcanoThrownProjectile>();
            int index = Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), tip, direction * VolcanoTuning.ThrowSpeed, type,
                Projectile.damage, Projectile.knockBack, owner.whoAmI, 0f, -1f, direction.ToRotation());
            if (index >= 0 && index < Main.maxProjectiles)
            {
                Projectile thrown = Main.projectile[index];
                thrown.CritChance = Projectile.CritChance;
                (thrown.ModProjectile as VolcanoThrownProjectile)?.Setup(Projectile.scale);
            }
            Projectile.Kill();
        }

        // Owner's client. A fully charged swing: the enemy that was hit bursts into a small lava explosion that also hurts what stands next to it.
        void SpawnBlast(NPC target)
        {
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            Vector2 point = Vector2.Clamp(Projectile.Center + direction * TotalLength * 0.7f, target.TopLeft, target.BottomRight);
            int damage = (int)(Projectile.damage * VolcanoTuning.BlastDamageMultiplier);
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), point, Vector2.Zero, ModContent.ProjectileType<VolcanoBlastProjectile>(), damage, 0f, Projectile.owner);
        }

        // The trail outlives the blade and keeps fading on its own (DetachedTrails).
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            // The lava (blade, flames, drips, droplets) in front of enemies, AFTER the trail so the trail is always behind it: the layers ask
            // to be drawn from here, once per frame.
            VolcanoFx.DrawLayers();

            DrawHandle(lightColor);
            DrawBladeDebug();
            return false;
        }

        // Your art: VolcanoHandle.png, drawn like a vanilla sword (tip to the upper right, 45 degrees) with its pivot at the CENTRE of the canvas,
        // placed on the joint where the lava blade starts (pad the canvas with transparent pixels to put the joint in the middle). Until the
        // file exists, the vanilla sprite cropped to its hilt corner stands in.
        void DrawHandle(Color lightColor)
        {
            VolcanoBlade.DrawHandle(handleArt, TextureAssets.Projectile[Type].Value, Projectile.Center, jointPosition, Projectile.rotation, Projectile.spriteDirection, Projectile.scale, lightColor, heatShown, flash);
        }

        // VolcanoTuning.ShowBladeDebug: a magenta square on the tip the hit test uses and a green square on the joint, so HandleLength and
        // BladeLength can be matched to your art by eye.
        void DrawBladeDebug()
        {
            if (!VolcanoTuning.ShowBladeDebug)
                return;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 tip = Projectile.Center + Projectile.rotation.ToRotationVector2() * TotalLength;
            Main.EntitySpriteDraw(pixel, tip - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.Magenta, 0f, new Vector2(0.5f), 5f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(pixel, jointPosition - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.LimeGreen, 0f, new Vector2(0.5f), 5f, SpriteEffects.None, 0);
        }
    }
}