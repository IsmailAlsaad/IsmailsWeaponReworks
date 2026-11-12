using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BallOHurt
{
    /// <summary>
    /// The ball that tears loose from the Ball O' Hurt at the apex of the tier 3 special. It is its own projectile, so the player can use the
    /// weapon again as soon as the chain is home. Three stages (ai[0]):
    /// FREE: it flies very fast (BallSpeed) in a STRAIGHT line with no gravity and mirror-bounces off tiles (the axis it hit is flipped, the
    ///   speed stays). It pierces enemies; the first enemy its path touches starts the hops. No enemy within FreeMaxTicks: it goes home.
    /// HOP: it picks a random enemy within HopRangePx (50 tiles) of itself (not the one it just reached unless it is the only one), aims at
    ///   where that enemy will be when it arrives and flies there in a STRAIGHT line through blocks, piercing everything on the way. Arriving at
    ///   the aim point counts one hop and picks the next enemy (a straight, sharp turn). With one enemy only, the ball flies on through it for
    ///   ReboundPx, then comes back. After MaxHops, or with no enemy left in range, it goes home.
    /// RETURN: it flies straight to the player's hand through blocks (harmless) and is caught there.
    /// MULTIPLAYER: the owner's client decides everything (tile bounces, the first hit, every target pick, the end) and syncs it with
    /// netUpdate at each event plus every ResyncTicks ticks while the ball is out. ai[0] is the stage; SendExtraAI carries the hop and bounce
    /// counters and the ball's velocity. Every machine flies the same straight lines and mirror bounces itself from the last synced position
    /// (nothing is synced per tick). All damage is the vanilla projectile hit on the owner's client; the effects are local and follow the
    /// synced counters. No custom packets.
    /// </summary>
    public class BallOHurtBallProjectile : ModProjectile
    {
        // Vanilla art; the weapon's own art is BallOHurtFlailProjectile.png (the same sprite as the head), when it exists.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallOHurt;

        enum Stage { Free = 0, Hop = 1, Return = 2 }

        static BallOHurtTuning Ball => BallOHurtTuning.Instance;
        static BallOHurtFx BallFx => BallOHurtFx.Instance;
        Stage CurrentStage => (Stage)(int)Projectile.ai[0];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // Synced through SendExtraAI.
        int hops;
        int bounces;
        Vector2 ballVelocity;

        // Per-machine state, never synced.
        int lastStage = -1;
        int age;
        int freeTicks;
        int hopTicks;
        int syncClock;
        int fxHops;
        int fxBounces;
        Vector2 sweepStart;
        PixelTrail trail;
        AfterimageTrail afterimages;
        RemoteHitTracker hitTracker;   // other machines only
        Asset<Texture2D> art;
        bool artTried;

        // Owner only: where the current hop is headed.
        Vector2 aimPoint;
        int aimNpc = -1;
        bool aimIsWaypoint;
        bool rebounded;
        int lastHit = -1;

        public override void SetDefaults()
        {
            Projectile.width = (int)Ball.HeadSize;
            Projectile.height = (int)Ball.HeadSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000;   // ended by the AI, never by this countdown
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Ball.BallHitCooldownTicks;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)hops);
            writer.Write((byte)bounces);
            writer.Write(ballVelocity.X);
            writer.Write(ballVelocity.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            hops = reader.ReadByte();
            bounces = reader.ReadByte();
            ballVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            if (age == 0 && ballVelocity == Vector2.Zero)
                ballVelocity = Projectile.velocity;   // the spawn velocity is the first direction (every machine gets it with the spawn)
            Projectile.velocity = Vector2.Zero;       // every move is a direct position change
            sweepStart = Projectile.Center;

            Stage stage = CurrentStage;
            if ((int)stage != lastStage)
            {
                EnterStage(stage);
                lastStage = (int)stage;
            }
            age++;

            // Effects follow the counters, so every machine plays them (the owner changes them, the others receive them).
            if (hops != fxHops)
            {
                fxHops = hops;
                if (hops > 0)
                    BallFx.Hop(Projectile.Center, hops);
            }
            if (bounces != fxBounces)
            {
                fxBounces = bounces;
                BallFx.TileBounce(Projectile.Center);
            }

            Vector2 oldCenter = Projectile.Center;
            switch (stage)
            {
                case Stage.Free:
                    UpdateFree(oldCenter);
                    break;
                case Stage.Hop:
                    UpdateHop(oldCenter);
                    break;
                default:
                    UpdateReturn(owner);
                    break;
            }
            if (!Projectile.active)
                return;

            Projectile.rotation += 0.45f;
            trail?.Add(Projectile.Center);
            trail?.Tick();
            if (!Main.dedServ)
            {
                afterimages ??= AfterimageTrail.Sprite(HeadTexture(), Ball.AfterimageTicks, 0.7f);
                afterimages.Tick();
                afterimages.Add(Projectile.Center, Projectile.rotation, 1f, Ball.GlowColor(3) * 0.8f);
            }
            BallFx.FlightDust(Projectile.Center, Projectile.Center - oldCenter, 3);
            Lighting.AddLight(Projectile.Center, 0.6f, 0.25f, 0.95f);

            if (!IsOwner)
            {
                if (stage != Stage.Return)
                {
                    hitTracker ??= new RemoteHitTracker();
                    hitTracker.Update(Projectile, (int)stage, npc => BallFx.HitSparks(npc.Center, 3));
                }
            }
            else if (stage != Stage.Return && ++syncClock >= Math.Max(1, Ball.ResyncTicks))
            {
                syncClock = 0;
                Projectile.netUpdate = true;
            }

            if (age > 3600)
                Projectile.Kill();   // safety
        }

        // Every machine, once per stage.
        void EnterStage(Stage stage)
        {
            switch (stage)
            {
                case Stage.Free:
                    if (!Main.dedServ)
                    {
                        trail = new PixelTrail(new FlailTrailStyle(Ball, 3), IsOwner);
                        BallFx.Detach(Projectile.Center, ballVelocity);
                    }
                    break;
                case Stage.Return:
                    BallFx.Recall(Projectile.Center);
                    break;
            }
        }

        // ---- stage FREE: straight lines, mirror bounces off tiles ----

        void UpdateFree(Vector2 oldCenter)
        {
            freeTicks++;
            MoveFree();
            if (!IsOwner)
                return;

            NPC first = FirstEnemyOnPath(oldCenter, Projectile.Center);
            if (first != null)
            {
                Projectile.ai[0] = (float)Stage.Hop;
                hops = 0;
                Reached(first.whoAmI);
                return;
            }
            if (freeTicks > Ball.FreeMaxTicks)
                StartReturn();
        }

        bool Solid(Vector2 point)
        {
            float box = Math.Max(2f, Ball.BallBoxPx);
            return Collision.SolidCollision(point - new Vector2(box * 0.5f), (int)box, (int)box);
        }

        // Every machine: the ball moves in small steps and mirrors off whatever tile it hits (the axis it hit is flipped, the speed stays).
        void MoveFree()
        {
            Vector2 v = ballVelocity;
            int steps = Math.Max(1, (int)Math.Ceiling(v.Length() / 8f));
            Vector2 step = v / steps;
            Vector2 position = Projectile.Center;
            int bouncesThisTick = 0;
            for (int i = 0; i < steps; i++)
            {
                Vector2 next = position + step;
                if (Solid(next))
                {
                    bool hitX = Solid(position + new Vector2(step.X, 0f));
                    bool hitY = Solid(position + new Vector2(0f, step.Y));
                    if (hitX && !hitY)
                        v.X = -v.X;
                    else if (hitY && !hitX)
                        v.Y = -v.Y;
                    else
                        v = -v;
                    step = v / steps;
                    bounces++;
                    bouncesThisTick++;
                    if (IsOwner)
                        Projectile.netUpdate = true;
                    if (bouncesThisTick >= 3)
                        break;
                    next = position + step;
                    if (Solid(next))
                        continue;   // jammed in a corner: stay for this step
                }
                position = next;
            }
            ballVelocity = v;
            Projectile.Center = position;
        }

        // ---- stage HOP: straight lines between enemies, through blocks ----

        void UpdateHop(Vector2 oldCenter)
        {
            hopTicks++;
            Projectile.Center += ballVelocity;   // a straight line, through blocks
            if (!IsOwner)
                return;

            // The target died on the way: pick another one from here.
            if (!aimIsWaypoint && aimNpc >= 0 && !Targetable(Main.npc[aimNpc]))
            {
                PickTarget();
                return;
            }

            bool arrived = DistanceToSegment(aimPoint, oldCenter, Projectile.Center) <= Ball.ArriveRadiusPx;
            if (!arrived && hopTicks <= Ball.HopMaxTicks)
                return;   // still on the way (a hop that takes too long counts as arrived, so the ball never flies on forever)
            if (aimIsWaypoint)
            {
                rebounded = true;
                PickTarget();
            }
            else
                Reached(aimNpc);
        }

        // Enemies the ball may fly at or count: anything that can be damaged (the target dummy too, which NPC.CanBeChasedBy rejects), no critters.
        static bool Targetable(NPC npc)
        {
            return Snarable.CanBite(npc) && !npc.CountsAsACritter;
        }

        static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.LengthSquared();
            float t = lengthSquared < 0.0001f ? 0f : MathHelper.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
            return Vector2.Distance(point, a + ab * t);
        }

        // Owner only: the ball reached enemy 'npcIndex' (the vanilla hit comes from the projectile's own collision): count it, pick the next
        // one or go home.
        void Reached(int npcIndex)
        {
            hops++;
            lastHit = npcIndex;
            rebounded = false;
            if (hops >= Ball.MaxHops)
            {
                StartReturn();
                return;
            }
            PickTarget();
        }

        // Owner only: a random enemy within HopRangePx of the ball, not the one just reached unless it is the only one. The ball turns at once
        // and flies at where the enemy will be. None in range: home.
        void PickTarget()
        {
            List<int> pool = new List<int>();
            bool lastInRange = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!Targetable(npc))
                    continue;
                float reach = Ball.HopRangePx + Math.Max(npc.width, npc.height) * 0.5f;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > reach * reach)
                    continue;
                if (i == lastHit)
                {
                    lastInRange = true;
                    continue;
                }
                pool.Add(i);
            }
            float speed = Math.Max(1f, Ball.BallSpeed);
            if (pool.Count == 0 && lastInRange)
            {
                if (!rebounded)
                {
                    // Only one enemy: the ball flies on through it for ReboundPx (a waypoint), then comes back.
                    aimIsWaypoint = true;
                    aimNpc = -1;
                    Vector2 direction = ballVelocity.SafeNormalize(Vector2.UnitX);
                    aimPoint = Projectile.Center + direction * Ball.ReboundPx;
                    ballVelocity = direction * speed;
                    hopTicks = 0;
                    Projectile.netUpdate = true;
                    return;
                }
                pool.Add(lastHit);
            }
            if (pool.Count == 0)
            {
                StartReturn();
                return;
            }

            int pick = pool[Main.rand.Next(pool.Count)];
            NPC target = Main.npc[pick];
            float travelTicks = Vector2.Distance(Projectile.Center, target.Center) / speed;
            aimPoint = target.Center + target.velocity * Math.Min(travelTicks, 30f);   // where it will be when the ball arrives
            aimNpc = pick;
            aimIsWaypoint = false;
            ballVelocity = (aimPoint - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
            hopTicks = 0;
            Projectile.netUpdate = true;
        }

        // Owner only: the nearest enemy the swept path touches this tick (the ball pierces, so this only decides when the hops begin).
        NPC FirstEnemyOnPath(Vector2 from, Vector2 to)
        {
            NPC best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!Targetable(npc))
                    continue;
                float unused = 0f;
                if (!Collision.CheckAABBvLineCollision(npc.Hitbox.TopLeft(), npc.Hitbox.Size(), from, to, Ball.HeadSize, ref unused))
                    continue;
                float distance = Vector2.DistanceSquared(from, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = npc;
                }
            }
            return best;
        }

        // ---- stage RETURN ----

        // Owner only: out of hops (or no enemy): the ball goes home. Also used to make room for a newer ball.
        void StartReturn()
        {
            if (!IsOwner || CurrentStage == Stage.Return)
                return;
            Projectile.ai[0] = (float)Stage.Return;
            Projectile.netUpdate = true;
        }

        // Every machine: straight to the hand through blocks; caught there.
        void UpdateReturn(Player owner)
        {
            Vector2 toHand = owner.MountedCenter - Projectile.Center;
            float speed = Math.Max(1f, Ball.BallReturnSpeed);
            if (toHand.Length() <= speed + Ball.CatchDistance)
            {
                BallFx.CatchHead(owner.MountedCenter);
                Projectile.Kill();
                return;
            }
            ballVelocity = toHand.SafeNormalize(Vector2.UnitX) * speed;
            Projectile.Center += ballVelocity;
        }

        // Owner's client only (the flail calls it right before it spawns a new ball): if the player already has 'maxBalls' balls out, the oldest
        // one is recalled now.
        public static void MakeRoom(int owner, int maxBalls)
        {
            int type = ModContent.ProjectileType<BallOHurtBallProjectile>();
            int count = 0;
            BallOHurtBallProjectile oldest = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.type != type || other.owner != owner || !(other.ModProjectile is BallOHurtBallProjectile ball))
                    continue;
                if (ball.CurrentStage == Stage.Return)
                    continue;
                count++;
                if (oldest == null || ball.age > oldest.age)
                    oldest = ball;
            }
            if (count >= maxBalls && oldest != null)
                oldest.StartReturn();
        }

        // ---- hits ----

        // While spinning along its path the ball hits everything it passed this tick, so a very fast ball never skips an enemy.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Vector2.DistanceSquared(sweepStart, Projectile.Center) > 400f * 400f ? Projectile.Center : sweepStart;
            float unused = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, Projectile.Center, Ball.HeadSize, ref unused);
        }

        public override bool? CanDamage()
        {
            return CurrentStage == Stage.Return ? false : (bool?)null;   // the way home is harmless
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BallFx.HitSparks(target.Center, 3);
        }

        // ---- drawing ----

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            DetachedTrails.Hand(afterimages);
            trail = null;
            afterimages = null;
        }

        Texture2D HeadTexture()
        {
            if (!artTried)
            {
                artTried = true;
                ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/BallOHurt/BallOHurtFlailProjectile", out art);   // the head's own art when its PNG exists
            }
            return art != null && art.IsLoaded ? art.Value : TextureAssets.Projectile[Type].Value;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            afterimages?.Draw();
            trail?.Draw();

            Texture2D texture = HeadTexture();
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, position, null, Ball.GlowColor(3) * 0.6f, Projectile.rotation, origin, 1.35f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, Ball.HotColor(3) * 0.5f, Projectile.rotation, origin, 1.15f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.6f), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}