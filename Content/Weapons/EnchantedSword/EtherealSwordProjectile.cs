using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The ethereal sword: a ghost's blade (drawn with the vanilla Enchanted Beam sprite) that floats around the enemy the third melee hit
    /// landed on and swings at it five times: up, down, thrust, up, down. It is ONE projectile with ONE pose function of time, so the
    /// sprite is drawn every frame and every swing starts exactly where the last one ended: nothing is respawned or switched between swings.
    /// ai[0] = the enemy index (-1 = none: the ghost leaves), ai[1] = side (its sign) and a random size (1..2), see EnchantedSwordPlayer.
    /// Multiplayer: every machine runs the same timeline from the spawn (the pose is a function of the tick count only), so nothing is
    /// synced per tick; only the owner damages and picks a new enemy when the old one dies (it tells the others through ai[0]).
    /// </summary>
    public class EtherealSwordProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.EnchantedBeam;

        enum Kind { Up, Down, Thrust }

        bool IsOwner => Projectile.owner == Main.myPlayer;

        bool started;
        int age;
        int leaveStart;                 // the age at which the ghost starts to leave: after the last swing, or earlier when it loses its enemy
        int side = 1;
        float tiltRad;
        float driftDir = 1f;
        Vector2 center;                 // where the enemy is (eased)
        float radius = 24f;             // half the enemy's size (eased)
        Vector2 hand, direction = Vector2.UnitX, hitTip, prevHand, prevHitTip;
        bool havePrev;
        bool striking, wasStriking;
        int swing = -1;
        int kind;
        SweepTrail trail;
        readonly RemoteHitTracker remote = new RemoteHitTracker();
        readonly Vector2[] armPoints = new Vector2[3];
        readonly float[] armT = { 0f, 0.5f, 1f };

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once per swing (the immunity is cleared at the start of each swing)
        }

        // ---- helpers ----

        static float Rad(float degrees)
        {
            return MathHelper.ToRadians(degrees);
        }

        static float Smooth(float p)
        {
            return MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(p, 0f, 1f));
        }

        static float EaseOut(float p)
        {
            return 1f - (float)Math.Pow(1f - MathHelper.Clamp(p, 0f, 1f), Math.Max(1f, EnchantedSwordTuning.StrikeEasePower));
        }

        float Alpha
        {
            get
            {
                float fadeIn = MathHelper.Clamp(age / Math.Max(1f, EnchantedSwordTuning.SpawnTicks * 0.7f), 0f, 1f);
                float fadeOut = 1f - MathHelper.Clamp((age - leaveStart) / (float)Math.Max(1, EnchantedSwordTuning.FadeOutTicks), 0f, 1f);
                return fadeIn * fadeOut;
            }
        }

        void ReadSeed()
        {
            float seed = Projectile.ai[1];
            side = seed >= 0f ? 1 : -1;
            float random = MathHelper.Clamp(Math.Abs(seed) - 1f, 0f, 1f);
            tiltRad = Rad(MathHelper.Lerp(-EnchantedSwordTuning.SideTiltDeg, EnchantedSwordTuning.SideTiltDeg, random));
            driftDir = ((int)(random * 1000f) % 2 == 0) ? 1f : -1f;
        }

        // ---- the pose: where the hand is and which way the blade points at 'time' (ticks since it appeared, may be fractional) ----
        // A swing is a function of u (0..1): up and down are wind-up, strike, settle; the thrust is pull-back, jab, recover. Every swing ends
        // where the next one starts (up ends high, down starts high and ends low, the thrust starts and ends low, up starts low), so the
        // whole sequence is one unbroken motion.
        void PoseAt(float time, out Vector2 handPos, out Vector2 bladeDir, out bool strike, out int swingIndex)
        {
            float t = Math.Min(time, leaveStart);
            int swingCount = Math.Max(1, EnchantedSwordTuning.GhostSwings);
            float s = MathHelper.Clamp((t - EnchantedSwordTuning.SpawnTicks) / Math.Max(1, EnchantedSwordTuning.SwingTicks), 0f, swingCount);
            swingIndex = Math.Min((int)s, swingCount - 1);
            float u = s - swingIndex;
            Kind swingKind = (Kind)(swingIndex % 3);

            float arc = Rad(EnchantedSwordTuning.SwingArcDeg);
            float wind = Rad(EnchantedSwordTuning.WindDeg);
            float over = Rad(EnchantedSwordTuning.OvershootDeg);
            float delta;
            float radial = 0f;
            strike = false;
            float p;

            if (swingKind == Kind.Thrust)
            {
                float windEnd = EnchantedSwordTuning.ThrustWindEnd;
                float strikeEnd = EnchantedSwordTuning.ThrustStrikeEnd;
                if (u < windEnd)
                {
                    p = Smooth(u / windEnd);
                    delta = MathHelper.Lerp(-arc, 0f, p);
                    radial = MathHelper.Lerp(0f, EnchantedSwordTuning.ThrustPullPx, p);
                }
                else if (u < strikeEnd)
                {
                    p = EaseOut((u - windEnd) / (strikeEnd - windEnd));
                    delta = 0f;
                    radial = MathHelper.Lerp(EnchantedSwordTuning.ThrustPullPx, -EnchantedSwordTuning.ThrustReachPx, p);
                    strike = true;
                }
                else
                {
                    p = Smooth((u - strikeEnd) / (1f - strikeEnd));
                    delta = MathHelper.Lerp(0f, -arc, p);
                    radial = MathHelper.Lerp(-EnchantedSwordTuning.ThrustReachPx, 0f, p);
                }
            }
            else
            {
                float from = swingKind == Kind.Up ? -arc : arc; // where the swing starts
                float to = -from;                               // and where it ends
                float travel = swingKind == Kind.Up ? 1f : -1f; // the way the blade moves
                float anticipateEnd = EnchantedSwordTuning.AnticipateEnd;
                float strikeEnd = EnchantedSwordTuning.StrikeEnd;
                if (u < anticipateEnd)
                {
                    p = Smooth(u / anticipateEnd);
                    delta = from - travel * wind * p;           // pulls back, against the way it is about to go
                }
                else if (u < strikeEnd)
                {
                    p = EaseOut((u - anticipateEnd) / (strikeEnd - anticipateEnd));
                    delta = MathHelper.Lerp(from - travel * wind, to + travel * over, p);
                    radial = -EnchantedSwordTuning.SweepLungePx * (float)Math.Sin(MathHelper.Pi * p);
                    strike = true;
                }
                else
                {
                    p = Smooth((u - strikeEnd) / (1f - strikeEnd));
                    delta = MathHelper.Lerp(to + travel * over, to, p);
                }
            }

            if (time >= leaveStart)
                strike = false;

            // The ghost glides in at the start and drifts out at the end.
            float arrive = 1f - Smooth(time / Math.Max(1f, EnchantedSwordTuning.SpawnTicks));
            float leave = Smooth((time - leaveStart) / Math.Max(1f, EnchantedSwordTuning.FadeOutTicks));
            radial += (arrive + leave) * EnchantedSwordTuning.SpawnRisePx;

            // It stands on one side of the enemy and slowly circles it.
            float phi = (side > 0 ? tiltRad : MathHelper.Pi - tiltRad) + driftDir * Rad(EnchantedSwordTuning.DriftDegPerSwing) * s;
            float distance = radius + EnchantedSwordTuning.HoverGap + radial;
            float bob = (float)Math.Sin(time * MathHelper.TwoPi / Math.Max(1, EnchantedSwordTuning.BobTicks)) * EnchantedSwordTuning.BobPx;
            handPos = center + phi.ToRotationVector2() * distance + new Vector2(0f, bob);

            // The blade points at the enemy; a raised tip is "up" on the screen from either side.
            float angle = phi + MathHelper.Pi + side * delta;
            bladeDir = angle.ToRotationVector2();
        }

        // ---- the enemy ----

        static bool Attackable(NPC npc)
        {
            return npc.active && npc.life > 0 && !npc.dontTakeDamage && !npc.friendly;
        }

        int FindNearest(Vector2 from)
        {
            float best = EnchantedSwordTuning.RetargetRange;
            int found = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!Attackable(npc) || !(npc.CanBeChasedBy(Projectile) || npc.type == NPCID.TargetDummy))
                    continue;
                float distance = Vector2.Distance(npc.Center, from);
                if (distance < best)
                {
                    best = distance;
                    found = i;
                }
            }
            return found;
        }

        void UpdateTarget()
        {
            int index = (int)Projectile.ai[0];
            NPC npc = index >= 0 && index < Main.maxNPCs ? Main.npc[index] : null;
            if (npc != null && Attackable(npc))
            {
                center = Vector2.Lerp(center, npc.Center, EnchantedSwordTuning.FollowEase);
                radius = MathHelper.Lerp(radius, Math.Max(npc.width, npc.height) * 0.5f, EnchantedSwordTuning.FollowEase);
                return;
            }
            if (age >= leaveStart)
                return;

            // Its enemy is gone: the owner picks the nearest other one (or none) and tells the others through ai[0].
            if (IsOwner)
            {
                int next = FindNearest(center);
                Projectile.ai[0] = next;
                Projectile.netUpdate = true;
                if (next < 0)
                    leaveStart = Math.Min(leaveStart, age);
            }
            else if (index < 0)
            {
                leaveStart = Math.Min(leaveStart, age);
            }
        }

        // ---- the tick ----

        public override void AI()
        {
            Projectile.timeLeft = 60;
            if (!Main.player[Projectile.owner].active)
            {
                Projectile.Kill();
                return;
            }

            if (!started)
            {
                int index = (int)Projectile.ai[0];
                if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].active)
                {
                    Projectile.Kill();
                    return;
                }
                started = true;
                ReadSeed();
                center = Main.npc[index].Center;
                radius = Math.Max(Main.npc[index].width, Main.npc[index].height) * 0.5f;
                leaveStart = EnchantedSwordTuning.SpawnTicks + Math.Max(1, EnchantedSwordTuning.GhostSwings) * Math.Max(1, EnchantedSwordTuning.SwingTicks);
                EnchantedSwordFx.Discharge(center);
                CameraShake.Punch(center, EnchantedSwordTuning.DischargeShake, 12);
            }

            age++;
            UpdateTarget();

            prevHand = hand;
            prevHitTip = hitTip;
            PoseAt(age, out hand, out direction, out striking, out int swingNow);
            hitTip = hand + direction * EnchantedSwordTuning.BladeLength * EnchantedSwordTuning.HitLengthScale;
            if (!havePrev)
            {
                prevHand = hand;
                prevHitTip = hitTip;
                havePrev = true;
            }

            Projectile.Center = hand;
            Projectile.rotation = direction.ToRotation();

            // A new swing: every enemy may be hit again, and the last strike's trail is let go to fade on its own.
            if (swingNow != swing)
            {
                swing = swingNow;
                kind = swing % 3;
                SegmentHit.ClearLocalImmunity(Projectile);
                DetachedTrails.Hand(trail);
                trail = null;
            }

            if (striking && !wasStriking)
                EnchantedSwordFx.PlaySwing(Vector2.Lerp(hand, hitTip, 0.5f), kind);
            wasStriking = striking;

            if (striking)
                LayTrail();
            trail?.Tick();

            // Other machines have no damage hook: they run the same hit test to show the hit effects.
            if (!IsOwner && striking)
                remote.Update(Projectile, swing, HitEffects);

            if (!Main.dedServ)
            {
                float alpha = Alpha;
                if (Main.rand.NextBool(2))
                    EnchantedSwordFx.Wisp(hand + direction * Main.rand.NextFloat(EnchantedSwordTuning.BladeLength), new Vector2(0f, -0.4f));
                Lighting.AddLight(Vector2.Lerp(hand, hitTip, 0.5f), 0.25f * alpha, 0.55f * alpha, 0.95f * alpha);
            }

            if (age >= leaveStart + EnchantedSwordTuning.FadeOutTicks)
                Projectile.Kill();
        }

        void LayTrail()
        {
            if (trail == null)
                trail = new SweepTrail(new EtherealSwordTrailStyle(), IsOwner);
            int samples = Math.Max(1, EnchantedSwordTuning.TrailSamplesPerTick);
            for (int i = 1; i <= samples; i++)
            {
                float time = age - 1f + i / (float)samples - EnchantedSwordTuning.TrailLagTicks;
                PoseAt(time, out Vector2 sampleHand, out Vector2 sampleDirection, out bool _, out int _);
                float length = EnchantedSwordTuning.BladeLength;
                trail.Add(sampleHand + sampleDirection * length * EnchantedSwordTuning.TrailOuter, sampleHand + sampleDirection * length * EnchantedSwordTuning.TrailInner);
            }
        }

        // ---- damage ----

        public override bool? CanDamage()
        {
            return striking ? (bool?)null : false;
        }

        // The blade is a line from the hand to the tip, swept from where it was last tick.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!striking)
                return false;
            return SegmentHit.Sweep(targetHitbox, prevHand, prevHitTip, hand, hitTip, EnchantedSwordTuning.HitWidth, EnchantedSwordTuning.SweepHitSteps);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > hand.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            HitEffects(target);
        }

        void HitEffects(NPC npc)
        {
            Vector2 point = Vector2.Clamp(SegmentHit.ClosestPoint(hand, hitTip, npc.Center), npc.Hitbox.TopLeft(), npc.Hitbox.BottomRight());
            bool thrust = kind == (int)Kind.Thrust;
            EnchantedSwordFx.GhostHit(point, thrust);
            if (thrust)
                CameraShake.Punch(point, EnchantedSwordTuning.ThrustShake, 10);
        }

        public override void OnKill(int timeLeft)
        {
            if (started)
                EnchantedSwordFx.Vanish(hand);
            DetachedTrails.Hand(trail);
            trail = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            if (!started || Main.dedServ)
                return false;

            trail?.Draw();

            float alpha = Alpha;
            DrawArm(alpha);

            Main.instance.LoadProjectile(ProjectileID.EnchantedBeam);
            Texture2D texture = TextureAssets.Projectile[ProjectileID.EnchantedBeam].Value;
            float grip = MathHelper.Clamp(EnchantedSwordTuning.GripFraction, 0f, 0.9f);
            float scale = EnchantedSwordTuning.BladeLength / Math.Max(1f, texture.Height * (1f - grip));
            float rotation = direction.ToRotation() + MathHelper.PiOver2; // the beam sprite points up
            Vector2 origin = new Vector2(texture.Width * 0.5f, texture.Height * (1f - grip));
            Vector2 position = hand - Main.screenPosition;

            // A soft glow that swells from the middle of the sprite.
            Vector2 middle = position + direction * (texture.Height * (0.5f - grip) * scale);
            Main.EntitySpriteDraw(texture, middle, null, EnchantedSwordTuning.BlueColor * (EnchantedSwordTuning.GlowAlpha * alpha), rotation,
                texture.Size() * 0.5f, scale * EnchantedSwordTuning.GlowScale, SpriteEffects.None, 0);

            // A pixel outline, then the sprite itself, a little see-through.
            float width = EnchantedSwordTuning.GhostOutlineWidth;
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.PiOver2 * i + MathHelper.PiOver4).ToRotationVector2() * width;
                Main.EntitySpriteDraw(texture, position + offset, null, EnchantedSwordTuning.CoreColor * (0.5f * alpha), rotation, origin, scale, SpriteEffects.None, 0);
            }
            Color body = new Color(205, 235, 255, 70) * (EnchantedSwordTuning.GhostAlpha * alpha);
            Main.EntitySpriteDraw(texture, position, null, body, rotation, origin, scale, SpriteEffects.None, 0);
            return false;
        }

        // The faint ghostly arm that trails from the hand, away from the enemy.
        void DrawArm(float alpha)
        {
            Vector2 away = (hand - center).SafeNormalize(Vector2.UnitX);
            Vector2 across = away.RotatedBy(MathHelper.PiOver2);
            float wobble = (float)Math.Sin(age * 0.35f) * 6f;
            float length = EnchantedSwordTuning.ArmLength;
            armPoints[0] = hand;
            armPoints[1] = hand + away * length * 0.5f + across * wobble;
            armPoints[2] = hand + away * length - across * wobble * 0.5f;
            float armWidth = EnchantedSwordTuning.ArmWidth;
            float armAlpha = EnchantedSwordTuning.ArmAlpha * alpha;
            PixelLine.Draw(armPoints, armT, 3, EnchantedSwordTuning.PixelSize,
                t => armWidth * (1f - 0.7f * t),
                t => EnchantedSwordTuning.BlueColor * (armAlpha * (1f - t)),
                true);
        }
    }
}