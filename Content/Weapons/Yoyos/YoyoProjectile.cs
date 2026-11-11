using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    public enum YoyoState
    {
        Idle = 0,   // in the hand: the idle animation, waiting for the button
        Out = 1,    // the trick is running (it hurts)
        Return = 2  // pulled back to the hand (harmless)
    }

    // What the yoyo touched during one Step.
    public struct YoyoContact
    {
        public bool Grounded;   // blocked while moving "down" (toward the player's feet side; up in the world when gravity is flipped)
        public bool BlockedX;   // blocked sideways
        public bool Ceiling;    // blocked while moving "up" (away from the feet side)
        public float Impact;    // the speed it had when it landed (Move: how far it wanted to go)
        public bool Blocked;    // Move only: a block cut the movement short
        public Vector2 Normal;  // Move only: the direction away from the block it hit (zero when not blocked)
    }

    /// <summary>
    /// The held yoyo (patch 1; since patch 5 the shared base of every yoyo's projectile): ONE projectile that lives for as long as a reworked yoyo is held (YoyoPlayer spawns it). It is the yoyo
    /// itself: Projectile.Center is the yoyo, the YoyoRope runs from the player's hand to it (a straight line while idle, a verlet rope while out or returning), and it poses the arm.
    /// IDLE: nothing pressed: the arm hangs down, the yoyo is let down gently, spins a little and is pulled back up, over and over.
    /// OUT: the left button runs the yoyo's trick (YoyoTrick; the Wooden Yoyo's is Walk the Dog). The throw side is the side of the screen
    /// the cursor is on (a trick may keep following the cursor's side while it is out; Walk the Dog does). It retracts after the trick's hits, when the string is at full length, or when the button
    /// is let go (not in the first ReleaseGraceTicks, so a tap still throws). RETURN: it is pulled to the hand and caught; if the button
    /// is still down it is thrown again after a short beat.
    /// MULTIPLAYER: the owner's client decides everything (the click, the side, hits, when to retract and catch). Every machine runs the
    /// same movement from the synced state, so SendExtraAI carries the state, the side, the velocity and two serials (throws and hits);
    /// the position travels with the normal projectile sync (netUpdate on every change and every SyncEveryTicks while out). The idle
    /// animation, the arm and the rope are never synced: every machine plays and simulates its own. Hits and damage are the owner's alone;
    /// the others hear about a hit through the hit serial and play its effects at the yoyo. No custom packets.
    /// Projectile.velocity stays zero: the yoyo moves itself (Vel) so tile collision can be done by hand.
    /// A yoyo is a subclass in its own folder (Content/Weapons/Yoyos/&lt;Yoyo&gt;/): it names its vanilla item, its tuning, its fx and its
    /// trick (a pattern from this folder, with custom behavior on top if wanted), and overrides Texture with the vanilla sprite.
    /// </summary>
    public abstract class YoyoProjectile : ModProjectile
    {
        // ---- what each yoyo supplies ----
        public abstract int YoyoItemType { get; }                         // the vanilla item this yoyo reworks
        protected abstract YoyoTuning CreateTuning();                     // its numbers
        protected abstract YoyoFx CreateFx();                             // its dust and sounds
        protected abstract YoyoTrick CreateTrick(YoyoTuning tuning);      // its trick (one per throw machine, made when the yoyo is held)
        protected virtual YoyoTrick CreateRightTrick(YoyoTuning tuning) => null;   // its right click trick (null = the right button does nothing)

        public YoyoTuning Tuning { get; private set; }
        public YoyoFx Fx { get; private set; }

        public Player Owner => Main.player[Projectile.owner];
        public bool IsOwner => Projectile.owner == Main.myPlayer;

        // 1 = normal gravity, -1 = flipped (the owner's gravDir, which every machine knows): "down" for the yoyo is toward the player's feet.
        public int GravDir => Owner.gravDir >= 0f ? 1 : -1;

        // How far the player is tilted (a minecart on a slope, some mounts): the hand, the arm and the idle yoyo turn with it. Not with flipped gravity.
        public float Tilt => GravDir > 0 ? Owner.fullRotation : 0f;

        // ---- shared with the trick ----
        public Vector2 Vel;            // the yoyo's own velocity (px per tick); synced
        public Vector2 Aim;            // a trick's throw target, relative to the player (Forward Pass); set by the owner at the throw; synced
        public int Side = 1;           // the side it was thrown to: 1 right, -1 left; synced
        public bool UsingRight;        // the throw was started with the right button (the right click trick); synced
        public bool Charged;           // a trick charged the yoyo (the Sleeper hold at max speed); what it does is up to the yoyo; synced
        public int OutTicks;           // ticks since the throw
        public int Hits;               // enemy hits since the throw (owner)
        public YoyoState State { get; private set; }
        public float SpinAngle => spin;   // how far the sprite is turned (a yoyo that draws on top of its sprite follows it)
        public Vector2 Hand => hand;      // where the string is held (a trick that shapes the string reads it)

        // The hand as it is NOW: 'hand' was found when the arm was last posed (before the player moved this tick), so a circle round it lags
        // a running player by a tick; this adds how far the player has moved since. A trick that circles the hand uses this.
        public Vector2 HandFollowed => hand + (Owner.MountedCenter - handAnchor);

        // ---- synced (SendExtraAI / ReceiveExtraAI) ----
        int throwSerial;
        int hitSerial;
        bool syncedOnce;
        int pendingState = -1;         // a state the owner moved to that this machine has not entered yet
        bool pendingHitFx;

        // ---- per machine, never synced ----
        int ticks;
        YoyoTrick trick;               // the trick of the throw (the left or the right one)
        YoyoTrick leftTrick;
        YoyoTrick rightTrick;          // null when this yoyo has no right click trick
        bool rightBlocked;             // the right button went down on the interface or a tile interaction: it does not count until it is let go
        YoyoTrick ActiveTrick => UsingRight && rightTrick != null ? rightTrick : leftTrick;
        YoyoRope rope;
        readonly List<Vector2> stringPath = new List<Vector2>();   // the string a trick draws by hand (YoyoTrick.StringPath)
        bool stringPathActive;
        YoyoRopeWrap wrap;             // where the string bends around blocks (visual only)
        Vector2 prevCenter;            // the yoyo's center at the start of this tick (the swept hitbox runs from here)
        int idleClock;
        int catchCooldown;
        float returnSpeed;
        float spin;
        float spinRate;
        float armWorld;                // the arm's angle in the player's own frame (down = +90 degrees)
        bool armReady;
        Vector2 hand;                  // where the string is held
        Vector2 handAnchor;            // where the player was when 'hand' was found (HandFollowed)
        YoyoAfterimages afterimages;   // visual only; the yoyo's tuning switches it on
        Asset<Texture2D> custom;
        bool customChecked;

        public override void SetStaticDefaults()
        {
            YoyoRegistry.Register(YoyoItemType, Type);   // lets YoyoPlayer find this projectile for the held yoyo
        }

        public override void SetDefaults()
        {
            Tuning = CreateTuning();
            Fx = CreateFx();
            Projectile.width = Tuning.HitboxSize;
            Projectile.height = Tuning.HitboxSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // the trick collides by hand (Step)
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000;    // refilled every tick: the yoyo lives as long as it is held
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Tuning.HitCooldownTicks;
        }

        // ---- net ----

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)State);
            writer.Write(Side >= 0);
            writer.Write((byte)(throwSerial & 255));
            writer.Write((byte)(hitSerial & 255));
            writer.Write(Vel.X);
            writer.Write(Vel.Y);
            writer.Write(Aim.X);
            writer.Write(Aim.Y);
            writer.Write(UsingRight);
            writer.Write(Charged);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int state = reader.ReadByte();
            Side = reader.ReadBoolean() ? 1 : -1;
            int throws = reader.ReadByte();
            int hits = reader.ReadByte();
            Vel = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            Aim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            UsingRight = reader.ReadBoolean();
            Charged = reader.ReadBoolean();
            if (!syncedOnce)
            {
                // A late joiner: take the state as it is, no effects, no replay.
                syncedOnce = true;
                State = (YoyoState)state;
                throwSerial = throws;
                hitSerial = hits;
                return;
            }
            throwSerial = throws;
            if (state != (int)State)
                pendingState = state;
            if (hits != hitSerial)
            {
                hitSerial = hits;
                pendingHitFx = true;
            }
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the yoyo out of their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != YoyoItemType)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 10000;
            Projectile.localNPCHitCooldown = Tuning.HitCooldownTicks;
            float ropeMax = Tuning.RopeLength();

            if (ticks == 0)
            {
                leftTrick = CreateTrick(Tuning);
                rightTrick = CreateRightTrick(Tuning);
                trick = leftTrick;
                rope = new YoyoRope(Tuning);
                wrap = new YoyoRopeWrap();
                afterimages = new YoyoAfterimages(Tuning);
                Projectile.Center = owner.MountedCenter;
                prevCenter = Projectile.Center;
            }
            ticks++;
            trick = ActiveTrick;   // the trick of the button that started the throw (the other machines learn it from the sync)

            // Something the owner decided that has reached this machine.
            if (pendingState >= 0)
            {
                YoyoState next = (YoyoState)pendingState;
                pendingState = -1;
                if (next == YoyoState.Out)
                {
                    LaunchFromCenter(owner);
                    trick.Begin(this, false);
                }
                EnterState(next);
            }
            if (pendingHitFx)
            {
                pendingHitFx = false;
                Fx.Hit(Projectile.Center, 1 + hitSerial % Math.Max(1, Math.Min(trick.MaxHits, 6)));
            }

            prevCenter = Projectile.Center;

            switch (State)
            {
                case YoyoState.Idle:
                    TickIdle(owner);
                    break;
                case YoyoState.Out:
                    TickOut(owner, ropeMax);
                    break;
                default:
                    TickReturn(owner);
                    break;
            }

            if (State != YoyoState.Idle && IsOwner && ticks % Math.Max(1, Tuning.SyncEveryTicks) == 0)
                Projectile.netUpdate = true;

            // The wrap: while out, the string bends around block corners (corner points fixed to the tiles). None while idle (a straight line)
            // or returning (the yoyo is pulled straight through blocks, a wrap there would only flicker).
            if (State == YoyoState.Out && Tuning.RopeWraps && trick.WrapsString)
                wrap.Update(hand, Projectile.Center, Tuning.RopeWrapMaxPivots, Tuning.RopeWrapClearancePx);
            else
                wrap.Clear();

            stringPathActive = State == YoyoState.Out && trick.StringPath(this, stringPath);
            if (State == YoyoState.Idle || stringPathActive)
                rope.Straight(hand, Projectile.Center);   // (a hand-drawn string: the next simulated rope starts from a straight line)
            else if (wrap.Active)
                rope.TightenAlong(hand, wrap.Pivots, Projectile.Center, Tuning.RopeWrapSnapSpeed);   // wrapped: the string follows its path around the corners
            else if (State == YoyoState.Return || (State == YoyoState.Out && trick.RopeTaut(this)))
                rope.Tighten(hand, Projectile.Center, Tuning.RopeTightenSpeed);   // pulled taut on the way back: the string straightens over a few ticks
            else
                rope.Update(hand, Projectile.Center, Tuning.RopeSlackFactor, Tuning.RopeExtraSlack, Tuning.RopeGravity * GravDir);
        }

        // ---- states ----

        void EnterState(YoyoState next)
        {
            State = next;
            switch (next)
            {
                case YoyoState.Out:
                    OutTicks = 0;
                    Hits = 0;
                    returnSpeed = 0f;
                    Fx.Throw(Projectile.Center, Side);
                    break;
                case YoyoState.Return:
                    returnSpeed = Math.Max(Tuning.ReturnStartSpeed, Vel.Length() * 0.4f);
                    Fx.Retract(Projectile.Center);
                    break;
                default:
                    idleClock = 0;
                    catchCooldown = Tuning.RethrowDelayTicks;
                    Vel = Vector2.Zero;
                    Fx.Catch(Projectile.Center);
                    break;
            }
        }

        // The owner's client: the button asks for a throw.
        void BeginTrick(Player owner, bool right)
        {
            UsingRight = right && rightTrick != null;
            trick = ActiveTrick;
            Side = Main.MouseWorld.X >= owner.Center.X ? 1 : -1;
            throwSerial++;
            LaunchFromCenter(owner);
            trick.Begin(this, true);
            EnterState(YoyoState.Out);
            Projectile.netUpdate = true;
        }

        public void BeginReturn()
        {
            if (State != YoyoState.Out)
                return;
            trick?.OnRetract(this);
            EnterState(YoyoState.Return);
            Projectile.netUpdate = true;
        }

        // The idle animation: rest in the hand, let down gently, spin, pulled back up. The arm follows the yoyo's extension.
        void TickIdle(Player owner)
        {
            int facing = owner.direction >= 0 ? 1 : -1;
            idleClock++;
            float extension = IdleExtension(idleClock);

            float armLocal = MathHelper.Lerp(MathHelper.ToRadians(Tuning.IdleArmRestDeg), MathHelper.ToRadians(Tuning.IdleArmDropDeg), extension);
            ApplyArm(owner, HeldSwordPose.ToWorld(facing, armLocal));

            float ropeMax = Tuning.RopeLength();
            float drop = Math.Min(Tuning.IdleDropPx, ropeMax) * extension * GravDir; // toward the feet (up when gravity is flipped); no block check, it may go through blocks
            float sway = (float)Math.Sin(idleClock * 0.12f) * Tuning.IdleSwayPx * extension;
            Projectile.Center = hand + new Vector2(sway, drop).RotatedBy(Tilt);   // "down" is down for the (tilted) player

            spinRate = MathHelper.Lerp(Tuning.IdleSpinRestSpeed, Tuning.IdleSpinSpeed, extension * extension);
            spin += spinRate * facing;

            if (IsOwner)
            {
                bool rightPressed = RightPressed(owner);
                if (catchCooldown > 0)
                    catchCooldown--;
                else if (owner.controlUseItem && !owner.mouseInterface)
                    BeginTrick(owner, false);
                else if (rightPressed)
                    BeginTrick(owner, true);
            }
        }

        // A throw starts at the center of the player (the idle yoyo may be inside blocks, so it never starts from where it hung). Runs on
        // every machine; the owner's position then travels with the normal sync.
        void LaunchFromCenter(Player owner)
        {
            Projectile.Center = owner.MountedCenter;
            prevCenter = Projectile.Center;   // a teleport, not a flight: nothing is swept
        }

        // The owner's client: is the right button asking for a throw? Only when this yoyo has a right click trick. A press that the
        // interface or a tile interaction (a chest, a door) took does not count until the button has been let go.
        bool RightPressed(Player owner)
        {
            if (rightTrick == null || !owner.controlUseTile)
            {
                rightBlocked = false;
                return false;
            }
            if (owner.mouseInterface || owner.tileInteractionHappened)
                rightBlocked = true;
            return !rightBlocked;
        }

        // The owner's client: is the button that started this throw still down?
        bool ButtonHeld(Player owner)
        {
            return UsingRight ? owner.controlUseTile : owner.controlUseItem;
        }

        // 0 = in the hand, 1 = let all the way down; one full cycle repeats.
        float IdleExtension(int clock)
        {
            int rest = Math.Max(0, Tuning.IdleRestTicks);
            int drop = Math.Max(1, Tuning.IdleDropTicks);
            int spinning = Math.Max(0, Tuning.IdleSpinTicks);
            int raise = Math.Max(1, Tuning.IdleRaiseTicks);
            int t = clock % (rest + drop + spinning + raise);
            if (t < rest)
                return 0f;
            t -= rest;
            if (t < drop)
            {
                float u = 1f - t / (float)drop;
                return 1f - u * u * u; // eased out: let down quickly, settling gently
            }
            t -= drop;
            if (t < spinning)
                return 1f;
            t -= spinning;
            float v = t / (float)raise;
            return 1f - v * v * (3f - 2f * v); // eased both ways: pulled up smoothly
        }

        void TickOut(Player owner, float ropeMax)
        {
            OutTicks++;
            if (IsOwner && trick.FollowsCursorSide)
            {
                // The owner's client decides: the side follows the cursor (past a small dead zone); Side travels in the existing sync.
                float dx = Main.MouseWorld.X - owner.Center.X;
                float deadzone = trick.CursorSideDeadzonePx;
                int wanted = dx > deadzone ? 1 : dx < -deadzone ? -1 : Side;
                if (wanted != Side)
                {
                    Side = wanted;
                    Projectile.netUpdate = true;
                }
            }
            owner.ChangeDir(Side); // every tick, so nothing (movement keys) can turn the player away from the throw

            trick.Tick(this);
            spin += trick.SpinStep(this);
            // A trick may pose the arm itself (Around the World tilts it up and down with the circle); otherwise the arm starts at
            // ThrowArmStartDeg and turns to point at the yoyo over ThrowArmBlendTicks.
            if (trick.ArmPose(this, out float trickArm))
                ApplyArm(owner, HeldSwordPose.ToWorld(Side, trickArm));
            else
            {
                float toYoyo = HeldAim.ToWorld(owner, (Projectile.Center - owner.MountedCenter).ToRotation()) - Tilt;   // the arm is drawn turned with the player
                float startArm = HeldSwordPose.ToWorld(Side, MathHelper.ToRadians(Tuning.ThrowArmStartDeg));
                float blend = MathHelper.Clamp(OutTicks / (float)Math.Max(1, Tuning.ThrowArmBlendTicks), 0f, 1f);
                ApplyArm(owner, startArm + MathHelper.WrapAngle(toYoyo - startArm) * blend);
            }

            if (!IsOwner)
                return;
            if (Vector2.Distance(hand, Projectile.Center) >= ropeMax)
                BeginReturn();                                           // the string is at full length
            else if (trick.RetractsOnRelease && !ButtonHeld(owner) && OutTicks > trick.ReleaseGraceTicks)
                BeginReturn();                                           // the button was let go
        }

        void TickReturn(Player owner)
        {
            // The arm points at the yoyo, then eases to CatchArmDeg as the yoyo gets close.
            float toYoyo = HeldAim.ToWorld(owner, (Projectile.Center - owner.MountedCenter).ToRotation()) - Tilt;   // the arm is drawn turned with the player
            float catchArm = HeldSwordPose.ToWorld(owner.direction >= 0 ? 1 : -1, MathHelper.ToRadians(Tuning.CatchArmDeg));
            float near = 1f - MathHelper.Clamp(Vector2.Distance(owner.MountedCenter, Projectile.Center) / Math.Max(1f, Tuning.CatchArmBlendDistance), 0f, 1f);
            ApplyArm(owner, toYoyo + MathHelper.WrapAngle(catchArm - toYoyo) * near);
            Vector2 toHand = hand - Projectile.Center;
            float distance = toHand.Length();
            returnSpeed = Math.Min(returnSpeed + Tuning.ReturnAccel, Tuning.ReturnMaxSpeed);
            if (distance > 0.01f)
            {
                Vector2 direction = toHand / distance;
                Vel = direction * returnSpeed;
                Projectile.Center += direction * Math.Min(distance, returnSpeed);
            }
            spin += Tuning.ReturnSpinSpeed * Side;

            if (IsOwner && distance <= Tuning.CatchDistance)
            {
                EnterState(YoyoState.Idle);
                Projectile.netUpdate = true;
            }
        }

        // ---- movement (called by the trick) ----

        // One tick of falling and moving with block collision: gravity, slopes, then blocks. Fills in what the yoyo touched.
        public YoyoContact Step(float gravity, float maxFall)
        {
            int width = Projectile.width;
            int height = Projectile.height;
            int g = GravDir;
            Vel.Y += gravity * g;
            Vel.Y = g > 0 ? Math.Min(Vel.Y, maxFall) : Math.Max(Vel.Y, -maxFall);

            if (Tuning.UseSlopes)
            {
                Vector4 slope = Collision.SlopeCollision(Projectile.position, Vel, width, height, gravity * g, true);
                Projectile.position = new Vector2(slope.X, slope.Y);
                Vel = new Vector2(slope.Z, slope.W);
            }

            Vector2 want = Vel;
            // In pieces of at most CollisionStepPx, each checked against the blocks, and an axis that a block stopped stays stopped for the
            // rest of the tick: a fast yoyo cannot clip a corner or jump a block (under that speed this is the single check it always was).
            int pieces = CollisionPieces(want);
            Vector2 piece = want / pieces;
            bool stopX = false;
            bool stopY = false;
            for (int i = 0; i < pieces; i++)
            {
                Vector2 wantPiece = new Vector2(stopX ? 0f : piece.X, stopY ? 0f : piece.Y);
                if (wantPiece == Vector2.Zero)
                    break;
                Vector2 movedPiece = Collision.TileCollision(Projectile.position, wantPiece, width, height, false, false, g);
                if (Math.Abs(movedPiece.X - wantPiece.X) > 0.01f)
                    stopX = true;
                if (Math.Abs(movedPiece.Y) < Math.Abs(wantPiece.Y) - 0.01f)
                    stopY = true;
                Projectile.position += movedPiece;
            }
            YoyoContact contact = default;
            contact.BlockedX = stopX;
            contact.Grounded = want.Y * g > 0f && stopY;   // blocked while moving toward the feet side
            contact.Ceiling = want.Y * g < 0f && stopY;    // blocked while moving away from it
            contact.Impact = contact.Grounded ? Math.Abs(want.Y) : 0f;
            if (contact.BlockedX)
                Vel.X = 0f;
            if (contact.Grounded || contact.Ceiling)
                Vel.Y = 0f;
            return contact;
        }

        // A plain move of the yoyo by 'delta' with block collision, no gravity and no slopes (a trick that steers the yoyo itself uses this).
        // Vel becomes how far it really moved. Blocked and Normal tell whether a block cut it short and which way that block faces.
        // throughPlatforms: platforms and other top-solid blocks do not stop it (it flies through them in every direction).
        public YoyoContact Move(Vector2 delta, bool throughPlatforms = false)
        {
            int g = GravDir;
            // In pieces of at most CollisionStepPx (see Step): a fast pass (the Forward Pass flies 20+ px per tick) cannot clip a corner.
            int pieces = CollisionPieces(delta);
            Vector2 piece = delta / pieces;
            bool stoppedX = false;
            bool stoppedY = false;
            Vector2 moved = Vector2.Zero;
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
                moved += movedPiece;
            }
            YoyoContact contact = default;
            contact.BlockedX = stoppedX;
            contact.Grounded = delta.Y * g > 0f && stoppedY;
            contact.Ceiling = delta.Y * g < 0f && stoppedY;
            contact.Blocked = stoppedX || stoppedY;
            if (contact.Blocked)
            {
                contact.Impact = delta.Length();
                Vector2 normal = new Vector2(stoppedX ? -Math.Sign(delta.X) : 0f, stoppedY ? -Math.Sign(delta.Y) : 0f);
                contact.Normal = normal.LengthSquared() > 0f ? Vector2.Normalize(normal) : Vector2.Zero;
            }
            Vel = moved;
            return contact;
        }

        // How many pieces a move of this length is cut into (each at most CollisionStepPx long).
        int CollisionPieces(Vector2 move)
        {
            float step = Math.Max(1f, Tuning.CollisionStepPx);
            return Math.Max(1, Math.Min(16, (int)Math.Ceiling(move.Length() / step)));
        }

        // ---- arm ----

        // Moves the owner's arm toward the given pose (an angle in the player's own frame, down = +90 degrees) and finds the hand.
        void ApplyArm(Player owner, float target)
        {
            if (!armReady)
            {
                armWorld = target;
                armReady = true;
            }
            else
                armWorld += MathHelper.WrapAngle(target - armWorld) * MathHelper.Clamp(Tuning.ArmFollow, 0.01f, 1f);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armWorld - MathHelper.PiOver2);
            hand = HeldSwordPose.HandWorld(owner, armWorld);
            if (Tilt != 0f)
            {
                // HandWorld does not know the tilt (a minecart, a mount): turn the hand with the player, about the player's own rotation point.
                // Player.RotatedRelativePoint is not used: on the Cosmic Car it turned the hand about a point ~17 px below fullRotationOrigin and
                // swung it ~6 px forward at 0.37 rad. HandWorld already holds gfxOffY, so the pivot gets it too.
                Vector2 pivot = owner.position + owner.fullRotationOrigin + new Vector2(0f, owner.gfxOffY);
                hand = pivot + (hand - pivot).RotatedBy(Tilt);
            }
            //if (owner.mount.Active && Main.GameUpdateCount % 30 == 0)
            //    Main.NewText($"rot {owner.fullRotation:0.00} off {owner.mount.PlayerOffset} offHb {owner.mount.PlayerOffsetHitbox} gfxY {owner.gfxOffY} arm {armWorld:0.00} hand0 {HeldSwordPose.HandWorld(owner, armWorld) - owner.position} hand {hand - owner.position}");
            handAnchor = owner.MountedCenter;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            if (State == YoyoState.Out)
                return trick == null || trick.HurtsWhileOut ? null : (bool?)false;
            return State == YoyoState.Return && trick != null && trick.HurtsWhileReturning(this) ? (bool?)null : false;
        }

        // The whole path the yoyo flew since the start of this tick counts as its hitbox (a line as wide as the yoyo), so a fast yoyo, or the
        // pull back through the ground, cannot skip over an enemy between two ticks. Only the owner's client hits.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Tuning.SweepHits || ticks == 0)
                return null;
            float travelled = Vector2.DistanceSquared(prevCenter, Projectile.Center);
            if (travelled < 1f || travelled > 600f * 600f)
                return null;
            float point = 0f;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), prevCenter, Projectile.Center, Tuning.HitboxSize, ref point))
                return true;
            return null;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = Side;
        }

        // Owner's client only. An enemy hit: effects, a recoil, and the retract after the trick's last hit.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Hits++;
            hitSerial++;
            Fx.Hit(Projectile.Center, Hits);
            YoyoShake.Hit(target.Center, Tuning.HitShake);
            trick.OnHit(this, target);
            if (Hits >= trick.MaxHits)
                BeginReturn();
            Projectile.netUpdate = true;
        }

        // ---- drawing ----

        // One afterimage per tick when the trick wants them (visual only, never synced); OnKill hands the rest to DetachedTrails.
        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active || ticks == 0 || trick == null)
                return;
            afterimages.Tick(GetTexture(), Tuning.Afterimages && State != YoyoState.Idle, Projectile.Center, spin, Vel.Length());
        }

        public override void OnKill(int timeLeft)
        {
            afterimages?.Release();
        }

        // The yoyo's own art when it exists (Tuning.TexturePath), else the vanilla sprite.
        protected virtual Texture2D GetTexture()
        {
            if (!customChecked)
            {
                customChecked = true;
                string path = Tuning.TexturePath;
                if (path != null)
                    ModContent.RequestIfExists<Texture2D>(path, out custom);
            }
            if (custom != null && custom.IsLoaded)
                return custom.Value;
            return TextureAssets.Projectile[Type].Value;   // the vanilla sprite the subclass's Texture points to
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ticks == 0)
                return false; // not posed yet (the first tick has not run)

            if (stringPathActive)
                rope.DrawPolyline(TextureAssets.MagicPixel.Value, stringPath);
            else
                rope.Draw(TextureAssets.MagicPixel.Value);

            Texture2D texture = GetTexture();
            Vector2 origin = texture.Size() * 0.5f;

            afterimages.Draw();   // empty unless the trick has afterimages

            Color light = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, light, spin, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}