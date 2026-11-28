using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Rope;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Boomerangs.CombatWrench
{
    /// <summary>
    /// The thrown Combat Wrench (W1: the throw only, no damage yet). It flies the Forward Pass ellipse toward the cursor (out along the
    /// upper side, back along the other half, one whole loop that starts and ends at the player) on a rope that runs from the player's
    /// hand to the butt of the wrench sprite. A solid block stops it (platforms do not) and it is pulled back to the hand and caught;
    /// a throw that touched nothing ends the same way after the loop. Gravity does not touch it. Only one is in flight at a time.
    /// ai[0] = phase (see Phase; W3 uses Out, Back, Snap, Reel, Retract, HandOver (the hold in the hand until the swing projectile takes over) and Return), ai[1] = the latched enemy's index plus 1 (0 = none),
    /// ai[2] = the aim angle in radians (world), set at the spawn.
    /// MULTIPLAYER: the owner's client decides the click and the aim (ai[2] travels in the spawn packet) and the moment it returns
    /// (ai[0] = Return with netUpdate); every machine, the server included, runs the same ellipse from the aim and the owner's
    /// position, stops at blocks itself and catches its own copy, so no kill packet is needed. The rope, trail, sparks and sounds are
    /// visual: every machine builds its own, nothing is synced.
    /// </summary>
    public class CombatWrenchProjectile : ModProjectile, IBehindHandDrawer
    {
        // Vanilla art until a CombatWrenchProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CombatWrench;

        static Asset<Texture2D> art;

        public enum Phase { Out = 0, Back = 1, Snap = 2, Reel = 3, Retract = 4, HandOver = 5, Return = 6 }

        public Phase CurrentPhase => (Phase)(int)Projectile.ai[0];

        int ticks;                 // ticks this projectile has run on this machine
        int passTicks;             // ticks along the ellipse
        int side = 1;              // the side it was thrown to: 1 right, -1 left
        bool stopped;              // a block stopped it on this machine; it waits for the owner's return
        bool returnStarted;
        float returnSpeed;
        float lastSpeed;           // how far it moved last tick
        Vector2 aim;               // the vector from the player to the far end of the ellipse
        EllipsePath path;          // created per projectile (ModProjectile copies share reference fields)
        VerletRope rope;           // visual only
        PixelTrail trail;          // visual only
        Vector2 hand;              // where the string is held
        float armWorld;
        bool armReady;

        // W2: the latch. The owner writes these when it latches; every other machine reads them from SendExtraAI.
        Vector2 contactOffset;     // where the head is pinned, relative to the enemy's center (synced)
        Vector2 snapCenter;        // the enemy's center when it was latched: a movable enemy is held there (synced)
        bool movable;              // the owner's Snarable.CanGrip decision: true = held in place (the Reel from W3 on), false = only strained against (synced)
        bool latchBegun;           // this machine has started the Snap
        int latchTicks;            // ticks since the latch began
        int latchedType = -1;      // the enemy's type when latched, so a recycled npc slot is never mistaken for it
        Vector2 snapStartCenter;
        float snapStartRotation;
        Phase stage = Phase.Snap;  // the latched stage this machine is in (a pure function of latchTicks)
        Vector2 reelDir;           // unit vector from the player toward the held enemy: the reel runs along it (it follows the cursor, see reelAngle)
        float reelAngle;           // the reel line's angle (world), starts at the latch direction and turns toward aimAngle
        float aimAngle;            // the owner's cursor angle from the player (world); the owner writes it, the others read it from SendExtraAI (synced)
        float sentAimAngle;        // owner only: the aimAngle the other machines last got
        int aimSendDelay;          // owner only: ticks until the next aim update may be sent
        Vector2 latchOwnerCenter;  // the player's center when the reel began (the reel's origin eases from here to the player's live center)
        float reelStartDistance;   // how far from the player the enemy was when latched
        Vector2 heldCenter;        // where the held enemy is (it only moves through open air)
        Vector2 retractStart;      // where the wrench was when the Retract began
        float retractStartRotation; // and how it was turned
        int reelTicks;             // how long the reel takes at ReelSpeedPx (worked out at the latch from the distance)
        int pickupDelay;           // ticks after the Snap until the enemy is PickupProgress of the way along its reel and the wrench is picked up
        bool swingSpawned;         // owner only: the heavy swing has been spawned
        bool havePrev;             // owner only: the swept contact test has last tick's positions
        Vector2 prevCenter;
        Vector2 prevTip;
        Vector2 prevHand;
        Vector2 prevButt;

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Melee/PreHardmode/Boomerangs/CombatWrench/CombatWrenchProjectile", out art);
        }

        public override void Unload()
        {
            art = null;
        }

        // The wrench's sprite (Ismail's file when it exists, the vanilla one until then); the heavy swing draws the same one.
        public static Texture2D WrenchTexture()
        {
            return art?.Value ?? TextureAssets.Projectile[ProjectileID.CombatWrench].Value;
        }

        public override void SetDefaults()
        {
            Projectile.width = CombatWrenchTuning.HitboxSize;
            Projectile.height = CombatWrenchTuning.HitboxSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;   // W1: no damage; it turns on for the hit ticks from W2 on
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // it moves and collides by itself (MoveWithBlocks)
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        // The wrench hurts only on the strike tick of an immovable enemy (the owner turns friendly on for that one tick).
        public override bool? CanDamage()
        {
            return Projectile.friendly;
        }

        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == (int)Projectile.ai[1] - 1 ? (bool?)null : false;   // only the latched enemy
        }

        // CanHitNPC already limits the strike to the latched enemy, and the wrench is friendly for one tick only, so a hit is a hit.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return true;
        }

        // The same damage the heavy swing's hit does (W4 uses HitDamageMultiplier too); the enemy does not move.
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            modifiers.SourceDamage *= CombatWrenchTuning.HitDamageMultiplier;
            modifiers.Knockback *= 0f;
            modifiers.HitDirectionOverride = target.Center.X >= owner.MountedCenter.X ? 1 : -1;
        }

        static float Tilt(Player owner)
        {
            return owner.gravDir >= 0f ? owner.fullRotation : 0f;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // Gone with the player, with the wrench out of their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.CombatWrench)
            {
                Projectile.Kill();
                return;
            }

            bool isOwner = Projectile.owner == Main.myPlayer;
            Projectile.timeLeft = 120;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;   // only the owner's strike tick turns it on

            if (ticks == 0)
                Begin(owner, isOwner);
            ticks++;

            if (Vector2.DistanceSquared(owner.Center, Projectile.Center) > CombatWrenchTuning.MaxDistancePx * CombatWrenchTuning.MaxDistancePx)
            {
                Projectile.Kill();
                return;
            }

            Vector2 before = Projectile.Center;
            if (CurrentPhase == Phase.Return)
                TickReturn(owner);
            else if (CurrentPhase == Phase.Snap || CurrentPhase == Phase.Reel || CurrentPhase == Phase.Retract || CurrentPhase == Phase.HandOver)
                TickLatched(owner, isOwner);
            else
                TickThrow(owner, isOwner);
            if (!Projectile.active)
                return;
            lastSpeed = Vector2.Distance(before, Projectile.Center);

            if (Main.dedServ)
                return;

            CombatWrenchFx.Flight(Projectile.Center, Projectile.Center - before);
            trail.Add(Projectile.Center);
            trail.Tick();

            // The string, from the hand to the butt of the sprite. While Out it is a loose verlet rope; on the way back and while
            // returning it is pulled taut.
            Vector2 butt = Butt();
            if (CurrentPhase == Phase.Out)
                rope.Update(hand, butt, CombatWrenchTuning.RopeSlackFactor, CombatWrenchTuning.RopeExtraSlack, CombatWrenchTuning.RopeGravity * (owner.gravDir >= 0f ? 1 : -1));
            else
                rope.Tighten(hand, butt, CombatWrenchTuning.RopeTightenSpeed);
        }

        // Every machine, the first tick: the ellipse from the aim angle in ai[2].
        void Begin(Player owner, bool isOwner)
        {
            float reach = Math.Max(48f, CombatWrenchTuning.RopeLengthPx - CombatWrenchTuning.ReachMarginPx);
            Vector2 direction = Projectile.ai[2].ToRotationVector2();
            aim = direction * reach;
            side = direction.X >= 0f ? 1 : -1;   // straight up or down counts as right

            path = new EllipsePath();
            path.Prepare(aim, owner.gravDir >= 0f ? 1 : -1, side);
            rope = new VerletRope(new CombatWrenchRopeLook());
            trail = new PixelTrail(new CombatWrenchTrailStyle(), isOwner);

            owner.ChangeDir(side);
            Projectile.Center = owner.MountedCenter;
            Projectile.rotation = 0f;
            Projectile.ai[0] = (float)Phase.Out;
            CombatWrenchFx.Throw(Projectile.Center, aim);
        }

        // Out and Back: the Forward Pass ellipse, followed from the player's current position (so running does not break the loop).
        void TickThrow(Player owner, bool isOwner)
        {
            FaceToward(owner, Projectile.Center.X); // every tick, so nothing (movement keys) can turn the player away from the wrench
            passTicks++;

            if (!stopped)
            {
                float t = MathHelper.Clamp(passTicks / (float)Math.Max(1, CombatWrenchTuning.PassTicks), 0f, 1f);
                float eased = MathHelper.Lerp(t, t * t * (3f - 2f * t), CombatWrenchTuning.PassEase);
                if (CurrentPhase == Phase.Out || CurrentPhase == Phase.Back)
                    Projectile.ai[0] = eased > 0.5f ? (float)Phase.Back : (float)Phase.Out;   // local only, never synced

                Vector2 target = owner.MountedCenter + path.Offset(eased, 1f, CombatWrenchTuning.PassWidthRatio);
                if (MoveWithBlocks(target - Projectile.Center, CombatWrenchTuning.ThroughPlatforms, out Vector2 normal))
                {
                    // A solid block in the way: it does not pass through. Impact sparks, then the owner pulls it back.
                    stopped = true;
                    CombatWrenchFx.Impact(Projectile.Center, normal);
                    if (isOwner)
                        BeginReturn();
                }
                else if (t >= 1f && isOwner)
                    BeginReturn();   // the loop is done: a short pull to the hand and the catch
            }

            // The arm starts at ThrowArmStartDeg and turns to point at the wrench over ThrowArmBlendTicks.
            float toWrench = HeldAim.ToWorld(owner, (Projectile.Center - owner.MountedCenter).ToRotation()) - Tilt(owner);
            float startArm = HeldSwordPose.ToWorld(side, MathHelper.ToRadians(CombatWrenchTuning.ThrowArmStartDeg));
            float blend = MathHelper.Clamp(passTicks / (float)Math.Max(1, CombatWrenchTuning.ThrowArmBlendTicks), 0f, 1f);
            ApplyArm(owner, startArm + MathHelper.WrapAngle(toWrench - startArm) * blend);

            // It spins only on the way out (and not while a block holds it). From the halfway point on it points straight away from the hand.
            if (CurrentPhase == Phase.Out)
            {
                if (!stopped)
                    Projectile.rotation += CombatWrenchTuning.SpinPerTick * side;
            }
            else
                PointAway();

            // Owner only: did the head or the rope touch an enemy?
            if (isOwner && !stopped && CurrentPhase != Phase.Return)
                TestContact();
        }

        // The wrench's head points straight away from the hand (along the taut string), turning there from wherever it was at PointFollow per tick.
        void PointAway()
        {
            Vector2 away = Projectile.Center - hand;
            if (away.LengthSquared() < 1f)
                return;
            float target = away.ToRotation() - HeadAngleRad();
            Projectile.rotation += MathHelper.WrapAngle(target - Projectile.rotation) * MathHelper.Clamp(CombatWrenchTuning.PointFollow, 0.01f, 1f);
        }

        // The head's direction from the sprite's center at rotation 0, in radians. A wrench thrown to the left is drawn flipped, so its head
        // (and the butt, the opposite end) mirror: the angle becomes 180 degrees minus the tuned one.
        float HeadAngleRad()
        {
            float angle = MathHelper.ToRadians(CombatWrenchTuning.HeadAngleDeg);
            return side < 0 ? MathHelper.Pi - angle : angle;
        }

        // Where the string is tied (the end of the sprite away from the head).
        Vector2 Butt()
        {
            return Projectile.Center + (Projectile.rotation + HeadAngleRad() + MathHelper.Pi).ToRotationVector2() * CombatWrenchTuning.RopeAttachPx;
        }

        // The tip of the wrench's head.
        Vector2 HeadTip()
        {
            return Projectile.Center + (Projectile.rotation + HeadAngleRad()).ToRotationVector2() * CombatWrenchTuning.HeadReachPx;
        }

        // Owner only, while Out or Back: the head (swept from the center to the tip, so a spinning or fast head cannot skip over a thin enemy)
        // and the string (swept from the hand to the butt) are tested against every enemy that Snarable.CanBite allows. The enemy nearest the
        // wrench wins; one enemy at a time.
        void TestContact()
        {
            Vector2 tip = HeadTip();
            Vector2 butt = Butt();
            if (!havePrev)
            {
                prevCenter = Projectile.Center;
                prevTip = tip;
                prevHand = hand;
                prevButt = butt;
                havePrev = true;
            }

            NPC best = null;
            bool bestByHead = false;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!Snarable.CanBite(npc) || npc.GetGlobalNPC<CarriedNPC>().Held)
                    continue;
                Rectangle box = npc.Hitbox;
                bool byHead = SegmentHit.Sweep(box, prevCenter, prevTip, Projectile.Center, tip, CombatWrenchTuning.HeadHitWidthPx, 3);
                bool byRope = !byHead && SegmentHit.Sweep(box, prevHand, prevButt, hand, butt, CombatWrenchTuning.RopeHitWidthPx, 3);
                if (!byHead && !byRope)
                    continue;
                float distance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (distance < bestDistance)
                {
                    best = npc;
                    bestByHead = byHead;
                    bestDistance = distance;
                }
            }

            prevCenter = Projectile.Center;
            prevTip = tip;
            prevHand = hand;
            prevButt = butt;

            if (best != null)
                Latch(best, bestByHead, tip, butt);
        }

        // Where the string first enters an enemy's box, searched from the wrench's end toward the hand (the way the wrench would arrive).
        static Vector2 RopeEntry(Vector2 handPos, Vector2 buttPos, Rectangle box)
        {
            float length = Vector2.Distance(handPos, buttPos);
            int steps = Math.Max(1, (int)Math.Ceiling(length / 4f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(buttPos, handPos, i / (float)steps);
                if (box.Contains(p.ToPoint()))
                    return p;
            }
            return Vector2.Clamp(SegmentHit.ClosestPoint(handPos, buttPos, box.Center.ToVector2()), box.TopLeft(), box.BottomRight());
        }

        // Owner only: decides the contact point, which way the head faces, and movable or immovable; the phase change and the netUpdate
        // carry it to every other machine (SendExtraAI has the rest).
        void Latch(NPC npc, bool byHead, Vector2 tip, Vector2 butt)
        {
            Rectangle box = npc.Hitbox;
            Vector2 contact = byHead ? Vector2.Clamp(tip, box.TopLeft(), box.BottomRight()) : RopeEntry(hand, butt, box);
            contactOffset = contact - npc.Center;
            snapCenter = npc.Center;
            Vector2 fromPlayer = npc.Center - Main.player[Projectile.owner].MountedCenter;
            aimAngle = fromPlayer.LengthSquared() > 16f ? fromPlayer.ToRotation() : (side >= 0 ? 0f : MathHelper.Pi);
            sentAimAngle = aimAngle;
            movable = Snarable.CanGrip(npc, CombatWrenchTuning.MaxHoldSize);
            Projectile.ai[0] = (float)Phase.Snap;
            Projectile.ai[1] = npc.whoAmI + 1;
            Projectile.netUpdate = true;
        }

        // The latched enemy, or null when it is gone (dead, despawned, or the slot now holds another type).
        NPC LatchedNpc()
        {
            int index = (int)Projectile.ai[1] - 1;
            if (index < 0 || index >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[index];
            return npc.active && npc.type == latchedType ? npc : null;
        }

        // Which way the head points while latched: straight away from the player, toward the contact point.
        Vector2 AwayFromPlayer(Player owner, Vector2 contact)
        {
            Vector2 away = contact - owner.MountedCenter;
            return away.LengthSquared() > 1f ? Vector2.Normalize(away) : new Vector2(side, 0f);
        }

        // The player always faces the side of the screen the given x is on (inside a small dead zone the player keeps the current facing).
        void FaceToward(Player owner, float x)
        {
            float dx = x - owner.MountedCenter.X;
            if (Math.Abs(dx) > CombatWrenchTuning.FaceDeadzonePx)
                owner.ChangeDir(dx > 0f ? 1 : -1);
        }

        // Every machine, the first latched tick.
        void BeginLatch(Player owner)
        {
            latchBegun = true;
            latchTicks = 0;
            snapStartCenter = Projectile.Center;
            snapStartRotation = Projectile.rotation;
            stage = Phase.Snap;
            int index = (int)Projectile.ai[1] - 1;
            latchedType = index >= 0 && index < Main.maxNPCs && Main.npc[index].active ? Main.npc[index].type : -1;
            heldCenter = snapCenter;
            Vector2 toEnemy = snapCenter - owner.MountedCenter;
            reelDir = toEnemy.LengthSquared() > 16f ? Vector2.Normalize(toEnemy) : new Vector2(side, 0f);   // the reel starts along this line, then follows the cursor
            reelAngle = reelDir.ToRotation();
            latchOwnerCenter = owner.MountedCenter;
            reelStartDistance = toEnemy.Length();
            float reelDistance = Math.Max(0f, Vector2.Distance(snapCenter, owner.MountedCenter) - CombatWrenchTuning.SwingDistancePx);
            reelTicks = Math.Max(CombatWrenchTuning.MinReelTicks, (int)Math.Ceiling(reelDistance / Math.Max(0.1f, CombatWrenchTuning.ReelSpeedPx)));   // a constant speed: the longer the way, the longer it takes
            float startDistance = Vector2.Distance(snapCenter, owner.MountedCenter);
            float travel = startDistance - CombatWrenchTuning.SwingDistancePx;
            pickupDelay = travel <= 0.01f ? 0 : Math.Min(reelTicks, (int)Math.Ceiling(reelTicks * MathHelper.Clamp(CombatWrenchTuning.PickupProgress, 0f, 1f)));   // the wrench is picked up when the enemy is this far along its reel
            NPC npc = LatchedNpc();
            Vector2 contact = (npc != null ? npc.Center : snapCenter) + contactOffset;
            CombatWrenchFx.Snap(contact, AwayFromPlayer(owner, contact));
        }

        // The held enemy's place this tick: it stays where it was latched during the Snap, then slides to the stop point in front of the player
        // at a constant speed (a function of the ticks only, whatever the wrench is doing), and then stays at the stop point. Always through
        // open air only.
        void HoldEnemy(Player owner, NPC npc)
        {
            // The enemy rides the reel line (it turns toward the cursor): its distance from the player shrinks at a constant speed down to
            // the stop point, and the line's origin eases from where the player was at the latch to where the player is now (so the Snap
            // does not drag it along with a running player).
            float u = MathHelper.Clamp((latchTicks - CombatWrenchTuning.SnapTicks) / (float)Math.Max(1, reelTicks), 0f, 1f);
            Vector2 origin = Vector2.Lerp(latchOwnerCenter, owner.MountedCenter, u);
            float distance = MathHelper.Lerp(reelStartDistance, CombatWrenchTuning.SwingDistancePx, u);
            Vector2 want = origin + reelDir * distance;
            heldCenter = CarriedNPC.ClampToOpenAir(heldCenter, want, npc);
            CarriedNPC.Hold(npc, heldCenter);
        }

        // Every machine, every tick of the reel: the reel line turns toward the owner's cursor (the owner reads the mouse and tells the others
        // through SendExtraAI), at most ReelAimTurnRad per tick. It stops following the moment the swing is spawned: that is the commit.
        void FollowAim(Player owner, bool isOwner)
        {
            if (swingSpawned || latchTicks <= CombatWrenchTuning.SnapTicks)
                return;
            if (isOwner)
            {
                Vector2 toMouse = Main.MouseWorld - owner.MountedCenter;
                float minimum = CombatWrenchTuning.ReelAimMinDistancePx;
                if (toMouse.LengthSquared() > minimum * minimum)
                    aimAngle = toMouse.ToRotation();
                aimSendDelay--;
                if (aimSendDelay <= 0 && Math.Abs(MathHelper.WrapAngle(aimAngle - sentAimAngle)) > CombatWrenchTuning.ReelAimSyncDelta)
                {
                    sentAimAngle = aimAngle;
                    aimSendDelay = 2;
                    Projectile.netUpdate = true;
                }
            }
            float turn = CombatWrenchTuning.ReelAimTurnRad;
            reelAngle += MathHelper.Clamp(MathHelper.WrapAngle(aimAngle - reelAngle), -turn, turn);
            reelDir = reelAngle.ToRotationVector2();
        }

        // The latched stages, a pure function of the ticks since the latch on every machine: Snap (the head flies to the contact point and
        // turns away from the player), Reel (a movable enemy is held and slid to the stop point at a constant speed; the wrench stays pinned
        // to it), Retract (the moment the enemy is PickupProgress of the way along its reel the wrench flies to the hand, while the enemy keeps sliding in), HandOver (the wrench waits in the hand
        // while the heavy swing projectile takes over: the wrench ends without letting the enemy go). An immovable enemy is not moved: the
        // wrench drags back a little on a cubic ease (the head still pinned to it), and the moment the drag ends it reels straight
        // back to the hand; that snap back is the hit (the same damage as the heavy swing), with no push forward first.
        void TickLatched(Player owner, bool isOwner)
        {
            if (!latchBegun)
                BeginLatch(owner);
            latchTicks++;

            int snapEnd = CombatWrenchTuning.SnapTicks;
            int dragEnd = snapEnd + CombatWrenchTuning.StrainDragTicks;
            int strikeTick = dragEnd;   // the hit lands the moment the drag ends, and the wrench reels straight back
            int reelEnd = snapEnd + reelTicks;                   // the enemy has arrived at the stop point
            int pickupTick = snapEnd + pickupDelay;              // the enemy is close enough: the wrench is picked up
            int retractEnd = pickupTick + CombatWrenchTuning.RetractTicks;
            int handOverEnd = Math.Max(reelEnd, retractEnd) + CombatWrenchTuning.HandOverHoldTicks;

            Phase now;
            if (latchTicks < snapEnd)
                now = Phase.Snap;
            else if (!movable || latchTicks < pickupTick)
                now = Phase.Reel;
            else if (latchTicks < retractEnd)
                now = Phase.Retract;
            else
                now = Phase.HandOver;

            NPC npc = LatchedNpc();
            bool returning = false;   // the owner has sent the wrench back this tick
            if (isOwner && (npc == null || (!movable && latchTicks >= strikeTick)))
            {
                BeginReturn();        // the enemy died or left, or the strike is done: the wrench goes back to the hand
                returning = true;
            }

            if (latchTicks == snapEnd)
                CombatWrenchFx.Reel(Projectile.Center);   // the drag or the reel begins

            // The stage changes FIRST, so what a stage starts from (the Retract's start point) is set before the stage uses it.
            if (!returning)
            {
                if (now != stage)
                {
                    stage = now;
                    if (now == Phase.Retract)
                    {
                        retractStart = Projectile.Center;
                        retractStartRotation = Projectile.rotation;
                        CombatWrenchFx.Retract(Projectile.Center);
                    }
                }
                Projectile.ai[0] = (float)now;   // local, like Out and Back; the owner's Return is the next synced change
            }

            if (movable && !returning && npc != null && SwingTakenOver())
            {
                // The heavy swing holds the enemy from here on (it must not be pulled around by two holders in one tick); the wrench ends quietly.
                Projectile.Kill();
                return;
            }

            if (npc != null)
            {
                if (movable)
                {
                    FollowAim(owner, isOwner);
                    HoldEnemy(owner, npc);
                }

                if (now == Phase.Snap || now == Phase.Reel)
                {
                    // Pinned: the head at the contact point, pointing away from the player. The Snap flies and turns there. An immovable
                    // enemy: after the Snap the wrench drags toward the player a little (cubic ease), then snaps back to the pin.
                    Vector2 contact = npc.Center + contactOffset;
                    Vector2 headDirection = AwayFromPlayer(owner, contact);
                    float pull = 0f;
                    if (!movable && latchTicks > snapEnd)
                    {
                        float u = MathHelper.Clamp((latchTicks - snapEnd) / (float)Math.Max(1, CombatWrenchTuning.StrainDragTicks), 0f, 1f);
                        float v = -2f * u + 2f;
                        pull = u < 0.5f ? 4f * u * u * u : 1f - v * v * v * 0.5f;   // a cubic ease; it holds at the full drag until the owner sends the wrench back
                    }
                    float dragPx = CombatWrenchTuning.StrainDragFraction * (CombatWrenchTuning.HeadReachPx + CombatWrenchTuning.RopeAttachPx);   // a fraction of the wrench's length
                    Vector2 pinned = contact - headDirection * CombatWrenchTuning.HeadReachPx - headDirection * (dragPx * pull);

                    float t = MathHelper.Clamp(latchTicks / (float)Math.Max(1, snapEnd), 0f, 1f);
                    float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                    Projectile.Center = Vector2.Lerp(snapStartCenter, pinned, eased);
                    float targetRotation = headDirection.ToRotation() - HeadAngleRad();
                    Projectile.rotation = snapStartRotation + MathHelper.WrapAngle(targetRotation - snapStartRotation) * eased;

                    // The strike: the wrench is back at the pin. Everyone plays the effects; only the owner deals the damage.
                    if (!movable && latchTicks == strikeTick)
                    {
                        CombatWrenchFx.Strike(contact, headDirection);
                        if (isOwner)
                            Projectile.friendly = true;
                    }
                }
                else
                {
                    // Picked up (Retract) and waiting for the swing (HandOver): the arm goes to the swing's starting angle FIRST, then the wrench
                    // lerps straight into the swing's starting pose (in the grip, turned by SwingStartTiltDeg). It never sinks into the player,
                    // and the swing begins exactly where it lands, so nothing pops.
                    side = HeldAim.Facing(reelAngle);
                    owner.ChangeDir(side);
                    ApplyArm(owner, StartArm(owner));
                    SwingStartPose(owner, out Vector2 poseCenter, out float poseRotation);
                    if (now == Phase.Retract)
                    {
                        float u = MathHelper.Clamp((latchTicks - pickupTick) / (float)Math.Max(1, CombatWrenchTuning.RetractTicks), 0f, 1f);
                        float eased = 1f - (1f - u) * (1f - u) * (1f - u);   // fast at first: it is "immediately" most of the way there
                        Projectile.Center = Vector2.Lerp(retractStart, poseCenter, eased);
                        Projectile.rotation = retractStartRotation + MathHelper.WrapAngle(poseRotation - retractStartRotation) * eased;
                    }
                    else
                    {
                        Projectile.Center = poseCenter;
                        Projectile.rotation = poseRotation;
                    }
                }
            }

            // The player faces the wrench's side and the arm points at the wrench. (Once the wrench is picked up, the block above has already
            // posed the arm for the swing's start.)
            bool carried = npc != null && (now == Phase.Retract || now == Phase.HandOver);
            if (!carried)
            {
                Vector2 armAt = Projectile.Center;
                FaceToward(owner, armAt.X);
                ApplyArm(owner, HeldAim.ToWorld(owner, (armAt - owner.MountedCenter).ToRotation()) - Tilt(owner));
            }

            if (movable && !returning && npc != null)
            {
                // W4: once the enemy has arrived and the wrench is in the hand, the owner starts the heavy swing (a separate projectile that holds
                // the enemy from its very first tick). The wrench keeps holding the enemy until that swing has run on this machine, then it ends
                // quietly (no catch, no release). If the swing never shows up (a lost packet) the old end is the fallback: let go and catch.
                if (isOwner && !swingSpawned && latchTicks >= retractEnd)
                    SpawnSwing(owner, npc);   // the wrench is caught: the swing's direction is fixed now; the enemy may still be on its way (the swing keeps it coming)
                if (latchTicks >= handOverEnd)
                {
                    CarriedNPC.Release(npc);
                    CombatWrenchFx.Catch(Projectile.Center);
                    Projectile.Kill();
                }
            }
        }

        // The arm's target while the wrench is picked up and handed over: along the reel line (which follows the cursor), turned by SwingStartArmDeg.
        float StartArm(Player owner)
        {
            int facing = HeldAim.Facing(reelAngle);
            return HeldAim.ToWorld(owner, reelAngle) - Tilt(owner) + facing * MathHelper.ToRadians(CombatWrenchTuning.SwingStartArmDeg);
        }

        // Where the heavy swing starts its wrench (the same numbers as the swing's pose at tick 0): in the grip, the head along the arm turned by
        // SwingStartTiltDeg. The arm must be posed first (armWorld and hand).
        void SwingStartPose(Player owner, out Vector2 center, out float rotation)
        {
            int facing = HeldAim.Facing(reelAngle);
            Vector2 blade = HeldSwordPose.BladeDirectionWorld(owner, armWorld + facing * MathHelper.ToRadians(CombatWrenchTuning.SwingStartTiltDeg)).RotatedBy(Tilt(owner));
            center = hand + blade * CombatWrenchTuning.GripOffsetPx;
            rotation = blade.ToRotation() - HeadAngleRad();
        }

        // Owner only: starts the heavy swing from the wrench's pose in the hand. ai[0] = the enemy's index plus 1, ai[1] = the world angle of the
        // reel line (player to the stop point), ai[2] = the arm the swing starts from, relative to the swing's own aim (so there is no pop).
        void SpawnSwing(Player owner, NPC npc)
        {
            swingSpawned = true;
            float angle = reelAngle;   // the commit: from here on the swing's direction never changes
            float aimArm = HeldAim.ToWorld(owner, angle) - Tilt(owner);
            float startLocal = HeldAim.Facing(angle) * MathHelper.WrapAngle(armWorld - aimArm);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), hand, Vector2.Zero, ModContent.ProjectileType<CombatWrenchSwingProjectile>(),
                Projectile.damage, Projectile.knockBack, owner.whoAmI, npc.whoAmI + 1, angle, startLocal);
        }

        // Every machine: the heavy swing for the enemy this wrench holds exists and has run a tick here, so it holds the enemy now.
        bool SwingTakenOver()
        {
            int swingType = ModContent.ProjectileType<CombatWrenchSwingProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.type == swingType && other.owner == Projectile.owner && (int)other.ai[0] == (int)Projectile.ai[1]
                    && ((CombatWrenchSwingProjectile)other.ModProjectile).Started)
                    return true;
            }
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(contactOffset.X);
            writer.Write(contactOffset.Y);
            writer.Write(snapCenter.X);
            writer.Write(snapCenter.Y);
            writer.Write(movable);
            writer.Write(aimAngle);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            contactOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            snapCenter = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            movable = reader.ReadBoolean();
            aimAngle = reader.ReadSingle();
        }

        // Owner only: from now on every machine pulls the wrench to the hand (ai[0] travels with the netUpdate).
        void BeginReturn()
        {
            Projectile.ai[0] = (float)Phase.Return;
            Projectile.netUpdate = true;
        }

        // Return: pulled to the hand, faster every tick, and caught (every machine catches its own copy).
        void TickReturn(Player owner)
        {
            if (!returnStarted)
            {
                returnStarted = true;
                returnSpeed = Math.Max(CombatWrenchTuning.ReturnStartSpeed, lastSpeed * 0.4f);
                CombatWrenchFx.Retract(Projectile.Center);
            }

            FaceToward(owner, Projectile.Center.X);

            // The arm points at the wrench, then eases to CatchArmDeg as the wrench gets close.
            float toWrench = HeldAim.ToWorld(owner, (Projectile.Center - owner.MountedCenter).ToRotation()) - Tilt(owner);
            float catchArm = HeldSwordPose.ToWorld(owner.direction >= 0 ? 1 : -1, MathHelper.ToRadians(CombatWrenchTuning.CatchArmDeg));
            float near = 1f - MathHelper.Clamp(Vector2.Distance(owner.MountedCenter, Projectile.Center) / Math.Max(1f, CombatWrenchTuning.CatchArmBlendDistance), 0f, 1f);
            ApplyArm(owner, toWrench + MathHelper.WrapAngle(catchArm - toWrench) * near);

            Vector2 toHand = hand - Projectile.Center;
            float distance = toHand.Length();
            returnSpeed = Math.Min(returnSpeed + CombatWrenchTuning.ReturnAccel, CombatWrenchTuning.ReturnMaxSpeed);
            if (distance > 0.01f)
                Projectile.Center += toHand / distance * Math.Min(distance, returnSpeed);
            PointAway();

            if (distance <= CombatWrenchTuning.CatchDistancePx)
            {
                CombatWrenchFx.Catch(Projectile.Center);
                Projectile.Kill();
            }
        }

        // A plain move by 'delta' with block collision, in pieces of at most CollisionStepPx (a fast pass cannot clip a corner). Returns
        // true when a block cut it short; 'normal' points away from that block.
        bool MoveWithBlocks(Vector2 delta, bool throughPlatforms, out Vector2 normal)
        {
            int g = Main.player[Projectile.owner].gravDir >= 0f ? 1 : -1;
            float step = Math.Max(1f, CombatWrenchTuning.CollisionStepPx);
            int pieces = Math.Max(1, Math.Min(16, (int)Math.Ceiling(delta.Length() / step)));
            Vector2 piece = delta / pieces;
            bool stoppedX = false;
            bool stoppedY = false;
            for (int i = 0; i < pieces; i++)
            {
                Vector2 wantPiece = new Vector2(stoppedX ? 0f : piece.X, stoppedY ? 0f : piece.Y);
                if (wantPiece == Vector2.Zero)
                    break;
                Vector2 movedPiece = Collision.TileCollision(Projectile.position, wantPiece, Projectile.width, Projectile.height, throughPlatforms, throughPlatforms, g);
                if (Math.Abs(movedPiece.X - wantPiece.X) > 0.01f)
                    stoppedX = true;
                if (Math.Abs(movedPiece.Y - wantPiece.Y) > 0.01f)
                    stoppedY = true;
                Projectile.position += movedPiece;
            }
            normal = new Vector2(stoppedX ? -Math.Sign(delta.X) : 0f, stoppedY ? -Math.Sign(delta.Y) : 0f);
            if (normal.LengthSquared() > 0f)
                normal = Vector2.Normalize(normal);
            return stoppedX || stoppedY;
        }

        // Moves the owner's arm toward the given pose (an angle in the player's own frame, down = +90 degrees) and finds the hand.
        void ApplyArm(Player owner, float target)
        {
            if (!armReady)
            {
                armWorld = target;
                armReady = true;
            }
            else
                armWorld += MathHelper.WrapAngle(target - armWorld) * MathHelper.Clamp(CombatWrenchTuning.ArmFollow, 0.01f, 1f);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armWorld - MathHelper.PiOver2);
            hand = HeldSwordPose.HandWorld(owner, armWorld);
            float tilt = Tilt(owner);
            if (tilt != 0f)
            {
                // HandWorld does not know the tilt (a minecart, a mount): turn the hand with the player, about the player's own rotation point.
                Vector2 pivot = owner.position + owner.fullRotationOrigin + new Vector2(0f, owner.gfxOffY);
                hand = pivot + (hand - pivot).RotatedBy(tilt);
            }
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ticks == 0 || rope == null)
                return false; // not posed yet (the first tick has not run)

            rope.Draw(TextureAssets.MagicPixel.Value);
            trail?.Draw();

            if (!BehindHand)
            {
                Texture2D texture = WrenchTexture();
                Color light = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, light, Projectile.rotation, texture.Size() * 0.5f, CombatWrenchTuning.SpriteScale, side < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
            }
            return false;
        }

        // Once the wrench is picked up (Retract) and while it waits for the swing (HandOver) it is drawn inside the player's layers, behind the
        // hand, like the swing's wrench; before that it is drawn here, over everything.
        bool BehindHand => CurrentPhase == Phase.Retract || CurrentPhase == Phase.HandOver;

        // Called by BehindHandLayer on every machine.
        public void AddBehindHandDrawData(ref PlayerDrawSet drawInfo)
        {
            if (ticks == 0 || rope == null || !BehindHand)
                return;
            Texture2D texture = WrenchTexture();
            Color light = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
            drawInfo.DrawDataCache.Add(new DrawData(texture, Projectile.Center - Main.screenPosition, null, light, Projectile.rotation, texture.Size() * 0.5f, CombatWrenchTuning.SpriteScale, side < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0));
        }
    }
}