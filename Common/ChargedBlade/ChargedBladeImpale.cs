using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common.ChargedBlade
{
    /// <summary>
    /// The shared blade's IMPALE (a second right click move beside the self-stab; only blades whose ImpaleTune is not null have it). Right click
    /// BELOW full charge swings the sword up toward the cursor in an uppercut, the blade held across the hand (ImpaleBladeAngleDeg). The first enemy on the blade is struck; a gripped-able one (Snarable.CanGrip)
    /// is IMPALED: held on the blade by CarriedNPC.Hold, the sword slowly lifts it toward straight up in ImpaleBeats beats, each beat ends in a pulse
    /// of damage, then the finisher (the weapon's OnImpaleFinish) and the charge jumps to full. A boss or an unmovable enemy takes the stab as a
    /// normal hit and is not impaled. An enemy that dies mid-way jumps straight to the finisher (the charge is still given).
    /// MULTIPLAYER: the owner's client decides (the press, the aim, the catch, every strike). It syncs a serial, the aim and the caught enemy's
    /// index in SendExtraAI; every machine (the server too) runs the same timeline and calls CarriedNPC.Hold every tick while it holds, so a dead
    /// blade frees the enemy by itself within 2 ticks. Damage is SimpleStrikeNPC from the owner (it syncs). No custom packets.
    /// </summary>
    public abstract partial class ChargedBladeProjectile
    {
        // ---- what a weapon fills in (all optional; no ImpaleTune = no impale) ----

        /// <summary>The impale's numbers, or null (the default) for a blade without an impale.</summary>
        protected virtual IImpaleTuning ImpaleTune => null;
        /// <summary>Owner's machine: may a right click start the impale now (on top of: below full charge, a fresh press)?</summary>
        protected virtual bool ImpaleAvailable(Player owner) { return true; }
        /// <summary>The charge's effect on swing speed (1 = normal). Default: faster with the charge, up to MaxSpeedScale. The Night's Edge slows down instead.</summary>
        protected virtual float ChargeSpeedScale(Player owner, float charge)
        {
            return MathHelper.Lerp(1f, Tune.MaxSpeedScale, (float)Math.Pow(MathHelper.Clamp(charge, 0f, 1f), Math.Max(0.01f, Tune.SpeedCurvePower)));
        }
        // Visuals and sounds, every machine.
        protected virtual void FxImpaleWindup(Vector2 hand) { }
        protected virtual void FxImpalePierce(Vector2 point, Vector2 direction, bool caught) { }
        protected virtual void FxImpaleBeat(Vector2 point, int beat) { }
        protected virtual void FxImpaleFinish(Vector2 point) { }
        /// <summary>Every machine, every tick of the hold: 'enemy' is where the impaled enemy is, 'bladeFrom' and 'bladeTo' the stretch of blade it feeds, 'progress' 0..1 over the whole hold, 'beat' the beats finished so far.</summary>
        protected virtual void FxImpaleHold(Vector2 enemy, Vector2 bladeFrom, Vector2 bladeTo, float progress, int beat) { }
        /// <summary>Owner's client, at the finisher (the enemy has been released): spawn the explosion. The charge is given right after.</summary>
        protected virtual void OnImpaleFinish(Player owner, Vector2 point) { }

        // ---- state ----
        const int ImpWindup = 0;
        const int ImpThrust = 1;
        const int ImpWait = 2;    // other machines: the thrust is over and the owner's catch has not arrived yet
        const int ImpHold = 3;
        const int ImpFinish = 4;
        const int ImpMiss = 5;

        bool impaling;
        int impPhase;
        float impTimer;           // ticks in the current phase
        int impHoldTick;
        int impBeatsDone;
        int impSerial;            // synced
        float impAim;             // synced (the player's own frame, like every swing aim)
        int impTarget = -1;       // synced: the impaled enemy's index, -1 = none
        bool impSyncedOnce;
        bool pendingImpale;
        float pendingImpaleAim;
        int impBuffer;            // owner: ticks a right click during a strike is still remembered
        bool impHit;              // owner: the thrust already struck something
        bool impCaught;           // owner: ... and it was impaled
        float impStartArm;        // the pose the wind-up starts from (player frame)
        float impStartBlade;
        float impArm;             // the pose of the last tick (player frame)
        float impBlade;
        float impReach;           // how far the sword is pushed out or drawn back (px)
        Vector2 impFrom;          // where the enemy was when it was caught
        Vector2 impLastAnchor;

        static float LerpAngle(float from, float to, float t)
        {
            return from + MathHelper.WrapAngle(to - from) * t;
        }

        // ---- sync ----

        void SendImpale(BinaryWriter writer)
        {
            if (ImpaleTune == null)
                return;
            writer.Write((byte)(impSerial & 255));
            writer.Write(impAim);
            writer.Write((short)impTarget);
        }

        void ReceiveImpale(BinaryReader reader)
        {
            if (ImpaleTune == null)
                return;
            int serial = reader.ReadByte();
            float readAim = reader.ReadSingle();
            int target = reader.ReadInt16();
            if (!impSyncedOnce)
            {
                impSyncedOnce = true; // the first packet only says where the counter is: a late joiner must not replay an old impale
                impSerial = serial;
                impTarget = -1;
                return;
            }
            impTarget = target;
            if (serial != impSerial)
            {
                impSerial = serial;
                pendingImpale = true;
                pendingImpaleAim = readAim;
            }
        }

        NPC TargetNpc()
        {
            if (impTarget < 0 || impTarget >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[impTarget];
            return npc.active && npc.life > 0 ? npc : null;
        }

        // The blade is gone (item swapped, player died): free the enemy now instead of in 2 ticks.
        void ReleaseImpaleTarget()
        {
            if (!impaling)
                return;
            NPC npc = TargetNpc();
            if (npc != null)
                CarriedNPC.Release(npc);
        }

        // ---- start ----

        // The owner starts an impale on a fresh right press below full charge (a press during a strike is remembered a few ticks); the other
        // machines start the same one when the owner's serial reaches them. Called BEFORE TryStartStab, which updates rightWasDown afterwards.
        void TryStartImpale(Player owner)
        {
            IImpaleTuning t = ImpaleTune;
            if (t == null)
                return;
            if (IsOwner)
            {
                bool rightDown = owner.controlUseTile && !owner.mouseInterface && !owner.tileInteractionHappened;
                bool pressed = rightDown && !rightWasDown;
                bool ready = chargeTarget < Tune.ChargedAt && ImpaleAvailable(owner);
                if (pressed && ready)
                    impBuffer = t.ImpaleBufferTicks;
                else if (impBuffer > 0)
                    impBuffer--;
                if (impBuffer > 0 && ready && !impaling && !stabbing && (!swinging || timer >= StrikeEnd))
                    BeginImpale(owner, HeldAim.Capture(owner));
            }
            else if (pendingImpale && !impaling && !stabbing)
            {
                pendingImpale = false;
                BeginImpale(owner, pendingImpaleAim);
            }
        }

        void BeginImpale(Player owner, float aimAngle)
        {
            if (swinging)
                EndSwing(owner); // settles the swing's pose into the resting angles the wind-up starts from

            impaling = true;
            impPhase = ImpWindup;
            impTimer = 0f;
            impHoldTick = 0;
            impBeatsDone = 0;
            impAim = aimAngle;
            facing = HeldAim.Facing(aimAngle);
            impStartArm = HeldSwordPose.ToWorld(facing, armLocal);
            impStartBlade = HeldSwordPose.ToWorld(facing, armLocal + tiltRad);
            impReach = 0f;
            impHit = false;
            impCaught = false;
            if (IsOwner)
            {
                impSerial = (impSerial + 1) & 255;
                impTarget = -1;
                impBuffer = 0;
                Projectile.netUpdate = true;
                owner.ChangeDir(facing);
            }
            FxImpaleWindup(Projectile.Center);
        }

        // ---- the pose of this tick ----

        void ImpalePose(Player owner, out float worldArm, out float worldBlade)
        {
            IImpaleTuning t = ImpaleTune;
            float a = impAim;
            float windArm = a + facing * Rad(t.ImpaleWindupArmDeg);
            float endArm = a + facing * Rad(t.ImpaleSwingEndDeg);
            float cross = -facing * Rad(t.ImpaleBladeAngleDeg); // the blade stands this far off the arm (90 = across the hand like a hook, 0 = straight along the arm)
            float pull = t.ImpaleWindupPull;
            float arm;
            float blade;
            float reach;
            if (impPhase == ImpWindup)
            {
                float s = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(impTimer / Math.Max(1f, t.ImpaleWindupTicks), 0f, 1f));
                arm = LerpAngle(impStartArm, windArm, s);
                blade = LerpAngle(impStartBlade, windArm + cross, s);
                reach = -pull * s;
            }
            else if (impPhase == ImpThrust)
            {
                // A lerp toward the end pose: each tick closes ImpaleSwingLerp of what is left, so the arm starts fast and settles softly onto the end
                // pose; rescaled by the whole swing's total so the last tick lands exactly on it.
                float keep = 1f - MathHelper.Clamp(t.ImpaleSwingLerp, 0.01f, 0.99f);
                float ticks = Math.Max(1f, t.ImpaleSwingTicks);
                float e = MathHelper.Clamp((1f - (float)Math.Pow(keep, impTimer + 1f)) / (1f - (float)Math.Pow(keep, ticks)), 0f, 1f);
                arm = LerpAngle(windArm, endArm, e);
                blade = arm + cross;
                reach = MathHelper.Lerp(-pull, t.ImpaleReach, e);
            }
            else
            {
                // The lift: in each beat the blade rises over the first part, then waits for the pulse.
                float lift = 0f;
                if (impPhase == ImpFinish)
                    lift = 1f;
                else if (impPhase == ImpHold)
                {
                    int beats = Math.Max(1, t.ImpaleBeats);
                    float beatLen = Math.Max(1f, t.ImpaleBeatTicks);
                    int b = Math.Min(beats - 1, (int)(impHoldTick / beatLen));
                    float inBeat = impHoldTick - b * beatLen;
                    float rise = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(inBeat / Math.Max(1f, beatLen * t.ImpaleRisePortion), 0f, 1f));
                    lift = (b + rise) / beats;
                }
                // Only the arm lifts, from where the swing ended to the held angle (ImpaleHeldAngleDeg above the horizontal, on the facing side). The blade
                // keeps the angle it stabbed at: it stays the same distance off the arm, so it never straightens out and the enemy is carried up on it.
                float heldRad = Rad(t.ImpaleHeldAngleDeg);
                float heldArm = new Vector2(facing * (float)Math.Cos(heldRad), -(float)Math.Sin(heldRad)).ToRotation();
                arm = LerpAngle(endArm, heldArm, lift);
                blade = arm + cross;
                reach = t.ImpaleReach;
            }
            impArm = arm;
            impBlade = blade;
            impReach = reach;
            worldArm = arm;
            worldBlade = blade;
        }

        // After ApplyPose: the sprite is pushed out along the blade (draw only, through the stab's slide), the tip follows, and the timeline advances.
        void AfterImpalePose(Player owner)
        {
            owner.itemAnimation = 2; // keeps the item "in use" for as long as the impale lasts
            owner.itemTime = 2;
            if (IsOwner)
                owner.ChangeDir(facing);
            stabBlend = 1f;
            stabSlide = impReach;
            stabShake = Vector2.Zero;
            tipPosition = Projectile.Center + Projectile.rotation.ToRotationVector2() * (TotalLength + impReach);
            AdvanceImpale(owner);
        }

        // ---- the timeline ----

        void AdvanceImpale(Player owner)
        {
            IImpaleTuning t = ImpaleTune;
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            switch (impPhase)
            {
                case ImpWindup:
                    if (++impTimer >= Math.Max(1, t.ImpaleWindupTicks))
                    {
                        impPhase = ImpThrust;
                        impTimer = 0f;
                        FxPlaySwing(tipPosition, 0.4f);
                    }
                    break;

                case ImpThrust:
                    if (IsOwner && !impHit)
                        ImpaleContact(owner, direction);
                    if (++impTimer >= Math.Max(1, t.ImpaleSwingTicks))
                    {
                        impTimer = 0f;
                        if (IsOwner)
                        {
                            if (impCaught)
                                StartImpaleHold(owner, direction);
                            else
                                impPhase = ImpMiss;
                        }
                        else if (impTarget >= 0 && TargetNpc() != null)
                            StartImpaleHold(owner, direction);
                        else
                            impPhase = ImpWait;
                    }
                    break;

                case ImpWait:
                    if (impTarget >= 0 && TargetNpc() != null)
                        StartImpaleHold(owner, direction);
                    else if (++impTimer > t.ImpaleWaitTicks)
                    {
                        impPhase = ImpMiss;
                        impTimer = 0f;
                    }
                    break;

                case ImpHold:
                    TickImpaleHold(owner, direction);
                    break;

                case ImpFinish:
                case ImpMiss:
                    if (++impTimer >= Math.Max(1, impPhase == ImpFinish ? t.ImpaleFinishTicks : t.ImpaleMissTicks))
                        EndImpale(owner);
                    break;
            }
        }

        // Owner's client, every thrust tick until something is struck: the first enemy on the blade line (an enemy that can be gripped wins over one
        // that cannot, a boss comes last, then the nearest to the hand). It takes the stab as a normal hit; a gripped-able one is also caught.
        void ImpaleContact(Player owner, Vector2 direction)
        {
            IImpaleTuning t = ImpaleTune;
            float length = TotalLength * Tune.HitLengthScale + impReach;
            float width = Tune.HitWidth * Projectile.scale;
            int best = -1;
            int bestRank = int.MaxValue;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!Snarable.CanBite(npc) || npc.life <= 0)
                    continue;
                if (!ImpaleBladeTouches(npc.Hitbox, width, length))
                    continue;
                int rank = npc.boss ? 2 : (Snarable.CanGrip(npc, t.ImpaleMaxSize) ? 0 : 1);
                float distance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (rank < bestRank || (rank == bestRank && distance < bestDistance))
                {
                    best = i;
                    bestRank = rank;
                    bestDistance = distance;
                }
            }
            if (best < 0)
                return;

            NPC target = Main.npc[best];
            bool grip = bestRank == 0;
            impHit = true;
            int damage = Math.Max(1, (int)(Projectile.damage * t.ImpaleStabDamageMultiplier));
            target.SimpleStrikeNPC(damage, owner.Center.X < target.Center.X ? 1 : -1, false, grip ? 0f : Projectile.knockBack, DamageClass.Melee);
            if (grip)
            {
                impCaught = true;
                impTarget = best;
                Projectile.netUpdate = true; // the other machines hear who is caught
            }
            else
            {
                Vector2 point = Vector2.Clamp(tipPosition, target.TopLeft, target.BottomRight);
                FxImpalePierce(point, direction, false);
                CameraShake.Punch(point, t.ImpaleHitShake, 10);
            }
        }

        // The blade is a line from the hand; the swing is fast, so it is checked at a few poses between the previous tick and this one (an arc, not a stab).
        bool ImpaleBladeTouches(Rectangle box, float width, float length)
        {
            int steps = Math.Max(1, Tune.SweepHitSteps);
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

        void StartImpaleHold(Player owner, Vector2 direction)
        {
            NPC npc = TargetNpc();
            if (npc == null)
            {
                ImpaleFinish(owner, null, tipPosition); // the stab itself killed it
                return;
            }
            IImpaleTuning t = ImpaleTune;
            impPhase = ImpHold;
            impTimer = 0f;
            impHoldTick = 0;
            impBeatsDone = 0;
            impFrom = npc.Center;
            impLastAnchor = npc.Center;
            flash = 1f;
            FxImpalePierce(npc.Center, direction, true);
            CameraShake.Punch(npc.Center, t.ImpaleHitShake, 10);
        }

        void TickImpaleHold(Player owner, Vector2 direction)
        {
            IImpaleTuning t = ImpaleTune;
            NPC npc = TargetNpc();
            // The enemy died (or is gone), or the owner has ended the hold: straight to the finisher.
            if (npc == null || (!IsOwner && impTarget < 0))
            {
                ImpaleFinish(owner, npc, impLastAnchor);
                return;
            }

            // The enemy sits on the blade (it slides there from where it was caught), never inside a wall.
            Vector2 anchor = Projectile.Center + direction * (TotalLength * t.ImpaleAnchorFraction + impReach);
            float seat = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(impHoldTick / Math.Max(1f, t.ImpaleSeatTicks), 0f, 1f));
            Vector2 position = CarriedNPC.ClampToOpenAir(owner.Center, Vector2.Lerp(impFrom, anchor, seat), npc);
            CarriedNPC.Hold(npc, position, Projectile); // the enemy's own draw draws the blade before its sprite (IDrawnBehindCarried), so the enemy covers it
            impLastAnchor = position;
            int beatLen = Math.Max(1, t.ImpaleBeatTicks);
            int beats = Math.Max(1, t.ImpaleBeats);
            // The trembling rises with the hold: ImpaleShake at the start, ImpaleShakeGrowth times as much at the end of the last beat.
            float holdProgress = MathHelper.Clamp(impHoldTick / (float)(beats * beatLen), 0f, 1f);
            stabShake = Main.rand.NextVector2Circular(1f, 1f) * t.ImpaleShake * MathHelper.Lerp(1f, Math.Max(0f, t.ImpaleShakeGrowth), holdProgress);
            FxImpaleHold(position, Projectile.Center + direction * (TotalLength * 0.3f + impReach), Projectile.Center + direction * (TotalLength * 0.95f + impReach), holdProgress, impBeatsDone);
            if (impBeatsDone < beats && impHoldTick == (impBeatsDone + 1) * beatLen - 1)
            {
                int beat = impBeatsDone;
                impBeatsDone++;
                ImpaleBeat(owner, npc, beat);
            }
            impHoldTick++;
            if (impHoldTick >= beats * beatLen)
                ImpaleFinish(owner, npc, position);
        }

        // The end of a beat: a pulse on every machine; the owner's client also strikes.
        void ImpaleBeat(Player owner, NPC npc, int beat)
        {
            IImpaleTuning t = ImpaleTune;
            flash = 1f;
            FxImpaleBeat(npc.Center, beat);
            CameraShake.Punch(npc.Center, t.ImpaleBeatShake, 8);
            if (!IsOwner)
                return;
            int damage = Math.Max(1, (int)(Projectile.damage * t.ImpaleBeatDamageMultiplier));
            npc.SimpleStrikeNPC(damage, owner.Center.X < npc.Center.X ? 1 : -1, false, 0f, DamageClass.Melee);
        }

        // The finisher: the enemy is let go, the weapon's explosion goes off (owner) and the charge jumps to full (owner; the other machines hear it
        // through the charge sync). Also reached when the enemy dies before the last beat.
        void ImpaleFinish(Player owner, NPC npc, Vector2 point)
        {
            impPhase = ImpFinish;
            impTimer = 0f;
            flash = 1f;
            if (npc != null)
                CarriedNPC.Release(npc);
            FxImpaleFinish(point);
            if (!IsOwner)
                return;
            impTarget = -1;
            OnImpaleFinish(owner, point);
            AddOwnerCharge(owner, 1f);
            Projectile.netUpdate = true;
        }

        // The arm and blade angles it ended in become the resting angles the ready pose eases away from; the push-out slides back through StabBlendOut.
        void EndImpale(Player owner)
        {
            ReleaseImpaleTarget();
            impaling = false;
            stabShake = Vector2.Zero;
            armLocal = HeldSwordPose.ToWorld(facing, impArm);
            tiltRad = MathHelper.WrapAngle(HeldSwordPose.ToWorld(facing, impBlade) - armLocal);
            owner.itemAnimation = 0;
            owner.itemTime = 0;
        }
    }
}