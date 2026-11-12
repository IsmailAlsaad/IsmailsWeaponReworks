using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.BallOHurt
{
    /// <summary>
    /// The Ball O' Hurt's flail head: the shared flail loop (Common/Flail/FlailProjectile.cs) plus its own parts: numbers and effects, and the
    /// tier 3 special. The head itself becomes the ball (no second projectile), in three stages:
    /// THROW: the head flies straight at the cursor (hurting enemies on the way). At the apex (or a step before the first tile it touches)
    ///   the ball tears loose and the chain is hidden.
    /// FREE: the ball flies very fast with no gravity and bounces off tiles like a mirror (speed kept). It pierces enemies. The first enemy
    ///   it hits starts the hops. No enemy within FreeMaxTicks: it goes home.
    /// HOP: the ball steers toward a random enemy within HopRangePx (50 tiles) of itself, flying through blocks, piercing everything on its
    ///   way. Reaching the target counts one hop and picks the next random enemy (not the one just reached unless it is the only one). After
    ///   MaxHops, or when no enemy is left in range, the shared retract flies the ball straight home through blocks; a held button then
    ///   restarts the spin at once.
    /// MULTIPLAYER: the owner's client decides everything (the tear-off, tile bounces, the first hit, every target pick, the end) and syncs
    /// it with netUpdate at each event plus every ResyncTicks ticks. SendExtraAI carries the stage, the hop and bounce counters, the target
    /// enemy index and the ball's velocity. Every machine flies the same flight itself from the last synced position (a straight line, the
    /// mirror bounces, the capped turn toward Main.npc[target].Center), so there is no per-tick sync. All damage is the vanilla projectile
    /// hit on the owner's client; effects are local and are triggered by the synced counters.
    /// </summary>
    public class BallOHurtFlailProjectile : FlailProjectile
    {
        // Vanilla art until a BallOHurtFlailProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallOHurt;

        protected override int ItemType => ItemID.BallOHurt;
        protected override FlailTuning Tuning => BallOHurtTuning.Instance;
        protected override FlailFx Fx => BallOHurtFx.Instance;
        protected override bool SweepsPath => CurrentPhase == Phase.Charge || CurrentPhase == Phase.Special;  // the ball is far faster than it is wide
        protected override bool ChainHidden => CurrentPhase == Phase.Special && stage != (int)Stage.Throw;     // the loose ball has no chain

        static BallOHurtTuning Ball => BallOHurtTuning.Instance;
        static BallOHurtFx BallFx => BallOHurtFx.Instance;

        enum Stage { Throw = 0, Free = 1, Hop = 2 }

        // Synced through SendExtraAI.
        int stage;
        int hops;
        int bounces;
        int targetIndex = -1;
        Vector2 ballVelocity;

        // Per-machine state, never synced.
        Vector2 specialTarget;
        int freeTicks;
        int hopTicks;
        int arriveCooldown;
        int syncClock;
        int lastHit = -1;        // owner only
        int fxStage;
        int fxHops;
        int fxBounces;
        bool recallPlayed;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)stage);
            writer.Write((byte)hops);
            writer.Write((byte)bounces);
            writer.Write((short)targetIndex);
            writer.Write(ballVelocity.X);
            writer.Write(ballVelocity.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            stage = reader.ReadByte();
            hops = reader.ReadByte();
            bounces = reader.ReadByte();
            targetIndex = reader.ReadInt16();
            ballVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        // ---- the special ----

        protected override void BeginSpecial(Player owner)
        {
            stage = (int)Stage.Throw;
            hops = 0;
            bounces = 0;
            targetIndex = -1;
            ballVelocity = Vector2.Zero;
            freeTicks = 0;
            hopTicks = 0;
            arriveCooldown = 0;
            syncClock = 0;
            lastHit = -1;
            fxStage = 0;
            fxHops = 0;
            fxBounces = 0;
            recallPlayed = false;
            specialTarget = owner.MountedCenter + AimAngle.ToRotationVector2() * AimReach;
            if (IsOwner)
                Projectile.localNPCHitCooldown = Math.Max(1, Ball.BallHitCooldownTicks);
        }

        protected override void UpdateSpecial(Player owner)
        {
            // Effects follow the counters, so every machine plays them (the owner changes them, the others receive them).
            if (stage != fxStage)
            {
                fxStage = stage;
                if (stage == (int)Stage.Free)
                    BallFx.Detach(Projectile.Center, ballVelocity);
            }
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
            switch ((Stage)stage)
            {
                case Stage.Throw:
                    UpdateThrow(oldCenter);
                    break;
                case Stage.Free:
                    UpdateFree(oldCenter);
                    break;
                default:
                    UpdateHop(oldCenter);
                    break;
            }
            if (CurrentPhase != Phase.Special)
                return;   // the owner ended the special

            trail?.Add(Projectile.Center);
            Fx.FlightDust(Projectile.Center, Projectile.Center - oldCenter, 3);
            Lighting.AddLight(Projectile.Center, 0.6f, 0.25f, 0.95f);

            if (IsOwner && stage != (int)Stage.Throw && ++syncClock >= Math.Max(1, Ball.ResyncTicks))
            {
                syncClock = 0;
                Projectile.netUpdate = true;
            }
        }

        // ---- stage 1: the straight throw ----

        void UpdateThrow(Vector2 oldCenter)
        {
            Vector2 toTarget = specialTarget - Projectile.Center;
            float speed = Ball.SpecialSpeed;
            bool arrived = toTarget.Length() <= speed;
            Projectile.Center = arrived ? specialTarget : Projectile.Center + toTarget.SafeNormalize(Vector2.UnitX) * speed;
            if (!IsOwner)
                return;
            bool blocked = phaseTimer > 2 && Collision.SolidCollision(Projectile.Center - new Vector2(6f), 12, 12);
            if (arrived || blocked)
                TearLoose(blocked ? oldCenter : Projectile.Center);
        }

        // Owner only: the ball goes loose where the head is (a step back from the tile if it ran into one), flying on in the throw's direction.
        void TearLoose(Vector2 at)
        {
            Projectile.Center = at;
            stage = (int)Stage.Free;
            freeTicks = 0;
            ballVelocity = AimAngle.ToRotationVector2() * Ball.BallSpeed;
            Projectile.damage = Math.Max(1, (int)(lockedDamage * Ball.BallDamageMultiplier));
            Projectile.netUpdate = true;
        }

        // ---- stage 2: free flight, bouncing off tiles ----

        void UpdateFree(Vector2 oldCenter)
        {
            freeTicks++;
            MoveFree();
            if (!IsOwner)
                return;

            NPC first = FirstEnemyOnPath(oldCenter, Projectile.Center);
            if (first != null)
            {
                stage = (int)Stage.Hop;
                hops = 0;
                Reached(first);
                return;
            }
            if (freeTicks > Ball.FreeMaxTicks)
                EndSpecial();
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

        // ---- stage 3: hopping between enemies ----

        void UpdateHop(Vector2 oldCenter)
        {
            if (arriveCooldown > 0)
                arriveCooldown--;
            hopTicks++;

            NPC target = CurrentTarget();
            if (IsOwner && (target == null || hopTicks > Ball.HopMaxTicks))
            {
                PickTarget();
                if (CurrentPhase != Phase.Special)
                    return;
                target = CurrentTarget();
            }

            Vector2 v = ballVelocity;
            if (target != null)
            {
                Vector2 toTarget = target.Center - Projectile.Center;
                if (arriveCooldown <= 0 && toTarget.Length() < Ball.HopSnapDistancePx)
                    v = toTarget.SafeNormalize(Vector2.UnitX) * Ball.BallSpeed;   // close enough: aim straight, so it never orbits the target
                else
                {
                    float have = v.ToRotation();
                    float turn = MathHelper.Clamp(MathHelper.WrapAngle(toTarget.ToRotation() - have), -Ball.HopTurnRate, Ball.HopTurnRate);
                    v = (have + turn).ToRotationVector2() * Ball.BallSpeed;
                }
            }
            ballVelocity = v;
            Projectile.Center += v;   // through blocks

            if (!IsOwner || target == null || arriveCooldown > 0)
                return;
            float unused = 0f;
            if (Collision.CheckAABBvLineCollision(target.Hitbox.TopLeft(), target.Hitbox.Size(), oldCenter, Projectile.Center, Tuning.HeadSize, ref unused))
                Reached(target);
        }

        NPC CurrentTarget()
        {
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[targetIndex];
            return Targetable(npc) ? npc : null;
        }

        // Enemies the ball may fly at or count: anything that can be damaged (the target dummy too, which NPC.CanBeChasedBy rejects), no critters.
        static bool Targetable(NPC npc)
        {
            return Snarable.CanBite(npc) && !npc.CountsAsACritter;
        }

        // Owner only: the ball reached 'npc' (the vanilla hit comes from the projectile's own collision): count it, pick the next one or go home.
        void Reached(NPC npc)
        {
            hops++;
            lastHit = npc.whoAmI;
            arriveCooldown = Math.Max(0, Ball.ArriveCooldownTicks);
            if (hops >= Ball.MaxHops)
            {
                EndSpecial();
                return;
            }
            PickTarget();
            Projectile.netUpdate = true;
        }

        // Owner only: a random enemy within HopRangePx of the ball, not the one just reached unless it is the only one. None in range: home.
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
            if (pool.Count == 0 && lastInRange)
                pool.Add(lastHit);
            if (pool.Count == 0)
            {
                EndSpecial();
                return;
            }
            targetIndex = pool[Main.rand.Next(pool.Count)];
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
                if (!Collision.CheckAABBvLineCollision(npc.Hitbox.TopLeft(), npc.Hitbox.Size(), from, to, Tuning.HeadSize, ref unused))
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

        // The recall sound: once, on every machine, when the ball turns for home.
        public override void PostAI()
        {
            base.PostAI();
            if (CurrentPhase == Phase.Retract && !recallPlayed)
            {
                recallPlayed = true;
                BallFx.Recall(Projectile.Center);
            }
        }
    }
}