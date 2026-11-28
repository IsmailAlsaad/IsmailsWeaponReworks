using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Boomerangs.CombatWrench
{
    /// <summary>
    /// The Combat Wrench's heavy swing (W4). When a reeled-in enemy has arrived in front of the player and the wrench is in the hand, the
    /// owner's CombatWrenchProjectile spawns this held projectile and ends. It holds the enemy at the stop point (CarriedNPC.Hold, every
    /// machine, every tick) and runs a fixed timeline: WINDUP (the arm goes back and up, the wrench grows into the grip), STRIKE (a fast
    /// overhead-to-forward hammer arc that speeds up into the hit, with a SweepTrail fan), HIT (the tick the arm crosses the line to the
    /// enemy: heavy hit sound, camera punch, sparks, the enemy is released and flung away from the player) and RECOVER (a placeholder until
    /// W5: the arm eases down and the swing ends).
    /// ai[0] = the held enemy's index plus 1, ai[1] = the world angle from the player to the stop point (the reel line), ai[2] = the arm the
    /// swing starts from, relative to its own aim (so the wrench's hand-over pose continues without a pop).
    /// MULTIPLAYER: the owner's client decides when the swing starts (it spawns the projectile, so the spawn packet carries ai[0..2]). After
    /// that every machine, the server included, runs the same timeline from its own tick count: the pose, the hold, the sounds, dust, trail,
    /// shake and the release are a pure function of the ticks, nothing else is synced. The damage is the owner's, through the normal hit path
    /// (the projectile is damaging for the hit tick and the one after it, and only the held enemy can be hit). The launch velocity is set by the
    /// server (or single player) on the hit tick, the way the Rotted Fork flings its impaled enemy, so no custom packets.
    /// </summary>
    public class CombatWrenchSwingProjectile : ModProjectile
    {
        // Vanilla art until a CombatWrenchProjectile.png exists (the same sprite the thrown wrench uses).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CombatWrench;

        int TargetIndex => (int)Projectile.ai[0] - 1;
        float ReelAngle => Projectile.ai[1];
        float StartLocal => Projectile.ai[2];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int Facing => HeldAim.Facing(ReelAngle);

        // Per-machine state: counters and visuals, never synced.
        int ticks;               // ticks this machine has run this projectile
        float timer;             // ticks along the timeline
        int targetType = -1;     // the held enemy's type, so a recycled npc slot is never mistaken for it
        Vector2 heldCenter;      // where the held enemy is (it only moves through open air)
        bool struck;             // the hit tick has passed on this machine
        int armed;               // owner: the swing may damage while this is above 0
        float flash;             // 1 right after the hit, fades to 0
        Vector2 hand;            // the hand this tick
        Vector2 tip;             // the tip of the wrench's head this tick
        Vector2 lastOwnerCenter; // the trail interpolates the owner's movement from it
        SweepTrail trail;

        // True once this machine has run the swing for a tick: the wrench projectile ends when it sees this.
        public bool Started => ticks > 0;

        // ---- timeline ----

        static int StrikeEnd => CombatWrenchTuning.WindupTicks + Math.Max(1, CombatWrenchTuning.StrikeTicks);
        static int RecoverEnd => StrikeEnd + Math.Max(1, CombatWrenchTuning.RecoverTicks);

        // 0 until the strike starts, then 0..1: slow at first and faster and faster into the hit (progress = t ^ StrikeEasePower).
        static float StrikeProgress(float time)
        {
            float t = MathHelper.Clamp((time - CombatWrenchTuning.WindupTicks) / Math.Max(1f, CombatWrenchTuning.StrikeTicks), 0f, 1f);
            return (float)Math.Pow(t, Math.Max(1f, CombatWrenchTuning.StrikeEasePower));
        }

        // The tick the arm crosses the line to the enemy (where the head meets it): worked out from the arc and the ease, no tuning of its own.
        static int HitTick
        {
            get
            {
                float start = Math.Max(1f, CombatWrenchTuning.ArmStartDeg);
                float end = Math.Max(0f, CombatWrenchTuning.ArmEndDeg);
                float fraction = start / (start + end);
                float t = (float)Math.Pow(fraction, 1.0 / Math.Max(1f, CombatWrenchTuning.StrikeEasePower));
                return CombatWrenchTuning.WindupTicks + Math.Max(1, (int)Math.Ceiling(t * Math.Max(1, CombatWrenchTuning.StrikeTicks)));
            }
        }

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        // The arm relative to the line to the enemy for a player facing right (negative = raised back and up, positive = down).
        static float ArmAt(float time, float startLocal)
        {
            int windup = Math.Max(1, CombatWrenchTuning.WindupTicks);
            float back = -Rad(CombatWrenchTuning.ArmStartDeg);
            float end = Rad(CombatWrenchTuning.ArmEndDeg);
            if (time < windup)
                return MathHelper.Lerp(startLocal, back, MathHelper.SmoothStep(0f, 1f, time / windup));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(back, end, StrikeProgress(time));
            return MathHelper.Lerp(end, Rad(CombatWrenchTuning.RestArmDeg), MathHelper.SmoothStep(0f, 1f, (time - StrikeEnd) / Math.Max(1, CombatWrenchTuning.RecoverTicks)));
        }

        // How far the head leans past the arm: over the shoulder in the wind-up, along the arm in the strike.
        static float TiltAt(float time)
        {
            int windup = Math.Max(1, CombatWrenchTuning.WindupTicks);
            float lean = Rad(CombatWrenchTuning.WindupTiltDeg);
            if (time < windup)
                return MathHelper.Lerp(0f, lean, MathHelper.SmoothStep(0f, 1f, time / windup));
            if (time <= StrikeEnd)
                return MathHelper.Lerp(lean, 0f, StrikeProgress(time));
            return 0f;
        }

        // The wrench grows into the grip during the wind-up (at the hand-over its center is at the hand).
        static float GripAt(float time)
        {
            int windup = Math.Max(1, CombatWrenchTuning.WindupTicks);
            return CombatWrenchTuning.GripOffsetPx * MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(time / windup, 0f, 1f));
        }

        // ---- pose ----

        static float Tilt(Player owner)
        {
            return owner.gravDir >= 0f ? owner.fullRotation : 0f;
        }

        float AimArm(Player owner)
        {
            return HeldAim.ToWorld(owner, ReelAngle) - Tilt(owner);
        }

        Vector2 HandAt(Player owner, float worldArm)
        {
            Vector2 handPos = HeldSwordPose.HandWorld(owner, worldArm);
            float tilt = Tilt(owner);
            if (tilt != 0f)
            {
                // HandWorld does not know the tilt (a minecart, a mount): turn the hand with the player, about the player's own rotation point.
                Vector2 pivot = owner.position + owner.fullRotationOrigin + new Vector2(0f, owner.gfxOffY);
                handPos = pivot + (handPos - pivot).RotatedBy(tilt);
            }
            return handPos;
        }

        // The hand, the direction the head points and the arm (player frame) at a point of the timeline.
        void PoseAt(Player owner, float time, out Vector2 handPos, out Vector2 bladeDirection, out float worldArm)
        {
            float armLocal = ArmAt(time, StartLocal);
            float tilt = TiltAt(time);
            float aim = AimArm(owner);
            worldArm = aim + Facing * armLocal;
            handPos = HandAt(owner, worldArm);
            bladeDirection = HeldSwordPose.BladeDirectionWorld(owner, aim + Facing * (armLocal + tilt)).RotatedBy(Tilt(owner));
        }

        // The head's direction from the sprite's center at rotation 0 (mirrored when the player faces left, as the thrown wrench is).
        float HeadAngleRad()
        {
            float angle = MathHelper.ToRadians(CombatWrenchTuning.HeadAngleDeg);
            return Facing < 0 ? MathHelper.Pi - angle : angle;
        }

        // ---- the held enemy ----

        // The held enemy, or null when it is gone (dead, despawned, or the slot now holds another type).
        NPC Target()
        {
            int index = TargetIndex;
            if (index < 0 || index >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[index];
            return npc.active && npc.type == targetType ? npc : null;
        }

        // Every machine, every tick until the hit: the enemy stays at the stop point in front of the player, through open air only.
        void HoldTarget(Player owner, NPC target)
        {
            Vector2 stop = owner.MountedCenter + ReelAngle.ToRotationVector2() * CombatWrenchTuning.SwingDistancePx;
            heldCenter = CarriedNPC.ClampToOpenAir(heldCenter, stop, target);
            CarriedNPC.Hold(target, heldCenter);
        }

        // ---- AI ----

        public override void SetDefaults()
        {
            Projectile.width = CombatWrenchTuning.HitboxSize;
            Projectile.height = CombatWrenchTuning.HitboxSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;     // CanDamage decides: only the hit ticks hurt
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;   // the held enemy once
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the wrench out of their hand, or when they get crowd-controlled (a stun or a freeze).
            // The held enemy is let go by itself a moment later (its carry stamp runs out).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.CombatWrench)
            {
                Projectile.Kill();
                return;
            }

            if (timer >= RecoverEnd)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 120;
            Projectile.velocity = Vector2.Zero;
            if (armed > 0)
                armed--;

            if (ticks == 0)
            {
                lastOwnerCenter = owner.Center;
                NPC first = TargetIndex >= 0 && TargetIndex < Main.maxNPCs && Main.npc[TargetIndex].active ? Main.npc[TargetIndex] : null;
                targetType = first != null ? first.type : -1;
                heldCenter = first != null ? first.Center : owner.MountedCenter + ReelAngle.ToRotationVector2() * CombatWrenchTuning.SwingDistancePx;
            }

            owner.ChangeDir(Facing);   // every tick, on every machine: the player faces the enemy

            // The pose: the arm, the hand, the wrench in it.
            PoseAt(owner, timer, out hand, out Vector2 bladeDirection, out float worldArm);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, worldArm - MathHelper.PiOver2);
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
            float grip = GripAt(timer);
            Projectile.Center = hand + bladeDirection * grip;
            Projectile.rotation = bladeDirection.ToRotation() - HeadAngleRad();
            tip = hand + bladeDirection * (grip + CombatWrenchTuning.HeadReachPx);

            NPC target = Target();
            if (!struck && target != null)
                HoldTarget(owner, target);

            int t = (int)timer;
            if (t == CombatWrenchTuning.WindupTicks)
                CombatWrenchFx.Swing(Projectile.Center, ReelAngle.ToRotationVector2());
            if (t >= CombatWrenchTuning.WindupTicks && t < StrikeEnd)
            {
                LayTrail(owner);
                Lighting.AddLight(tip, 0.45f, 0.5f, 0.6f);
            }
            if (!struck && t == HitTick)
                Strike(owner, target);

            flash = Math.Max(0f, flash - 0.12f);
            trail?.Tick();
            lastOwnerCenter = owner.Center;
            ticks++;
            timer++;
        }

        // The hit tick, on every machine: the effects, the release and the launch; the owner also arms the damage.
        void Strike(Player owner, NPC target)
        {
            struck = true;
            flash = 1f;
            Vector2 away = ReelAngle.ToRotationVector2();
            Vector2 contact = target != null ? Vector2.Clamp(tip, target.Hitbox.TopLeft(), target.Hitbox.BottomRight()) : tip;
            CombatWrenchFx.HeavyHit(contact, away);

            if (target != null)
            {
                // The enemy is let go (its AI is back on this tick, so the velocity given next is kept) and flung away from the player.
                // Server (or single player) only: it sets the velocity and syncs it.
                CarriedNPC.Release(target);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 launch = away + new Vector2(0f, -CombatWrenchTuning.LaunchLift * (owner.gravDir >= 0f ? 1f : -1f));
                    target.velocity = Vector2.Normalize(launch) * CombatWrenchTuning.LaunchSpeed * target.knockBackResist;
                    target.netUpdate = true;
                }
            }

            if (IsOwner && target != null)
                armed = 2;   // the hit tick and the next one (the enemy can only be hit once)
        }

        // Many points per tick, laid between the previous tick's pose and this one, so the fan follows a smooth arc.
        void LayTrail(Player owner)
        {
            if (Main.dedServ)
                return;
            int samples = Math.Max(1, CombatWrenchTuning.TrailSamplesPerTick);
            for (int k = 0; k < samples; k++)
            {
                float fraction = (k + 1f) / samples;
                AddTrailSample(owner, timer - 1f + fraction, fraction);
            }
        }

        // 'fraction' is how far between the previous tick and this one the sample is: the owner is placed where they were at that moment,
        // so the fan stays one connected shape while the player moves.
        void AddTrailSample(Player owner, float time, float fraction)
        {
            time -= CombatWrenchTuning.TrailLagTicks;
            PoseAt(owner, time, out Vector2 handAt, out Vector2 bladeDirection, out float unusedArm);
            Vector2 shift = Vector2.Lerp(lastOwnerCenter, owner.Center, fraction) - owner.Center;
            float length = CombatWrenchTuning.GripOffsetPx + CombatWrenchTuning.HeadReachPx;
            float outer = CombatWrenchTuning.TrailOuter;
            float inner = PixelFan.Expand(StrikeProgress(time), CombatWrenchTuning.TrailExpandBy, CombatWrenchTuning.TrailInner, outer);
            trail ??= new SweepTrail(new CombatWrenchSweepTrailStyle(), IsOwner);
            float along = MathHelper.Clamp((time - CombatWrenchTuning.WindupTicks) / Math.Max(1f, CombatWrenchTuning.StrikeTicks), 0f, 1f);
            trail.AddBlade(handAt + shift, bladeDirection, length, outer, inner, along);
        }

        // ---- hits ----

        // Only the hit ticks hurt, and only the held enemy; the hit itself is scripted (the enemy is right there), so there is no touch test.
        public override bool? CanDamage()
        {
            return armed > 0 ? (bool?)null : false;
        }

        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == TargetIndex ? (bool?)null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return armed > 0;
        }

        // The heavy hit: the damage multiplier; no knockback here (the launch is the swing's own, set on the hit tick).
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= CombatWrenchTuning.HitDamageMultiplier;
            modifiers.Knockback *= 0f;
            modifiers.HitDirectionOverride = Math.Cos(ReelAngle) >= 0.0 ? 1 : -1;
        }

        // The fan outlives the swing and keeps fading on its own (DetachedTrails).
        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            if (ticks == 0)
                return false; // not posed yet (the first tick has not run)

            trail?.Draw();

            Texture2D texture = CombatWrenchProjectile.WrenchTexture();
            Color light = Lighting.GetColor((int)(Projectile.Center.X / 16f), (int)(Projectile.Center.Y / 16f));
            SpriteEffects effects = Facing < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, position, null, light, Projectile.rotation, origin, 1f, effects, 0);
            if (flash > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, Color.White * (0.6f * flash), Projectile.rotation, origin, 1f + 0.25f * flash, effects, 0);

            if (CombatWrenchTuning.ShowHitboxDebug)
                DrawDebug();
            return false;
        }

        // CombatWrenchTuning.ShowHitboxDebug: a magenta line from the hand to the head's tip and a cyan dot at the stop point.
        void DrawDebug()
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float length = Vector2.Distance(hand, tip);
            int dots = Math.Max(2, (int)(length / 2f));
            for (int i = 0; i <= dots; i++)
            {
                Vector2 point = Vector2.Lerp(hand, tip, i / (float)dots) - Main.screenPosition;
                Main.spriteBatch.Draw(pixel, point, new Rectangle(0, 0, 1, 1), Color.Magenta, 0f, new Vector2(0.5f), new Vector2(2f), SpriteEffects.None, 0f);
            }
            Vector2 stop = Owner.MountedCenter + ReelAngle.ToRotationVector2() * CombatWrenchTuning.SwingDistancePx - Main.screenPosition;
            Main.spriteBatch.Draw(pixel, stop, new Rectangle(0, 0, 1, 1), Color.Cyan, 0f, new Vector2(0.5f), new Vector2(4f), SpriteEffects.None, 0f);
        }
    }
}