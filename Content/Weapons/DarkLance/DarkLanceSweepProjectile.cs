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
    /// The Dark Lance's SWEEP (left click, every other click): the held lance is pulled back a little, then makes one fast 360 degree turn
    /// around the player (smooth start and end), leaving a fan of dark trail and shedding embers, and hurts every enemy its shaft passes
    /// once. Every flame orb its shaft touches is GATHERED: the owner flags it (orb ai[2] = 1) and counts it in DarkLancePlayer.Collected;
    /// the next click, a thrust, spends the count.
    /// SPAWN VALUES (the GlobalItem's Shoot passes them): ai[0] = the spin direction (1 or -1), ai[1] = 0, ai[2] = the aim in the player's own frame.
    /// MULTIPLAYER: the owner's client decides the click, the aim, which orbs are gathered and every hit. The angle is a pure function of the
    /// projectile's age, so every machine poses, draws and sounds the same sweep with no per-tick packets; the gathered flag travels on the
    /// orb (ai[2] + netUpdate). Other players' machines find the hits themselves and only show sparks (RemoteHitTracker). Visual state is never synced.
    /// </summary>
    public class DarkLanceSweepProjectile : ModProjectile
    {
        // The sweep draws the vanilla PROJECTILE sprite (the long-handled one); the charge and the thrust still use the item sprite.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DarkLance;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int SpinDirection => Projectile.ai[0] >= 0f ? 1 : -1;
        float BaseAim => Projectile.ai[2];

        // Per-machine state: counters and visuals, never synced.
        int age;
        float turned;          // radians turned along the spin direction (a little negative while pulling back, 2 pi at the end)
        float speedFraction;   // this tick's turning speed, 0 to 1 of about the fastest tick
        bool striking;         // inside the turn, fast enough to hit and gather
        bool whooshed;
        bool hasPose;
        Vector2 butt;
        Vector2 tip;
        Vector2 prevButt;
        Vector2 prevTip;
        Vector2 dir;           // the lance's direction this tick and last tick, and the hand it turns around (the gather test)
        Vector2 prevDir;
        Vector2 pivot;
        SweepTrail trail;
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
            Projectile.localNPCHitCooldown = 12;   // every enemy once per sweep
            Projectile.ownerHitCheck = true;       // it cannot hit through walls
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
            int windup = Math.Max(0, DarkLanceTuning.SweepWindupTicks);
            int sweep = Math.Max(1, DarkLanceTuning.SweepTicks);
            int recover = Math.Max(0, DarkLanceTuning.SweepRecoverTicks);
            if (age >= windup + sweep + recover)
            {
                owner.itemAnimation = 0; // the next use may start as soon as the cooldown is over
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            if (age == 0 && IsOwner)
                owner.ChangeDir(HeldAim.Facing(BaseAim)); // the player faces the cursor side for the whole sweep

            float lastTurned = turned;
            turned = TurnAt(age, windup, sweep);
            speedFraction = MathHelper.Clamp(Math.Abs(turned - lastTurned) / (MathHelper.TwoPi / sweep * 1.6f), 0f, 1f);
            striking = age >= windup && age < windup + sweep && speedFraction >= DarkLanceTuning.SweepMinHitSpeed;
            float aim = BaseAim + SpinDirection * turned;

            Projectile.scale = DarkLanceTuning.SweepScale * owner.GetAdjustedItemScale(owner.HeldItem);
            float shaft = ShaftLength();

            Vector2 lastDir = dir;
            Vector2 lastButt = butt;
            Vector2 lastTip = tip;

            // Like the Rotted Fork spin (and the vanilla Sleepy Octopod): no arm pose of our own. The item uses the Shoot use style and
            // vanilla picks the body frame (arm out, raised or lowered) from itemRotation, which is set below.
            Vector2 hand = owner.RotatedRelativePoint(owner.MountedCenter, false, true);
            Vector2 direction = HeldAim.ToWorldDirection(owner, aim);
            butt = hand - direction * shaft * DarkLanceTuning.SweepGrip;
            dir = direction;
            pivot = hand;
            tip = butt + direction * shaft;
            Projectile.Center = owner.MountedCenter; // knockback pushes away from the player
            Projectile.rotation = direction.ToRotation();
            Projectile.spriteDirection = SpinDirection;
            owner.heldProj = Projectile.whoAmI;      // also ends the projectile when the player swaps items
            owner.SetDummyItemTime(2);               // keeps the item "in use" for as long as the sweep lasts
            float seenFromRight = owner.direction == 1 ? aim : MathHelper.Pi - aim; // the aim as a right-facing player would have it
            owner.itemRotation = MathHelper.WrapAngle(seenFromRight + MathHelper.PiOver4) * owner.direction;

            prevDir = hasPose ? lastDir : dir;
            prevButt = hasPose ? lastButt : butt;
            prevTip = hasPose ? lastTip : tip;
            hasPose = true;

            owner.GetModPlayer<DarkLancePlayer>().LanceTip = tip; // gathered orbs fly to it (every machine)

            if (IsOwner && age >= windup && age < windup + sweep)   // the whole turn, also its slow start and end
                Gather();

            if (!Main.dedServ)
                Visuals(owner, windup);

            // Other players' machines do not run OnHitNPC; they find the hit themselves and only show the sparks.
            if (!IsOwner && !Main.dedServ && striking)
                (remoteHits ??= new RemoteHitTracker()).Update(Projectile, 0, npc => ThrustFx.HitSpark(SegmentHit.ClosestPoint(butt, tip, npc.Center), DarkLanceTuning.Palette));

            age++;
        }

        // The turn, as a pure function of the age: pulled back during the windup, then one full circle with a smooth start and end.
        float TurnAt(int atAge, int windup, int sweep)
        {
            float pull = DarkLanceTuning.SweepPullBack;
            if (atAge < windup)
                return -pull * MathHelper.SmoothStep(0f, 1f, (atAge + 1f) / Math.Max(1, windup));
            float u = MathHelper.Clamp((atAge - windup + 1f) / sweep, 0f, 1f);
            return -pull + (MathHelper.TwoPi + pull) * MathHelper.SmoothStep(0f, 1f, u);
        }

        // Owner only: every orb the lance has turned past (by angle, see Touches) is gathered. The flag travels with the orb; every machine then flies it into the tip.
        void Gather()
        {
            DarkLancePlayer state = Owner.GetModPlayer<DarkLancePlayer>();
            int orbType = ModContent.ProjectileType<DarkLanceOrbProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile orb = Main.projectile[i];
                if (!orb.active || orb.owner != Projectile.owner || orb.type != orbType || orb.ai[0] >= 1f || orb.ai[2] >= 1f)
                    continue;
                if (!Touches(orb))
                    continue;
                orb.ai[2] = 1f;
                orb.netUpdate = true;
                state.Collected = Math.Min(DarkLanceTuning.MaxOrbs, state.Collected + 1);
            }
        }

        // Does the lance gather this orb? A pure ANGLE test, no colliders: the orb is gathered when its direction from the hand lies between
        // the lance's direction last tick and this tick's. Where the orb is (near, far, lagging behind a running player, moving) does not
        // matter, and a fast turn cannot skip it, so a sweep through every angle gathers every orb.
        bool Touches(Projectile orb)
        {
            Vector2 v = orb.Center - pivot;
            if (v.LengthSquared() < 1f)
                return true;   // exactly on the hand: every angle includes it
            float whole = Cross(prevDir, dir);
            if (Math.Abs(whole) < 0.00001f)
                return false;  // the lance did not turn this tick
            float a = Cross(prevDir, v);
            float b = Cross(v, dir);
            return whole > 0f ? (a >= 0f && b >= 0f) : (a <= 0f && b <= 0f);
        }

        // Butt to tip: the projectile texture's diagonal (a dedicated server has no textures and uses the item sprite's).
        float ShaftLength()
        {
            if (Main.dedServ)
            {
                Item sample = ContentSamples.ItemsByType[ItemID.DarkLance];
                return new Vector2(sample.width, sample.height).Length() * Projectile.scale;
            }
            Texture2D art = TextureAssets.Projectile[Type].Value;
            return new Vector2(art.Width, art.Height).Length() * Projectile.scale;
        }

        // The sprite with its butt at 'butt' pointing along the lance. DarkLanceTuning.SweepSpriteRotation (degrees) turns the art if it
        // looks off; it is mirrored with the sprite so the same number works facing either way.
        void DrawLance(Texture2D texture, float scale, Color color)
        {
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            float extra = MathHelper.ToRadians(DarkLanceTuning.SweepSpriteRotation) * (Projectile.spriteDirection >= 0 ? 1f : -1f);
            Main.EntitySpriteDraw(texture, butt - Main.screenPosition, null, color, Projectile.rotation + rotationOffset + extra, origin, scale, effects, 0);
        }

        float Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        // Every machine: the start ring, the whoosh, flecks and embers off the tip, the light and the trail fan.
        void Visuals(Player owner, int windup)
        {
            ThrustPalette palette = DarkLanceTuning.Palette;

            if (age == 0)
                DarkLanceFx.SweepStart(owner.Center);
            if (!whooshed && age >= windup)
            {
                whooshed = true;
                DarkLanceFx.SweepWhoosh(tip);
            }

            if (speedFraction > 0.15f)
            {
                DarkLanceFx.Flecks(tip, tip - prevTip, speedFraction, palette);
                if (Main.rand.NextFloat() < DarkLanceTuning.SweepEmberChance * speedFraction)
                    DarkLanceEmbers.Shed(tip, (tip - prevTip) * DarkLanceTuning.EmberInherit * Main.rand.NextFloat(0.3f,0.7f));
            }
            Lighting.AddLight(tip, palette.Mid.ToVector3() * 0.7f * speedFraction);

            //trail ??= new SweepTrail(new ThrustTrailStyle(palette, DarkLanceTuning.SweepTrailLifeTicks, DarkLanceTuning.SweepTrailRails, DarkLanceTuning.PixelSize, DarkLanceTuning.SweepTrailOpacity), IsOwner);
            //if (speedFraction > 0.15f)
            //{
            //    for (int i = 1; i <= 2; i++)
            //    {
            //        float f = i / 2f;
            //        Vector2 root = Vector2.Lerp(prevButt, butt, f);
            //        Vector2 outer = Vector2.Lerp(prevTip, tip, f);
            //        trail.Add(outer, Vector2.Lerp(outer, root, DarkLanceTuning.SweepTrailInner));
            //    }
            //}
            //trail.Tick();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!hasPose || !striking)
                return false;
            return SegmentHit.Sweep(targetHitbox, prevButt, prevTip, butt, tip, DarkLanceTuning.SweepHitWidth * Projectile.scale, 3);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X >= Owner.Center.X ? 1 : -1; // always away from the player
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ThrustFx.HitSpark(SegmentHit.ClosestPoint(butt, tip, target.Center), DarkLanceTuning.Palette);
        }

        public override void OnKill(int timeLeft)
        {
            if (IsOwner)
                Owner.GetModPlayer<ThrustPlayer>().StartCooldown(ItemID.DarkLance, DarkLanceTuning.SweepCooldownTicks);
            DetachedTrails.Hand(trail); // the fan keeps fading on its own
            trail = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //trail?.Draw();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            ThrustPalette palette = DarkLanceTuning.Palette;
            float scale = Projectile.scale;
            float rotation = Projectile.rotation;
            float glow = 0.3f + 0.7f * speedFraction;
            int flip = Projectile.spriteDirection;

            DrawLance(texture, scale * 1.18f, palette.Halo * (0.45f * glow));
            DrawLance(texture, scale * 1.07f, palette.Mid * (0.5f * glow));
            DrawLance(texture, scale, Color.Lerp(lightColor, Color.White, 0.3f + 0.4f * glow));

            // a dark glow on the tip that swells while the lance is fast
            DarkLanceFx.DrawGlow(tip, DarkLanceTuning.TipGlowMinRadius + 0.5f * (DarkLanceTuning.TipGlowMaxRadius - DarkLanceTuning.TipGlowMinRadius) * speedFraction, DarkLanceTuning.TipGlowStrength * (0.4f + 0.6f * speedFraction));
            return false;
        }
    }
}