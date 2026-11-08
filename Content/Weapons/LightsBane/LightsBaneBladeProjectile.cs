using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// The held Light's Bane (patch 1): ONE projectile that lives for as long as the sword is held (LightsBanePlayer spawns it). It poses
    /// the arm, draws the sword and runs the left click swing: a chop centered on the cursor, alternating up and down, repeating while the
    /// button stays down. THE SWINGS FLOW: the projectile never ends between two swings (it is the same projectile for as long as the sword is
    /// held, so the sprite never blinks out for a frame), and while the button is held a swing does not recover and then start over: the
    /// previous strike carries on through a short wind-up (ChainAnticipate*) straight into the next strike. Only when the button is let go does
    /// the arm settle back (RecoverTicks) and rest in the ready pose.
    /// The first enemy each swing touches (a true melee hit, nothing else exists yet) adds to the charge (LightsBanePlayer.Charge): the more
    /// charge the faster the swing (up to MaxSpeedScale), the stronger the trail and the glow; at full charge the blade wears a pulsing
    /// purple pixel outline (Common/PixelOutline).
    /// MULTIPLAYER: the owner's client decides everything (the click, the aim, the speed, the hits, the charge). What the other machines need
    /// travels in SendExtraAI: the charge (a byte), a swing serial, the swing's aim, speed and whether it flows out of the previous one; they
    /// start the same swing when the serial changes and run the same timers for pose, trail, sound and sparks. Hits and damage are the
    /// owner's alone (RemoteHitTracker gives the others their hit sparks). No custom packets.
    /// </summary>
    public class LightsBaneBladeProjectile : ModProjectile
    {
        // The vanilla item sprite is the sword (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.LightsBane;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // ---- synced (SendExtraAI / ReceiveExtraAI) ----
        int swingSerial;          // changes with every swing the owner starts
        float swingAim;           // the swing's aim (radians, the player's own frame)
        float swingSpeed = 1f;    // the swing's speed: charge x the owner's melee speed, decided at the click
        bool swingChained;        // the swing flows out of the previous one (no ready pose in between)
        float chargeTarget;       // the real charge 0..1
        int stabSerial;           // changes with every stab the owner starts

        // ---- per machine: counters and visuals, never synced ----
        bool pendingSwing;        // a swing the owner started that this machine has not begun yet
        float pendingAim;
        float pendingSpeed = 1f;
        bool pendingChained;
        bool pendingStab;         // a stab the owner started that this machine has not begun yet
        bool syncedOnce;          // the first packet only tells where the stab counter is (a late joiner must not replay an old stab)
        bool rightWasDown;        // owner: the right button on the previous tick (a stab needs a fresh press)
        int stabBuffer;           // owner: ticks a right click during a strike is still remembered
        bool stabbing;
        float stabTimer;          // ticks since the stab began
        bool stabImpacted;
        float stabBlend;          // 0..1: how much of the reversed grip is drawn (it fades back out after the stab)
        float stabSlide;          // how far the sprite is slid along the blade at full reversed grip (px, negative = back through the fist)
        float stabTipDist;        // hand to tip while stabbing (px)
        float stabStartBlade;     // the blade's world angle when the stab began
        float stabStartArm;       // the arm's facing-local angle when the stab began
        float stabLastBlade;      // the blade angle the stab pose gave to ApplyPose last (to continue from when the stab ends)
        Vector2 stabShake;
        float lastSentCharge = -1f;
        int ticks;
        bool swinging;
        float timer;              // ticks since the swing began
        int facing = 1;           // the side the sword is on (the swing's side while swinging, else the player's)
        float aim;                // the swing's aim
        float timeScale = 1f;     // 1 / the swing's speed
        bool isChained;           // this swing flows out of the previous one
        float startArmRel;        // the swing's start pose: arm relative to the aim (mirrored by the facing, positive = below) ...
        float startTilt;          // ... and the blade tilt
        float armRel;             // the current swing pose
        float tiltRel;
        float armLocal = MathHelper.ToRadians(LightsBaneTuning.ReadyArmDeg); // the resting pose, facing-local (positive = down)
        float tiltRad = MathHelper.ToRadians(LightsBaneTuning.ReadyTiltDeg);
        float chargeShown;        // the charge that is drawn: eases toward the real one
        float outlineShown;       // 0..1, how visible the full-charge outline is
        bool wasCharged;
        bool strikeStarted;
        bool swingGaveCharge;     // owner: this swing already added its charge (a swing counts at most one hit toward the charge)
        float flash;
        Vector2 lastOwnerCenter;
        Vector2 prevCenter;       // the hand and angle on the previous tick: the hit test sweeps from them
        float prevRotation;
        Vector2 prevTip;
        Vector2 tipPosition;
        SweepTrail trail;
        LightsBaneSweepTrailStyle trailStyle;
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

        float TotalLength => LightsBaneTuning.BladeLength * Projectile.scale;

        bool IsUp => startArmRel > 0f;
        float Travel => IsUp ? -1f : 1f;

        float FromArm => Rad(IsUp ? LightsBaneTuning.SwingLowDeg : LightsBaneTuning.SwingHighDeg);
        float ToArm => Rad(IsUp ? LightsBaneTuning.SwingHighDeg : LightsBaneTuning.SwingLowDeg);
        float FromTilt => Rad(IsUp ? LightsBaneTuning.TiltLowDeg : LightsBaneTuning.TiltHighDeg);
        float ToTilt => Rad(IsUp ? LightsBaneTuning.TiltHighDeg : LightsBaneTuning.TiltLowDeg);

        // A swing that flows out of the previous one winds up longer and further: the previous strike's momentum carries it.
        float AnticipateTicksNow => isChained ? LightsBaneTuning.ChainAnticipateTicks : LightsBaneTuning.AnticipateTicks;
        float AnticipateRadNow => Rad(isChained ? LightsBaneTuning.ChainAnticipateDeg : LightsBaneTuning.AnticipateDeg);

        float StrikeStart => AnticipateTicksNow * timeScale;
        float StrikeEnd => StrikeStart + LightsBaneTuning.SwingTicks * timeScale;
        float TotalTicks => StrikeEnd + LightsBaneTuning.RecoverTicks * timeScale;

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
            float t = MathHelper.Clamp((time - StrikeStart) / Math.Max(0.01f, LightsBaneTuning.SwingTicks * timeScale), 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, Math.Max(1f, LightsBaneTuning.SwingEasePower));
        }

        float SwingArmAt(float time)
        {
            float back = FromArm - Travel * AnticipateRadNow;
            float over = ToArm + Travel * Rad(LightsBaneTuning.OvershootDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(startArmRel, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(0.01f, StrikeStart)));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(back, over, SwingProgress(time));
            return MathHelper.Lerp(over, ToArm, MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(0.01f, LightsBaneTuning.RecoverTicks * timeScale)));
        }

        float SwingTiltAt(float time)
        {
            float back = FromTilt - Travel * Rad(LightsBaneTuning.AnticipateTiltDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(startTilt, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(0.01f, StrikeStart)));
            return MathHelper.Lerp(back, ToTilt, SwingProgress(time));
        }

        // ---- sync ----

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)MathHelper.Clamp((float)Math.Round(chargeTarget * 255f), 0f, 255f));
            writer.Write((byte)(swingSerial & 255));
            writer.Write(swingAim);
            writer.Write(swingSpeed);
            writer.Write(swingChained);
            writer.Write((byte)(stabSerial & 255));
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            chargeTarget = reader.ReadByte() / 255f;
            int serial = reader.ReadByte();
            float readAim = reader.ReadSingle();
            float readSpeed = reader.ReadSingle();
            bool readChained = reader.ReadBoolean();
            int readStab = reader.ReadByte();
            if (!syncedOnce)
                stabSerial = readStab;
            else if (readStab != stabSerial)
            {
                stabSerial = readStab;
                pendingStab = true;
            }
            syncedOnce = true;
            if (serial != swingSerial)
            {
                swingSerial = serial;
                pendingSwing = true;
                pendingAim = readAim;
                pendingSpeed = readSpeed;
                pendingChained = readChained;
            }
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.LightsBane)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 10000;
            Projectile.scale = LightsBaneTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            if (ticks == 0)
            {
                lastOwnerCenter = owner.Center;
                facing = owner.direction >= 0 ? 1 : -1;
                if (IsOwner)
                    chargeTarget = owner.GetModPlayer<LightsBanePlayer>().Charge;
                chargeShown = chargeTarget;
                wasCharged = chargeTarget >= LightsBaneTuning.ChargedAt;
                outlineShown = wasCharged ? 1f : 0f;
                if (IsOwner)
                    rightWasDown = owner.controlUseTile; // a button already held when the blade forms is not a fresh press
            }

            if (IsOwner)
            {
                // The projectile carries the item's current damage (buffs and reforges change it).
                Item item = owner.HeldItem;
                Projectile.damage = owner.GetWeaponDamage(item);
                Projectile.knockBack = owner.GetWeaponKnockback(item, item.knockBack);
                Projectile.CritChance = owner.GetWeaponCrit(item);

                chargeTarget = owner.GetModPlayer<LightsBanePlayer>().Charge;
                if (Math.Abs(chargeTarget - lastSentCharge) >= LightsBaneTuning.ChargeSyncStep || (chargeTarget != lastSentCharge && (chargeTarget <= 0f || chargeTarget >= 1f)))
                {
                    lastSentCharge = chargeTarget;
                    Projectile.netUpdate = true;
                }
            }
            chargeShown += (chargeTarget - chargeShown) * MathHelper.Clamp(LightsBaneTuning.ChargeShowEase, 0.01f, 1f);
            if (Math.Abs(chargeTarget - chargeShown) < 0.002f)
                chargeShown = chargeTarget;
            bool charged = chargeTarget >= LightsBaneTuning.ChargedAt;
            outlineShown += ((charged ? 1f : 0f) - outlineShown) * MathHelper.Clamp(LightsBaneTuning.OutlineFade, 0.01f, 1f);

            // A stab (right click at full charge) is checked first, so it wins over the next swing.
            TryStartStab(owner);

            // A swing starts: from the ready pose, or flowing out of the one that just finished its strike.
            TryStartSwing();

            prevCenter = Projectile.Center; // the previous tick's pose (the first tick fixes it below)
            prevRotation = Projectile.rotation;
            prevTip = tipPosition;

            float worldArm;
            float worldBlade;
            if (stabbing)
            {
                StabPose(owner, out worldArm, out worldBlade);
            }
            else if (swinging)
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
                float bob = (float)Math.Sin(MathHelper.TwoPi * ticks / Math.Max(1, LightsBaneTuning.IdleBobTicks)) * Rad(LightsBaneTuning.IdleBobDeg);
                float ease = MathHelper.Clamp(LightsBaneTuning.IdleEase, 0.01f, 1f);
                armLocal += MathHelper.WrapAngle(Rad(LightsBaneTuning.ReadyArmDeg) + bob - armLocal) * ease;
                tiltRad += (Rad(LightsBaneTuning.ReadyTiltDeg) - tiltRad) * ease;
                worldArm = HeldSwordPose.ToWorld(facing, armLocal);
                worldBlade = HeldSwordPose.ToWorld(facing, armLocal + tiltRad);
            }

            ApplyPose(owner, worldArm, worldBlade);
            if (stabbing)
            {
                // The stab's blade is shorter than the sword: its tip is where the stab pose put it.
                tipPosition = Projectile.Center + Projectile.rotation.ToRotationVector2() * stabTipDist;
                owner.itemAnimation = 2; // keeps the item "in use" for as long as the stab lasts
                owner.itemTime = 2;
                if (IsOwner)
                    owner.ChangeDir(facing);
                if (!stabImpacted && stabBlend > 0.5f)
                {
                    flash = Math.Max(flash, 0.5f * stabBlend);
                    if (Main.rand.NextBool(2))
                        LightsBaneFx.Sparkle(Projectile.Center, tipPosition);
                }
                TickStab(owner);
            }
            else if (stabBlend > 0f)
                stabBlend = Math.Max(0f, stabBlend - LightsBaneTuning.StabBlendOut);
            if (ticks == 0)
            {
                prevCenter = Projectile.Center;
                prevRotation = Projectile.rotation;
                prevTip = tipPosition;
            }

            if (!Main.dedServ)
            {
                if (charged && !wasCharged)
                {
                    flash = 1f;
                    LightsBaneFx.Charged(Projectile.Center, tipPosition);
                }
                if (charged || chargeTarget < 0.85f)
                    wasCharged = charged;
                if (outlineShown > 0.3f && Main.rand.NextFloat() < LightsBaneTuning.SparkleChance * outlineShown)
                    LightsBaneFx.Sparkle(Projectile.Center, tipPosition);
                if (outlineShown > 0.05f)
                    Lighting.AddLight(tipPosition, 0.35f * outlineShown, 0.12f * outlineShown, 0.7f * outlineShown);
            }

            flash = Math.Max(0f, flash - 1f / Math.Max(1, LightsBaneTuning.FlashTicks));

            if (swinging)
            {
                SwingEffects();
                if (!IsOwner && Damaging)
                    remoteHits.Update(Projectile, swingSerial, npc => LightsBaneFx.HitSpark(npc.Center, chargeShown));

                trail?.Tick();
                timer++;
                if (timer >= TotalTicks)
                    EndSwing(owner);
            }

            lastOwnerCenter = owner.Center;
            ticks++;
        }

        // ---- the stab (right click at full charge) ----
        // One timeline inside the held blade (the sprite never leaves): TURN (the arm goes up, the blade turns to point at the chest and slides back
        // through the fist until the fist holds it by its sharp end, hilt sticking out behind the hand; the sword keeps its full size), AIM (it
        // trembles), PLUNGE (fast, the tip is driven into the chest, the fist slides up the blade), HOLD (it stays
        // buried while the burst plays). At the impact the owner only adds the imbue buff: everything else (the look, the afterimages, the speed, the full charge) follows the buff.

        // The owner starts a stab on a fresh right press (not over the inventory, not on a chest or a door) while the sword is fully charged and the
        // player is not imbued; a press during a strike is remembered a few ticks and fires as soon as the strike is over. The other machines start
        // the same stab when the owner's stab serial reaches them.
        void TryStartStab(Player owner)
        {
            if (IsOwner)
            {
                bool rightDown = owner.controlUseTile && !owner.mouseInterface && !owner.tileInteractionHappened;
                bool pressed = rightDown && !rightWasDown;
                rightWasDown = rightDown;
                bool ready = chargeTarget >= LightsBaneTuning.ChargedAt && owner.GetModPlayer<LightsBanePlayer>().ImbueTicks <= 0;
                if (pressed && ready)
                    stabBuffer = LightsBaneTuning.StabBufferTicks;
                else if (stabBuffer > 0)
                    stabBuffer--;
                if (stabBuffer > 0 && ready && !stabbing && (!swinging || timer >= StrikeEnd))
                    BeginStab(owner);
            }
            else if (pendingStab && !stabbing)
            {
                pendingStab = false;
                BeginStab(owner);
            }
        }

        void BeginStab(Player owner)
        {
            if (swinging)
                EndSwing(owner); // settles the swing's pose into the resting angles the stab starts from

            stabbing = true;
            stabTimer = 0f;
            stabImpacted = false;
            facing = owner.direction >= 0 ? 1 : -1;
            stabStartArm = armLocal;
            stabStartBlade = Projectile.rotation;

            if (IsOwner)
            {
                stabSerial = (stabSerial + 1) & 255;
                stabBuffer = 0;
                Projectile.netUpdate = true;
                owner.ChangeDir(facing);
            }
            LightsBaneFx.PlayStabWindup(Projectile.Center);
        }

        // The arm, the blade's angle, the slide (reversed grip) and the tip for this tick of the stab. Angles are given the way ApplyPose wants them (a
        // world angle that it mirrors itself under inverted gravity).
        void StabPose(Player owner, out float worldArm, out float worldBlade)
        {
            float turn = Math.Max(1f, LightsBaneTuning.StabTurnTicks);
            float aimEnd = turn + LightsBaneTuning.StabAimTicks;
            float plungeEnd = aimEnd + Math.Max(1f, LightsBaneTuning.StabPlungeTicks);
            float s = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(stabTimer / turn, 0f, 1f)); // 0..1 over the turn

            float armNow = MathHelper.Lerp(stabStartArm, Rad(LightsBaneTuning.StabArmDeg), s);
            worldArm = HeldSwordPose.ToWorld(facing, armNow);
            Vector2 hand = HeldSwordPose.HandWorld(owner, worldArm);
            Vector2 chest = owner.Center + new Vector2(0f, LightsBaneTuning.StabChestOffsetY);

            // The blade turns from where it was to pointing from the hand at the chest (the short way round).
            float aimAngle = (chest - hand).ToRotation();
            float angle = stabStartBlade + MathHelper.WrapAngle(aimAngle - stabStartBlade) * s;
            worldBlade = owner.gravDir == -1f ? -angle : angle;
            stabLastBlade = worldBlade;

            // The tip (its distance from the hand): the full sword's length at first, then (during the turn) the sprite slides back until the tip is at
            // the aim distance, then the plunge to the depth.
            float full = TotalLength;
            float reach = Vector2.Distance(hand, chest);
            float tipAim = Math.Max(2f, reach - LightsBaneTuning.StabAimDistance);
            float tipEnd = reach + LightsBaneTuning.StabDepth;
            float tipNow = tipAim;
            if (stabTimer >= aimEnd)
            {
                float u = MathHelper.Clamp((stabTimer - aimEnd) / (plungeEnd - aimEnd), 0f, 1f);
                tipNow = MathHelper.Lerp(tipAim, tipEnd, 1f - (1f - u) * (1f - u));
            }

            // Reversed grip: the sprite keeps its full size and is slid back along the blade, so the fist ends up near the sharp end and the hilt
            // sticks out behind the hand. The tip is then 'tipNow' px in front of the hand (full length at s = 0, tipNow at s = 1).
            stabBlend = s;
            stabSlide = tipNow - full;
            stabTipDist = full + stabSlide * s;

            stabShake = stabTimer >= turn ? Main.rand.NextVector2Circular(1f, 1f) * LightsBaneTuning.StabShake * (stabTimer >= plungeEnd ? 1f : 0.5f) : Vector2.Zero;
        }

        // Runs after the pose of this tick: the impact (once, when the plunge ends) and the end of the stab.
        void TickStab(Player owner)
        {
            float plungeEnd = LightsBaneTuning.StabTurnTicks + LightsBaneTuning.StabAimTicks + Math.Max(1, LightsBaneTuning.StabPlungeTicks);
            if (!stabImpacted && stabTimer >= plungeEnd)
                StabImpact(owner);
            stabTimer++;
            if (stabTimer >= plungeEnd + LightsBaneTuning.StabHoldTicks)
                EndStab(owner);
        }

        // The blade is buried: the burst on every machine; on the owner's machine also the charge drops to 0 and the imbue begins.
        void StabImpact(Player owner)
        {
            stabImpacted = true;
            Vector2 chest = owner.Center + new Vector2(0f, LightsBaneTuning.StabChestOffsetY);
            flash = 1f;
            LightsBaneFx.StabImpact(chest);
            CameraShake.Punch(chest, LightsBaneTuning.StabHitShake, 12);
            if (!IsOwner)
                return;
            // The buff is the whole imbue (LightsBanePlayer reacts to it from any source): vanilla syncs the buff list to the others.
            owner.AddBuff(ModContent.BuffType<LightsBaneImbueBuff>(), LightsBaneTuning.ImbueTicks);
        }

        // The stab is over: the arm and blade angles it ended in become the resting angles the ready pose eases away from, and the reversed grip
        // fades out (StabBlendOut) while the sword slides back out of the chest into the hand.
        void EndStab(Player owner)
        {
            stabbing = false;
            stabShake = Vector2.Zero;
            armLocal = Rad(LightsBaneTuning.StabArmDeg);
            tiltRad = MathHelper.WrapAngle(HeldSwordPose.ToWorld(facing, stabLastBlade) - armLocal);
            owner.itemAnimation = 0;
            owner.itemTime = 0;
        }

        // The swing speed the owner starts a swing with: the charge's speed-up times the player's melee speed (the result travels to everyone).
        float ComputeSpeed()
        {
            float chargeScale = MathHelper.Lerp(1f, LightsBaneTuning.MaxSpeedScale, (float)Math.Pow(MathHelper.Clamp(chargeTarget, 0f, 1f), Math.Max(0.01f, LightsBaneTuning.SpeedCurvePower)));
            float melee = MathHelper.Lerp(1f, Owner.GetTotalAttackSpeed(DamageClass.Melee), LightsBaneTuning.AttackSpeedInfluence);
            return MathHelper.Clamp(melee * chargeScale, LightsBaneTuning.MinSpeed, LightsBaneTuning.MaxSpeed);
        }

        // Starts the next swing when the left button asks for one (the owner) or when the owner's swing has reached this machine. A swing that is
        // still striking is never interrupted; one that has finished its strike (even if it is already recovering) is flowed out of.
        void TryStartSwing()
        {
            if (stabbing)
                return; // the stab runs its own timeline; a held left click resumes swinging right after it
            bool afterStrike = swinging && timer >= StrikeEnd;
            if (swinging && !afterStrike)
                return;

            if (IsOwner)
            {
                if (Owner.controlUseItem && !Owner.mouseInterface)
                    BeginSwing(HeldAim.Capture(Owner), ComputeSpeed(), swinging);
            }
            else if (pendingSwing)
            {
                pendingSwing = false;
                BeginSwing(pendingAim, MathHelper.Clamp(pendingSpeed, LightsBaneTuning.MinSpeed, LightsBaneTuning.MaxSpeed), pendingChained);
            }
        }

        // Starts a swing from the pose the sword is in right now (no jump, also when it flows out of the previous swing: the new swing's first
        // pose is exactly the one the old swing would show this tick). The owner also tells everyone else and clears the hit memory.
        void BeginSwing(float swingAimAngle, float speed, bool chain)
        {
            if (swinging)
            {
                // Settle the pose of the old swing into the resting angles BEFORE the swing's numbers change.
                armLocal = HeldSwordPose.ToWorld(facing, WorldArm(SwingArmAt(timer)));
                tiltRad = SwingTiltAt(timer);
            }

            swinging = true;
            timer = 0f;
            strikeStarted = false;
            isChained = chain;
            aim = swingAimAngle;
            facing = HeldAim.Facing(aim);
            timeScale = 1f / Math.Max(0.01f, speed);
            startArmRel = MathHelper.WrapAngle(HeldSwordPose.ToWorld(facing, armLocal) - aim) * facing;
            startTilt = tiltRad;
            armRel = startArmRel;
            tiltRel = startTilt;
            DetachedTrails.Hand(trail);
            trail = null;
            trailStyle = null;

            if (IsOwner)
            {
                swingSerial = (swingSerial + 1) & 255;
                swingGaveCharge = false;
                swingAim = aim;
                swingSpeed = speed;
                swingChained = chain;
                Array.Clear(Projectile.localNPCImmunity, 0, Projectile.localNPCImmunity.Length); // every enemy can be hit again by this swing
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
            swinging = false;
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
                flash = Math.Max(flash, 0.6f);
                LightsBaneFx.PlaySwing(tipPosition, chargeShown);
            }

            if (timer >= StrikeStart && timer < StrikeEnd)
            {
                // Many points per tick, laid between the previous tick's pose and this one, so the pixel line follows a smooth arc.
                int samples = Math.Max(1, LightsBaneTuning.TrailSamplesPerTick);
                for (int k = 0; k < samples; k++)
                {
                    float fraction = (k + 1f) / samples;
                    AddTrailSample(timer - 1f + fraction, fraction);
                }
                if (trailStyle != null)
                    trailStyle.Charge = chargeShown;
                LightsBaneFx.SwingDust(tipPosition, tipPosition - prevTip, chargeShown);
                Lighting.AddLight(tipPosition, 0.4f + 0.3f * chargeShown, 0.15f, 0.8f);
            }
        }

        // 'fraction' is how far between the previous tick and this one the sample is (0..1): the owner is placed where they were at that
        // moment, so the trail stays one connected line while the player moves during the swing.
        void AddTrailSample(float time, float fraction)
        {
            if (Main.dedServ)
                return;

            time -= LightsBaneTuning.TrailLagTicks;
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm)) + shift;
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            float length = TotalLength;
            // The fan starts as a line at the tip and its inner edge slides down the blade toward the hand while the strike builds up; a
            // charged sword reaches further down.
            float expand = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(SwingProgress(time) / Math.Max(0.01f, LightsBaneTuning.TrailExpandBy), 0f, 1f));
            float outer = LightsBaneTuning.TrailOuter;
            float innerTarget = MathHelper.Lerp(LightsBaneTuning.TrailInnerLow, LightsBaneTuning.TrailInnerHigh, MathHelper.Clamp(chargeShown, 0f, 1f));
            float inner = MathHelper.Lerp(outer, innerTarget, expand);
            if (trail == null)
            {
                trailStyle = new LightsBaneSweepTrailStyle();
                trail = new SweepTrail(trailStyle, IsOwner);
            }
            // 'along' = where in the strike this sample is; the style (TailFirst) makes the tail live shorter, so it fades out first.
            float along = MathHelper.Clamp((time - StrikeStart) / Math.Max(0.01f, LightsBaneTuning.SwingTicks * timeScale), 0f, 1f);
            trail.Add(hand + direction * length * outer, hand + direction * length * inner, along);
        }

        // The blade is a line from the hand to the tip. The strike is fast, so the test sweeps: it checks the line at a few poses between the
        // previous tick and this one, and a thin enemy cannot slip through between two ticks.
        bool BladeTouches(Rectangle box, float width)
        {
            int steps = Math.Max(1, LightsBaneTuning.SweepHitSteps);
            float length = TotalLength * LightsBaneTuning.HitLengthScale;
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
            tipPosition = hand + blade.ToRotationVector2() * TotalLength;
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
            return BladeTouches(targetHitbox, LightsBaneTuning.HitWidth * Projectile.scale);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
            modifiers.SourceDamage *= LightsBaneTuning.DamageMultiplier;
            modifiers.Knockback *= LightsBaneTuning.KnockbackMultiplier;
        }

        // Owner's client only. A true melee hit of the swing: sparks, a sound, a camera punch, and (for the first hit of the swing) charge.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            LightsBaneFx.HitSpark(target.Center, chargeShown);
            CameraShake.Punch(target.Center, LightsBaneTuning.HitShake * (0.6f + chargeShown), 8);
            if (!swingGaveCharge)
            {
                swingGaveCharge = true; // only the first hit of a swing charges the blade
                LightsBanePlayer.AddCharge(Owner, LightsBaneTuning.ChargePerHit);
                LightsBaneFx.ChargeHit(tipPosition);
                Projectile.netUpdate = true; // the other machines hear the new charge through SendExtraAI
            }
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

            Vector2 position = Projectile.Center + HeldSwordPose.GripShift(Projectile.rotation, LightsBaneTuning.GripOffset, Projectile.scale) - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;
            if (stabBlend > 0f)
            {
                // The stab's reversed grip: the sprite keeps its full size and slides back through the fist (draw only), so the fist holds the
                // blade by its sharp end, the tip points at the chest and the hilt sticks out behind the hand.
                position += stabShake + Projectile.rotation.ToRotationVector2() * (stabSlide * stabBlend);
            }

            // The charge: a soft glow behind the sword that grows with it (drawn around the CENTER of the blade sprite, so it swells outward
            // evenly), and at full charge the pulsing pixel outline (which brings its own glow, so this one steps aside).
            float glow = LightsBaneTuning.GlowMaxAlpha * chargeShown * chargeShown * (1f - outlineShown);
            if (glow > 0.01f)
                PixelOutline.DrawGlow(texture, position, rotation, scale, effects, Projectile.spriteDirection, LightsBaneTuning.OutlineColor * glow, LightsBaneTuning.GlowScale);
            if (outlineShown > 0.01f)
            {
                float strength = outlineShown * PixelOutline.Pulse(ticks, LightsBaneTuning.OutlinePulseTicks, LightsBaneTuning.OutlineMin, LightsBaneTuning.OutlineMax);
                PixelOutline.Draw(texture, position, rotation, origin, scale, effects, Projectile.spriteDirection, strength, LightsBaneTuning.OutlineWidth, LightsBaneTuning.GlowScale,
                    LightsBaneTuning.OutlineColor, LightsBaneTuning.OutlineCoreColor);
            }

            // The sword itself, brightened a little (a strike and the moment it fills flare it).
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.2f + 0.5f * flash), rotation, origin, scale, effects, 0);
            if (flash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, LightsBaneTuning.CoreColor * (0.7f * flash), rotation, origin, scale, effects, 0);

            DrawBladeDebug();
            return false;
        }

        // LightsBaneTuning.ShowBladeDebug: a magenta square on the tip the hit test uses and a green one on the hand.
        void DrawBladeDebug()
        {
            if (!LightsBaneTuning.ShowBladeDebug)
                return;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 tip = Projectile.Center + Projectile.rotation.ToRotationVector2() * TotalLength * LightsBaneTuning.HitLengthScale;
            Main.EntitySpriteDraw(pixel, tip - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.Magenta, 0f, new Vector2(0.5f), 5f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.LimeGreen, 0f, new Vector2(0.5f), 5f, SpriteEffects.None, 0);
        }
    }
}