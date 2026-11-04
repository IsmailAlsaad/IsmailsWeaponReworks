using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Common
{
    /// <summary>
    /// The shared held projectile for every thrusting spear. It follows the owner's hand, poses the arm, runs a ThrustChain (a burst of
    /// thrusts, or an endless one), draws the vanilla item sprite with a streak trail, and hits along the shaft. A weapon derives from it
    /// and only supplies numbers (ItemType, Chain, Reach, Palette) and, if it needs them, the virtual hooks.
    /// SPAWN VALUES (the GlobalItem's Shoot passes them): ai[0] = seed (0..9999, the angle variance) + 10000 * mode (a free value for the
    /// weapon, for example the Swordfish's rage), ai[1] = 0 (the owner writes the end age + 1 here for an endless chain), ai[2] = the aim in
    /// the player's own frame (HeldAim.Capture).
    /// MULTIPLAYER: the owner's client decides everything (the click, the aim, every hit, when an endless chain ends); at the start of each
    /// thrust after the first it re-captures the cursor into ai[2] with netUpdate. Which thrust is running, its extension and its jitter
    /// are pure functions of the projectile's age and the seed, so every machine poses, draws and sounds the same thrust with no packets
    /// (a machine that gets the new aim a few ticks late only sees that thrust start from the old one). Hits are detected by the owner's
    /// client like any projectile; other players' machines show the hit sparks through RemoteHitTracker.
    /// </summary>
    public abstract class HeldSpearProjectile : ModProjectile
    {
        // ---- what a weapon supplies ----

        protected abstract int ItemType { get; }              // the vanilla item this belongs to; also supplies the sprite
        protected abstract ThrustChain Chain { get; }
        protected abstract float Reach { get; }               // how far the spear is pushed forward at full extension (pixels)
        protected abstract ThrustPalette Palette { get; }

        protected virtual float SpearScale => 1f;
        protected virtual float GripFraction => 0.35f;        // how much of the shaft sticks out behind the hand at rest
        protected virtual float HitWidth => 22f;              // thickness of the hit line (pixels at scale 1)
        protected virtual bool RetargetEachThrust => true;    // the owner re-aims at the cursor at the start of every thrust after the first
        protected virtual int TrailLifeTicks => 12;
        protected virtual int TrailRails => 5;
        protected virtual float TrailOpacity => 0.7f;
        protected virtual float PixelSize => 2f;
        protected virtual float TrailTaper => 0f;             // 0 = a streak of even width; 0.7 = the tail starts as a point and reaches full width 70% of the way through the strike

        protected virtual void OnThrustStart(int index) { }
        protected virtual void OnStrikeStart(int index) { }  // every machine, the tick the spear starts to shoot out (Direction, Tip and Butt are already posed)
        protected virtual void OnTipStep(Vector2 tip, Vector2 direction, float extension) { }
        protected virtual void OnTipHit(NPC target, NPC.HitInfo hit, int damageDone) { }
        protected virtual bool ShouldContinue(int index) { return false; } // endless chains only: owner decides at each thrust start
        protected virtual float ThrustPitch(int index) { return -0.05f + 0.08f * (index % 4); }
        protected virtual float SpriteRotation => 0f;          // radians added when the sprite is drawn (mirrored with it), for art that is not drawn like the item sprite
        protected virtual Vector2 SpritePivot => Vector2.Zero;   // texture pixels the pivot is shifted by

        // Butt to tip. The item sprite's diagonal by default; a spear that draws another texture measures that one.
        protected virtual float ShaftLength()
        {
            Item sample = ContentSamples.ItemsByType[ItemType];
            return new Vector2(sample.width, sample.height).Length() * Projectile.scale;
        }

        // The vanilla item sprite is the projectile sprite.
        public override string Texture => "Terraria/Images/Item_" + ItemType;

        // ---- what a weapon can read ----

        protected Player Owner => Main.player[Projectile.owner];
        protected bool IsOwner => Projectile.owner == Main.myPlayer;
        protected float BaseAim => Projectile.ai[2];
        protected int Seed => (int)Projectile.ai[0] % 10000;
        protected int Mode => (int)Projectile.ai[0] / 10000;
        protected int EndAge => (int)Projectile.ai[1] - 1;    // -1 = no end set yet
        protected int Age => age;
        protected int ThrustIndex => lastIndex;
        protected float Extension => extension;
        protected bool Striking => striking;
        protected Vector2 Butt;                                // where the spear's butt is
        protected Vector2 Tip;                                 // where its tip is
        protected Vector2 Direction;                           // the shaft's world direction (a unit vector)

        // Per-machine state: counters and visuals, never synced.
        int age;                  // ticks this machine has run this projectile
        int lastIndex = -1;       // the thrust the last tick was in
        float fromAim;            // the aim the current thrust blends from
        float toAim;              // and the one it points at (the base aim plus this thrust's jitter)
        int facing = 1;
        float extension;
        bool striking;
        bool hasPose;
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
            Projectile.timeLeft = 10000; // ended by the AI, never by this countdown
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once per thrust (cleared at each thrust start)
            Projectile.ownerHitCheck = true;
        }

        public override void AI()
        {
            Player owner = Owner;

            // Gone with the player, with the item in their hand, or when they get crowd-controlled (a stun or a freeze).
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemType)
            {
                Projectile.Kill();
                return;
            }

            ThrustChain chain = Chain;
            if (chain.IsFinished(age) || (EndAge >= 0 && age >= EndAge))
            {
                owner.itemAnimation = 0; // the next use may start on the very next tick
                owner.itemTime = 0;
                Projectile.Kill();
                return;
            }

            chain.Locate(age, out int index, out int tickIn);
            if (index != lastIndex)
                BeginThrust(index, chain);

            ThrustTimeline timeline = chain.Timeline;
            bool inThrust = tickIn < timeline.TotalTicks;
            extension = inThrust ? timeline.Extension(tickIn) : 0f;
            striking = inThrust && timeline.IsStriking(tickIn);

            // The aim blends from the previous thrust's aim over the windup, so the arm swings round instead of snapping.
            float blend = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((tickIn + 1f) / Math.Max(1, timeline.WindupTicks), 0f, 1f));
            float aim = fromAim + MathHelper.WrapAngle(toAim - fromAim) * blend;

            Projectile.scale = SpearScale * owner.GetAdjustedItemScale(owner.HeldItem);
            float shaft = ShaftLength();

            Vector2 lastButt = Butt;
            Vector2 lastTip = Tip;

            SpearPose.Arm(owner, aim);
            Vector2 hand = SpearPose.Hand(owner, aim);
            Direction = HeldAim.ToWorldDirection(owner, aim);
            Butt = hand + Direction * (-shaft * GripFraction + extension * Reach);
            Tip = Butt + Direction * shaft;
            Projectile.Center = Butt;
            Projectile.rotation = Direction.ToRotation();
            Projectile.spriteDirection = facing;
            owner.heldProj = Projectile.whoAmI; // also ends the projectile when the player swaps items
            owner.itemAnimation = 2;            // keeps the item "in use" for as long as the spear is out
            owner.itemTime = 2;

            prevButt = hasPose ? lastButt : Butt;
            prevTip = hasPose ? lastTip : Tip;
            hasPose = true;

            if (tickIn == timeline.StrikeStart)
            {
                ThrustFx.PlayThrust(Tip, ThrustPitch(index));
                OnStrikeStart(index);
            }

            if (striking && extension > 0.3f)
            {
                ThrustFx.TipStreak(Tip, Direction, Palette);
                OnTipStep(Tip, Direction, extension);
            }

            LayTrail(tickIn, timeline);
            Lighting.AddLight(Tip, Palette.Mid.ToVector3() * 0.5f * Math.Max(0f, extension));

            // Other players' machines do not run OnHitNPC; they find the hit themselves and only show the sparks.
            if (!IsOwner && !Main.dedServ && striking)
                (remoteHits ??= new RemoteHitTracker()).Update(Projectile, index, npc => ThrustFx.HitSpark(SegmentHit.ClosestPoint(Butt, Tip, npc.Center), Palette));

            age++;
        }

        // Every machine, at the first tick of each thrust.
        void BeginThrust(int index, ThrustChain chain)
        {
            bool first = lastIndex < 0;
            lastIndex = index;

            if (IsOwner)
            {
                bool dirty = false;
                if (index > 0 && RetargetEachThrust)
                {
                    Projectile.ai[2] = HeldAim.Capture(Owner);
                    dirty = true;
                }
                // An endless chain: decide now whether THIS thrust is the last one, a whole thrust ahead of its end, so the other machines
                // have time to hear about it (the end age is this thrust's recovery end).
                if (chain.Endless && !ShouldContinue(index))
                {
                    Projectile.ai[1] = chain.EndOfThrust(index) + 1f;
                    dirty = true;
                }
                if (dirty)
                    Projectile.netUpdate = true;
            }

            float target = BaseAim + AimSpread.Offset(Seed, index, chain.SpreadDeg);
            fromAim = first ? target : toAim;
            toAim = target;
            facing = HeldAim.Facing(toAim);
            if (IsOwner)
                Owner.ChangeDir(facing);

            SegmentHit.ClearLocalImmunity(Projectile); // every enemy may be hit again

            if (!Main.dedServ)
            {
                DetachedTrails.Hand(trail); // the last thrust's streak keeps fading on its own
                trail = new SweepTrail(new ThrustTrailStyle(Palette, TrailLifeTicks, TrailRails, PixelSize, TrailOpacity), IsOwner);
            }

            OnThrustStart(index);
        }

        void LayTrail(int tickIn, ThrustTimeline timeline)
        {
            if (trail == null)
                return;
            // The streak grows while the spear shoots out and stays out; the way back lays nothing.
            if (tickIn >= timeline.StrikeStart && tickIn < timeline.StrikeEnd && extension > 0.05f)
            {
                float back = (Tip - Butt).Length() * 0.5f;
                float taper = TrailTaper;
                int strikeLength = Math.Max(1, timeline.StrikeEnd - timeline.StrikeStart);
                for (int i = 1; i <= 2; i++)
                {
                    float f = i / 2f;
                    Vector2 outer = Vector2.Lerp(prevTip, Tip, f);
                    // With a taper the oldest part of the streak is a thin point that widens toward the spear.
                    float width = 1f;
                    if (taper > 0f)
                    {
                        float along = (tickIn - timeline.StrikeStart - 1f + f) / strikeLength;
                        width = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(along / taper, 0f, 1f));
                    }
                    trail.Add(outer, outer - Direction * (back * width));
                }
            }
            trail.Tick();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!striking)
                return false;
            return SegmentHit.Sweep(targetHitbox, prevButt, prevTip, Butt, Tip, HitWidth * Projectile.scale, 2);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ThrustFx.HitSpark(SegmentHit.ClosestPoint(Butt, Tip, target.Center), Palette);
            OnTipHit(target, hit, damageDone);
        }

        public override void OnKill(int timeLeft)
        {
            if (IsOwner)
                Owner.GetModPlayer<ThrustPlayer>().StartCooldown(ItemType, Chain.CooldownTicks);
            DetachedTrails.Hand(trail);
            trail = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            ThrustPalette palette = Palette;
            float scale = Projectile.scale;
            float rotation = Projectile.rotation;
            float glow = 0.2f + 0.8f * MathHelper.Clamp(extension, 0f, 1f);

            SpearPose.DrawSprite(texture, Butt, rotation, Projectile.spriteDirection, scale * 1.18f, palette.Halo * (0.45f * glow), SpriteRotation, SpritePivot);
            SpearPose.DrawSprite(texture, Butt, rotation, Projectile.spriteDirection, scale * 1.07f, palette.Mid * (0.5f * glow), SpriteRotation, SpritePivot);
            SpearPose.DrawSprite(texture, Butt, rotation, Projectile.spriteDirection, scale, Color.Lerp(lightColor, Color.White, 0.3f + 0.4f * glow), SpriteRotation, SpritePivot);
            return false;
        }
    }
}