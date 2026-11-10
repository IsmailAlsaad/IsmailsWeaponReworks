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

        public YoyoTuning Tuning { get; private set; }
        public YoyoFx Fx { get; private set; }

        public Player Owner => Main.player[Projectile.owner];
        public bool IsOwner => Projectile.owner == Main.myPlayer;

        // 1 = normal gravity, -1 = flipped (the owner's gravDir, which every machine knows): "down" for the yoyo is toward the player's feet.
        public int GravDir => Owner.gravDir >= 0f ? 1 : -1;

        // ---- shared with the trick ----
        public Vector2 Vel;            // the yoyo's own velocity (px per tick); synced
        public Vector2 Aim;            // a trick's throw target, relative to the player (Forward Pass); set by the owner at the throw; synced
        public int Side = 1;           // the side it was thrown to: 1 right, -1 left; synced
        public int OutTicks;           // ticks since the throw
        public int Hits;               // enemy hits since the throw (owner)
        public YoyoState State { get; private set; }
        public float SpinAngle => spin;   // how far the sprite is turned (a yoyo that draws on top of its sprite follows it)

        // ---- synced (SendExtraAI / ReceiveExtraAI) ----
        int throwSerial;
        int hitSerial;
        bool syncedOnce;
        int pendingState = -1;         // a state the owner moved to that this machine has not entered yet
        bool pendingHitFx;

        // ---- per machine, never synced ----
        int ticks;
        YoyoTrick trick;
        YoyoRope rope;
        int idleClock;
        int catchCooldown;
        float returnSpeed;
        float spin;
        float spinRate;
        float armWorld;                // the arm's angle in the player's own frame (down = +90 degrees)
        bool armReady;
        Vector2 hand;                  // where the string is held
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
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int state = reader.ReadByte();
            Side = reader.ReadBoolean() ? 1 : -1;
            int throws = reader.ReadByte();
            int hits = reader.ReadByte();
            Vel = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            Aim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
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
                trick = CreateTrick(Tuning);
                rope = new YoyoRope(Tuning);
                afterimages = new YoyoAfterimages(Tuning);
                Projectile.Center = owner.MountedCenter;
            }
            ticks++;

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

            if (State == YoyoState.Idle)
                rope.Straight(hand, Projectile.Center);
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
        void BeginTrick(Player owner)
        {
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
            Projectile.Center = hand + new Vector2(sway, drop);

            spinRate = MathHelper.Lerp(Tuning.IdleSpinRestSpeed, Tuning.IdleSpinSpeed, extension * extension);
            spin += spinRate * facing;

            if (IsOwner)
            {
                if (catchCooldown > 0)
                    catchCooldown--;
                else if (owner.controlUseItem && !owner.mouseInterface)
                    BeginTrick(owner);
            }
        }

        // A throw starts at the center of the player (the idle yoyo may be inside blocks, so it never starts from where it hung). Runs on
        // every machine; the owner's position then travels with the normal sync.
        void LaunchFromCenter(Player owner)
        {
            Projectile.Center = owner.MountedCenter;
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
            // The arm starts at ThrowArmStartDeg and turns to point at the yoyo over ThrowArmBlendTicks.
            float toYoyo = HeldAim.ToWorld(owner, (Projectile.Center - owner.MountedCenter).ToRotation());
            float startArm = HeldSwordPose.ToWorld(Side, MathHelper.ToRadians(Tuning.ThrowArmStartDeg));
            float blend = MathHelper.Clamp(OutTicks / (float)Math.Max(1, Tuning.ThrowArmBlendTicks), 0f, 1f);
            ApplyArm(owner, startArm + MathHelper.WrapAngle(toYoyo - startArm) * blend);

            if (!IsOwner)
                return;
            if (Vector2.Distance(hand, Projectile.Center) >= ropeMax)
                BeginReturn();                                           // the string is at full length
            else if (trick.RetractsOnRelease && !owner.controlUseItem && OutTicks > Tuning.ReleaseGraceTicks)
                BeginReturn();                                           // the button was let go
        }

        void TickReturn(Player owner)
        {
            // The arm points at the yoyo, then eases to CatchArmDeg as the yoyo gets close.
            float toYoyo = HeldAim.ToWorld(owner, (Projectile.Center - owner.MountedCenter).ToRotation());
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
            Vector2 moved = Collision.TileCollision(Projectile.position, want, width, height, false, false, g);
            YoyoContact contact = default;
            bool stoppedY = Math.Abs(moved.Y) < Math.Abs(want.Y) - 0.01f;
            contact.BlockedX = Math.Abs(moved.X - want.X) > 0.01f;
            contact.Grounded = want.Y * g > 0f && stoppedY;   // blocked while moving toward the feet side
            contact.Ceiling = want.Y * g < 0f && stoppedY;    // blocked while moving away from it
            contact.Impact = contact.Grounded ? Math.Abs(want.Y) : 0f;
            Projectile.position += moved;
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
            Vector2 moved = Collision.TileCollision(Projectile.position, delta, Projectile.width, Projectile.height, throughPlatforms, throughPlatforms, g);
            YoyoContact contact = default;
            bool stoppedX = Math.Abs(moved.X - delta.X) > 0.01f;
            bool stoppedY = Math.Abs(moved.Y - delta.Y) > 0.01f;
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
            Projectile.position += moved;
            Vel = moved;
            return contact;
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
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return State == YoyoState.Out ? (bool?)null : false;
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