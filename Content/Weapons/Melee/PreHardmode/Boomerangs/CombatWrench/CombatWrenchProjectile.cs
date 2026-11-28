using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Rope;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
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
    /// ai[0] = phase (see Phase; W1 uses Out, Back and Return), ai[1] = free (the latched enemy from W2 on),
    /// ai[2] = the aim angle in radians (world), set at the spawn.
    /// MULTIPLAYER: the owner's client decides the click and the aim (ai[2] travels in the spawn packet) and the moment it returns
    /// (ai[0] = Return with netUpdate); every machine, the server included, runs the same ellipse from the aim and the owner's
    /// position, stops at blocks itself and catches its own copy, so no kill packet is needed. The rope, trail, sparks and sounds are
    /// visual: every machine builds its own, nothing is synced.
    /// </summary>
    public class CombatWrenchProjectile : ModProjectile
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

        public override bool? CanDamage()
        {
            return false;
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
            Vector2 butt = Projectile.Center + (Projectile.rotation + MathHelper.ToRadians(CombatWrenchTuning.ButtAngleDeg)).ToRotationVector2() * CombatWrenchTuning.RopeAttachPx;
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

            Projectile.Center = owner.MountedCenter;
            Projectile.rotation = 0f;
            Projectile.ai[0] = (float)Phase.Out;
            CombatWrenchFx.Throw(Projectile.Center, aim);
        }

        // Out and Back: the Forward Pass ellipse, followed from the player's current position (so running does not break the loop).
        void TickThrow(Player owner, bool isOwner)
        {
            owner.ChangeDir(side); // every tick, so nothing (movement keys) can turn the player away from the throw
            passTicks++;
            Projectile.rotation += CombatWrenchTuning.SpinPerTick * side;

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
            Projectile.rotation += CombatWrenchTuning.ReturnSpinPerTick * side;

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

            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Color light = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, light, Projectile.rotation, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}