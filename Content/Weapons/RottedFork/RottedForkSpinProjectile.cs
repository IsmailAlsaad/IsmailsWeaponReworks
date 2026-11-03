using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.RottedFork
{
    /// <summary>
    /// The held Rotted Fork spinning around the player while the left button is held: the fork points straight out from the hand like a
    /// clock hand and turns continuously, leaving a fan of blood-red trail, and hurts everything its shaft sweeps through (the same enemy
    /// again after a hit cooldown).
    /// SPAWN VALUES (the GlobalItem's Shoot passes them): ai[0] = the spin direction (1 or -1), ai[1] = 0 (the owner writes the end age + 1
    /// here when the button is released), ai[2] = the first aim in the player's own frame (HeldAim.Capture).
    /// MULTIPLAYER: the owner's client decides everything (the click, the first aim, the release, every hit). The angle is a pure function
    /// of the projectile's age (the spin speed ramps up from the start and winds down toward the synced end age), so every machine poses,
    /// draws and sounds the same spin with no per-tick packets; only the release travels (ai[1] + netUpdate). The owner faces the cursor
    /// while spinning (the direction is synced like any player's). Other players' machines find the hits themselves and only show sparks
    /// (RemoteHitTracker); the damage is the owner's alone. Visual state is never synced.
    /// </summary>
    public class RottedForkSpinProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite.
        public override string Texture => "Terraria/Images/Item_" + ItemID.TheRottedFork;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        int SpinDirection => Projectile.ai[0] >= 0f ? 1 : -1;
        float BaseAim => Projectile.ai[2];
        int EndAge => (int)Projectile.ai[1] - 1;   // -1 = no release yet

        // Per-machine state: counters and visuals, never synced.
        int age;
        float turned;          // how far the fork has turned since the start (radians, always positive)
        float speedFraction;   // this tick's spin speed, 0 to 1 of full speed
        int halfTurns;
        bool hasPose;
        Vector2 butt;
        Vector2 tip;
        Vector2 direction;
        Vector2 prevButt;
        Vector2 prevTip;
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
            Projectile.localNPCHitCooldown = 14;   // set from RottedForkTuning every tick
            Projectile.ownerHitCheck = true;       // it cannot hit through walls
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.TheRottedFork)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.localNPCHitCooldown = Math.Max(1, RottedForkTuning.HitCooldownTicks);

            if (IsOwner)
            {
                owner.ChangeDir(Main.MouseWorld.X >= owner.Center.X ? 1 : -1); // the player faces the cursor for the whole spin
                if (EndAge < 0 && !owner.controlUseItem && age >= RottedForkTuning.MinSpinTicks)
                {
                    Projectile.ai[1] = age + RottedForkTuning.WindDownTicks + 1f; // the release: every machine winds down to this age
                    Projectile.netUpdate = true;
                }
            }

            if (EndAge >= 0 && age >= EndAge)
            {
                owner.itemAnimation = 0; // the next use may start as soon as the cooldown is over
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            // The spin speed: it ramps up from the start and slows down to a stop over the last ticks before the end age.
            float ramp = MathHelper.SmoothStep(RottedForkTuning.StartSpeedFraction, 1f, MathHelper.Clamp(age / (float)Math.Max(1, RottedForkTuning.RampTicks), 0f, 1f));
            float wind = 1f;
            if (EndAge >= 0)
                wind = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((EndAge - age) / (float)Math.Max(1, RottedForkTuning.WindDownTicks), 0f, 1f));
            speedFraction = ramp * wind;
            turned += RottedForkTuning.SpinSpeed * speedFraction;
            float aim = BaseAim + SpinDirection * turned;

            Projectile.scale = RottedForkTuning.SpearScale * owner.GetAdjustedItemScale(owner.HeldItem);
            Item sample = ContentSamples.ItemsByType[ItemID.TheRottedFork];
            float shaft = new Vector2(sample.width, sample.height).Length() * Projectile.scale;

            Vector2 lastButt = butt;
            Vector2 lastTip = tip;

            // Like the vanilla Sleepy Octopod: no arm pose of our own. The item uses the Shoot use style while it spins (see
            // RottedForkGlobalItem.CanUseItem) and vanilla picks the body frame (arm out, raised or lowered) from itemRotation, which is set
            // below. The fork spins around the middle of the player, like the Octopod's staff.
            Vector2 hand = owner.RotatedRelativePoint(owner.MountedCenter, false, true);
            direction = HeldAim.ToWorldDirection(owner, aim);
            butt = hand + direction * (-shaft * RottedForkTuning.GripFraction + RottedForkTuning.Reach);
            tip = butt + direction * shaft;
            Projectile.Center = owner.MountedCenter; // knockback pushes away from the player
            Projectile.rotation = direction.ToRotation();
            Projectile.spriteDirection = SpinDirection; // flipped while the fork turns counter-clockwise on screen
            owner.heldProj = Projectile.whoAmI;      // also ends the projectile when the player swaps items
            owner.SetDummyItemTime(2);               // keeps the item "in use" for as long as the fork spins (the vanilla way, same as the Octopod)
            float seenFromRight = owner.direction == 1 ? aim : MathHelper.Pi - aim; // the aim as a right-facing player would have it
            owner.itemRotation = MathHelper.WrapAngle(seenFromRight + MathHelper.PiOver4) * owner.direction; // what the Octopod's staff rotation gives vanilla

            prevButt = hasPose ? lastButt : butt;
            prevTip = hasPose ? lastTip : tip;
            hasPose = true;

            if (!Main.dedServ)
                Visuals(owner, shaft);

            // Other players' machines do not run OnHitNPC; they find the hit themselves and only show the sparks.
            if (!IsOwner && !Main.dedServ && speedFraction >= RottedForkTuning.MinHitSpeed)
                (remoteHits ??= new RemoteHitTracker()).Update(Projectile, age / Math.Max(1, RottedForkTuning.HitCooldownTicks), npc => ThrustFx.HitSpark(SegmentHit.ClosestPoint(butt, tip, npc.Center), RottedForkTuning.Palette));

            age++;
        }

        // Every machine: the spin-up ring, a whoosh every half turn, flecks off the tip, the light and the trail fan.
        void Visuals(Player owner, float shaft)
        {
            ThrustPalette palette = RottedForkTuning.Palette;

            if (age == 0)
                RottedForkFx.SpinUp(owner.Center, shaft * 0.7f);

            int half = (int)(turned / MathHelper.Pi);
            if (half != halfTurns)
            {
                halfTurns = half;
                RottedForkFx.Whoosh(tip, speedFraction);
            }

            RottedForkFx.Flecks(tip, tip - prevTip, speedFraction, palette);
            Lighting.AddLight(tip, palette.Mid.ToVector3() * 0.6f * speedFraction);

            trail ??= new SweepTrail(new ThrustTrailStyle(palette, RottedForkTuning.TrailLifeTicks, RottedForkTuning.TrailRails, RottedForkTuning.PixelSize, RottedForkTuning.TrailOpacity), IsOwner);
            if (speedFraction > 0.15f)
            {
                for (int i = 1; i <= 2; i++)
                {
                    float f = i / 2f;
                    Vector2 outer = Vector2.Lerp(prevTip, tip, f);
                    Vector2 root = Vector2.Lerp(prevButt, butt, f);
                    trail.Add(outer, Vector2.Lerp(outer, root, RottedForkTuning.TrailInnerFraction));
                }
            }
            trail.Tick();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!hasPose || speedFraction < RottedForkTuning.MinHitSpeed)
                return false;
            return SegmentHit.Sweep(targetHitbox, prevButt, prevTip, butt, tip, RottedForkTuning.HitWidth * Projectile.scale, 3);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X >= Owner.Center.X ? 1 : -1; // always away from the player
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ThrustFx.HitSpark(SegmentHit.ClosestPoint(butt, tip, target.Center), RottedForkTuning.Palette);
        }

        public override void OnKill(int timeLeft)
        {
            if (IsOwner)
                Owner.GetModPlayer<ThrustPlayer>().StartCooldown(ItemID.TheRottedFork, RottedForkTuning.CooldownTicks);
            DetachedTrails.Hand(trail); // the fan keeps fading on its own
            trail = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            ThrustPalette palette = RottedForkTuning.Palette;
            float scale = Projectile.scale;
            float rotation = Projectile.rotation;
            float glow = 0.3f + 0.7f * MathHelper.Clamp(speedFraction, 0f, 1f);

            int flip = Projectile.spriteDirection;
            SpearPose.DrawSprite(texture, butt, rotation, flip, scale * 1.18f, palette.Halo * (0.45f * glow));
            SpearPose.DrawSprite(texture, butt, rotation, flip, scale * 1.07f, palette.Mid * (0.5f * glow));
            SpearPose.DrawSprite(texture, butt, rotation, flip, scale, Color.Lerp(lightColor, Color.White, 0.3f + 0.4f * glow));
            return false;
        }
    }
}