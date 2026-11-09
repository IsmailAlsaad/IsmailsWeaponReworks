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
    /// The ghost sword the Enchanted Sword conjures when its 3rd thrust hit discharges (patch 2). A ghost holds the sword (the vanilla
    /// Enchanted Beam sprite) and floats around the enemy; the sword does 4 quick slashes across it (the ghost drifts around the enemy while
    /// it slashes), then on the 5th swing it backs away a little (trembling, stardust drawn into the tip), holds a beat and drives itself
    /// point-first into the enemy's middle; the moment it lands it explodes into a stardust burst (the Enchanted Boomerang's) and is gone.
    /// THE WHOLE SEQUENCE IS ONE POSE FUNCTION OF THE PROJECTILE'S AGE (Pose): one projectile, one continuous motion, so nothing flickers
    /// between swings (each slash ends exactly where the next begins).
    /// MULTIPLAYER: the owner spawns it (EtherealSwordProjectile.Spawn) with ai[0] = the enemy's index, ai[1] = the approach angle and
    /// ai[2] = the orbit direction (+1 or -1); these arrive with the spawn, so every machine plays the same sequence from the same age and the
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
        int SlashStart => EnchantedSwordTuning.GhostFadeInTicks;
        int SlashEnd => SlashStart + EnchantedSwordTuning.GhostSlashes * EnchantedSwordTuning.GhostSlashTicks;
        int BackoffEnd => SlashEnd + EnchantedSwordTuning.GhostBackoffTicks;
        int HoldEnd => BackoffEnd + EnchantedSwordTuning.GhostHoldTicks;
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

        // The owner's client spawns the ghost at the enemy that took the discharging hit. At most MaxGhosts exist per player: a 4th makes the
        // oldest one leave.
        public static void Spawn(Player owner, NPC target, int damage, float knockback, IEntitySource source)
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

            float approach = (owner.Center - target.Center).ToRotation();
            float direction = Main.rand.NextBool() ? 1f : -1f;
            Projectile.NewProjectile(source, target.Center, Vector2.Zero, type, damage, knockback, owner.whoAmI, target.whoAmI, approach, direction);
        }

        // ---- the pose ----

        // Where the hilt is, which way the blade points (world angle) and how visible the ghost is, at 'ageNow' ticks of age (it may be
        // fractional: the trail asks for points between two ticks). Uses the current focus.
        void Pose(float ageNow, out Vector2 hiltAt, out float angleAt, out float alphaAt)
        {
            float length = EnchantedSwordTuning.GhostBladeLength;
            float hold = EnchantedSwordTuning.GhostHoldDistance + EnchantedSwordTuning.GhostHoldPerSize * halfSize;
            float arc = MathHelper.ToRadians(EnchantedSwordTuning.GhostSlashArcDeg);
            float phi = Phi0 + Dir * MathHelper.ToRadians(EnchantedSwordTuning.GhostOrbitDegPerTick) * Math.Min(ageNow, SlashEnd);
            Vector2 away = phi.ToRotationVector2();

            float sweep;
            float distance = hold;
            if (ageNow < SlashEnd)
            {
                // The slashes alternate: the blade fans across the enemy one way, then back the other way; each slash ends where the next starts.
                float u = Math.Max(0f, ageNow - SlashStart) / Math.Max(1, EnchantedSwordTuning.GhostSlashTicks);
                int index = Math.Min((int)u, EnchantedSwordTuning.GhostSlashes - 1);
                float ease = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(u - index, 0f, 1f));
                float from = index % 2 == 0 ? -arc : arc;
                sweep = MathHelper.Lerp(from, -from, ease);
            }
            else
            {
                float lastSweep = EnchantedSwordTuning.GhostSlashes % 2 == 0 ? -arc : arc;
                float back = EnchantedSwordTuning.GhostBackoffPx;
                if (ageNow < BackoffEnd)
                {
                    // Backing away: the blade swings round to point straight at the enemy while the ghost pulls back.
                    float ease = MathHelper.SmoothStep(0f, 1f, (ageNow - SlashEnd) / Math.Max(1, EnchantedSwordTuning.GhostBackoffTicks));
                    sweep = MathHelper.Lerp(lastSweep, 0f, ease);
                    distance = hold + back * ease;
                }
                else if (ageNow < HoldEnd)
                {
                    sweep = 0f;
                    distance = hold + back;
                }
                else
                {
                    // The drive: accelerating (a power ease-in) from the back-off point until the blade is buried in the enemy.
                    float t = MathHelper.Clamp((ageNow - HoldEnd) / Math.Max(1, EnchantedSwordTuning.GhostDriveTicks), 0f, 1f);
                    float p = (float)Math.Pow(t, Math.Max(1f, EnchantedSwordTuning.GhostDriveEasePower));
                    sweep = 0f;
                    distance = MathHelper.Lerp(hold + back, length * EnchantedSwordTuning.GhostEmbedFraction, p);
                }
            }

            hiltAt = focus + away * distance;
            angleAt = phi + MathHelper.Pi + Dir * sweep;

            // The tremble of the charge (backing away and holding) and a small quiver while buried.
            float tremble = 0f;
            if (ageNow >= SlashEnd && ageNow < HoldEnd)
                tremble = EnchantedSwordTuning.GhostTremblePx * MathHelper.Clamp((ageNow - SlashEnd) / Math.Max(1, EnchantedSwordTuning.GhostBackoffTicks), 0f, 1f);
            if (tremble > 0f)
                hiltAt += new Vector2(-away.Y, away.X) * (float)Math.Sin(ageNow * 2.3f) * tremble;

            alphaAt = ageNow < SlashStart ? ageNow / Math.Max(1f, SlashStart) : 1f;
            alphaAt = MathHelper.Clamp(alphaAt, 0f, 1f);
        }

        // The blade does damage during the slashes and from the start of the drive until it is buried.
        bool Damaging => !leaving && ((age >= SlashStart && age < SlashEnd) || (age >= HoldEnd && age <= DriveEnd));
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
                focus = TargetValid() ? Main.npc[Target].Center : Projectile.Center;
                if (TargetValid())
                    halfSize = Math.Max(Main.npc[Target].width, Main.npc[Target].height) * 0.5f;
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

        // The slash number (1..), or a high number for the stab: RemoteHitTracker shows one hit spark per enemy per phase.
        int PhaseSerial()
        {
            if (Stabbing)
                return 99;
            return 1 + (age - SlashStart) / Math.Max(1, EnchantedSwordTuning.GhostSlashTicks);
        }

        // The things that happen at a moment of the sequence (every machine plays the same ones; the owner also clears the hit memory so the
        // next slash or the stab can hit again).
        void Events()
        {
            if (leaving)
                return;

            if (age == 0)
                EnchantedSwordFx.GhostAppear(focus);

            if (age >= SlashStart && age < SlashEnd && (age - SlashStart) % Math.Max(1, EnchantedSwordTuning.GhostSlashTicks) == 0)
            {
                if (IsOwner)
                    SegmentHit.ClearLocalImmunity(Projectile);
                NewTrail();
                EnchantedSwordFx.PlaySwing(tipPosition, false);
            }

            if (age >= SlashStart && age < SlashEnd)
                EnchantedSwordFx.GhostDust(tipPosition, tipPosition - prevTip);

            if (age >= SlashEnd && age < HoldEnd)
            {
                if (age == SlashEnd)
                    EnchantedSwordFx.StabChargeStart(hilt);
                EnchantedSwordFx.StabCharge(tipPosition, (age - SlashEnd) / (float)Math.Max(1, HoldEnd - SlashEnd));
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
            float phaseStart = Stabbing ? HoldEnd : SlashStart + ((age - SlashStart) / Math.Max(1, EnchantedSwordTuning.GhostSlashTicks)) * EnchantedSwordTuning.GhostSlashTicks;
            float phaseLength = Stabbing ? Math.Max(1, EnchantedSwordTuning.GhostDriveTicks) : Math.Max(1, EnchantedSwordTuning.GhostSlashTicks);
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
            modifiers.SourceDamage *= Stabbing ? EnchantedSwordTuning.GhostStabDamageMultiplier : EnchantedSwordTuning.GhostSlashDamageMultiplier;
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