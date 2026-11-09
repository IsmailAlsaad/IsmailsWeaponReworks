using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Content.Weapons.EnchantedBoomerang;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.EnchantedSword
{
    /// <summary>
    /// The ghost sword the thrown ethereal beam (EtherealBeamProjectile) conjures where it hits an enemy. It is the beam's own sword (the vanilla
    /// Enchanted Beam sprite) and it does NOT stop: it keeps flying through the enemy along the beam's line of flight while it SPINS
    /// (GhostSpinTurns full turns, hitting the enemy as it goes), until its middle is past the enemy's far edge. The spin goes on, slowing down,
    /// until the tip points back at the enemy, and right then the blade drives itself into the enemy from behind (the last strike of the old
    /// 5-strike combo, the 4 slashes before it are gone), and the moment it lands it explodes into a stardust burst (the Enchanted
    /// Boomerang's) and is gone.
    /// THE WHOLE SEQUENCE IS ONE POSE FUNCTION OF THE PROJECTILE'S AGE (Pose, Spin): one projectile, one continuous motion, so nothing flickers
    /// or stops between the beam, the spin and the stab (the spin starts from rest at the beam's own angle, the pass starts at the beam's own
    /// position, the turn slows down while the drive speeds up).
    /// MULTIPLAYER: the owner spawns it (EtherealSwordProjectile.Spawn) AT THE BEAM'S TIP with ai[0] = the enemy's index, ai[1] = the approach
    /// angle (from the enemy back toward where the beam came from, so the flight direction is the opposite) and ai[2] = the spin direction
    /// (+1 or -1); these and the spawn position arrive with the spawn, so every machine plays the same sequence from the same age and the
    /// enemy's synced position, and nothing is synced per tick. Only a retarget (the enemy died before the stab, the owner picks the nearest
    /// other enemy, or -1 = none, the ghost leaves) is a netUpdate of ai[0]. Damage is the owner's alone; the other machines get hit sparks
    /// through RemoteHitTracker. No custom packets.
    /// </summary>
    public class EtherealSwordProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.EnchantedBeam;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        int Target => (int)Projectile.ai[0];
        float Phi0 => Projectile.ai[1];
        float Dir => Projectile.ai[2] >= 0f ? 1f : -1f;

        // ---- timeline (ticks of age) ----
        // pass (the spinning blade flies through the enemy; passTicks long, worked out on the first tick) -> turn (still spinning, slowing down, until
        // the tip points back at the enemy) -> hold (none by default) -> drive (the stab from behind)
        int PassEnd => passTicks;
        int TurnEnd => PassEnd + EnchantedSwordTuning.GhostTurnTicks;
        int HoldEnd => TurnEnd + EnchantedSwordTuning.GhostHoldTicks;   // the drive starts here
        int DriveEnd => HoldEnd + EnchantedSwordTuning.GhostDriveTicks;
        int TotalTicks => DriveEnd + 2; // the blade explodes on tick DriveEnd (the last tick it can hit) and the ghost is gone one tick later

        // ---- per machine, never synced ----
        int ticks;
        int age;                    // the pose's age: stops while the ghost is leaving
        bool exploded;              // the blade has exploded: it is no longer drawn
        bool leaving;               // the enemy is gone before the stab and no other enemy is near: the ghost fades out
        int leaveTicks;
        Vector2 focus;              // the enemy's center, smoothed (a retarget glides instead of jumping)
        float halfSize = 12f;       // half the enemy's size: the ghost keeps further away from a big enemy
        Vector2 travelDir = Vector2.UnitX; // the beam's direction of flight (the blade keeps flying this way through the enemy)
        float entryAlong;           // where the blade's MIDDLE starts, measured from the enemy's center along travelDir (negative = before it) ...
        float entryAcross;          // ... and sideways from the line through the enemy's center
        float exitAlong;            // where the middle is when the pass ends (just past the enemy's far edge)
        float endAlong;             // where the middle is when the turn ends (the run-up of the stab)
        int passTicks = 1;          // how long the pass lasts (set on the first tick, from the way through the enemy)
        int spinHitIndex = -1;      // which spin hit the blade is on (the owner clears the hit memory when it moves on)
        Vector2 hilt;
        float angle;                // the blade's direction (world angle)
        float alpha;
        Vector2 prevHilt;
        Vector2 prevTip;
        float prevAngle;
        Vector2 tipPosition;
        SweepTrail trail;
        EnchantedSwordSweepTrailStyle trailStyle;
        AfterimageTrail afterimages;
        Texture2D texture;
        readonly RemoteHitTracker remoteHits = new RemoteHitTracker();

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600; // the sequence ends it long before this
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once per slash and once for the stab (the owner clears it at each start)
        }

        // The owner's client spawns the ghost where the thrown beam hit an enemy: AT THE BEAM'S TIP ('tip'), so the ghost is the beam's sword with no jump;
        // 'approach' is the angle from the enemy back toward where the beam came from (the beam's flight direction is the opposite). At most
        // MaxGhosts exist per player: a 4th makes the oldest one leave.
        public static void Spawn(Player owner, NPC target, Vector2 tip, int damage, float knockback, IEntitySource source, float approach)
        {
            int type = ModContent.ProjectileType<EtherealSwordProjectile>();
            int count = 0;
            Projectile oldest = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != owner.whoAmI || other.type != type)
                    continue;
                count++;
                if (oldest == null || other.timeLeft < oldest.timeLeft)
                    oldest = other;
            }
            if (count >= EnchantedSwordTuning.MaxGhosts && oldest != null)
                oldest.Kill();

            float direction = Main.rand.NextBool() ? 1f : -1f;
            Projectile.NewProjectile(source, tip, Vector2.Zero, type, damage, knockback, owner.whoAmI, target.whoAmI, approach, direction);
        }

        // ---- the pose ----

        // How far the blade has spun (radians, only ever grows) at 'ageNow' ticks of age: it starts from rest at the beam's own angle, makes
        // GhostSpinTurns full turns by the end of the pass (reaching the average speed there, no jump), then half a turn more, slowing down to
        // a stop, so that the tip points back at the enemy when the stab starts.
        float Spin(float ageNow)
        {
            float total = MathHelper.TwoPi * EnchantedSwordTuning.GhostSpinTurns;
            float pass = Math.Max(1, passTicks);
            if (ageNow <= pass)
            {
                float u = MathHelper.Clamp(ageNow / pass, 0f, 1f);
                return total * u * u * (2f - u); // starts at rest, ends at the average speed (total / pass per tick)
            }
            float turn = Math.Max(1, EnchantedSwordTuning.GhostTurnTicks);
            float v = MathHelper.Clamp((ageNow - pass) / turn, 0f, 1f);
            float speedEnd = total / pass * turn;
            return Hermite(total, Math.Min(speedEnd, 2.9f * MathHelper.Pi), total + MathHelper.Pi, 0f, v);
        }

        // A smooth curve from p0 (leaving with slope m0) to p1 (arriving with slope m1) as u runs 0..1 (m0 and m1 are per whole run).
        static float Hermite(float p0, float m0, float p1, float m1, float u)
        {
            float u2 = u * u;
            float u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * p0 + (u3 - 2f * u2 + u) * m0 + (-2f * u3 + 3f * u2) * p1 + (u3 - u2) * m1;
        }

        // Where the hilt is, which way the blade points (world angle) and how visible the ghost is, at 'ageNow' ticks of age (it may be
        // fractional: the trail asks for points between two ticks). Uses the current focus.
        void Pose(float ageNow, out Vector2 hiltAt, out float angleAt, out float alphaAt)
        {
            float length = EnchantedSwordTuning.GhostBladeLength;
            Vector2 forward = travelDir;
            Vector2 side = new Vector2(-forward.Y, forward.X);
            float pass = Math.Max(1, passTicks);
            float turn = Math.Max(1, EnchantedSwordTuning.GhostTurnTicks);

            // The blade spins around its own middle; the spin direction is random. After the whole spin the tip points back at the enemy.
            angleAt = forward.ToRotation() + Dir * Spin(ageNow);
            alphaAt = 1f; // the beam was fully visible

            if (ageNow < HoldEnd)
            {
                float along;
                float across;
                if (ageNow <= pass)
                {
                    // The pass: the middle keeps flying along the beam's line, straight through the enemy.
                    along = MathHelper.Lerp(entryAlong, exitAlong, MathHelper.Clamp(ageNow / pass, 0f, 1f));
                    across = entryAcross;
                }
                else
                {
                    // The turn: it carries on at the same speed and slows to a stop at the run-up point, drifting onto the line through the enemy's middle.
                    float v = MathHelper.Clamp((ageNow - pass) / turn, 0f, 1f);
                    float speedEnd = (exitAlong - entryAlong) / pass * turn;
                    along = Hermite(exitAlong, MathHelper.Clamp(speedEnd, 0f, 2.9f * (endAlong - exitAlong)), endAlong, 0f, v);
                    across = entryAcross * (1f - MathHelper.SmoothStep(0f, 1f, v));
                }
                Vector2 middle = focus + forward * along + side * across;
                hiltAt = middle - angleAt.ToRotationVector2() * (length * 0.5f);

                // A tremble while it holds before the drive (only if GhostHoldTicks is above 0).
                if (ageNow >= TurnEnd)
                    hiltAt += side * (float)Math.Sin(ageNow * 2.3f) * EnchantedSwordTuning.GhostTremblePx;
            }
            else
            {
                // The drive: from behind the enemy straight at its middle, accelerating (a power ease-in) until the blade is buried in it. The blade
                // already points at the enemy (the spin ended exactly there), so it is the hilt that moves.
                float t = MathHelper.Clamp((ageNow - HoldEnd) / Math.Max(1, EnchantedSwordTuning.GhostDriveTicks), 0f, 1f);
                float p = (float)Math.Pow(t, Math.Max(1f, EnchantedSwordTuning.GhostDriveEasePower));
                float hiltDistance = MathHelper.Lerp(endAlong + length * 0.5f, length * EnchantedSwordTuning.GhostEmbedFraction, p);
                hiltAt = focus + forward * hiltDistance;
            }
        }

        // The blade does damage while it spins through the enemy and from the start of the drive until it is buried.
        bool Damaging => ticks > 0 && !leaving && (age < PassEnd || (age >= HoldEnd && age <= DriveEnd));
        bool Stabbing => age >= HoldEnd;

        // ---- AI ----

        bool TargetValid()
        {
            int target = Target;
            return target >= 0 && target < Main.maxNPCs && Main.npc[target].active && Main.npc[target].life > 0;
        }

        public override void AI()
        {
            Player owner = Owner;
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }

            if (ticks == 0)
            {
                // The spawn position is the beam's tip: the blade's middle starts half a blade length behind it, flying the beam's way (the
                // opposite of the approach angle), and has to get past the enemy's far edge (the support of its box along the flight direction).
                Vector2 tip = Projectile.Center;
                bool valid = TargetValid();
                focus = valid ? Main.npc[Target].Center : tip;
                travelDir = (-Phi0.ToRotationVector2()).SafeNormalize(Vector2.UnitX);
                Vector2 side = new Vector2(-travelDir.Y, travelDir.X);
                float extent = 0f;
                if (valid)
                {
                    NPC npc = Main.npc[Target];
                    halfSize = Math.Max(npc.width, npc.height) * 0.5f;
                    extent = npc.width * 0.5f * Math.Abs(travelDir.X) + npc.height * 0.5f * Math.Abs(travelDir.Y);
                }
                Vector2 middle = tip - travelDir * (EnchantedSwordTuning.GhostBladeLength * 0.5f);
                Vector2 relative = middle - focus;
                entryAlong = Vector2.Dot(relative, travelDir);
                entryAcross = Vector2.Dot(relative, side);
                exitAlong = Math.Max(extent + EnchantedSwordTuning.GhostExitMargin, entryAlong + EnchantedSwordTuning.GhostExitMargin);
                endAlong = exitAlong + EnchantedSwordTuning.GhostRunUpPx;
                int wanted = (int)Math.Ceiling((exitAlong - entryAlong) / Math.Max(1f, EnchantedSwordTuning.GhostPassSpeed));
                passTicks = Math.Max(1, Math.Clamp(wanted, EnchantedSwordTuning.GhostPassMinTicks, Math.Max(EnchantedSwordTuning.GhostPassMinTicks, EnchantedSwordTuning.GhostPassMaxTicks)));
            }

            // The enemy died (or left) before the stab: the owner picks the nearest other enemy; with none near, the ghost leaves.
            if (IsOwner && !TargetValid() && age < HoldEnd && !leaving)
            {
                int best = -1;
                float bestDistance = EnchantedSwordTuning.GhostRetargetRange * EnchantedSwordTuning.GhostRetargetRange;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.CanBeChasedBy(Projectile))
                        continue;
                    float distance = Vector2.DistanceSquared(npc.Center, focus);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = i;
                    }
                }
                Projectile.ai[0] = best;
                Projectile.netUpdate = true;
            }

            if (TargetValid())
            {
                NPC npc = Main.npc[Target];
                focus = Vector2.Lerp(focus, npc.Center, 0.6f);
                halfSize = MathHelper.Lerp(halfSize, Math.Max(npc.width, npc.height) * 0.5f, 0.3f);
            }
            else if (age < HoldEnd)
                leaving = true; // after the drive starts the sequence plays out on the enemy's last position

            prevHilt = hilt;
            prevAngle = angle;
            prevTip = tipPosition;
            Pose(age, out hilt, out angle, out alpha);
            if (leaving)
                alpha *= 1f - leaveTicks / (float)Math.Max(1, EnchantedSwordTuning.GhostFadeOutTicks);
            Projectile.Center = hilt;
            Projectile.rotation = angle;
            tipPosition = hilt + angle.ToRotationVector2() * EnchantedSwordTuning.GhostBladeLength;
            if (ticks == 0)
            {
                prevHilt = hilt;
                prevAngle = angle;
            }

            Events();

            if (!Main.dedServ)
            {
                Lighting.AddLight(tipPosition, 0.3f * alpha, 0.5f * alpha, 0.8f * alpha);
                trail?.Tick();
                if (Damaging)
                    LayTrail();
                if (texture == null)
                {
                    Main.instance.LoadProjectile(ProjectileID.EnchantedBeam);
                    texture = TextureAssets.Projectile[ProjectileID.EnchantedBeam].Value;
                    Texture2D art = texture;
                    afterimages = new AfterimageTrail(EnchantedSwordTuning.GhostAfterimageTicks, (image, ageFade) =>
                    {
                        Main.EntitySpriteDraw(art, image.Position - Main.screenPosition, null, image.Color * (1f - ageFade), image.Rotation, Pivot(art), image.Scale, SpriteEffects.None, 0);
                    });
                }
                afterimages.Tick();
                if (alpha > 0.05f && !exploded)
                    afterimages.Add(hilt, DrawRotation(), DrawScale(), EnchantedSwordTuning.BlueColor * (0.45f * alpha));
            }

            if (!IsOwner && Damaging)
                remoteHits.Update(Projectile, PhaseSerial(), npc => EnchantedSwordFx.HitSpark(npc.Center, 2f));

            if (leaving)
            {
                leaveTicks++;
                if (leaveTicks >= EnchantedSwordTuning.GhostFadeOutTicks)
                    Projectile.Kill();
            }
            else
            {
                age++;
                if (age >= TotalTicks)
                    Projectile.Kill();
            }
            ticks++;
        }

        // The spin hit number (1..), or a high number for the stab: RemoteHitTracker shows one hit spark per enemy per phase.
        int PhaseSerial()
        {
            if (Stabbing)
                return 99;
            return 1 + (int)(Spin(age) / Math.Max(0.01f, MathHelper.ToRadians(EnchantedSwordTuning.GhostSpinHitDeg)));
        }

        // The things that happen at a moment of the sequence (every machine plays the same ones; the owner also clears the hit memory so the
        // next spin hit or the stab can hit again).
        void Events()
        {
            if (leaving)
                return;

            if (age == 0)
            {
                EnchantedSwordFx.GhostAppear(focus);
                if (IsOwner)
                    SegmentHit.ClearLocalImmunity(Projectile);
                NewTrail();
                EnchantedSwordFx.PlaySwing(tipPosition, false);
            }

            if (age < PassEnd)
            {
                // The spinning blade may hit the enemy again every GhostSpinHitDeg of spin (the owner forgets who it has hit).
                int hitIndex = (int)(Spin(age) / Math.Max(0.01f, MathHelper.ToRadians(EnchantedSwordTuning.GhostSpinHitDeg)));
                if (hitIndex != spinHitIndex)
                {
                    if (spinHitIndex >= 0)
                    {
                        if (IsOwner)
                            SegmentHit.ClearLocalImmunity(Projectile);
                        EnchantedSwordFx.PlaySwing(tipPosition, false);
                    }
                    spinHitIndex = hitIndex;
                }
                EnchantedSwordFx.GhostDust(tipPosition, tipPosition - prevTip);
            }

            // The turn (and the hold): the spin slows down, the tip comes round to the enemy and stardust is drawn into it.
            if (age >= PassEnd && age < HoldEnd)
            {
                if (age == PassEnd)
                    EnchantedSwordFx.StabChargeStart(hilt);
                EnchantedSwordFx.StabCharge(tipPosition, (age - PassEnd) / (float)Math.Max(1, HoldEnd - PassEnd));
            }

            if (age == HoldEnd)
            {
                if (IsOwner)
                    SegmentHit.ClearLocalImmunity(Projectile);
                NewTrail();
                EnchantedSwordFx.PlaySwing(tipPosition, true);
            }

            if (age >= HoldEnd && age <= DriveEnd)
                EnchantedSwordFx.GhostDust(tipPosition, tipPosition - prevTip);

            if (age == DriveEnd)
            {
                // The blade explodes the moment it lands: the owner spawns the Enchanted Boomerang's stardust burst (it plays the burst on every
                // machine and hurts the enemies around the tip) and the sword itself is gone.
                exploded = true;
                EnchantedSwordFx.StabImpact(tipPosition);
                if (IsOwner)
                {
                    CameraShake.Punch(focus, EnchantedSwordTuning.GhostStabShake, 12);
                    int damage = Math.Max(1, (int)(Projectile.damage * EnchantedSwordTuning.GhostExplosionDamageMultiplier));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), tipPosition, Vector2.Zero, ModContent.ProjectileType<StardustBurstProjectile>(), damage, Projectile.knockBack, Projectile.owner);
                }
            }
        }

        void NewTrail()
        {
            if (Main.dedServ)
                return;
            DetachedTrails.Hand(trail);
            trailStyle = new EnchantedSwordSweepTrailStyle { Stage = 1f };
            trail = new SweepTrail(trailStyle, IsOwner);
        }

        // Points of the swing's fan between the previous tick and this one (the pose is a function of age, so they come straight from it).
        void LayTrail()
        {
            if (trail == null)
                return;
            int samples = Math.Max(1, EnchantedSwordTuning.TrailSamplesPerTick);
            float phaseStart = Stabbing ? HoldEnd : 0f;
            float phaseLength = Stabbing ? Math.Max(1, EnchantedSwordTuning.GhostDriveTicks) : Math.Max(1, passTicks);
            for (int k = 0; k < samples; k++)
            {
                float sampleAge = age - 1f + (k + 1f) / samples;
                Pose(sampleAge, out Vector2 sampleHilt, out float sampleAngle, out float sampleAlpha);
                Vector2 direction = sampleAngle.ToRotationVector2();
                float length = EnchantedSwordTuning.GhostBladeLength;
                float inner = Stabbing ? EnchantedSwordTuning.TrailInnerThrust : EnchantedSwordTuning.GhostTrailInner;
                float along = MathHelper.Clamp((sampleAge - phaseStart) / phaseLength, 0f, 1f);
                trail.Add(sampleHilt + direction * length * EnchantedSwordTuning.TrailOuter, sampleHilt + direction * length * inner, along);
            }
        }

        // ---- hits ----

        public override bool? CanDamage()
        {
            return Damaging ? (bool?)null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Damaging)
                return false;
            int steps = Math.Max(1, EnchantedSwordTuning.SweepHitSteps);
            float length = EnchantedSwordTuning.GhostBladeLength * EnchantedSwordTuning.HitLengthScale;
            float deltaAngle = MathHelper.WrapAngle(angle - prevAngle);
            for (int i = 1; i <= steps; i++)
            {
                float fraction = i / (float)steps;
                Vector2 start = Vector2.Lerp(prevHilt, hilt, fraction);
                Vector2 end = start + (prevAngle + deltaAngle * fraction).ToRotationVector2() * length;
                if (SegmentHit.Overlaps(targetHitbox, start, end, EnchantedSwordTuning.HitWidth))
                    return true;
            }
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > hilt.X ? 1 : -1;
            modifiers.SourceDamage *= Stabbing ? EnchantedSwordTuning.GhostStabDamageMultiplier : EnchantedSwordTuning.GhostSpinDamageMultiplier;
            if (Stabbing)
                modifiers.Knockback *= EnchantedSwordTuning.GhostStabKnockbackMultiplier;
        }

        // Owner's client only.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            EnchantedSwordFx.HitSpark(target.Center, 2f);
            if (!Stabbing)
                CameraShake.Punch(target.Center, EnchantedSwordTuning.HitShake, 6);
        }

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;
            DetachedTrails.Hand(afterimages);
            afterimages = null;
        }

        // ---- drawing ----

        // The point of the sprite that sits on the hilt and that the sprite turns around (EnchantedSwordTuning.GhostSpritePivotX / Y).
        static Vector2 Pivot(Texture2D art)
        {
            return new Vector2(art.Width * EnchantedSwordTuning.GhostSpritePivotX, art.Height * EnchantedSwordTuning.GhostSpritePivotY);
        }

        float DrawRotation()
        {
            return angle + MathHelper.ToRadians(EnchantedSwordTuning.GhostSpriteOffsetDeg);
        }

        // The sprite is stretched to GhostBladeLength (the beam sprite is short): the hilt end sits at the bottom of the sprite.
        float DrawScale()
        {
            return texture == null ? 1f : EnchantedSwordTuning.GhostBladeLength / Math.Max(1f, texture.Height);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ticks == 0 || texture == null)
                return false;

            trail?.Draw();
            afterimages?.Draw();
            if (exploded)
                return false;

            Vector2 origin = Pivot(texture);
            Vector2 position = hilt - Main.screenPosition;
            float rotation = DrawRotation();
            float scale = DrawScale();
            float opacity = alpha * EnchantedSwordTuning.GhostOpacity;
            // A soft blue glow grown from the middle of the sprite (wherever the pivot puts it), then the ghost sword itself (additive: it
            // glows like the stardust).
            Vector2 middle = position + new Vector2((0.5f - EnchantedSwordTuning.GhostSpritePivotX) * texture.Width, (0.5f - EnchantedSwordTuning.GhostSpritePivotY) * texture.Height).RotatedBy(rotation) * scale;
            Vector2 glowOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            Main.EntitySpriteDraw(texture, middle, null, EnchantedSwordTuning.BlueColor * (0.5f * opacity), rotation, glowOrigin, scale * EnchantedSwordTuning.GhostGlowScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, new Color(255, 255, 255, 0) * opacity, rotation, origin, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}