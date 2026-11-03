using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BloodButcherer
{
    /// <summary>
    /// The held Blood Butcherer. It draws the jawed blade, poses the owner's arm and runs the bite (left click): WINDUP (pull back),
    /// THRUST (lunge along the aim, jaws shut so it looks like a normal sword), OPEN (the two halves pivot open on the handle and
    /// show their teeth), GAPE (hangs open), BITE (the jaws slam shut). The bite is the only part that damages. At the start of the
    /// bite the owner checks the catch wedge (the triangle between the pivot and the two open jaw tips): an enemy inside is CAUGHT,
    /// otherwise it is a MISS (which recovers). A caught enemy that can be gripped is pinned in the shut jaws (GRIP: the blade drags it
    /// in, spins a full circle and throws it at the cursor, see BloodButchererGripNPC); one that cannot (boss, worm, immovable, too big)
    /// is only clamped for a moment. A blood tether ties the blade's handle to the player's real hand the whole time.
    /// MULTIPLAYER: the owner's client decides everything (the click, the aim, the catch) and passes what the other machines need in
    /// the spawn packet: ai[2] = the aim angle (the player's own frame; during the grip the owner keeps it updated with the cursor until
    /// the lock, with netUpdate, and the last value is the throw direction). The decision travels in ai[0] (0 undecided, 1 miss,
    /// 2 clamp, 3 grip) and ai[1] (caught enemy index + 1) with netUpdate. Every machine runs the same timers for the pose, jaws, sounds,
    /// dust and trails; at the end of the bite a non-owner waits in the shut pose for the decision (BloodButchererTuning.DecideWaitTicks)
    /// and assumes a miss if it never arrives. Hits are detected by the owner's client like any projectile. No custom packets.
    /// </summary>
    public class BloodButchererSwingProjectile : ModProjectile
    {
        // The vanilla item sprite is the fallback projectile sprite (until the three art files exist).
        public override string Texture => "Terraria/Images/Item_" + ItemID.BloodButcherer;

        public enum Branch { Undecided = 0, Miss = 1, Clamp = 2, Grip = 3 }

        static Asset<Texture2D> handleArt;
        static Asset<Texture2D> upperArt;
        static Asset<Texture2D> lowerArt;

        Branch Decision => (Branch)(int)Projectile.ai[0];
        int CaughtIndex => (int)Projectile.ai[1] - 1;
        float Aim => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int Facing => facing != 0 ? facing : (Math.Cos(Projectile.ai[2]) >= 0.0 ? 1 : -1); // a bite faces the side of its first aim on every machine, also while the blade spins behind the player

        // Per-machine state: counters and visuals, never synced.
        int ticks;               // ticks this machine has run this projectile
        float timer;             // ticks along the timeline (frozen while waiting for the owner's decision)
        int waited;              // ticks spent waiting for it
        int lastEventTick = -1;  // the timeline tick the one-shot effects last ran for
        bool decided;            // owner only: the catch decision has been made
        bool clampPlayed;        // the clamp effects have played
        float flash;             // 1 right after a bite or a clamp, fades to 0
        float jawOpen;           // 0..1: how far the jaws are open on this tick
        Vector2 handPos;         // the hand, pushed along the aim by the lunge (also Projectile.Center)
        Vector2 pivot;           // where the jaws pivot
        Vector2 bladeDir;        // the blade's direction (a unit vector)
        float jawLength;         // pivot to the jaw tips, in pixels (scaled)
        int facing;              // set on the first tick: the side the first aim pointed to
        float startAim;          // the aim the bite started with (the player's own frame)
        float prevTimer = -1f;   // the timeline value of the previous tick
        float spinTotal = MathHelper.TwoPi; // how far the blade sweeps in the spin (follows the cursor)
        float lastBlade;         // last tick's blade angle, for the angular speed
        float angularSpeed;      // radians per tick the blade turned this tick
        bool bladeKnown;
        float windStart = -1f;   // the timeline time the spin ended (the throw, or the enemy dying first); -1 while it is still spinning
        float windAngle;         // the blade's angle at that moment (the player's own frame)
        float windOmega;         // its angular speed then (radians per tick, signed)
        float windOffset;        // how far out it was pushed then
        bool thrown;             // the enemy really was thrown (so the jaws fly open)
        SweepTrail spinTrail;    // the fan the spinning blade leaves (null until the spin starts)
        bool grabbed;            // the pin has started on this machine
        Vector2 grabFrom;        // where the enemy was when it was caught (the pull starts here)
        int gripClock;           // ticks spent gripping (owner: paces the periodic hits)
        int launchUntil = -1;    // owner: the throw hit may land until this tick
        int spinSoundClock;
        Vector2 armHand;         // the owner's real hand: where the tether starts
        Vector2 tetherBend;
        float tetherStretch;
        BloodButchererTether tether;
        Vector2 lastOwnerCenter; // the trail interpolates the owner's movement from it
        PixelTrail jawA;         // the line each jaw tip leaves (null until the thrust starts)
        PixelTrail jawB;
        Vector2 lastTipA;
        Vector2 lastTipB;
        bool hasTips;

        // ---- timeline ----

        static int ThrustStart => BloodButchererTuning.WindupTicks;
        static int OpenStart => ThrustStart + BloodButchererTuning.ThrustTicks;
        static int GapeStart => OpenStart + BloodButchererTuning.OpenTicks;
        static int BiteStart => GapeStart + BloodButchererTuning.GapeTicks;
        static int BiteEnd => BiteStart + BloodButchererTuning.BiteTicks;
        static int PullEnd => BiteEnd + BloodButchererTuning.PullTicks;        // grip: the enemy is dragged to the middle of the mouth
        static int SpinEnd => PullEnd + BloodButchererTuning.SpinTicks;        // grip: the full circle, ends in the throw
        static int LockAt => SpinEnd - BloodButchererTuning.LockTicks;         // grip: the aim stops following the cursor

        // What this machine believes happened: a non-owner that never hears the decision assumes a miss.
        Branch Resolved
        {
            get
            {
                Branch branch = Decision;
                return branch == Branch.Undecided && waited >= BloodButchererTuning.DecideWaitTicks ? Branch.Miss : branch;
            }
        }

        int RecoverStart
        {
            get
            {
                switch (Resolved)
                {
                    case Branch.Clamp: return BiteEnd + BloodButchererTuning.ClampTicks;
                    case Branch.Grip: return (int)(windStart >= 0f ? windStart : SpinEnd) + BloodButchererTuning.WindDownTicks; // the blade keeps spinning and slows down until then
                    default: return BiteEnd;
                }
            }
        }
        int TotalTicks => RecoverStart + (Resolved == Branch.Grip ? BloodButchererTuning.WindDownRecoverTicks : BloodButchererTuning.RecoverTicks);
        bool Caught => Resolved == Branch.Clamp || Resolved == Branch.Grip;
        // The bite, then (grip) the held ticks until the spin ends, then only the throw's own tick or two. An enemy that died first ends it at once.
        bool Damaging => (timer >= BiteStart && timer < BiteEnd)
            || (Resolved == Branch.Grip && timer >= BiteEnd && (windStart < 0f ? timer < SpinEnd + 2f : thrown && timer < windStart + 2f));

        NPC CaughtNpc
        {
            get
            {
                int index = CaughtIndex;
                return index >= 0 && index < Main.maxNPCs && Main.npc[index].active ? Main.npc[index] : null;
            }
        }

        // An enemy the jaws can pin and throw: not a boss, not a worm segment, not immovable, not huge. Anything else is only clamped (patch 3: chomped).
        static bool CanGrip(NPC npc)
        {
            return !npc.boss && npc.knockBackResist > 0f && npc.realLife < 0 && Math.Max(npc.width, npc.height) <= BloodButchererTuning.MaxGrabSize;
        }

        // The world direction of an aim given in the player's own frame.
        Vector2 WorldDir(float aim)
        {
            return (Owner.gravDir == -1f ? -aim : aim).ToRotationVector2();
        }

        // The blade's angle (the player's own frame) at a time on the timeline: the first aim until the spin, then at least a full circle toward
        // the side the first aim faced that ends on the live aim, accelerating (ease-in) so the throw leaves at the blade's top speed, then
        // (after the throw, or when the enemy died first) it keeps that speed and slows down evenly to a stop.
        float AngleAt(float time)
        {
            if (Resolved != Branch.Grip)
                return startAim;
            if (windStart >= 0f && time >= windStart)
            {
                float span = Math.Max(1, BloodButchererTuning.WindDownTicks);
                float s = MathHelper.Clamp((time - windStart) / span, 0f, 1f);
                return windAngle + windOmega * span * (s - 0.5f * s * s);
            }
            return SpinAngleAt(time);
        }

        // Angular speed (radians per tick, signed) at a time: the same two phases as AngleAt.
        float OmegaAt(float time)
        {
            if (Resolved != Branch.Grip)
                return 0f;
            if (windStart >= 0f && time >= windStart)
                return windOmega * (1f - MathHelper.Clamp((time - windStart) / Math.Max(1, BloodButchererTuning.WindDownTicks), 0f, 1f));
            return SpinOmegaAt(time);
        }

        float SpinAngleAt(float time)
        {
            if (time <= PullEnd)
                return startAim;
            float u = MathHelper.Clamp((time - PullEnd) / Math.Max(1, BloodButchererTuning.SpinTicks), 0f, 1f);
            return startAim + Facing * spinTotal * u * u;
        }

        float SpinOmegaAt(float time)
        {
            if (time <= PullEnd)
                return 0f;
            float span = Math.Max(1, BloodButchererTuning.SpinTicks);
            float u = MathHelper.Clamp((time - PullEnd) / span, 0f, 1f);
            return Facing * spinTotal * 2f * u / span;
        }

        // The spin is over (the throw, or the enemy is gone): from here on the blade keeps its angle, speed and reach and slows down from them.
        void BeginWindDown(float at)
        {
            windAngle = SpinAngleAt(at);
            windOmega = SpinOmegaAt(at);
            windOffset = OffsetAt(at);
            windStart = at;
        }

        // How far the blade is pushed along the aim from the hand (negative = pulled back).
        float OffsetAt(float time)
        {
            float pull = BloodButchererTuning.WindupPullback;
            float thrust = BloodButchererTuning.ThrustDistance;
            float held = thrust + BloodButchererTuning.BiteLungeDistance;
            if (time < ThrustStart)
                return MathHelper.Lerp(0f, -pull, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, ThrustStart)));
            if (time < OpenStart)
            {
                float u = (time - ThrustStart) / Math.Max(1, BloodButchererTuning.ThrustTicks);
                return MathHelper.Lerp(-pull, thrust, 1f - (float)Math.Pow(1f - u, 3)); // fast out, slowing at the end
            }
            if (time < BiteStart)
                return thrust;
            if (time < BiteEnd)
            {
                float u = (time - BiteStart) / Math.Max(1, BloodButchererTuning.BiteTicks);
                return MathHelper.Lerp(thrust, held, 1f - (float)Math.Pow(1f - u, 2));
            }
            if (time < RecoverStart)
            {
                if (Resolved != Branch.Grip)
                    return held;
                float spin = BloodButchererTuning.SpinOffset;
                if (windStart >= 0f && time >= windStart) // the wind-down: it comes back in to the player while it slows
                    return MathHelper.Lerp(windOffset, 0f, MathHelper.SmoothStep(0f, 1f, (time - windStart) / Math.Max(1, BloodButchererTuning.WindDownTicks)));
                if (time < PullEnd) // the blade comes back in, dragging the enemy
                    return MathHelper.Lerp(held, spin, MathHelper.SmoothStep(0f, 1f, (time - BiteEnd) / Math.Max(1, BloodButchererTuning.PullTicks)));
                return spin;
            }
            if (Resolved == Branch.Grip)
                return 0f; // the blade is back in the hand
            return MathHelper.Lerp(held, 0f, MathHelper.SmoothStep(0f, 1f, (time - RecoverStart) / Math.Max(1, BloodButchererTuning.RecoverTicks)));
        }

        // 0 = shut, 1 = fully open: eases open, hangs, then slams shut with an ease-in so the bite lands hard.
        float OpenAt(float time)
        {
            if (time < OpenStart)
                return 0f;
            if (time < GapeStart)
            {
                float u = (time - OpenStart) / Math.Max(1, BloodButchererTuning.OpenTicks);
                return 1f - (float)Math.Pow(1f - u, 3);
            }
            if (time < BiteStart)
                return 1f;
            if (time < BiteEnd)
            {
                float u = (time - BiteStart) / Math.Max(1, BloodButchererTuning.BiteTicks);
                return 1f - (float)Math.Pow(u, 2.5);
            }
            if (Resolved != Branch.Grip || !thrown || windStart < 0f || time < windStart)
                return 0f; // shut: the bite, the grip and the spin, and a blade whose enemy died before the throw
            // The throw: the jaws fly open, hang open for a moment, then close again while the blade slows down.
            float since = time - windStart;
            if (since < BloodButchererTuning.ReleaseOpenTicks)
            {
                float u = since / Math.Max(1, BloodButchererTuning.ReleaseOpenTicks);
                return 1f - (1f - u) * (1f - u);
            }
            float holdEnd = BloodButchererTuning.ReleaseOpenTicks + BloodButchererTuning.ReleaseHoldTicks;
            if (since < holdEnd)
                return 1f;
            return 1f - MathHelper.SmoothStep(0f, 1f, (since - holdEnd) / Math.Max(1f, BloodButchererTuning.WindDownTicks - holdEnd));
        }

        // ---- AI ----

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
            Projectile.localNPCHitCooldown = -1; // every enemy once per bite
            Projectile.ownerHitCheck = true;
        }

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/BloodButcherer/BloodButchererHandle", out handleArt);
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/BloodButcherer/BloodButchererJawUpper", out upperArt);
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/BloodButcherer/BloodButchererJawLower", out lowerArt);
        }

        public override void Unload()
        {
            handleArt = null;
            upperArt = null;
            lowerArt = null;
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.BloodButcherer)
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

            if (ticks == 0)
            {
                lastOwnerCenter = owner.Center;
                startAim = Projectile.ai[2]; // the spawn packet carries it, so every machine starts from the same aim
                facing = Math.Cos(startAim) >= 0.0 ? 1 : -1;
                if (IsOwner)
                    owner.ChangeDir(Facing);
            }

            Projectile.scale = BloodButchererTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);
            UpdateAim();
            jawOpen = OpenAt(timer);

            owner.itemAnimation = 2; // keeps the item "in use" for as long as the blade is out
            owner.itemTime = 2;
            ApplyPose(owner);

            if (IsOwner && !decided && (int)timer == BiteStart)
                Decide();

            GripStep();
            UpdateTether();
            Effects();
            LayTrail();

            flash = Math.Max(0f, flash - 1f / Math.Max(1, BloodButchererTuning.FlashTicks));
            jawA?.Tick();
            jawB?.Tick();
            spinTrail?.Tick();
            lastOwnerCenter = owner.Center;
            ticks++;

            // At the end of the bite a machine that has not heard the owner's decision yet holds the shut pose for a moment.
            bool waiting = (int)timer >= BiteEnd && Decision == Branch.Undecided && waited < BloodButchererTuning.DecideWaitTicks;
            prevTimer = timer;
            if (waiting)
                waited++;
            else
                timer++;
        }

        // ---- grip: aim, spin, pin and throw ----

        // Once the bite has caught something grippable the owner follows the cursor until the lock (netUpdate, a few ticks apart) and every
        // machine eases the sweep of the spin toward the one that ends on the live aim (the last synced ai[2]).
        void UpdateAim()
        {
            if (Resolved != Branch.Grip || timer < BiteEnd)
                return;

            // The player faces the cursor for the whole spin and the wind-down (the direction is synced like any player's).
            if (IsOwner)
                Owner.ChangeDir(Main.MouseWorld.X >= Owner.Center.X ? 1 : -1);
            if (windStart >= 0f)
                return; // the spin is over, the aim is final

            if (IsOwner && timer < LockAt)
            {
                Vector2 toMouse = Main.MouseWorld - Owner.MountedCenter;
                if (toMouse.LengthSquared() > 1f)
                {
                    float aim = toMouse.ToRotation();
                    if (Owner.gravDir == -1f)
                        aim = -aim;
                    if (Math.Abs(MathHelper.WrapAngle(aim - Projectile.ai[2])) > 0.02f)
                    {
                        Projectile.ai[2] = aim;
                        if (ticks % 3 == 0)
                            Projectile.netUpdate = true;
                    }
                }
            }
            if (IsOwner && prevTimer < LockAt && timer >= LockAt)
                Projectile.netUpdate = true; // make sure the final aim is out

            // The sweep in the spin direction: ALWAYS at least one full circle, plus however far the cursor is ahead of the first aim (0 to
            // 360 degrees more), so the blade finishes pointing at the cursor. It follows the cursor at a limited speed, and after the lock it
            // closes the rest of the gap exactly by the end of the spin.
            float ahead = Facing * (Projectile.ai[2] - startAim);
            ahead -= MathHelper.TwoPi * (float)Math.Floor(ahead / MathHelper.TwoPi);
            float target = MathHelper.TwoPi + ahead;
            float step = BloodButchererTuning.SpinFollow;
            if (timer >= LockAt)
                step = Math.Max(step, Math.Abs(target - spinTotal) / Math.Max(1f, SpinEnd - timer));
            spinTotal += MathHelper.Clamp(target - spinTotal, -step, step);
        }

        // Every machine: pin the enemy in the mouth while it is gripped, drip and whoosh, and throw it at the end of the spin. If it dies (or
        // leaves) first there is no throw: the blade just carries on spinning from where it is, slowing down, and comes back to the player.
        void GripStep()
        {
            if (Resolved != Branch.Grip || windStart >= 0f || timer < BiteEnd)
                return;

            NPC npc = CaughtNpc;
            if (npc == null)
            {
                BeginWindDown(timer);
                return;
            }
            if (timer >= SpinEnd)
            {
                Launch(npc);
                BeginWindDown(timer);
                return;
            }

            Pin(npc);
            gripClock++;
            if (IsOwner && gripClock % Math.Max(1, BloodButchererTuning.GripTickInterval) == 0)
                Projectile.localNPCImmunity[npc.whoAmI] = 0; // the next periodic hit may land
            if (Main.rand.NextBool(3))
                BloodButchererFx.GripDrip(npc.Center);
            if (timer >= PullEnd && spinSoundClock++ % 8 == 0)
                BloodButchererFx.PlaySpin(npc.Center, MathHelper.Lerp(BloodButchererTuning.SpinWhooshPitchLow, BloodButchererTuning.SpinWhooshPitchHigh, (timer - PullEnd) / Math.Max(1, BloodButchererTuning.SpinTicks)));
        }

        // The enemy sits in the middle of the shut jaws: dragged in from where it was caught during the pull, then carried around by the spin.
        // The spot is clamped along the ray from the player so the enemy never ends up inside tiles.
        void Pin(NPC npc)
        {
            if (!grabbed)
            {
                grabbed = true;
                grabFrom = npc.Center;
            }
            float pull = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((timer - BiteEnd) / Math.Max(1, BloodButchererTuning.PullTicks), 0f, 1f));
            Vector2 mouth = pivot + bladeDir * (jawLength * BloodButchererTuning.PinRatio);
            Vector2 target = ClampToOpenAir(Owner.Center, Vector2.Lerp(grabFrom, mouth, pull), npc);

            npc.Center = target;
            npc.velocity = Vector2.Zero;
            BloodButchererGripNPC grip = npc.GetGlobalNPC<BloodButchererGripNPC>();
            grip.HoldTick = (long)Main.GameUpdateCount;
            grip.LaunchTick = -1000;
            if (Main.netMode == NetmodeID.Server && ticks % 3 == 0)
                npc.netUpdate = true; // the server's copy is the authority
        }

        static Vector2 ClampToOpenAir(Vector2 from, Vector2 target, NPC npc)
        {
            Vector2 delta = target - from;
            float length = delta.Length();
            if (length < 1f)
                return target;
            Vector2 direction = delta / length;
            Vector2 half = new Vector2(npc.width, npc.height) * 0.5f;
            Vector2 safe = from;
            for (float d = 8f; ; d += 8f)
            {
                float at = Math.Min(d, length);
                Vector2 p = from + direction * at;
                if (Collision.SolidCollision(p - half, npc.width, npc.height))
                    return safe;
                safe = p;
                if (at >= length)
                    return safe;
            }
        }

        // The end of the spin: the jaws fly open and the enemy leaves toward the cursor.
        void Launch(NPC npc)
        {
            Vector2 direction = WorldDir(Projectile.ai[2]);
            Vector2 where = npc != null ? npc.Center : pivot + bladeDir * jawLength * 0.6f;
            if (npc != null)
            {
                npc.velocity = direction * BloodButchererTuning.LaunchSpeed;
                BloodButchererGripNPC grip = npc.GetGlobalNPC<BloodButchererGripNPC>();
                grip.HoldTick = -1000;
                grip.LaunchTick = (long)Main.GameUpdateCount;
                if (Main.netMode == NetmodeID.Server)
                    npc.netUpdate = true;
                if (IsOwner)
                {
                    Projectile.localNPCImmunity[npc.whoAmI] = 0; // the throw hit
                    launchUntil = ticks + 1;
                }
            }
            thrown = npc != null;
            flash = 1f;
            BloodButchererFx.PlayThrow(where);
            BloodButchererFx.ThrowBurst(where, direction);
            CameraShake.Punch(where, BloodButchererTuning.LaunchShake, 18, "BloodButchererLaunch");
        }

        // ---- blood tether ----

        // Ties the handle to the real hand: slack it droops under gravity (and drips), taut and spinning it is pulled straight and trails
        // behind the blade. Pure function of the pose, so every machine builds the same one.
        void UpdateTether()
        {
            float distance = Vector2.Distance(armHand, handPos);
            float rest = BloodButchererTuning.TetherRestLength;
            float taut = MathHelper.Clamp(Math.Abs(angularSpeed) / 0.12f, 0f, 1f); // a fast spin pulls it straight
            float sag = (float)Math.Sqrt(3f * Math.Max(distance, 4f) * Math.Max(0f, rest - distance) / 8f) * BloodButchererTuning.TetherSag * (1f - taut);
            sag = Math.Min(sag, BloodButchererTuning.TetherMaxSag);

            Vector2 down = new Vector2(0f, Owner.gravDir);
            Vector2 trailDirection = bladeDir.RotatedBy(-Math.Sign(angularSpeed) * MathHelper.PiOver2);
            float trailing = Math.Min(Math.Abs(angularSpeed) * BloodButchererTuning.TetherTrailing, BloodButchererTuning.TetherMaxTrailing);
            tetherBend = down * sag + trailDirection * trailing;
            tetherStretch = MathHelper.Clamp(distance / Math.Max(1f, rest), 0f, 1f);

            if (sag > 1.5f && distance > 3f && Main.rand.NextBool(5))
                BloodButchererFx.TetherDrip((armHand + handPos) * 0.5f + tetherBend);
        }

        // Moves the owner's arm to the aim and puts the projectile in the hand, pushed along the aim by the lunge.
        void ApplyPose(Player owner)
        {
            float aim = AngleAt(timer);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, aim);
            float blade = owner.gravDir == -1f ? -aim : aim;
            bladeDir = blade.ToRotationVector2();
            armHand = hand;
            angularSpeed = bladeKnown ? MathHelper.WrapAngle(blade - lastBlade) : 0f;
            lastBlade = blade;
            bladeKnown = true;

            Vector2 position = hand + bladeDir * OffsetAt(timer);
            // The clamp makes the whole blade tremble.
            if (Resolved == Branch.Clamp && timer >= BiteEnd && timer < BiteEnd + BloodButchererTuning.ClampTicks)
                position += Main.rand.NextVector2Circular(1.5f, 1.5f);

            Projectile.rotation = blade;
            Projectile.Center = position;
            Projectile.spriteDirection = Facing;
            handPos = position;
            pivot = position + bladeDir * (BloodButchererTuning.HandleLength * Projectile.scale);
            jawLength = (BloodButchererTuning.BladeLength - BloodButchererTuning.HandleLength) * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        // ---- the catch (owner only) ----

        static bool CanBite(NPC npc, Projectile projectile)
        {
            return npc.active && !npc.friendly && !npc.dontTakeDamage && npc.CanBeChasedBy(projectile);
        }

        // At the start of the bite: the nearest enemy (to the middle of the mouth) inside the catch wedge is caught. The decision
        // reaches the other machines through ai[0], ai[1] and netUpdate.
        void Decide()
        {
            decided = true;
            Vector2 mouth = pivot + bladeDir * jawLength * 0.6f;
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!CanBite(npc, Projectile))
                    continue;
                Rectangle box = npc.Hitbox;
                box.Inflate((int)BloodButchererTuning.CatchPadding, (int)BloodButchererTuning.CatchPadding);
                if (!WedgeTouches(box))
                    continue;
                float distance = Vector2.DistanceSquared(npc.Center, mouth);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            Projectile.ai[0] = (float)(best >= 0 ? (CanGrip(Main.npc[best]) ? Branch.Grip : Branch.Clamp) : Branch.Miss);
            Projectile.ai[1] = best + 1;
            Projectile.netUpdate = true;
        }

        // The catch wedge: the triangle between the pivot and the two jaw tips at full gape. The bite is quick, so it is tested against
        // the full gape for the whole bite: forgiving on purpose.
        bool WedgeTouches(Rectangle box)
        {
            float spread = MathHelper.ToRadians(BloodButchererTuning.JawOpenDeg);
            Vector2 a = pivot;
            Vector2 b = pivot + bladeDir.RotatedBy(spread) * jawLength;
            Vector2 c = pivot + bladeDir.RotatedBy(-spread) * jawLength;

            if (InTriangle(box.Center.ToVector2(), a, b, c)
                || InTriangle(box.TopLeft(), a, b, c) || InTriangle(box.TopRight(), a, b, c)
                || InTriangle(box.BottomLeft(), a, b, c) || InTriangle(box.BottomRight(), a, b, c))
                return true;

            // A thin enemy can cross an edge without having a corner inside.
            float point = 0f;
            return Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), a, b, 4f, ref point)
                || Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), b, c, 4f, ref point)
                || Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), c, a, 4f, ref point);
        }

        static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Side(p, a, b);
            float d2 = Side(p, b, c);
            float d3 = Side(p, c, a);
            bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }

        static float Side(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.X - b.X) * (a.Y - b.Y) - (a.X - b.X) * (p.Y - b.Y);
        }

        // ---- effects and trails (every machine) ----

        Vector2 CaughtCenter(Vector2 fallback)
        {
            int index = CaughtIndex;
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].active)
                return Main.npc[index].Center;
            return fallback;
        }

        void Effects()
        {
            int t = (int)timer;
            Vector2 mouth = pivot + bladeDir * jawLength * 0.6f;

            if (t != lastEventTick)
            {
                lastEventTick = t;
                if (t == ThrustStart)
                    BloodButchererFx.PlayLunge(handPos);
                if (t == OpenStart)
                {
                    BloodButchererFx.PlayOpen(pivot);
                    BloodButchererFx.MouthMist(pivot, bladeDir);
                }
                if (t == BiteStart)
                {
                    flash = 1f;
                    BloodButchererFx.PlayBite(mouth);
                    BloodButchererFx.BiteSnap(mouth);
                }
            }

            // The clamp lands once the bite is over and the decision says something was caught.
            if (!clampPlayed && t >= BiteEnd && Caught)
            {
                clampPlayed = true;
                flash = 1f;
                Vector2 where = CaughtCenter(mouth);
                BloodButchererFx.ClampBurst(where);
                BloodButchererFx.PlayClamp(where);
                CameraShake.Punch(where, BloodButchererTuning.ClampShake, 18, "BloodButchererClamp");
            }

            float angle = MathHelper.ToRadians(BloodButchererTuning.JawOpenDeg) * jawOpen;
            if (jawOpen > 0.5f && Main.rand.NextBool(4))
                BloodButchererFx.Drool(pivot + bladeDir.RotatedBy(Main.rand.NextBool() ? angle : -angle) * jawLength);

            // The spin and the wind-down: blood flies off the tip along the tangent, and the blade whooshes while it is still fast.
            if (Resolved == Branch.Grip && timer >= PullEnd && timer < RecoverStart)
            {
                float omega = OmegaAt(timer);
                float speed = Math.Abs(omega);
                if (speed > 0.12f && Main.rand.NextBool(2))
                    BloodButchererFx.SpinFling(pivot + bladeDir * jawLength, bladeDir.RotatedBy(Math.Sign(omega) * MathHelper.PiOver2) * (speed * jawLength * 0.7f));
                if (windStart >= 0f && speed > 0.1f && spinSoundClock++ % 8 == 0)
                    BloodButchererFx.PlaySpin(pivot, MathHelper.Clamp(MathHelper.Lerp(BloodButchererTuning.SpinWhooshPitchLow, BloodButchererTuning.SpinWhooshPitchHigh, speed / 0.5f) - 0.3f, -1f, 1f));
            }

            Lighting.AddLight(mouth, 0.45f + 0.5f * flash, 0.05f + 0.1f * flash, 0.05f + 0.1f * flash);
        }

        // Both jaw tips lay a line from the start of the thrust to the end of the bite: one line while the jaws are shut (the tips
        // coincide), forking into two arcs when they open and meeting again in the bite. Several points per tick, laid between the
        // previous tick's pose and this one, so the pixel line follows a smooth arc.
        void LayTrail()
        {
            if (Main.dedServ)
                return;
            if (ticks == 0)
                return;

            int samples = Math.Max(1, BloodButchererTuning.TrailSamplesPerTick);
            for (int k = 1; k <= samples; k++)
            {
                float fraction = k / (float)samples;
                float time = MathHelper.Lerp(prevTimer, timer, fraction);
                if (TrailActive(time))
                    AddJawSample(time, fraction);
            }

            // The spin and the wind-down leave a fan along the whole blade instead (a few more samples per tick: the tip moves fast).
            if (Resolved == Branch.Grip)
            {
                int spinSamples = Math.Max(1, BloodButchererTuning.SpinTrailSamplesPerTick);
                for (int k = 1; k <= spinSamples; k++)
                {
                    float fraction = k / (float)spinSamples;
                    float time = MathHelper.Lerp(prevTimer, timer, fraction);
                    if (time >= PullEnd && time < RecoverStart)
                        AddSpinSample(time, fraction);
                }
            }
        }

        // The thrust and the bite: the two jaw lines.
        bool TrailActive(float time)
        {
            return time >= ThrustStart && time < BiteEnd;
        }

        // One slice of the fan: the outer edge is the tip, the inner edge slides from the tip down the blade as the blade speeds up (like the
        // Ice Blade's higher tiers covering more width) and back up again as it slows down. It works the same when the enemy died mid-spin.
        void AddSpinSample(float time, float fraction)
        {
            float aim = AngleAt(time);
            Vector2 direction = WorldDir(aim);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, aim) + shift + direction * OffsetAt(time);
            float length = BloodButchererTuning.BladeLength * Projectile.scale;
            float fill = MathHelper.Clamp(Math.Abs(OmegaAt(time)) / Math.Max(0.01f, BloodButchererTuning.TrailFullSpeed), 0f, 1f);
            float inner = MathHelper.Lerp(1f, BloodButchererTuning.TrailFillTo, MathHelper.SmoothStep(0f, 1f, fill));

            spinTrail ??= new SweepTrail(new BloodButchererSpinTrailStyle(), IsOwner);
            spinTrail.Add(hand + direction * length, hand + direction * length * inner);
        }

        void AddJawSample(float time, float fraction)
        {
            float aim = AngleAt(time);
            Vector2 direction = WorldDir(aim);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, aim) + shift + direction * OffsetAt(time);
            Vector2 axis = hand + direction * (BloodButchererTuning.HandleLength * Projectile.scale);
            float angle = MathHelper.ToRadians(BloodButchererTuning.JawOpenDeg) * OpenAt(time);
            Vector2 tipA = axis + direction.RotatedBy(angle) * jawLength;
            Vector2 tipB = axis + direction.RotatedBy(-angle) * jawLength;

            // Skip a sample that sits on the previous one (the gape holds still): a zero-length segment adds nothing.
            if (hasTips && Vector2.DistanceSquared(tipA, lastTipA) < 0.25f && Vector2.DistanceSquared(tipB, lastTipB) < 0.25f)
                return;

            jawA ??= new PixelTrail(new BloodButchererJawTrailStyle(), IsOwner);
            jawB ??= new PixelTrail(new BloodButchererJawTrailStyle(), IsOwner);
            jawA.Add(tipA);
            jawB.Add(tipB);
            lastTipA = tipA;
            lastTipB = tipB;
            hasTips = true;
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return Damaging ? (bool?)null : false;
        }

        // After the bite only the gripped enemy can be hurt (the held ticks and the throw).
        public override bool? CanHitNPC(NPC target)
        {
            if (timer >= BiteEnd && target.whoAmI != CaughtIndex)
                return false;
            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Damaging)
                return false;
            if (timer >= BiteEnd)
                return true; // the pinned enemy is in the jaws by construction (CanHitNPC already limits it to that one)
            return WedgeTouches(targetHitbox);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
            if (timer < BiteEnd)
            {
                modifiers.SourceDamage *= target.whoAmI == CaughtIndex ? BloodButchererTuning.BiteDamageMultiplier : BloodButchererTuning.OtherDamageMultiplier;
                return;
            }
            // The gripped enemy stays where the jaws hold it: no knockback. A light hit every few ticks, one big hit for the throw.
            modifiers.Knockback *= 0f;
            modifiers.SourceDamage *= ticks <= launchUntil ? BloodButchererTuning.LaunchDamageMultiplier : BloodButchererTuning.GripTickDamageMultiplier;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BloodButchererFx.HitBlood(target.Center);
        }

        // The trails outlive the bite and keep fading on their own (DetachedTrails).
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(jawA);
            DetachedTrails.Hand(jawB);
            DetachedTrails.Hand(spinTrail);
            jawA = null;
            jawB = null;
            spinTrail = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            spinTrail?.Draw();
            jawA?.Draw();
            jawB?.Draw();

            tether ??= new BloodButchererTether();
            tether.Draw(armHand, handPos, tetherBend, tetherStretch, flash, ticks, lightColor);

            float angle = MathHelper.ToRadians(BloodButchererTuning.JawOpenDeg) * jawOpen;
            Color body = Color.Lerp(lightColor, Color.White, 0.25f + 0.4f * flash);
            if (handleArt?.IsLoaded == true && upperArt?.IsLoaded == true && lowerArt?.IsLoaded == true)
                DrawArt(body, angle);
            else
                DrawFallback(body, angle);
            return false;
        }

        // Your art: three sprites drawn like a vanilla sword (tip to the upper right, 45 degrees). Each one's pivot is the centre of its own
        // canvas, and every centre is put on the jaw joint, so the jaws swing about it and the handle (padded with transparent pixels) sticks
        // out behind it. Facing left mirrors them like the vanilla sprite (HeldSwordPose.Sprite), so the upper jaw stays on top.
        void DrawArt(Color body, float angle)
        {
            Vector2 position = pivot - Main.screenPosition;
            HeldSwordPose.Sprite(handleArt.Value, Projectile.spriteDirection, out _, out float rotationOffset, out SpriteEffects effects);
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;

            DrawPiece(handleArt.Value, position, rotation, scale, effects, body);
            DrawPiece(lowerArt.Value, position, rotation + Facing * angle, scale, effects, body);
            DrawPiece(upperArt.Value, position, rotation - Facing * angle, scale, effects, body);

            if (flash > 0.01f)
            {
                Color glow = BloodButchererTuning.CrimsonColor * (0.9f * flash);
                DrawPiece(lowerArt.Value, position, rotation + Facing * angle, scale * 1.06f, effects, glow);
                DrawPiece(upperArt.Value, position, rotation - Facing * angle, scale * 1.06f, effects, glow);
            }
        }

        static void DrawPiece(Texture2D texture, Vector2 position, float rotation, float scale, SpriteEffects effects, Color color)
        {
            Vector2 origin = new Vector2(texture.Width, texture.Height) * 0.5f;
            Main.EntitySpriteDraw(texture, position, null, color, rotation, origin, scale, effects, 0);
        }

        // Until the art exists: the vanilla sprite. Shut, it is one normal sword. Open, two copies of it splay out on the pivot and the
        // teeth are drawn as pixel spikes.
        void DrawFallback(Color body, float angle)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            float scale = Projectile.scale;

            if (jawOpen < 0.01f)
            {
                DrawSword(texture, handPos, Projectile.rotation + rotationOffset, origin, scale, effects, body);
                if (flash > 0.01f)
                    DrawSword(texture, handPos, Projectile.rotation + rotationOffset, origin, scale * 1.06f, effects, BloodButchererTuning.CrimsonColor * (0.9f * flash));
                return;
            }

            for (int jaw = 0; jaw < 2; jaw++)
            {
                float a = jaw == 0 ? angle : -angle;
                Vector2 hilt = pivot + (handPos - pivot).RotatedBy(a);
                DrawSword(texture, hilt, Projectile.rotation + a + rotationOffset, origin, scale, effects, body);
                if (flash > 0.01f)
                    DrawSword(texture, hilt, Projectile.rotation + a + rotationOffset, origin, scale * 1.06f, effects, BloodButchererTuning.CrimsonColor * (0.9f * flash));

                Vector2 jawDirection = bladeDir.RotatedBy(a);
                Vector2 inward = jawDirection.RotatedBy(-Math.Sign(a) * MathHelper.PiOver2); // toward the blade's axis
                BloodButchererFx.DrawTeeth(pivot, jawDirection, jawLength, inward, jawOpen, scale, body);
            }
        }

        static void DrawSword(Texture2D texture, Vector2 worldPosition, float rotation, Vector2 origin, float scale, SpriteEffects effects, Color color)
        {
            Main.EntitySpriteDraw(texture, worldPosition - Main.screenPosition, null, color, rotation, origin, scale, effects, 0);
        }
    }
}