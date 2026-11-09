using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The held Enchanted Sword (patch 1): ONE projectile that lives for as long as the sword is held (EnchantedSwordPlayer spawns it). It
    /// poses the arm, draws the sword and runs the left click: swings centered on the cursor: up, down, up, down ... and a THRUST after an up or
    /// down swing that hit an enemy (then up or down again), while the button stays down. THE SWINGS FLOW: the projectile never ends between two swings (so the sprite never blinks out for a
    /// frame), and while the button is held a swing does not recover and restart: the moment a strike finishes, the next swing starts from
    /// the exact pose the old one is in, with a wind-up that carries the previous motion on. A thrust is the arm pointing at the cursor
    /// while the sword slides back through the fist and is then driven forward past it (the sprite is slid along the blade, so the hit line
    /// moves with it). Only when the button is let go does the arm settle back and rest in the ready pose.
    /// The first enemy an up or down swing touches only makes the NEXT swing a thrust. The first enemy each THRUST touches (a true melee hit)
    /// adds one hit (EnchantedSwordPlayer.Hits): only the thrust charges the sword. Each hit gives the blade a stronger ethereal outline (a
    /// pixel outline, Common/PixelOutline); the 3rd discharges: a stardust burst and the ghost sword (EtherealSwordProjectile).
    /// MULTIPLAYER: the owner's client decides everything (the click, the aim, the kind of swing, the hits). What the other machines need
    /// travels in SendExtraAI: the hit count (the outline stage), a swing serial with the swing's aim, speed, kind and whether it flows out
    /// of the previous one, and a discharge serial with the enemy it hit; they start the same swing when the serial changes and run the same
    /// timers for pose, trail, sound and sparks. Hits and damage are the owner's alone (RemoteHitTracker gives the others their hit
    /// sparks). No custom packets.
    /// </summary>
    public class EnchantedSwordBladeProjectile : ModProjectile
    {
        // The vanilla item sprite is the sword (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.EnchantedSword;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // ---- synced (SendExtraAI / ReceiveExtraAI) ----
        int swingSerial;          // changes with every swing the owner starts
        float swingAim;           // the swing's aim (radians, the player's own frame)
        float swingSpeed = 1f;    // the swing's speed (the owner's melee speed), decided at the click
        bool swingChained;        // the swing flows out of the previous one (no ready pose in between)
        int swingKind;            // 0 = up, 1 = down, 2 = thrust
        int stageTarget;          // the real hit count (0 .. HitsToDischarge - 1): the outline stage
        int dischargeSerial;      // changes with every discharge
        int dischargeTarget = -1; // the enemy the discharging hit touched

        // ---- per machine: counters and visuals, never synced ----
        bool pendingSwing;        // a swing the owner started that this machine has not begun yet
        float pendingAim;
        float pendingSpeed = 1f;
        bool pendingChained;
        int pendingKind;
        bool pendingDischarge;    // a discharge this machine has not played yet
        int pendingDischargeTarget = -1;
        bool syncedOnce;          // the first packet only tells where the counters are (a late joiner must not replay an old discharge)
        int stageSeen;            // the stage the stage-up effects were last played for
        int lastSentStage = -1;
        int ticks;
        bool swinging;
        float timer;              // ticks since the swing began
        int facing = 1;           // the side the sword is on (the swing's side while swinging, else the player's)
        float aim;                // the swing's aim
        float timeScale = 1f;     // 1 / the swing's speed
        bool isChained;           // this swing flows out of the previous one
        int kind;                 // the current swing's kind: 0 = up, 1 = down, 2 = thrust
        int lastKind;             // owner: the kind of the last swing (the next one follows from it, see NextKind)
        int lastSwingDir;         // owner: the last UP or DOWN swing (0 or 1): after a thrust the swings go on with the other one
        bool hasSwung;
        int sinceSwing;           // ticks since the last swing ended (the cycle starts over after CycleResetTicks)
        float startArmRel;        // the swing's start pose: arm relative to the aim (mirrored by the facing, positive = below) ...
        float startTilt;          // ... the blade tilt ...
        float startSlide;         // ... and how far the sword is slid along its blade (px, positive = forward)
        float armRel;             // the current swing pose
        float tiltRel;
        float slideNow;
        float armLocal = MathHelper.ToRadians(EnchantedSwordTuning.ReadyArmDeg); // the resting pose, facing-local (positive = down)
        float tiltRad = MathHelper.ToRadians(EnchantedSwordTuning.ReadyTiltDeg);
        float slideRest;          // the resting slide (eases to 0 in the ready pose)
        float stageShown;         // the stage that is drawn (0..2): eases toward the real one
        bool strikeStarted;
        bool swingGaveHit;        // owner: this swing already counted a hit (a swing counts at most one)
        float flash;
        Vector2 lastOwnerCenter;
        Vector2 prevCenter;       // the hand and angle on the previous tick: the hit test sweeps from them
        float prevRotation;
        Vector2 prevTip;
        Vector2 tipPosition;
        SweepTrail trail;
        EnchantedSwordSweepTrailStyle trailStyle;
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

        // ---- helpers ----

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        float TotalLength => EnchantedSwordTuning.BladeLength * Projectile.scale;

        bool IsThrust => kind == 2;
        bool IsUp => kind == 0;
        float Travel => IsUp ? -1f : 1f; // the swing's direction along the arc: up = toward negative angles

        float FromArm => Rad(IsUp ? EnchantedSwordTuning.SwingLowDeg : EnchantedSwordTuning.SwingHighDeg);
        float ToArm => Rad(IsUp ? EnchantedSwordTuning.SwingHighDeg : EnchantedSwordTuning.SwingLowDeg);
        float FromTilt => Rad(IsUp ? EnchantedSwordTuning.TiltLowDeg : EnchantedSwordTuning.TiltHighDeg);
        float ToTilt => Rad(IsUp ? EnchantedSwordTuning.TiltHighDeg : EnchantedSwordTuning.TiltLowDeg);

        // A swing that flows out of the previous one winds up longer and further: the previous strike's momentum carries it.
        float AnticipateTicksNow
        {
            get
            {
                if (IsThrust)
                    return isChained ? EnchantedSwordTuning.ThrustChainAnticipateTicks : EnchantedSwordTuning.ThrustAnticipateTicks;
                return isChained ? EnchantedSwordTuning.ChainAnticipateTicks : EnchantedSwordTuning.AnticipateTicks;
            }
        }
        float AnticipateRadNow => Rad(isChained ? EnchantedSwordTuning.ChainAnticipateDeg : EnchantedSwordTuning.AnticipateDeg);
        float StrikeTicksNow => IsThrust ? EnchantedSwordTuning.ThrustTicks : EnchantedSwordTuning.SwingTicks;

        float StrikeStart => AnticipateTicksNow * timeScale;
        float StrikeEnd => StrikeStart + StrikeTicksNow * timeScale;
        float TotalTicks => StrikeEnd + EnchantedSwordTuning.RecoverTicks * timeScale;

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
            float t = MathHelper.Clamp((time - StrikeStart) / Math.Max(0.01f, StrikeTicksNow * timeScale), 0f, 1f);
            float power = IsThrust ? EnchantedSwordTuning.ThrustEasePower : EnchantedSwordTuning.SwingEasePower;
            return 1f - (float)Math.Pow(1f - t, Math.Max(1f, power));
        }

        float SwingArmAt(float time)
        {
            if (IsThrust)
            {
                // The arm comes to the pull-back angle, then straightens at the cursor.
                float windArm = Rad(EnchantedSwordTuning.ThrustWindArmDeg);
                if (time < StrikeStart)
                    return MathHelper.Lerp(startArmRel, windArm, MathHelper.SmoothStep(0f, 1f, time / Math.Max(0.01f, StrikeStart)));
                return MathHelper.Lerp(windArm, 0f, SwingProgress(time));
            }
            float back = FromArm - Travel * AnticipateRadNow;
            float over = ToArm + Travel * Rad(EnchantedSwordTuning.OvershootDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(startArmRel, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(0.01f, StrikeStart)));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(back, over, SwingProgress(time));
            return MathHelper.Lerp(over, ToArm, MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(0.01f, EnchantedSwordTuning.RecoverTicks * timeScale)));
        }

        float SwingTiltAt(float time)
        {
            if (IsThrust)
            {
                // The blade stays parallel to the aim: its tilt cancels the arm's.
                float windTilt = -Rad(EnchantedSwordTuning.ThrustWindArmDeg);
                if (time < StrikeStart)
                    return MathHelper.Lerp(startTilt, windTilt, MathHelper.SmoothStep(0f, 1f, time / Math.Max(0.01f, StrikeStart)));
                return MathHelper.Lerp(windTilt, 0f, SwingProgress(time));
            }
            float back = FromTilt - Travel * Rad(EnchantedSwordTuning.AnticipateTiltDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(startTilt, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(0.01f, StrikeStart)));
            return MathHelper.Lerp(back, ToTilt, SwingProgress(time));
        }

        // How far the sword is slid along its blade (px, positive = forward): a swing slides it back to the hand, a thrust pulls it back
        // through the fist and then drives it forward past it.
        float SwingSlideAt(float time)
        {
            float anticipate = MathHelper.SmoothStep(0f, 1f, time / Math.Max(0.01f, StrikeStart));
            if (!IsThrust)
                return time < StrikeStart ? MathHelper.Lerp(startSlide, 0f, anticipate) : 0f;
            float pull = -EnchantedSwordTuning.ThrustPullPx * Projectile.scale;
            float reach = EnchantedSwordTuning.ThrustReachPx * Projectile.scale;
            if (time < StrikeStart)
                return MathHelper.Lerp(startSlide, pull, anticipate);
            if (time <= StrikeEnd)
                return MathHelper.Lerp(pull, reach, SwingProgress(time));
            return MathHelper.Lerp(reach, reach * EnchantedSwordTuning.ThrustSettle, MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(0.01f, EnchantedSwordTuning.RecoverTicks * timeScale)));
        }

        // ---- sync ----

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)MathHelper.Clamp(stageTarget, 0, 255));
            writer.Write((byte)(swingSerial & 255));
            writer.Write(swingAim);
            writer.Write(swingSpeed);
            writer.Write(swingChained);
            writer.Write((byte)swingKind);
            writer.Write((byte)(dischargeSerial & 255));
            writer.Write((short)dischargeTarget);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            stageTarget = reader.ReadByte();
            int serial = reader.ReadByte();
            float readAim = reader.ReadSingle();
            float readSpeed = reader.ReadSingle();
            bool readChained = reader.ReadBoolean();
            int readKind = reader.ReadByte();
            int readDischarge = reader.ReadByte();
            int readTarget = reader.ReadInt16();
            if (!syncedOnce)
                dischargeSerial = readDischarge;
            else if (readDischarge != dischargeSerial)
            {
                dischargeSerial = readDischarge;
                pendingDischarge = true;
                pendingDischargeTarget = readTarget;
            }
            syncedOnce = true;
            if (serial != swingSerial)
            {
                swingSerial = serial;
                pendingSwing = true;
                pendingAim = readAim;
                pendingSpeed = readSpeed;
                pendingChained = readChained;
                pendingKind = readKind;
            }
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.EnchantedSword)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 10000;
            Projectile.scale = EnchantedSwordTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            if (ticks == 0)
            {
                lastOwnerCenter = owner.Center;
                facing = owner.direction >= 0 ? 1 : -1;
                if (IsOwner)
                    stageTarget = owner.GetModPlayer<EnchantedSwordPlayer>().Hits;
                stageShown = stageTarget;
                stageSeen = stageTarget;
            }

            if (IsOwner)
            {
                // The projectile carries the item's current damage (buffs and reforges change it).
                Item item = owner.HeldItem;
                Projectile.damage = owner.GetWeaponDamage(item);
                Projectile.knockBack = owner.GetWeaponKnockback(item, item.knockBack);
                Projectile.CritChance = owner.GetWeaponCrit(item);

                stageTarget = owner.GetModPlayer<EnchantedSwordPlayer>().Hits;
                if (stageTarget != lastSentStage)
                {
                    lastSentStage = stageTarget;
                    Projectile.netUpdate = true;
                }
            }
            stageShown += (stageTarget - stageShown) * MathHelper.Clamp(EnchantedSwordTuning.StageShowEase, 0.01f, 1f);
            if (Math.Abs(stageTarget - stageShown) < 0.01f)
                stageShown = stageTarget;

            // A swing starts: from the ready pose, or flowing out of the one that just finished its strike.
            TryStartSwing();

            prevCenter = Projectile.Center; // the previous tick's pose (the first tick fixes it below)
            prevRotation = Projectile.rotation;
            prevTip = tipPosition;

            float worldArm;
            float worldBlade;
            float slide;
            if (swinging)
            {
                if (IsOwner)
                    owner.ChangeDir(facing); // every tick, so nothing (movement keys) can turn the player away from the swing
                armRel = SwingArmAt(timer);
                tiltRel = SwingTiltAt(timer);
                slideNow = SwingSlideAt(timer);
                worldArm = WorldArm(armRel);
                worldBlade = WorldBlade(armRel, tiltRel);
                slide = slideNow;
                owner.itemAnimation = 2; // keeps the item "in use" for as long as the swing lasts
                owner.itemTime = 2;
            }
            else
            {
                // The ready pose: the arm eases into it after a swing and bobs a little, so the sword is never dead still.
                sinceSwing++;
                facing = owner.direction >= 0 ? 1 : -1;
                float bob = (float)Math.Sin(MathHelper.TwoPi * ticks / Math.Max(1, EnchantedSwordTuning.IdleBobTicks)) * Rad(EnchantedSwordTuning.IdleBobDeg);
                float ease = MathHelper.Clamp(EnchantedSwordTuning.IdleEase, 0.01f, 1f);
                armLocal += MathHelper.WrapAngle(Rad(EnchantedSwordTuning.ReadyArmDeg) + bob - armLocal) * ease;
                tiltRad += (Rad(EnchantedSwordTuning.ReadyTiltDeg) - tiltRad) * ease;
                slideRest -= slideRest * ease;
                worldArm = HeldSwordPose.ToWorld(facing, armLocal);
                worldBlade = HeldSwordPose.ToWorld(facing, armLocal + tiltRad);
                slide = slideRest;
            }

            ApplyPose(owner, worldArm, worldBlade, slide);
            if (ticks == 0)
            {
                prevCenter = Projectile.Center;
                prevRotation = Projectile.rotation;
                prevTip = tipPosition;
            }

            if (!Main.dedServ)
            {
                // The outline just got stronger (on every machine: the owner sees its own count, the others the synced one).
                if (stageTarget > stageSeen)
                {
                    flash = Math.Max(flash, 0.7f);
                    EnchantedSwordFx.StageUp(Projectile.Center, tipPosition, stageTarget);
                }
                float stage01 = EnchantedSwordTuning.Stage01(stageShown);
                if (stageShown > 0.3f && Main.rand.NextFloat() < EnchantedSwordTuning.SparkleChance * stage01)
                    EnchantedSwordFx.Sparkle(Projectile.Center, tipPosition);
                if (stageShown > 0.05f)
                    Lighting.AddLight(tipPosition, 0.25f * stage01 + 0.1f, 0.4f * stage01 + 0.1f, 0.6f * stage01 + 0.2f);
            }
            stageSeen = stageTarget;

            if (pendingDischarge)
            {
                pendingDischarge = false;
                Discharge();
            }

            flash = Math.Max(0f, flash - 1f / Math.Max(1, EnchantedSwordTuning.FlashTicks));

            if (swinging)
            {
                SwingEffects();
                if (!IsOwner && Damaging)
                    remoteHits.Update(Projectile, swingSerial, npc => EnchantedSwordFx.HitSpark(npc.Center, stageShown));

                trail?.Tick();
                timer++;
                if (timer >= TotalTicks)
                    EndSwing(owner);
            }

            lastOwnerCenter = owner.Center;
            ticks++;
        }

        // The 3rd hit: the stardust burst where the enemy is (or at the tip if it is gone), a flash, a camera punch for the owner. The ghost sword
        // itself is spawned by OnHitNPC (owner only; every machine gets it as a normal projectile).
        void Discharge()
        {
            flash = 1f;
            Vector2 center = tipPosition;
            int target = IsOwner ? dischargeTarget : pendingDischargeTarget;
            if (target >= 0 && target < Main.maxNPCs && Main.npc[target].active)
                center = Main.npc[target].Center;
            EnchantedSwordFx.Discharge(center, Projectile.Center, tipPosition, EnchantedSwordTuning.DischargeBurstRadius);
            if (IsOwner)
                CameraShake.Punch(center, EnchantedSwordTuning.DischargeShake, 10);
        }

        // The swings alternate up, down, up, down ... A THRUST only follows an up or down swing that hit an enemy (swingGaveHit still holds the
        // last swing's result here: BeginSwing clears it afterwards); after the thrust the swings go on with the other direction. After the sword
        // rested a while (or the first time) it starts at StartKind.
        int NextKind()
        {
            if (!hasSwung || sinceSwing > EnchantedSwordTuning.CycleResetTicks)
                return Math.Clamp(EnchantedSwordTuning.StartKind, 0, 1);
            if (lastKind != 2 && swingGaveHit)
                return 2;
            if (lastKind == 2)
                return 1 - lastSwingDir;
            return 1 - lastKind;
        }

        // The swing speed the owner starts a swing with: the player's melee speed (the result travels to everyone).
        float ComputeSpeed()
        {
            float melee = MathHelper.Lerp(1f, Owner.GetTotalAttackSpeed(DamageClass.Melee), EnchantedSwordTuning.AttackSpeedInfluence);
            return MathHelper.Clamp(melee, EnchantedSwordTuning.MinSpeed, EnchantedSwordTuning.MaxSpeed);
        }

        // Starts the next swing when the left button asks for one (the owner) or when the owner's swing has reached this machine. A swing that is
        // still striking is never interrupted; one that has finished its strike (even if it is already recovering) is flowed out of.
        void TryStartSwing()
        {
            bool afterStrike = swinging && timer >= StrikeEnd;
            if (swinging && !afterStrike)
                return;

            if (IsOwner)
            {
                if (Owner.controlUseItem && !Owner.mouseInterface)
                    BeginSwing(HeldAim.Capture(Owner), ComputeSpeed(), swinging, NextKind());
            }
            else if (pendingSwing)
            {
                pendingSwing = false;
                BeginSwing(pendingAim, MathHelper.Clamp(pendingSpeed, EnchantedSwordTuning.MinSpeed, EnchantedSwordTuning.MaxSpeed), pendingChained, Math.Clamp(pendingKind, 0, 2));
            }
        }

        // Starts a swing from the pose the sword is in right now (no jump, also when it flows out of the previous swing: the new swing's first
        // pose is exactly the one the old swing would show this tick). The owner also tells everyone else and clears the hit memory.
        void BeginSwing(float swingAimAngle, float speed, bool chain, int newKind)
        {
            if (swinging)
            {
                // Settle the pose of the old swing into the resting values BEFORE the swing's numbers (and kind) change.
                armLocal = HeldSwordPose.ToWorld(facing, WorldArm(SwingArmAt(timer)));
                tiltRad = SwingTiltAt(timer);
                slideRest = SwingSlideAt(timer);
            }

            kind = newKind;
            lastKind = newKind;
            if (newKind != 2)
                lastSwingDir = newKind;
            hasSwung = true;
            sinceSwing = 0;
            swinging = true;
            timer = 0f;
            strikeStarted = false;
            isChained = chain;
            aim = swingAimAngle;
            facing = HeldAim.Facing(aim);
            timeScale = 1f / Math.Max(0.01f, speed);
            startArmRel = MathHelper.WrapAngle(HeldSwordPose.ToWorld(facing, armLocal) - aim) * facing;
            startTilt = tiltRad;
            startSlide = slideRest;
            armRel = startArmRel;
            tiltRel = startTilt;
            slideNow = startSlide;
            DetachedTrails.Hand(trail);
            trail = null;
            trailStyle = null;

            if (IsOwner)
            {
                swingSerial = (swingSerial + 1) & 255;
                swingGaveHit = false;
                swingAim = aim;
                swingSpeed = speed;
                swingChained = chain;
                swingKind = kind;
                SegmentHit.ClearLocalImmunity(Projectile); // every enemy can be hit again by this swing
                Projectile.netUpdate = true;
                Owner.ChangeDir(facing);
            }
        }

        // The swing is over and the button is up: the sword keeps the pose it ended in (turned back to the facing-local angles the ready pose
        // uses) and eases into the ready pose.
        void EndSwing(Player owner)
        {
            armRel = SwingArmAt(timer);
            tiltRel = SwingTiltAt(timer);
            armLocal = HeldSwordPose.ToWorld(facing, WorldArm(armRel));
            tiltRad = tiltRel;
            slideRest = SwingSlideAt(timer);
            swinging = false;
            sinceSwing = 0;
            owner.itemAnimation = 0;
            owner.itemTime = 0;
            DetachedTrails.Hand(trail);
            trail = null;
            trailStyle = null;
        }

        void SwingEffects()
        {
            if (!strikeStarted && timer >= StrikeStart)
            {
                strikeStarted = true;
                flash = Math.Max(flash, 0.5f);
                EnchantedSwordFx.PlaySwing(tipPosition, IsThrust);
            }

            if (timer >= StrikeStart && timer < StrikeEnd)
            {
                // Many points per tick, laid between the previous tick's pose and this one, so the pixel line follows a smooth arc.
                int samples = Math.Max(1, EnchantedSwordTuning.TrailSamplesPerTick);
                for (int k = 0; k < samples; k++)
                {
                    float fraction = (k + 1f) / samples;
                    AddTrailSample(timer - 1f + fraction, fraction);
                }
                if (trailStyle != null)
                    trailStyle.Stage = EnchantedSwordTuning.Stage01(stageShown);
                EnchantedSwordFx.SwingDust(tipPosition, tipPosition - prevTip, stageShown);
                Lighting.AddLight(tipPosition, 0.45f, 0.6f, 0.9f);
            }
        }

        // 'fraction' is how far between the previous tick and this one the sample is (0..1): the owner is placed where they were at that
        // moment, so the trail stays one connected line while the player moves during the swing.
        void AddTrailSample(float time, float fraction)
        {
            if (Main.dedServ)
                return;

            time -= EnchantedSwordTuning.TrailLagTicks;
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            float slide = SwingSlideAt(time);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm)) + shift + direction * slide;
            float length = TotalLength;
            // The fan starts as a line at the tip and its inner edge slides down the blade toward the hand while the strike builds up; a
            // sword with more hits reaches further down.
            float expand = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(SwingProgress(time) / Math.Max(0.01f, EnchantedSwordTuning.TrailExpandBy), 0f, 1f));
            float outer = EnchantedSwordTuning.TrailOuter;
            float innerTarget = IsThrust
                ? EnchantedSwordTuning.TrailInnerThrust
                : MathHelper.Lerp(EnchantedSwordTuning.TrailInnerLow, EnchantedSwordTuning.TrailInnerHigh, EnchantedSwordTuning.Stage01(stageShown));
            float inner = MathHelper.Lerp(outer, innerTarget, expand);
            if (trail == null)
            {
                trailStyle = new EnchantedSwordSweepTrailStyle();
                trail = new SweepTrail(trailStyle, IsOwner);
            }
            // 'along' = where in the strike this sample is; the style (TailFirst) makes the tail live shorter, so it fades out first.
            float along = MathHelper.Clamp((time - StrikeStart) / Math.Max(0.01f, StrikeTicksNow * timeScale), 0f, 1f);
            trail.Add(hand + direction * length * outer, hand + direction * length * inner, along);
        }

        // The blade is a line from the hand to the tip. The strike is fast, so the test sweeps: it checks the line at a few poses between the
        // previous tick and this one (the hand moves along the blade during a thrust), and a thin enemy cannot slip through between two ticks.
        bool BladeTouches(Rectangle box, float width)
        {
            int steps = Math.Max(1, EnchantedSwordTuning.SweepHitSteps);
            float length = TotalLength * EnchantedSwordTuning.HitLengthScale;
            float deltaAngle = MathHelper.WrapAngle(Projectile.rotation - prevRotation);
            for (int i = 1; i <= steps; i++)
            {
                float fraction = i / (float)steps;
                Vector2 start = Vector2.Lerp(prevCenter, Projectile.Center, fraction);
                Vector2 end = start + (prevRotation + deltaAngle * fraction).ToRotationVector2() * length;
                if (SegmentHit.Overlaps(box, start, end, width))
                    return true;
            }
            return false;
        }

        // Moves the owner's arm and puts the projectile at the hand, slid 'slide' px along the blade; the blade points along Projectile.rotation.
        void ApplyPose(Player owner, float worldArm, float worldBlade, float slide)
        {
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, worldArm - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, worldArm);
            float blade = worldBlade;
            if (owner.gravDir == -1f)
                blade = -blade;
            Vector2 direction = blade.ToRotationVector2();
            Projectile.rotation = blade;
            Projectile.Center = hand + direction * slide;
            Projectile.spriteDirection = facing;
            tipPosition = Projectile.Center + direction * TotalLength;
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
            return BladeTouches(targetHitbox, EnchantedSwordTuning.HitWidth * Projectile.scale);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
            modifiers.SourceDamage *= EnchantedSwordTuning.DamageMultiplier * (IsThrust ? EnchantedSwordTuning.ThrustDamageMultiplier : 1f);
            modifiers.Knockback *= EnchantedSwordTuning.KnockbackMultiplier * (IsThrust ? EnchantedSwordTuning.ThrustKnockbackMultiplier : 1f);
        }

        // Owner's client only. A true melee hit: sparks, a sound, a camera punch, and (for the first hit of the swing) one hit toward the discharge.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            EnchantedSwordFx.HitSpark(target.Center, stageShown);
            CameraShake.Punch(target.Center, EnchantedSwordTuning.HitShake * (0.6f + 0.2f * stageShown), 8);
            if (swingGaveHit)
                return;
            swingGaveHit = true; // only the first hit of a swing matters: an up or down swing that hit makes the NEXT swing a thrust (NextKind)
            if (!IsThrust)
                return;
            if (EnchantedSwordPlayer.AddHit(Owner)) // only the thrust charges the sword
            {
                dischargeSerial = (dischargeSerial + 1) & 255;
                dischargeTarget = target.whoAmI;
                pendingDischarge = true; // played by AI on this machine; the others get it through SendExtraAI
                EtherealSwordProjectile.Spawn(Owner, target, Projectile.damage, Projectile.knockBack, Projectile.GetSource_FromThis());
            }
            Projectile.netUpdate = true; // the other machines hear the new count through SendExtraAI
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
            if (ticks == 0)
                return false; // not posed yet (the first tick has not run)

            trail?.Draw();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);

            Vector2 position = Projectile.Center + HeldSwordPose.GripShift(Projectile.rotation, EnchantedSwordTuning.GripOffset, Projectile.scale) - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;

            // The ethereal outline: stronger with every hit (wider, brighter, pulsing faster), with its soft glow grown from the MIDDLE of the sprite.
            float outline = EnchantedSwordTuning.OutlineStrengthAt(stageShown);
            if (outline > 0.01f)
            {
                float strength = outline * PixelOutline.Pulse(ticks, EnchantedSwordTuning.PulseTicksAt(stageShown), EnchantedSwordTuning.OutlineMin, EnchantedSwordTuning.OutlineMax);
                PixelOutline.Draw(texture, position, rotation, origin, scale, effects, Projectile.spriteDirection, strength, EnchantedSwordTuning.OutlineWidthAt(stageShown),
                    EnchantedSwordTuning.GlowScale, EnchantedSwordTuning.OutlineColor, EnchantedSwordTuning.OutlineCoreColor);
            }

            // The sword itself, brightened a little (a strike, a stage-up and the discharge flare it).
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.2f + 0.5f * flash), rotation, origin, scale, effects, 0);
            if (flash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, EnchantedSwordTuning.CoreColor * (0.7f * flash), rotation, origin, scale, effects, 0);

            DrawBladeDebug();
            return false;
        }

        // EnchantedSwordTuning.ShowBladeDebug: a magenta square on the tip the hit test uses and a green one on the hand.
        void DrawBladeDebug()
        {
            if (!EnchantedSwordTuning.ShowBladeDebug)
                return;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 tip = Projectile.Center + Projectile.rotation.ToRotationVector2() * TotalLength * EnchantedSwordTuning.HitLengthScale;
            Main.EntitySpriteDraw(pixel, tip - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.Magenta, 0f, new Vector2(0.5f), 5f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.LimeGreen, 0f, new Vector2(0.5f), 5f, SpriteEffects.None, 0);
        }
    }
}