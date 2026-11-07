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

namespace IsmailsWeaponReworks.Content.Weapons.NightsEdge
{
    /// <summary>
    /// The held Night's Edge. It draws the sword, poses the owner's arm and runs the left click swing, which is the Starfury's: SWING DOWN /
    /// SWING UP, a custom melee chop centered on the cursor: a slow heavy anticipation, a fast strike that decelerates, a short recover.
    /// Consecutive swings alternate down, up, down ... and each one starts where the previous one ended, so a held button flows.
    /// THE COMBO: every consecutive swing runs faster than the one before (the timeline advances by speedScale per tick) and a swing that
    /// HITS an enemy charges the sword by one step (once per swing): a soft purple glow grows with the charge, and when the ChargeSwings-th
    /// hitting swing lands the sword is fully charged: a flash, a ring of dark flame, and from then on a pulsing purple outline (the Dark
    /// Lance's, through Common/SpriteOutline), the fastest swing and the edge flames (NightsEdgeFlames: metaballs along the blade edge,
    /// clipped to Ismail's mask, and free flames that leave it). On up swings the sprite is mirrored across the blade so the sharp edge leads.
    /// An expanding sweep trail (SweepTrail) fills the area the blade crosses. The strike hits every enemy once.
    /// MULTIPLAYER: the owner's client decides everything (which swing it is, the combo level, where the arm starts, the aim, the speed) and
    /// passes what the other machines need in the spawn packet: ai[0] = phase (0 down, 1 up) + 10 * combo level, ai[1] = start arm angle
    /// (relative to the aim), ai[2] = the aim angle captured at the click (radians, the player's own frame), velocity X = the timeline
    /// speed, velocity Y = the blade tilt the swing starts from (both read on the first tick, then cleared). Every machine then runs the same
    /// timers for the pose, sounds, dust, glow and trail.
    /// The owner's client is the only one that sees the hit, so it tells the others ONCE through SendExtraAI (hitLanded): every machine then
    /// steps its glow and plays the charge-up from it. The flames are visual only and nothing about them is synced.
    /// Hits are detected by the owner's client like any projectile. No custom packets.
    /// </summary>
    public class NightsEdgeSwingProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.NightsEdge;

        public enum Phase { SwingDown = 0, SwingUp = 1 }

        bool IsUp => (int)Projectile.ai[0] % 10 == 1;
        int Level => (int)Projectile.ai[0] / 10;   // consecutive swings before this one
        float StartArm => Projectile.ai[1];
        float Aim => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        // A swing faces the side of its aim on every machine (so nobody's pose lags the synced direction).
        int Facing => Math.Cos(Aim) >= 0.0 ? 1 : -1;
        int MaxLevel => Math.Max(1, NightsEdgeTuning.ChargeSwings);
        bool Charged => Level >= MaxLevel;

