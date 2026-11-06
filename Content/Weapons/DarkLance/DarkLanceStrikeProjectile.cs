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
    /// SPAWN VALUES (the GlobalItem's Shoot passes them): ai[0] = spin direction (1 or -1) times (1 + gathered orbs), ai[1] = the thrust aim
    /// (starts as the base aim; the owner re-captures the cursor into it when the coil starts), ai[2] = the aim in the player's own frame.
    /// MULTIPLAYER: the owner's client decides the click, the aims, which orbs are gathered and every hit. Angle, extension, scale and sounds
    /// are pure functions of the age, so every machine poses and draws the same strike without per-tick packets. The gathered count (ai[0],
    /// updated with every gathered orb) and the thrust aim (ai[1], set once) travel with netUpdate; the gathered flag also rides on the orb
    /// (ai[2] + netUpdate). Other players' machines find the hits themselves and only show sparks (RemoteHitTracker). Trails are visual only.
    /// </summary>
    public class DarkLanceStrikeProjectile : ModProjectile
    {
        // The strike draws the vanilla PROJECTILE sprite (the long-handled one).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DarkLance;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int SpinDirection => Projectile.ai[0] >= 0f ? 1 : -1;
        int Gathered => Math.Min(DarkLanceTuning.MaxOrbs, Math.Max(0, (int)Math.Round(Math.Abs(Projectile.ai[0])) - 1));
        float ThrustAim => Projectile.ai[1];
        float BaseAim => Projectile.ai[2];
        float Size => 1f + DarkLanceTuning.SizePerOrb * Gathered;
        float ThrustDamageRatio => DarkLanceTuning.ThrustDamageMultiplier * (1f + DarkLanceTuning.DamagePerOrb * Gathered) / Math.Max(0.01f, DarkLanceTuning.SweepDamageMultiplier);

        // The phase lengths in ticks, read from the tuning every tick (hot reload), and the ages the phases end at.
        int pA, pS, pC, pW, pT, pH, pR;
        int b1, b2, b3, b4, b5, b6, b7;

        // Per-machine state: counters and visuals, never synced.
        int age;
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
            if (t > b3)
            {
                float end = BaseAim + SpinDirection * (MathHelper.TwoPi + DarkLanceTuning.SweepOvershoot);
                aim = end + MathHelper.WrapAngle(ThrustAim - end) * Smooth((t - b3) / pW);
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
            if (t <= b4)
                return -coilDraw * Smooth((t - b3) / pW);
            if (t <= b5)
            {
                float u = (t - b4) / pT;
                float eased = 1f - (float)Math.Pow(1f - u, Math.Max(1f, DarkLanceTuning.ThrustEasePower));
                return MathHelper.Lerp(-coilDraw, 1f + DarkLanceTuning.ThrustOvershoot, eased);   // shoots out past full reach ...
            }
            if (t <= b6)
                return MathHelper.Lerp(1f + DarkLanceTuning.ThrustOvershoot, 1f, Smooth((t - b5) / pH));   // ... and settles
            if (t <= b7)
                return 1f - Smooth((t - b6) / pR);
            return 0f;
        }

        // 0 during the sweep, 1 from the end of the coil: how much the lance has grown into its thrust shape (size, grip).
        float ThrustBlend(float t)
        {
            return t <= b3 ? 0f : Smooth((t - b3) / pW);
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
            if (age >= b7)
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
                if (age == b3)
                {
                    // The coil starts: the thrust goes where the cursor is NOW (or along the click, when ThrustRetarget is off).
                    Projectile.ai[1] = DarkLanceTuning.ThrustRetarget ? HeldAim.Capture(owner) : BaseAim;
                    Projectile.netUpdate = true;
                    owner.ChangeDir(HeldAim.Facing(Projectile.ai[1]));
                }
            }

            float t = age + 1f;
            float lastTurn = TurnAt(age);
            turnNow = TurnAt(t);

            Vector2 lastButt = butt;
            Vector2 lastTip = tip;
            // Like the Rotted Fork spin (and the vanilla Sleepy Octopod): no arm pose of our own. The item uses the Shoot use style and
            // vanilla picks the body frame (arm out, raised or lowered) from itemRotation, which is set below.
            Vector2 hand = owner.RotatedRelativePoint(owner.MountedCenter, false, true);
            Pose(t, hand, out butt, out tip, out direction, out float scale);
            Projectile.scale = scale;
            pivot = hand;
            Projectile.Center = owner.MountedCenter; // knockback pushes away from the player
            Projectile.rotation = direction.ToRotation();
            Projectile.spriteDirection = age >= b3 ? HeldAim.Facing(ThrustAim) : SpinDirection;
            owner.heldProj = Projectile.whoAmI;      // also ends the projectile when the player swaps items
            owner.SetDummyItemTime(2);               // keeps the item "in use" for as long as the strike lasts
            float aim = AimAt(t);
            float seenFromRight = owner.direction == 1 ? aim : MathHelper.Pi - aim; // the aim as a right-facing player would have it
            owner.itemRotation = MathHelper.WrapAngle(seenFromRight + MathHelper.PiOver4) * owner.direction;

            prevButt = hasPose ? lastButt : butt;
            prevTip = hasPose ? lastTip : tip;
            hasPose = true;

            // Speed: how fast the lance is turning (up to the end of the decel), and how fast the tip moves in the thrust.
            float angular = age < b3 ? Math.Abs(turnNow - lastTurn) / Math.Max(0.05f, DarkLanceTuning.SweepSpeedRef) : 0f;
            float linear = age >= b4 ? (tip - prevTip).Length() / Math.Max(1f, DarkLanceTuning.ThrustSpeedRef) : 0f;
            speedFraction = MathHelper.Clamp(Math.Max(angular, linear), 0f, 1f);

            bool sweepPhase = age >= b1 && age < b3;   // the whole turn, its acceleration and its overshoot
            inThrust = age >= b4 && age < b6;
            striking = (sweepPhase && speedFraction >= DarkLanceTuning.SweepMinHitSpeed) || inThrust;

            owner.GetModPlayer<DarkLancePlayer>().LanceTip = tip; // gathered orbs fly to it (every machine)

            if (age == b4)
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
                Visuals(owner, t, hand);

            // Other players' machines do not run OnHitNPC; they find the hit themselves and only show the sparks.
            if (!IsOwner && !Main.dedServ && striking)
                (remoteHits ??= new RemoteHitTracker()).Update(Projectile, inThrust ? 1 : 0, npc => ThrustFx.HitSpark(SegmentHit.ClosestPoint(butt, tip, npc.Center), DarkLanceTuning.Palette));

            age++;
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
                Projectile.ai[0] = SpinDirection * (1f + count);
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
        void DrawLance(Texture2D texture, float scale, Color color, float centerScale = 0f)
        {
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            float SpritePivotX = 100f;   // texture pixels (unscaled)
            float SpritePivotY = 0f;
            origin += new Vector2(SpritePivotX * (Projectile.spriteDirection >= 0 ? 1f : -1f), SpritePivotY);
            float extra = MathHelper.ToRadians(DarkLanceTuning.SweepSpriteRotation) * (Projectile.spriteDirection >= 0 ? 1f : -1f);
            float rotation = Projectile.rotation + rotationOffset + extra;
            Vector2 position = butt;
            if (centerScale > 0f && centerScale != scale)   // a glow copy grows around the middle of the sprite, not around the butt
            {
                Vector2 toCenter = texture.Size() * 0.5f - origin;
                position += toCenter.RotatedBy(rotation) * (centerScale - scale);
            }
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color, rotation, origin, scale, effects, 0);
        }

        // Every machine: the start ring, the whoosh, flecks and embers off the tip, the light and the line trails.
        void Visuals(Player owner, float t, Vector2 hand)
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
            Lighting.AddLight(tip, palette.Mid.ToVector3() * 0.7f * Math.Max(speedFraction, Math.Max(0f, ExtensionAt(t))));

            // The line trails. The pose is a pure function of the time, so each tick is sampled several times: the circle stays round.
            if (age == b1)
                sweepTrail = new PixelTrail(new DarkLanceStrikeTrailStyle(1f), IsOwner);
            if (age == b3)
            {
                DetachedTrails.Hand(sweepTrail);   // it keeps fading on its own
                sweepTrail = null;
            }
            if (age == b4)
                thrustTrail = new PixelTrail(new DarkLanceStrikeTrailStyle(DarkLanceTuning.StrikeTrailThrustScale), IsOwner);

            bool laySweep = sweepTrail != null && age >= b1 && age < b3 && speedFraction > DarkLanceTuning.StrikeTrailMinSpeed;
            bool layThrust = thrustTrail != null && age >= b4 && age < b5;
            if (laySweep || layThrust)
            {
                PixelTrail trail = laySweep ? sweepTrail : thrustTrail;
                int steps = Math.Max(1, DarkLanceTuning.StrikeTrailSubSteps);
                for (int i = 1; i <= steps; i++)
                {
                    Pose(t - 1f + i / (float)steps, hand, out _, out Vector2 point, out _, out _);
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
            float extension = Math.Max(0f, ExtensionAt(age));
            float glow = 0.3f + 0.7f * Math.Max(speedFraction, Math.Min(1f, extension));

            DrawLance(texture, scale * 1.18f, palette.Halo * (0.45f * glow), scale);
            DrawLance(texture, scale * 1.07f, palette.Mid * (0.5f * glow), scale);
            DrawLance(texture, scale, Color.Lerp(lightColor, Color.White, 0.3f + 0.4f * glow));

            // a dark glow on the tip that swells while the lance is fast or carries gathered orbs
            float fill = Gathered / (float)Math.Max(1, DarkLanceTuning.MaxOrbs);
            float swell = MathHelper.Clamp(0.45f * speedFraction + 0.55f * fill, 0f, 1f);
            float radius = DarkLanceTuning.TipGlowMinRadius + (DarkLanceTuning.TipGlowMaxRadius - DarkLanceTuning.TipGlowMinRadius) * swell;
            DarkLanceFx.DrawGlow(tip, radius, DarkLanceTuning.TipGlowStrength * (0.4f + 0.6f * Math.Max(speedFraction, fill)));
            return false;
        }
    }
}