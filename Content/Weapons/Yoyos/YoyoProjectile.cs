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
        public bool Grounded;   // blocked while moving down
        public bool BlockedX;   // blocked sideways
        public bool Ceiling;    // blocked while moving up
        public float Impact;    // the downward speed it had when it landed
    }

    /// <summary>
    /// The held yoyo (patch 1): ONE projectile that lives for as long as a reworked yoyo is held (YoyoPlayer spawns it). It is the yoyo
    /// itself: Projectile.Center is the yoyo, the YoyoRope runs from the player's hand to it (a straight line while idle, a verlet rope while out or returning), and it poses the arm.
    /// IDLE: nothing pressed: the arm hangs down, the yoyo is let down gently, spins a little and is pulled back up, over and over.
    /// OUT: the left button runs the yoyo's trick (YoyoTrick; the Wooden Yoyo's is Walk the Dog). The throw side is the side of the screen
    /// the cursor is on, locked at the throw. It retracts after the trick's hits, when the string is at full length, or when the button
    /// is let go (not in the first ReleaseGraceTicks, so a tap still throws). RETURN: it is pulled to the hand and caught; if the button
    /// is still down it is thrown again after a short beat.
    /// MULTIPLAYER: the owner's client decides everything (the click, the side, hits, when to retract and catch). Every machine runs the
    /// same movement from the synced state, so SendExtraAI carries the state, the side, the velocity and two serials (throws and hits);
    /// the position travels with the normal projectile sync (netUpdate on every change and every SyncEveryTicks while out). The idle
    /// animation, the arm and the rope are never synced: every machine plays and simulates its own. Hits and damage are the owner's alone;
    /// the others hear about a hit through the hit serial and play its effects at the yoyo. No custom packets.
    /// Projectile.velocity stays zero: the yoyo moves itself (Vel) so tile collision can be done by hand.
    /// </summary>
    public class YoyoProjectile : ModProjectile
    {
        // The vanilla Wooden Yoyo sprite until Content/Weapons/Yoyos/WoodenYoyoProjectile.png exists.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodYoyo;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // ---- shared with the trick ----
        public Vector2 Vel;            // the yoyo's own velocity (px per tick); synced
        public int Side = 1;           // the side it was thrown to: 1 right, -1 left; synced
        public int OutTicks;           // ticks since the throw
        public int Hits;               // enemy hits since the throw (owner)
        public YoyoState State { get; private set; }

        // ---- synced (SendExtraAI / ReceiveExtraAI) ----
        int throwSerial;
        int hitSerial;
        bool syncedOnce;
        int pendingState = -1;         // a state the owner moved to that this machine has not entered yet
        bool pendingHitFx;

        // ---- per machine, never synced ----
        int ticks;
        int itemType;
        YoyoTrick trick;
        readonly YoyoRope rope = new YoyoRope();
        int idleClock;
        float idleFree;                // the free space below the hand the idle drop may use (px)
        bool idleFreeReady;
        int catchCooldown;
        float returnSpeed;
        float spin;
        float spinRate;
        float armWorld;                // the arm's angle in the player's own frame (down = +90 degrees)
        bool armReady;
        Vector2 hand;                  // where the string is held
        readonly Vector2[] history = new Vector2[4];
        Asset<Texture2D> custom;
        bool customChecked;

        public override void SetDefaults()
        {
            Projectile.width = YoyoTuning.HitboxSize;
            Projectile.height = YoyoTuning.HitboxSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // the trick collides by hand (Step)
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000;    // refilled every tick: the yoyo lives as long as it is held
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = YoyoTuning.HitCooldownTicks;
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
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int state = reader.ReadByte();
            Side = reader.ReadBoolean() ? 1 : -1;
            int throws = reader.ReadByte();
            int hits = reader.ReadByte();
            Vel = new Vector2(reader.ReadSingle(), reader.ReadSingle());
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
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || !YoyoTuning.IsReworked(owner.HeldItem.type) || (ticks > 0 && owner.HeldItem.type != itemType))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 10000;
            Projectile.localNPCHitCooldown = YoyoTuning.HitCooldownTicks;
            float ropeMax = YoyoTuning.RopeLength(owner.HeldItem);

            if (ticks == 0)
            {
                itemType = owner.HeldItem.type;
                trick = YoyoTricks.Create(itemType);
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
                    LaunchFromHand(owner);
                    trick.Begin(this, false);
                }
                EnterState(next);
            }
            if (pendingHitFx)
            {
                pendingHitFx = false;
                YoyoFx.Hit(Projectile.Center, 1 + hitSerial % Math.Max(1, YoyoTuning.WalkHitsPerThrow));
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

            if (State != YoyoState.Idle)
            {
                for (int i = history.Length - 1; i > 0; i--)
                    history[i] = history[i - 1];
                history[0] = Projectile.Center;
                if (IsOwner && ticks % Math.Max(1, YoyoTuning.SyncEveryTicks) == 0)
                    Projectile.netUpdate = true;
            }
            else
            {
                for (int i = 0; i < history.Length; i++)
                    history[i] = Projectile.Center;
            }

            if (State == YoyoState.Idle)
                rope.Straight(hand, Projectile.Center);
            else
                rope.Update(hand, Projectile.Center, YoyoTuning.WalkRopeSlackFactor, YoyoTuning.WalkRopeExtraSlack, YoyoTuning.WalkRopeGravity);
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
                    YoyoFx.Throw(Projectile.Center, Side);
                    break;
                case YoyoState.Return:
                    returnSpeed = Math.Max(YoyoTuning.ReturnStartSpeed, Vel.Length() * 0.4f);
                    YoyoFx.Retract(Projectile.Center);
                    break;
                default:
                    idleClock = 0;
                    idleFreeReady = false;
                    catchCooldown = YoyoTuning.RethrowDelayTicks;
                    Vel = Vector2.Zero;
                    YoyoFx.Catch(Projectile.Center);
                    break;
            }
        }

        // The owner's client: the button asks for a throw.
        void BeginTrick(Player owner)
        {
            Side = Main.MouseWorld.X >= owner.Center.X ? 1 : -1;
            throwSerial++;
            LaunchFromHand(owner);
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

            float armLocal = MathHelper.Lerp(MathHelper.ToRadians(YoyoTuning.IdleArmRestDeg), MathHelper.ToRadians(YoyoTuning.IdleArmDropDeg), extension);
            ApplyArm(owner, HeldSwordPose.ToWorld(facing, armLocal));

            float ropeMax = YoyoTuning.RopeLength(owner.HeldItem);
            // The drop never goes into the ground: it shrinks at once when the free space below the hand does, and recovers gradually.
            float freeNow = FreeDropBelow(Math.Min(YoyoTuning.IdleDropPx, ropeMax));
            if (!idleFreeReady || freeNow < idleFree)
                idleFree = freeNow;
            else
                idleFree = Math.Min(freeNow, idleFree + Math.Max(0.1f, YoyoTuning.IdleDropRecoverPx));
            idleFreeReady = true;
            float drop = idleFree * extension;
            float sway = (float)Math.Sin(idleClock * 0.12f) * YoyoTuning.IdleSwayPx * extension;
            Projectile.Center = hand + new Vector2(sway, drop);

            spinRate = MathHelper.Lerp(YoyoTuning.IdleSpinRestSpeed, YoyoTuning.IdleSpinSpeed, extension * extension);
            spin += spinRate * facing;

            if (IsOwner)
            {
                if (catchCooldown > 0)
                    catchCooldown--;
                else if (owner.controlUseItem && !owner.mouseInterface)
                    BeginTrick(owner);
            }
        }

        // A throw starts at the hand (the string's holding point), never at the idle position below it, which can be inside the floor.
        // If the hand itself is inside a block, the yoyo starts at the first free spot on the way to the player's center. Runs on every
        // machine; the owner's position then travels with the normal sync.
        void LaunchFromHand(Player owner)
        {
            Vector2 start = armReady ? hand : owner.MountedCenter;
            Projectile.Center = start;
            if (!Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                return;
            Vector2 safe = owner.MountedCenter;
            int steps = 8;
            for (int i = 1; i <= steps; i++)
            {
                Projectile.Center = Vector2.Lerp(start, safe, i / (float)steps);
                if (!Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                    return;
            }
            Projectile.Center = safe;
        }

        // How far the idle yoyo may be let down from the hand before it would touch a block (0 to want), checked in small steps.
        float FreeDropBelow(float want)
        {
            int width = (int)Math.Max(2f, YoyoTuning.IdleGroundCheckWidth);
            float d = 0f;
            while (d < want)
            {
                float next = Math.Min(want, d + 2f);
                Vector2 topLeft = hand + new Vector2(-width * 0.5f, next - Projectile.height * 0.5f);
                if (Collision.SolidCollision(topLeft, width, Projectile.height))
                    break;
                d = next;
            }
            return d;
        }

        // 0 = in the hand, 1 = let all the way down; one full cycle repeats.
        static float IdleExtension(int clock)
        {
            int rest = Math.Max(0, YoyoTuning.IdleRestTicks);
            int drop = Math.Max(1, YoyoTuning.IdleDropTicks);
            int spinning = Math.Max(0, YoyoTuning.IdleSpinTicks);
            int raise = Math.Max(1, YoyoTuning.IdleRaiseTicks);
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
            owner.ChangeDir(Side); // every tick, so nothing (movement keys) can turn the player away from the throw

            trick.Tick(this);
            spin += Vel.X * YoyoTuning.SpinPerPixel;
            // The arm starts at ThrowArmStartDeg and turns to point at the yoyo over ThrowArmBlendTicks.
            float toYoyo = HeldAim.ToWorld(owner, (Projectile.Center - owner.MountedCenter).ToRotation());
            float startArm = HeldSwordPose.ToWorld(Side, MathHelper.ToRadians(YoyoTuning.ThrowArmStartDeg));
            float blend = MathHelper.Clamp(OutTicks / (float)Math.Max(1, YoyoTuning.ThrowArmBlendTicks), 0f, 1f);
            ApplyArm(owner, startArm + MathHelper.WrapAngle(toYoyo - startArm) * blend);

            if (!IsOwner)
                return;
            if (Vector2.Distance(hand, Projectile.Center) >= ropeMax)
                BeginReturn();                                           // the string is at full length
            else if (!owner.controlUseItem && OutTicks > YoyoTuning.ReleaseGraceTicks)
                BeginReturn();                                           // the button was let go
        }

        void TickReturn(Player owner)
        {
            // The arm points at the yoyo, then eases to CatchArmDeg as the yoyo gets close.
            float toYoyo = HeldAim.ToWorld(owner, (Projectile.Center - owner.MountedCenter).ToRotation());
            float catchArm = HeldSwordPose.ToWorld(owner.direction >= 0 ? 1 : -1, MathHelper.ToRadians(YoyoTuning.CatchArmDeg));
            float near = 1f - MathHelper.Clamp(Vector2.Distance(owner.MountedCenter, Projectile.Center) / Math.Max(1f, YoyoTuning.CatchArmBlendDistance), 0f, 1f);
            ApplyArm(owner, toYoyo + MathHelper.WrapAngle(catchArm - toYoyo) * near);
            Vector2 toHand = hand - Projectile.Center;
            float distance = toHand.Length();
            returnSpeed = Math.Min(returnSpeed + YoyoTuning.ReturnAccel, YoyoTuning.ReturnMaxSpeed);
            if (distance > 0.01f)
            {
                Vector2 direction = toHand / distance;
                Vel = direction * returnSpeed;
                Projectile.Center += direction * Math.Min(distance, returnSpeed);
            }
            spin += YoyoTuning.ReturnSpinSpeed * Side;

            if (IsOwner && distance <= YoyoTuning.CatchDistance)
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
            Vel.Y = Math.Min(Vel.Y + gravity, maxFall);

            if (YoyoTuning.UseSlopes)
            {
                Vector4 slope = Collision.SlopeCollision(Projectile.position, Vel, width, height, gravity, true);
                Projectile.position = new Vector2(slope.X, slope.Y);
                Vel = new Vector2(slope.Z, slope.W);
            }

            Vector2 want = Vel;
            Vector2 moved = Collision.TileCollision(Projectile.position, want, width, height, false, false, 1);
            YoyoContact contact = default;
            contact.BlockedX = Math.Abs(moved.X - want.X) > 0.01f;
            contact.Grounded = want.Y > 0f && moved.Y < want.Y - 0.01f;
            contact.Ceiling = want.Y < 0f && moved.Y > want.Y + 0.01f;
            contact.Impact = contact.Grounded ? want.Y : 0f;
            Projectile.position += moved;
            if (contact.BlockedX)
                Vel.X = 0f;
            if (contact.Grounded || contact.Ceiling)
                Vel.Y = 0f;
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
                armWorld += MathHelper.WrapAngle(target - armWorld) * MathHelper.Clamp(YoyoTuning.ArmFollow, 0.01f, 1f);
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
            YoyoFx.Hit(Projectile.Center, Hits);
            CameraShake.Punch(target.Center, YoyoTuning.HitShake, 6);
            trick.OnHit(this, target);
            if (Hits >= trick.MaxHits)
                BeginReturn();
            Projectile.netUpdate = true;
        }

        // ---- drawing ----

        Texture2D GetTexture()
        {
            if (!customChecked)
            {
                customChecked = true;
                ModContent.RequestIfExists<Texture2D>(YoyoTuning.WoodenTexturePath, out custom);
            }
            return custom != null && custom.IsLoaded ? custom.Value : TextureAssets.Projectile[Type].Value;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ticks == 0)
                return false; // not posed yet (the first tick has not run)

            rope.Draw(TextureAssets.MagicPixel.Value);

            Texture2D texture = GetTexture();
            Vector2 origin = texture.Size() * 0.5f;

            // A short blur behind a fast yoyo.
            float speed = Vector2.Distance(history[0], history[history.Length - 1]) / history.Length;
            if (State != YoyoState.Idle && speed > 2f)
            {
                for (int i = history.Length - 1; i >= 1; i--)
                {
                    Color blur = Lighting.GetColor((int)(history[i].X / 16f), (int)(history[i].Y / 16f)) * (0.3f * (1f - i / (float)history.Length));
                    Main.EntitySpriteDraw(texture, history[i] - Main.screenPosition, null, blur, spin - i * 0.3f, origin, 1f, SpriteEffects.None, 0);
                }
            }

            Color light = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, light, spin, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}