        // Per-machine state: counters and visuals, never synced.
        int ticks;             // ticks this machine has run this projectile
        float timer;           // the swing's timeline (advances by speedScale per tick)
        float speedScale = 1f; // timeline ticks per real tick (the combo and melee attack speed), read from the spawn velocity on the first tick
        float armLocal;        // arm angle relative to the aim for a player facing right (positive = down)
        float tiltRad;         // blade tilt relative to the arm
        Vector2 tipPosition;
        Vector2 lastOwnerCenter; // where the owner was on the previous tick: the trail interpolates the owner's movement from it
        Vector2 prevCenter;      // the blade's hand position and angle on the previous tick: the hit test sweeps from them
        float prevRotation;
        SweepTrail trail;      // the fan behind the blade (null until a swing lays a point)
        float flash;           // 1 right after the charge-up, fades to 0
        float chargeShown;     // 0..1, the purple glow's strength: follows the combo smoothly
        float outlineShown;    // 0..1, the outline's fade in and out
        bool chargeUpDone;     // the swing that completes the charge has played its charge-up
        bool hitLanded;        // this swing has hit an enemy (the owner sets it, SendExtraAI tells the others): it adds one step of charge
        NightsEdgeFlameRig flames;     // the edge flames of this sword (created on first use: projectile instances are copies of one prototype)
        float startTilt;               // the blade tilt the swing starts from (read from the spawn velocity's Y on the first tick)

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
            Projectile.localNPCHitCooldown = -1; // every enemy once per swing
            Projectile.ownerHitCheck = true;
        }

        // ---- helpers ----

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        // The arm and the blade as world angles (in the player's own frame, see ApplyPose): relative to the aim and mirrored by the facing.
        float WorldArm(float arm)
        {
            return Aim + Facing * arm;
        }

        float WorldBlade(float arm, float tilt)
        {
            return Aim + Facing * (arm + tilt);
        }

        // The swing runs from one end of the arc to the other: down goes high to low, up goes low to high.
        float FromArm => Rad(IsUp ? NightsEdgeTuning.SwingLowDeg : NightsEdgeTuning.SwingHighDeg);
        float ToArm => Rad(IsUp ? NightsEdgeTuning.SwingHighDeg : NightsEdgeTuning.SwingLowDeg);
        float FromTilt => Rad(IsUp ? NightsEdgeTuning.TiltLowDeg : NightsEdgeTuning.TiltHighDeg);
        float ToTilt => Rad(IsUp ? NightsEdgeTuning.TiltHighDeg : NightsEdgeTuning.TiltLowDeg);

        // The swing is anticipation, strike, recover (see NightsEdgeTuning), all in timeline ticks. A swing that continues from the previous
        // one starts its anticipation from where the arm is; a fresh combo starts it from the rest pose.
        int StrikeStart => NightsEdgeTuning.AnticipateTicks;
        int StrikeEnd => StrikeStart + NightsEdgeTuning.SwingTicks;
        int TotalTicks => StrikeEnd + NightsEdgeTuning.RecoverTicks;

        // +1 when the arm angle grows during the swing (down), -1 when it shrinks (up).
        float Travel => IsUp ? -1f : 1f;

        // 0 until the strike starts, then eased from 0 to 1: fastest right after the anticipation, decelerating until the strike ends.
        float SwingProgress(float time)
        {
            float t = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, NightsEdgeTuning.SwingTicks), 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, Math.Max(1f, NightsEdgeTuning.SwingEasePower));
        }

        float SwingArmAt(float time)
        {
            float back = FromArm - Travel * Rad(NightsEdgeTuning.AnticipateDeg);   // pulled back, away from where the strike goes
            float over = ToArm + Travel * Rad(NightsEdgeTuning.OvershootDeg);      // carried a little past the end of the arc
            if (time < StrikeStart)
                return MathHelper.Lerp(StartArm, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(back, over, SwingProgress(time));
            return MathHelper.Lerp(over, ToArm, MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(1, NightsEdgeTuning.RecoverTicks)));
        }

        float SwingTiltAt(float time)
        {
            float back = FromTilt - Travel * Rad(NightsEdgeTuning.AnticipateTiltDeg);
            if (time < StrikeStart)
                return MathHelper.Lerp(startTilt, back, MathHelper.SmoothStep(0f, 1f, time / Math.Max(1, StrikeStart)));
            return MathHelper.Lerp(back, ToTilt, SwingProgress(time));
        }

        bool Damaging => timer >= StrikeStart && timer <= StrikeEnd;

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.NightsEdge)
            {
                Projectile.Kill();
                return;
            }

            if (ticks == 0)
            {
                // The spawn packet carries the timeline speed in the velocity's X (every machine reads the same value).
                speedScale = Projectile.velocity.X > 0.01f ? MathHelper.Clamp(Projectile.velocity.X, NightsEdgeTuning.SpeedScaleMin, NightsEdgeTuning.SpeedScaleMax) : 1f;
                startTilt = Projectile.velocity.Y;
                Projectile.velocity = Vector2.Zero;
            }

            if (timer >= TotalTicks)
            {
                owner.itemAnimation = 0; // the next use may start on the very next tick
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            if (ticks == 0 && IsOwner)
                owner.ChangeDir(Facing);

            Projectile.scale = NightsEdgeTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);

            prevCenter = Projectile.Center; // the previous tick's pose (the first tick fixes it below)
            prevRotation = Projectile.rotation;
            if (ticks == 0)
                lastOwnerCenter = owner.Center;

            armLocal = SwingArmAt(timer);
            tiltRad = SwingTiltAt(timer);

            owner.itemAnimation = 2; // keeps the item "in use" for as long as the sword is out
            owner.itemTime = 2;
            ApplyPose(owner);
            if (ticks == 0)
            {
                prevCenter = Projectile.Center;
                prevRotation = Projectile.rotation;
            }

            UpdateCharge();
            SwingEffects();
            FlameEffects();

            flash = Math.Max(0f, flash - 1f / Math.Max(1, NightsEdgeTuning.FlashTicks));
            trail?.Tick();
            lastOwnerCenter = owner.Center;
            ticks++;
            timer += speedScale;
        }

        // Every machine: the glow follows the combo, the swing that completes the charge plays the charge-up when it hits, and the outline
        // fades in once the sword is charged.
        void UpdateCharge()
        {
            bool completing = Level == MaxLevel - 1; // this swing is the one that fully charges the sword
            if (ticks == 0)
            {
                chargeShown = MathHelper.Clamp(Level / (float)MaxLevel, 0f, 1f);
                outlineShown = Charged ? 1f : 0f;
            }

            if (completing && !chargeUpDone && hitLanded)
            {
                chargeUpDone = true;
                flash = 1f;
                NightsEdgeFx.ChargeUp(tipPosition);
                NightsEdgeFx.PlayChargeUp(tipPosition);
                CameraShake.Punch(tipPosition, NightsEdgeTuning.ChargeUpPunch, 12);
            }

            float target = MathHelper.Clamp((Level + (hitLanded ? 1 : 0)) / (float)MaxLevel, 0f, 1f);
            chargeShown = MathHelper.Lerp(chargeShown, target, MathHelper.Clamp(NightsEdgeTuning.ChargeGlowFollow, 0.01f, 1f));
            float outlineTarget = Charged || chargeUpDone ? 1f : 0f;
            outlineShown += (outlineTarget - outlineShown) * MathHelper.Clamp(NightsEdgeTuning.OutlineFade, 0.01f, 1f);

            if (chargeShown > 0.05f && Main.rand.NextFloat() < 0.1f + 0.4f * chargeShown)
                NightsEdgeFx.ChargeSparkle(Projectile.Center, tipPosition);
        }

        void SwingEffects()
        {
            float previous = timer - speedScale; // the timeline time of the previous tick

            if (previous < StrikeStart && timer >= StrikeStart)
                NightsEdgeFx.PlaySwing(tipPosition, IsUp, Charged);

            // Many points per tick, laid between the previous tick's pose and this one, so the pixel line follows a smooth arc. Only the
            // part of this tick's time that lies inside the strike counts.
            float from = Math.Max(previous, StrikeStart);
            float to = Math.Min(timer, StrikeEnd);
            if (to > from)
            {
                int samples = Math.Max(1, (int)Math.Ceiling(NightsEdgeTuning.TrailSamplesPerTick * (to - from)));
                for (int k = 0; k < samples; k++)
                {
                    float time = from + (to - from) * (k + 1f) / samples;
                    float fraction = MathHelper.Clamp((time - previous) / Math.Max(0.01f, speedScale), 0f, 1f);
                    AddTrailSample(time, fraction);
                }
                NightsEdgeFx.SlashDust(tipPosition, Charged);
                Lighting.AddLight(tipPosition, 0.55f, 0.2f, 0.9f);
            }
        }

        // 'fraction' is how far between the previous tick and this one the sample is (0..1): the owner is placed where they were at that
        // moment, so the trail stays one connected line while the player moves during the swing.
        void AddTrailSample(float time, float fraction)
        {
            if (Main.dedServ)
                return;

            time -= NightsEdgeTuning.TrailLagTicks * speedScale;
            float arm = SwingArmAt(time);
            float tilt = SwingTiltAt(time);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, Owner.Center, fraction) - Owner.Center;
            Vector2 hand = HeldSwordPose.HandWorld(Owner, WorldArm(arm)) + shift;
            Vector2 direction = HeldSwordPose.BladeDirectionWorld(Owner, WorldBlade(arm, tilt));
            float length = NightsEdgeTuning.BladeLength * Projectile.scale * 0.9f;
            // The fan starts as a line at the tip and its inner edge slides down the blade toward the hand while the swing builds up.
            float expand = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(SwingProgress(time) / Math.Max(0.01f, NightsEdgeTuning.TrailExpandBy), 0f, 1f));
            float outer = NightsEdgeTuning.TrailOuter;
            float inner = MathHelper.Lerp(outer, NightsEdgeTuning.TrailInner, expand);
            trail ??= new SweepTrail(new NightsEdgeSweepTrailStyle(MathHelper.Clamp(Level / (float)MaxLevel, 0f, 1f)), IsOwner);
            // 'along' = where in the strike this sample is; the style (TailFirst) makes the tail live shorter, so it fades out first.
            float along = MathHelper.Clamp((time - StrikeStart) / Math.Max(1, NightsEdgeTuning.SwingTicks), 0f, 1f);
            trail.Add(hand + direction * length * outer, hand + direction * length * inner, along);
        }

        // Moves the owner's arm and puts the projectile in the hand; the blade points along Projectile.rotation.
        void ApplyPose(Player owner)
        {
            float worldArm = WorldArm(armLocal);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, worldArm - MathHelper.PiOver2);
            Vector2 hand = HeldSwordPose.HandWorld(owner, worldArm);
            float blade = WorldBlade(armLocal, tiltRad);
            if (owner.gravDir == -1f)
                blade = -blade;
            Projectile.rotation = blade;
            Projectile.Center = hand;
            Projectile.spriteDirection = Facing;
            tipPosition = hand + blade.ToRotationVector2() * NightsEdgeTuning.BladeLength * Projectile.scale;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
        }

        // ---- the edge flames (visual only, every machine) ----

        // The flames grow in over the last combo steps (they follow the glow, so they grow smoothly). The edge layer is private to this
        // player and clipped to the mask; the swing's PreDraw draws it in front of the sword.
        void FlameEffects()
        {
            if (Main.dedServ)
                return;
            flames ??= new NightsEdgeFlameRig();
            flames.Tick(Projectile, IsUp, NightsEdgeFlames.Strength(chargeShown, MaxLevel), tipPosition);
        }

        // ---- sync ----

        // One flag: the owner saw this swing hit something (netUpdate is raised once, at that hit).
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(hitLanded);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            if (reader.ReadBoolean())
                hitLanded = true;
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return Damaging ? (bool?)null : false;
        }

        // The blade is a line from the hand to the tip.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Damaging)
                return false;
            return BladeTouches(targetHitbox, NightsEdgeTuning.HitWidth * Projectile.scale);
        }

        // The strike is fast, so the test sweeps: it checks the line at a few poses between the previous tick and this one, and a thin
        // enemy cannot slip through between two ticks.
        bool BladeTouches(Rectangle box, float width)
        {
            int steps = Math.Max(1, NightsEdgeTuning.SweepHitSteps);
            float length = NightsEdgeTuning.BladeLength * NightsEdgeTuning.HitLengthScale * Projectile.scale;
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

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Owner.Center.X ? 1 : -1;
        }

        // Owner's client only: sparks, a sound and a small camera punch on a true hit.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            NightsEdgeFx.HitSpark(target.Center);
            NightsEdgeFx.PlayHit(target.Center);
            CameraShake.Punch(target.Center, NightsEdgeTuning.HitShake, 8);

            // The first enemy this swing hits charges the sword by one step (once per swing, however many enemies it hits).
            if (!hitLanded)
            {
                hitLanded = true;
                Projectile.netUpdate = true; // the other machines hear about it through SendExtraAI
                Owner.GetModPlayer<NightsEdgePlayer>().Charge = Math.Min(MaxLevel, Level + 1);
            }
        }

        // The trail outlives the swing and keeps fading on its own (DetachedTrails). The owner remembers how this swing ended, so the
        // next one continues from there and alternates.
        public override void OnKill(int timeLeft)
        {
            if (IsOwner)
            {
                NightsEdgePlayer state = Owner.GetModPlayer<NightsEdgePlayer>();
                state.LastPhase = IsUp ? (int)Phase.SwingUp : (int)Phase.SwingDown;
                state.LastEndArm = armLocal;
                state.LastEndWorldArm = WorldArm(armLocal);
                state.LastEndWorldBlade = WorldBlade(armLocal, tiltRad);
                state.LastEndTick = Main.GameUpdateCount;
            }
            DetachedTrails.Hand(trail);
            trail = null;
        }

        // ---- drawing ----

        // One copy of the sword sprite (hilt at the hand, along the blade). centerScale above 0 grows the copy around the MIDDLE of the sprite
        // instead of the hilt (the glow and outline copies), centerScale being the normal scale. GripOffset moves the picture back along the
        // blade (draw only).
        void DrawSword(Texture2D texture, float scale, Color color, float centerScale = 0f, Vector2 offset = default)
        {
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects, IsUp);
            float rotation = Projectile.rotation + rotationOffset;
            Vector2 position = Projectile.Center + offset + HeldSwordPose.GripShift(Projectile.rotation, NightsEdgeTuning.GripOffset, Projectile.scale);
            if (centerScale > 0f && centerScale != scale)
            {
                Vector2 toCenter = texture.Size() * 0.5f - origin;
                position += toCenter.RotatedBy(rotation) * (centerScale - scale);
            }
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color, rotation, origin, scale, effects, 0);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float scale = Projectile.scale;
            float wave = 0.5f + 0.5f * (float)Math.Sin(MathHelper.TwoPi * (Main.GameUpdateCount % (uint)Math.Max(1, NightsEdgeTuning.OutlinePulseTicks)) / Math.Max(1, NightsEdgeTuning.OutlinePulseTicks));

            // The charge: a purple glow around the middle of the blade that grows with every swing of the combo.
            if (chargeShown > 0.02f)
            {
                float alpha = NightsEdgeTuning.ChargeGlowAlpha * chargeShown * (0.8f + 0.2f * wave);
                DrawSword(texture, scale * (1.3f + 0.1f * chargeShown), NightsEdgeTuning.ChargeGlowColor * (0.6f * alpha), scale);
                DrawSword(texture, scale * 1.12f, NightsEdgeTuning.ChargeGlowColor * (0.8f * alpha), scale);
            }

            // Fully charged: the pulsing outline, like the Dark Lance's primed one.
            if (outlineShown > 0.01f)
            {
                float strength = outlineShown * MathHelper.Lerp(NightsEdgeTuning.OutlineMin, NightsEdgeTuning.OutlineMax, wave);
                SpriteOutline.Draw(strength, NightsEdgeTuning.OutlineWidth, NightsEdgeTuning.OutlineGlowScale, scale, NightsEdgeTuning.OutlineColor, NightsEdgeTuning.OutlineCoreColor,
                    (color, copyScale, centerScale, offset) => DrawSword(texture, copyScale, color, centerScale, offset));
            }

            DrawSword(texture, scale, Color.Lerp(lightColor, Color.White, 0.35f));

            // The edge flames, in front of the sword (the layer is clipped to the mask and drawn once per frame).
            MetaballLayers.Of<NightsEdgeEdgeFlameStyle>(Projectile.owner).DrawInBatch();

            if (flash > 0.01f)
                DrawSword(texture, scale, NightsEdgeTuning.OutlineCoreColor * (0.8f * flash));
            return false;
        }
    }
}