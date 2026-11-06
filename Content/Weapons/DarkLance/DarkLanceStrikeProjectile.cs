using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>
    /// The Dark Lance's left click: ONE flowing strike (it replaces the old separate sweep and thrust). The whole movement is a pure function
    /// of the projectile's age, in phases that run into each other with no hard stop:
    ///   ANTICIPATION  the lance is wound back against the spin and drawn toward the player (slow, heavy),
    ///   SWEEP         one turn that ACCELERATES (the speed keeps rising to the end of the phase), gathering every orb it passes,
    ///   DECEL         it does not stop: the turn carries on past the full circle (overshoot) and loses speed smoothly,
    ///   COIL          the lance swings back from the overshoot to the cursor while it is drawn back, the thrust size grows in,
    ///   THRUST        it shoots out fast, past full reach, and settles (HOLD), hurting everything on its way,
    ///   RECOVER       it eases back to the hand.
    /// The sweep leaves a line trail (the shared PixelTrail / PixelLine renderer, sampled several times per tick so the circle is smooth);
    /// the thrust leaves a shorter one. Every gathered orb makes the thrust bigger and stronger; with a full ring the first enemy the thrust
    /// hits explodes and throws 3-4 falling flame embers (DarkLanceFlameEmberProjectile).
    /// A second way in (patch 9): when the right-click charge has made all 8 orbs, the charge spawns this projectile in MODE 1: the same
    /// wind-up and sweep (it gathers every orb but hurts nothing), then the lance is CHARGED: it waits in a hold, held in the hand (arm
    /// stretched), always pointing at the cursor, wearing a pulsing purple outline. The next left click is the release (MODE 2): the thrust
    /// runs on at once, with no sweep before it.
    /// SPAWN VALUES: ai[0] = spin direction (1 or -1) times (1 + gathered orbs + 100 * mode), ai[1] = the thrust aim (starts as the base aim;
    /// the owner re-captures the cursor into it when the coil starts, and every tick of the hold), ai[2] = the aim in the player's own frame.
    /// MULTIPLAYER: the owner's client decides the click, the aims, which orbs are gathered and every hit. Angle, extension, scale and sounds
    /// are pure functions of the age, so every machine poses and draws the same strike without per-tick packets. The gathered count and mode
    /// (ai[0], updated with every gathered orb and at the release) and the thrust aim (ai[1], set once; during the hold sent whenever the
    /// cursor has moved enough, the other machines glide toward it) travel with netUpdate; the gathered flag also rides on the orb
    /// (ai[2] + netUpdate). Other players' machines find the hits themselves and only show sparks (RemoteHitTracker). Trails are visual only.
    /// </summary>
    public class DarkLanceStrikeProjectile : ModProjectile
    {
        // The strike draws the vanilla PROJECTILE sprite (the long-handled one).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DarkLance;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int SpinDirection => Projectile.ai[0] >= 0f ? 1 : -1;
        int Mode => (int)(Math.Abs(Projectile.ai[0]) / 100f);   // 0 = the full strike, 1 = collect the orbs then wait charged, 2 = released from the charge
        int Gathered => Math.Min(DarkLanceTuning.MaxOrbs, Math.Max(0, (int)Math.Round(Math.Abs(Projectile.ai[0])) % 100 - 1));
        float ThrustAim => Projectile.ai[1];
        float BaseAim => Projectile.ai[2];
        float Size => 1f + DarkLanceTuning.SizePerOrb * Gathered;
        float ThrustDamageRatio => DarkLanceTuning.ThrustDamageMultiplier * (1f + DarkLanceTuning.DamagePerOrb * Gathered) / Math.Max(0.01f, DarkLanceTuning.SweepDamageMultiplier);

        // The phase lengths in ticks, read from the tuning every tick (hot reload), and the ages the phases end at.
        int pA, pS, pC, pW, pT, pH, pR;
        int b1, b2, b3, b4, b5, b6, b7;
        int tailStart;                 // where the coil/thrust part of the timeline starts (b3, or b4 after the charged hold)
        int tb4, tb5, tb6, tb7;        // the thrust start, thrust end, hold end and total, counted from tailStart

        // Per-machine state: counters and visuals, never synced.
        int age;
        int vAge;              // the age the movement uses: the real age, but a charged lance waits in its hold until the click
        int lastV = -1;
        int goAge = -1;        // the age this machine saw the release at
        float curT;
        float smoothAim;       // the thrust aim as this machine shows it (the owner's is exact, the others glide toward it)
        float outlineShown;    // 0..1, how visible the purple outline is
        bool holding;          // charged and waiting for the click
        bool hadCenter;
        Vector2 prevCenterHand;
        float lastSentAim;
        int lastSentAge = -10;
        float speedFraction;   // this tick's speed, 0 to 1 of the fastest
        float turnNow;         // radians turned along the spin direction so far (negative while winding back)
        bool striking;
        bool inThrust;
        bool exploded;         // owner only
        bool hasPose;
        bool whooshed;
        Vector2 butt;
        Vector2 tip;
        Vector2 direction;
        Vector2 prevButt;
        Vector2 prevTip;
        Vector2 pivot;         // the hand the lance turns around (the gather test measures every orb's angle from it)
        PixelTrail sweepTrail;
        PixelTrail thrustTrail;
        readonly Vector2[] outlineDirections =
        {
            new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f)
        };
        RemoteHitTracker remoteHits; // created on first use: a field initializer here would be shared by every projectile of the type

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
            Projectile.timeLeft = 2;               // kept alive by the AI, ended by the AI
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;   // every enemy once per part (cleared when the thrust starts)
            Projectile.ownerHitCheck = true;       // it cannot hit through walls
        }

        void ReadPlan()
        {
            pA = Math.Max(1, DarkLanceTuning.AnticipationTicks);
            pS = Math.Max(2, DarkLanceTuning.SweepTicks);
            pC = Math.Max(1, DarkLanceTuning.DecelTicks);
            pW = Math.Max(1, DarkLanceTuning.CoilTicks);
            pT = Math.Max(1, DarkLanceTuning.ThrustTicks);
            pH = Math.Max(0, DarkLanceTuning.ThrustHoldTicks);
            pR = Math.Max(1, DarkLanceTuning.RecoverTicks);
            b1 = pA;
            b2 = b1 + pS;
            b3 = b2 + pC;
            b4 = b3 + pW;
            b5 = b4 + pT;
            b6 = b5 + pH;
            b7 = b6 + pR;
            tailStart = Mode == 0 ? b3 : b4;
            tb4 = tailStart + pW;
            tb5 = tb4 + pT;
            tb6 = tb5 + pH;
            tb7 = tb6 + pR;
        }

        // ---- the movement: pure functions of the time t (in ticks; the pose of tick n is t = n + 1) ----

        static float Smooth(float u)
        {
            return MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(u, 0f, 1f));
        }

        // How far the lance has turned along the spin direction (radians). Winds back during the anticipation, then the sweep speeds up
        // all the way to its end (u^power), then the decel phase carries on with a speed that falls linearly to zero: the speed is
        // continuous everywhere, so nothing jerks. The sweep's distance is chosen so the decel ends exactly at a full circle plus the overshoot.
        float TurnAt(float t)
        {
            float pull = DarkLanceTuning.AnticipationAngle;
            if (t <= b1)
                return -pull * Smooth(t / pA);
            float power = Math.Max(1f, DarkLanceTuning.SweepAccelPower);
            float target = MathHelper.TwoPi + DarkLanceTuning.SweepOvershoot;
            float reach = (target + pull) / (1f + power * pC / (2f * pS));   // distance covered by the accelerating part, from the wound-back angle
            if (t <= b2)
                return -pull + reach * (float)Math.Pow((t - b1) / pS, power);
            if (t <= b3)
            {
                float speedAtEnd = power * reach / pS;   // radians per tick when the sweep part ends
                float c = t - b2;
                return -pull + reach + speedAtEnd * (c - c * c / (2f * pC));
            }
            return target;
        }

        // The aim in the player's own frame. During the sweep it follows the turn; during the coil it swings from the overshoot to the thrust aim.
        float AimAt(float t)
        {
            float turn = TurnAt(t);
            float aim = BaseAim + SpinDirection * turn;
            if (Mode >= 1 && t < b1)
                aim += MathHelper.WrapAngle(ChargeAim() - BaseAim) * (1f - Smooth(t / pA));   // starts from the pose the charge left the lance in
            if (t > b3)
            {
                float end = BaseAim + SpinDirection * (MathHelper.TwoPi + DarkLanceTuning.SweepOvershoot);
                aim = end + MathHelper.WrapAngle(smoothAim - end) * Smooth((t - b3) / pW);
            }
            return aim;
        }

        // How far the lance is pushed along its direction, in units of the thrust reach (negative = drawn back toward the player).
        float ExtensionAt(float t)
        {
            float anticipationDraw = DarkLanceTuning.AnticipationDraw;
            float coilDraw = DarkLanceTuning.CoilDraw;
            if (t <= b1)
                return -anticipationDraw * Smooth(t / pA);
            if (t <= b3)
                return -anticipationDraw * (1f - Smooth((t - b1) / (b3 - b1)));   // lets go of the draw while it turns
            if (t <= tailStart)
                return 0f;   // a charged lance swings to the cursor and waits with the lance held out
            if (t <= tailStart + pW)
                return -coilDraw * Smooth((t - tailStart) / pW);
            if (t <= tb5)
            {
                float u = (t - tb4) / pT;
                float eased = 1f - (float)Math.Pow(1f - u, Math.Max(1f, DarkLanceTuning.ThrustEasePower));
                return MathHelper.Lerp(-coilDraw, 1f + DarkLanceTuning.ThrustOvershoot, eased);   // shoots out past full reach ...
            }
            if (t <= tb6)
                return MathHelper.Lerp(1f + DarkLanceTuning.ThrustOvershoot, 1f, Smooth((t - tb5) / pH));   // ... and settles
            if (t <= tb7)
                return 1f - Smooth((t - tb6) / pR);
            return 0f;
        }

        // 0 during the sweep, 1 from the end of the coil: how much the lance has grown into its thrust shape (size, grip).
        float ThrustBlend(float t)
        {
            return t <= tailStart ? 0f : Smooth((t - tailStart) / pW);
        }

        // 0 for the full strike (the lance turns around the chest). A charged lance (modes 1 and 2) is held in the hand from the coil on.
        float ArmBlend(float t)
        {
            return Mode == 0 || t <= b3 ? 0f : Smooth((t - b3) / pW);
        }

        Vector2 HandAt(float t, Vector2 centerHand)
        {
            float blend = ArmBlend(t);
            return blend <= 0f ? centerHand : Vector2.Lerp(centerHand, SpearPose.Hand(Owner, AimAt(t)), blend);
        }

        // The pose the charge left the lance in (pointing up, leaning toward the facing side), in the player's own frame.
        float ChargeAim()
        {
            return -MathHelper.PiOver2 + DarkLanceTuning.LanceTilt * HeldAim.Facing(BaseAim);
        }

        float TextureDiagonal()
        {
            if (Main.dedServ)
            {
                Item sample = ContentSamples.ItemsByType[ItemID.DarkLance];   // a dedicated server has no textures
                return new Vector2(sample.width, sample.height).Length();
            }
            Texture2D art = TextureAssets.Projectile[Type].Value;
            return new Vector2(art.Width, art.Height).Length();
        }

        // Where the lance is at time t, for the given hand.
        void Pose(float t, Vector2 hand, out Vector2 buttOut, out Vector2 tipOut, out Vector2 directionOut, out float scaleOut)
        {
            float aim = AimAt(t);
            float grow = ThrustBlend(t);
            float size = Size;
            directionOut = HeldAim.ToWorldDirection(Owner, aim);
            scaleOut = MathHelper.Lerp(DarkLanceTuning.SweepScale, DarkLanceTuning.ThrustScale * size, grow) * Owner.GetAdjustedItemScale(Owner.HeldItem);
            float shaft = TextureDiagonal() * scaleOut;
            float grip = MathHelper.Lerp(DarkLanceTuning.SweepGrip, DarkLanceTuning.ThrustGrip, grow);
            float reach = DarkLanceTuning.ThrustReach * size;
            buttOut = hand - directionOut * shaft * grip + directionOut * ExtensionAt(t) * reach;
            if (Mode >= 1 && t < b1)
            {
                // coming out of the charge: the butt starts where the charge held it and moves to where the strike holds it
                Vector2 chargeDirection = HeldAim.ToWorldDirection(Owner, ChargeAim());
                Vector2 normalAtStart = hand - chargeDirection * TextureDiagonal() * DarkLanceTuning.SweepScale * Owner.GetAdjustedItemScale(Owner.HeldItem) * DarkLanceTuning.SweepGrip;
                Vector2 chargeButt = hand + new Vector2(Owner.direction * DarkLanceTuning.HandOffsetX, DarkLanceTuning.HandOffsetY * Owner.gravDir);
                buttOut += (chargeButt - normalAtStart) * (1f - Smooth(t / pA));
            }
            tipOut = buttOut + directionOut * shaft;
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.DarkLance)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            ReadPlan();
            Projectile.localNPCHitCooldown = inThrust ? -1 : DarkLanceTuning.SweepHitCooldownTicks;   // how soon the sweep may hit the same enemy again (-1 = once per sweep)

            if (Mode == 2 && goAge < 0)
                goAge = age;   // the click on the charged lance: every machine starts the thrust from here
            // The age the movement runs on: the real one, except that a charged lance (mode 1) waits in its hold until the click, then the thrust runs on.
            if (Mode == 0)
                vAge = age;
            else if (goAge < 0)
                vAge = Math.Min(age, b4 - 1);
            else
                vAge = b4 + (age - goAge);

            if (vAge >= tb7)
            {
                owner.itemAnimation = 0; // the next use may start as soon as the cooldown is over
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            if (IsOwner)
            {
                if (age == 0)
                    owner.ChangeDir(HeldAim.Facing(BaseAim)); // the player faces the cursor side for the whole sweep
                if (Mode == 0 && age == b3)
                {
                    // The coil starts: the thrust goes where the cursor is NOW (or along the click, when ThrustRetarget is off).
                    Projectile.ai[1] = DarkLanceTuning.ThrustRetarget ? HeldAim.Capture(owner) : BaseAim;
                    Projectile.netUpdate = true;
                    owner.ChangeDir(HeldAim.Facing(Projectile.ai[1]));
                }
                else if (Mode == 1 && age >= b3)
                {
                    // The charged lance always points at the cursor: the aim is sent whenever it has moved enough (the other machines glide toward it).
                    float live = HeldAim.Capture(owner);
                    Projectile.ai[1] = live;
                    if (age == b3 || (age - lastSentAge >= 2 && Math.Abs(MathHelper.WrapAngle(live - lastSentAim)) > DarkLanceTuning.PrimedAimSyncDelta))
                    {
                        Projectile.netUpdate = true;
                        lastSentAim = live;
                        lastSentAge = age;
                    }
                    owner.ChangeDir(HeldAim.Facing(live));
                }
            }
            if (!hasPose || IsOwner)
                smoothAim = Projectile.ai[1];
            else
                smoothAim += MathHelper.WrapAngle(Projectile.ai[1] - smoothAim) * MathHelper.Clamp(DarkLanceTuning.PrimedAimFollow, 0.01f, 1f);

            float t = vAge + 1f;
            curT = t;
            float lastTurn = TurnAt(t - 1f);
            turnNow = TurnAt(t);

            Vector2 lastButt = butt;
            Vector2 lastTip = tip;
            // Like the Rotted Fork spin (and the vanilla Sleepy Octopod): the sweep has no arm pose of our own. The item uses the Shoot use style and
            // vanilla picks the body frame (arm out, raised or lowered) from itemRotation, which is set below. A charged lance (from the coil on)
            // is held in the stretched front arm instead.
            Vector2 centerHand = owner.RotatedRelativePoint(owner.MountedCenter, false, true);
            if (!hadCenter)
            {
                prevCenterHand = centerHand;
                hadCenter = true;
            }
            Vector2 hand = HandAt(t, centerHand);
            Pose(t, hand, out butt, out tip, out direction, out float scale);
            Projectile.scale = scale;
            pivot = hand;
            Projectile.Center = owner.MountedCenter; // knockback pushes away from the player
            Projectile.rotation = direction.ToRotation();
            Projectile.spriteDirection = age >= b3 ? HeldAim.Facing(smoothAim) : SpinDirection;
            owner.heldProj = Projectile.whoAmI;      // also ends the projectile when the player swaps items
            holding = Mode == 1 && age >= b4;
            if (!holding)
                owner.SetDummyItemTime(2);           // keeps the item "in use" for as long as the strike lasts (a charged lance waits without it, so the click can start the thrust)
            if (Mode >= 1 && ArmBlend(t) > 0f)
                SpearPose.Arm(owner, AimAt(t));
            float aim = AimAt(t);
            float seenFromRight = owner.direction == 1 ? aim : MathHelper.Pi - aim; // the aim as a right-facing player would have it
            owner.itemRotation = MathHelper.WrapAngle(seenFromRight + MathHelper.PiOver4) * owner.direction;

            prevButt = hasPose ? lastButt : butt;
            prevTip = hasPose ? lastTip : tip;
            hasPose = true;

            // Speed: how fast the lance is turning (up to the end of the decel), and how fast the tip moves in the thrust.
            float angular = vAge < b3 ? Math.Abs(turnNow - lastTurn) / Math.Max(0.05f, DarkLanceTuning.SweepSpeedRef) : 0f;
            float linear = vAge >= tb4 ? (tip - prevTip).Length() / Math.Max(1f, DarkLanceTuning.ThrustSpeedRef) : 0f;
            speedFraction = MathHelper.Clamp(Math.Max(angular, linear), 0f, 1f);

            bool sweepPhase = age >= b1 && age < b3;   // the whole turn, its acceleration and its overshoot
            inThrust = vAge >= tb4 && vAge < tb6;
            // the auto sweep of a charged lance (mode 1) gathers the orbs but hurts nothing
            striking = (sweepPhase && Mode == 0 && speedFraction >= DarkLanceTuning.SweepMinHitSpeed) || inThrust;

            owner.GetModPlayer<DarkLancePlayer>().LanceTip = tip; // gathered orbs fly to it (every machine)

            if (vAge == tb4 && vAge != lastV)
            {
                SegmentHit.ClearLocalImmunity(Projectile);   // an enemy the sweep hit may be hit again by the thrust
                if (!Main.dedServ)
                    DarkLanceFx.ThrustStart(tip, Gathered);
                if (IsOwner)
                    CameraShake.Punch(tip, 0.6f + 0.25f * Gathered, 10, "DarkLanceThrust");
            }

            if (IsOwner && sweepPhase)
                Gather();

            if (!Main.dedServ)
                Visuals(owner, t, centerHand);

            // Other players' machines do not run OnHitNPC; they find the hit themselves and only show the sparks.
            if (!IsOwner && !Main.dedServ && striking)
                (remoteHits ??= new RemoteHitTracker()).Update(Projectile, inThrust ? 1 : 0, npc => ThrustFx.HitSpark(SegmentHit.ClosestPoint(butt, tip, npc.Center), DarkLanceTuning.Palette));

            prevCenterHand = centerHand;
            lastV = vAge;
            age++;
        }

        // The charged lance (mode 1, waiting for the click) that this player has out, or null. Owner's client.
        public static Projectile FindPrimed(Player player)
        {
            int type = ModContent.ProjectileType<DarkLanceStrikeProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type && p.ModProjectile is DarkLanceStrikeProjectile strike && strike.Mode == 1)
                    return p;
            }
            return null;
        }

        // The auto sweep is over and the lance waits for the click.
        public bool Ready => holding;

        // Owner only: the click on the charged lance. The thrust goes where the cursor is now; every machine sees the mode change and runs it on.
        public void Release()
        {
            Projectile.ai[1] = HeldAim.Capture(Owner);
            Projectile.ai[0] = SpinDirection * (1f + Gathered + 200f);
            Projectile.netUpdate = true;
            Owner.ChangeDir(HeldAim.Facing(Projectile.ai[1]));
        }

        // Owner only: every orb the lance has turned past (by angle, see Touches) is gathered. The flag travels with the orb and the count
        // with this projectile (ai[0]); every machine then flies the orb into the tip.
        void Gather()
        {
            int orbType = ModContent.ProjectileType<DarkLanceOrbProjectile>();
            // The turn is over on this tick: a full circle has been made, so every orb that is still left has been turned past by definition.
            bool lastTick = age >= b3 - 1;
            int count = Gathered;
            bool changed = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile orb = Main.projectile[i];
                if (!orb.active || orb.owner != Projectile.owner || orb.type != orbType || orb.ai[0] >= 1f || orb.ai[2] >= 1f)
                    continue;
                if (!lastTick && !Touches(orb))
                    continue;
                orb.ai[2] = 1f;
                orb.netUpdate = true;
                count = Math.Min(DarkLanceTuning.MaxOrbs, count + 1);
                changed = true;
            }
            if (changed)
            {
                Projectile.ai[0] = SpinDirection * (1f + count + 100f * Mode);
                Projectile.netUpdate = true;
            }
        }

        // Has the lance turned past this orb? A pure ANGLE test, no colliders and no crossing to catch. Every orb has a "need": how far the
        // lance must turn from its start aim to point at it, measured along the turn and folded into [-pull, 2 pi - pull), the range the
        // lance covers. The orb is gathered as soon as the TOTAL turn so far has reached its need, so nothing can slip through: an orb that
        // moves toward or past the lance is simply already passed, and one that runs ahead is caught by the end (the turn always reaches a
        // full circle). Where the orb is (near, far, lagging) never matters.
        bool Touches(Projectile orb)
        {
            Vector2 v = orb.Center - pivot;
            if (v.LengthSquared() < 1f)
                return true;   // exactly on the hand: every angle includes it
            float pull = DarkLanceTuning.AnticipationAngle;
            float worldSpin = Owner.gravDir == -1f ? -SpinDirection : SpinDirection;   // which way the lance turns on screen (the aim is mirrored with gravity)
            float need = worldSpin * MathHelper.WrapAngle(v.ToRotation() - HeldAim.ToWorld(Owner, BaseAim));   // -pi to pi from the start aim, along the turn
            need -= MathHelper.TwoPi * (float)Math.Floor((need + pull) / MathHelper.TwoPi);                  // folded into [-pull, 2 pi - pull)
            return turnNow >= need;
        }

        // The sprite with its butt at 'butt' pointing along the lance. DarkLanceTuning.SweepSpriteRotation (degrees) turns the art if it
        // looks off; it is mirrored with the sprite so the same number works facing either way.
        void DrawLance(Texture2D texture, float scale, Color color, float centerScale = 0f, Vector2 offset = default)
        {
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            float SpritePivotX = 100f;   // texture pixels (unscaled)
            float SpritePivotY = 0f;
            origin += new Vector2(SpritePivotX * (Projectile.spriteDirection >= 0 ? 1f : -1f), SpritePivotY);
            float extra = MathHelper.ToRadians(DarkLanceTuning.SweepSpriteRotation) * (Projectile.spriteDirection >= 0 ? 1f : -1f);
            float rotation = Projectile.rotation + rotationOffset + extra;
            Vector2 position = butt + offset;
            if (centerScale > 0f && centerScale != scale)   // a glow copy grows around the middle of the sprite, not around the butt
            {
                Vector2 toCenter = texture.Size() * 0.5f - origin;
                position += toCenter.RotatedBy(rotation) * (centerScale - scale);
            }
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color, rotation, origin, scale, effects, 0);
        }

        // Every machine: the start ring, the whoosh, flecks and embers off the tip, the light and the line trails.
        void Visuals(Player owner, float t, Vector2 centerHand)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;

            if (age == 0)
                DarkLanceFx.SweepStart(owner.Center);
            if (!whooshed && age >= b1)
            {
                whooshed = true;
                DarkLanceFx.SweepWhoosh(tip);
                if (IsOwner)
                    CameraShake.Punch(owner.Center, DarkLanceTuning.SweepPunch, 8, "DarkLanceSweep");
            }
            if (Mode == 1 && age == b4)
            {
                DarkLanceFx.Primed(tip);   // the lance is charged: a click and a flash
                if (IsOwner)
                    CameraShake.Punch(tip, DarkLanceTuning.PrimedPunch, 8, "DarkLancePrimed");
            }

            if (speedFraction > 0.15f)
            {
                DarkLanceFx.Flecks(tip, tip - prevTip, speedFraction, palette);
                if (Main.rand.NextFloat() < DarkLanceTuning.SweepEmberChance * speedFraction)
                    DarkLanceEmbers.Shed(tip, (tip - prevTip) * DarkLanceTuning.EmberInherit * Main.rand.NextFloat(0.3f, 0.7f));
            }
            if (age >= b1)
                DarkLanceEmbers.TipCharge(tip, Gathered, age / 60f);   // the gathered orbs, a living blob on the tip
            if (inThrust && ExtensionAt(t) > 0.3f)
            {
                ThrustFx.TipStreak(tip, direction, palette);
                if (Main.rand.NextBool(2))
                    DarkLanceEmbers.Shed(tip - direction * 10f, -direction * 1.2f);
            }

            // the purple outline of the charged lance fades in while it waits and out when it is released
            outlineShown += ((holding ? 1f : 0f) - outlineShown) * MathHelper.Clamp(DarkLanceTuning.PrimedOutlineFade, 0.01f, 1f);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(MathHelper.TwoPi * age / Math.Max(1, DarkLanceTuning.PrimedPulseTicks));
            if (outlineShown > 0.05f && Main.rand.NextFloat() < 0.08f + 0.12f * outlineShown)
                DarkLanceEmbers.Shed(Vector2.Lerp(butt, tip, Main.rand.NextFloat()), new Vector2(0f, -0.3f));
            Lighting.AddLight(tip, palette.Mid.ToVector3() * (0.7f * Math.Max(speedFraction, Math.Max(0f, ExtensionAt(t))) + 0.5f * outlineShown * (0.6f + 0.4f * pulse)));

            // The line trails. The pose is a pure function of the time, so each tick is sampled several times: the circle stays round. The
            // player's own movement is spread over the samples too (the hand slides from last tick's place to this tick's), so a moving
            // player does not make the line jump from tick to tick.
            if (age == b1)
                sweepTrail = new PixelTrail(new DarkLanceStrikeTrailStyle(1f), IsOwner);
            if (age == b3)
            {
                DetachedTrails.Hand(sweepTrail);   // it keeps fading on its own
                sweepTrail = null;
            }
            if (vAge == tb4 && vAge != lastV)
                thrustTrail = new PixelTrail(new DarkLanceStrikeTrailStyle(DarkLanceTuning.StrikeTrailThrustScale), IsOwner);

            bool laySweep = sweepTrail != null && age >= b1 && age < b3 && speedFraction > DarkLanceTuning.StrikeTrailMinSpeed;
            bool layThrust = thrustTrail != null && vAge >= tb4 && vAge < tb5;
            if (laySweep || layThrust)
            {
                PixelTrail trail = laySweep ? sweepTrail : thrustTrail;
                int steps = Math.Max(1, DarkLanceTuning.StrikeTrailSubSteps);
                for (int i = 1; i <= steps; i++)
                {
                    float f = i / (float)steps;
                    float tt = t - 1f + f;
                    Pose(tt, HandAt(tt, Vector2.Lerp(prevCenterHand, centerHand, f)), out _, out Vector2 point, out _, out _);
                    trail.Add(point);
                }
            }
            sweepTrail?.Tick();
            thrustTrail?.Tick();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!hasPose || !striking)
                return false;
            float width = inThrust ? DarkLanceTuning.ThrustHitWidth * Size : DarkLanceTuning.SweepHitWidth;
            return SegmentHit.Sweep(targetHitbox, prevButt, prevTip, butt, tip, width * Projectile.scale, 3);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X >= Owner.Center.X ? 1 : -1; // always away from the player
            if (inThrust)
                modifiers.SourceDamage *= ThrustDamageRatio;   // the projectile's own damage is the sweep's; the thrust hits harder
        }

        // Owner only (OnHitNPC runs on the owner's client). The first enemy the thrust hits with any gathered orb blows up, once (the fewer
        // orbs, the smaller the blast); with a full ring it also throws falling flame embers.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ThrustFx.HitSpark(SegmentHit.ClosestPoint(butt, tip, target.Center), DarkLanceTuning.Palette);
            if (!inThrust || exploded || Gathered < 1)
                return;
            exploded = true;
            int thrustDamage = Math.Max(1, (int)(Projectile.damage * ThrustDamageRatio));
            int blastDamage = Math.Max(1, (int)(thrustDamage * DarkLanceTuning.BlastDamageMultiplier));
            Vector2 point = SegmentHit.ClosestPoint(butt, tip, target.Center);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), point, Vector2.Zero, ModContent.ProjectileType<DarkLanceBlastProjectile>(), blastDamage, Projectile.knockBack, Projectile.owner, Gathered);   // ai[0] = orbs: sets the size on every machine

            if (Gathered >= DarkLanceTuning.EmberBurstMinOrbs)
                SpawnFlameEmbers(target, Math.Max(1, (int)(blastDamage * DarkLanceTuning.FlameEmberDamageMultiplier)));
        }

        // 3-4 embers leave the enemy upward in a fan, then gravity takes them (DarkLanceFlameEmberProjectile).
        void SpawnFlameEmbers(NPC target, int damage)
        {
            int count = Main.rand.Next(Math.Max(1, DarkLanceTuning.FlameEmberMin), Math.Max(DarkLanceTuning.FlameEmberMin, DarkLanceTuning.FlameEmberMax) + 1);
            int type = ModContent.ProjectileType<DarkLanceFlameEmberProjectile>();
            for (int i = 0; i < count; i++)
            {
                // an even fan, each ember with a little random on top, so they never leave in one clump
                float fan = MathHelper.Lerp(-1f, 1f, (i + 0.5f) / count) + Main.rand.NextFloat(-0.2f, 0.2f);
                float angle = MathHelper.ToRadians(-90f + DarkLanceTuning.FlameEmberFanDegrees * fan);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(DarkLanceTuning.FlameEmberSpeedMin, DarkLanceTuning.FlameEmberSpeedMax);
                Vector2 position = target.Center + Main.rand.NextVector2Circular(target.width * 0.25f, target.height * 0.25f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, velocity, type, damage, Projectile.knockBack, Projectile.owner);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (IsOwner)
                Owner.GetModPlayer<ThrustPlayer>().StartCooldown(ItemID.DarkLance, DarkLanceTuning.StrikeCooldownTicks);
            DetachedTrails.Hand(sweepTrail);    // the lines keep fading on their own
            DetachedTrails.Hand(thrustTrail);
            sweepTrail = null;
            thrustTrail = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            sweepTrail?.Draw();
            thrustTrail?.Draw();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            ThrustPalette palette = DarkLanceTuning.Palette;
            float scale = Projectile.scale;
            float extension = Math.Max(0f, ExtensionAt(curT));
            float wave = 0.5f + 0.5f * (float)Math.Sin(MathHelper.TwoPi * age / Math.Max(1, DarkLanceTuning.PrimedPulseTicks));
            float glow = Math.Max(0.3f + 0.7f * Math.Max(speedFraction, Math.Min(1f, extension)), outlineShown * (0.45f + 0.4f * wave));

            // The charged outline, like the Muramasa's sheathed one: a soft glow behind the lance and the sprite repeated in the eight directions
            // around it. Both pulse; the colors have alpha 0 = additive.
            if (outlineShown > 0.01f)
            {
                float strength = outlineShown * MathHelper.Lerp(DarkLanceTuning.PrimedOutlineMin, DarkLanceTuning.PrimedOutlineMax, wave);
                DrawLance(texture, scale * DarkLanceTuning.PrimedGlowScale, DarkLanceTuning.PrimedOutlineColor * (0.4f * strength), scale);
                float width = DarkLanceTuning.PrimedOutlineWidth;
                for (int i = 0; i < outlineDirections.Length; i++)
                    DrawLance(texture, scale, DarkLanceTuning.PrimedOutlineColor * (0.9f * strength), 0f, outlineDirections[i] * width);
                for (int i = 0; i < 4; i++)
                    DrawLance(texture, scale, DarkLanceTuning.PrimedOutlineCoreColor * (0.5f * strength), 0f, outlineDirections[i] * (width * 0.5f));
            }

            DrawLance(texture, scale * 1.18f, palette.Halo * (0.45f * glow), scale);
            DrawLance(texture, scale * 1.07f, palette.Mid * (0.5f * glow), scale);
            DrawLance(texture, scale, Color.Lerp(lightColor, Color.White, 0.3f + 0.4f * glow));

            // a dark glow on the tip that swells while the lance is fast or carries gathered orbs
            float fill = Gathered / (float)Math.Max(1, DarkLanceTuning.MaxOrbs);
            float swell = MathHelper.Clamp(0.45f * speedFraction + 0.55f * fill, 0f, 1f);
            float radius = DarkLanceTuning.TipGlowMinRadius + (DarkLanceTuning.TipGlowMaxRadius - DarkLanceTuning.TipGlowMinRadius) * swell * (1f + 0.12f * outlineShown * wave);
            DarkLanceFx.DrawGlow(tip, radius, DarkLanceTuning.TipGlowStrength * (0.4f + 0.6f * Math.Max(speedFraction, fill)));
            return false;
        }
    }
}