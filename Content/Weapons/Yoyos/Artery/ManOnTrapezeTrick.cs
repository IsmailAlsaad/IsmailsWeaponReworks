using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    /// <summary>
    /// The Artery's trick, Man on Trapeze: the ForwardPassTrick pattern, but the string tries to LOOP an enemy.
    /// ARC: the pass flies as a Forward Pass. Every tick the owner's client checks whether the string (the line hand to yoyo) or the yoyo
    /// touches an enemy that can be damaged (Snarable.CanBite); the first one found is the target (one loop at a time).
    /// CATCH (LoopCatchTicks): the yoyo leaves the ellipse and swings AROUND the enemy on one smooth path (a spiral round the enemy,
    /// bent so it starts with the speed and direction the yoyo already had: no harsh turn from the straight line to sideways) and comes
    /// back to the string line on the player's side of the enemy. The string is not redrawn: it is the line hand to where the catch began
    /// plus the yoyo's own flight path, so the loop FORMS from the flight: where the path comes back to the line, the string has a teardrop
    /// loop over the enemy with the yoyo at its tip. The flight path is remembered relative to the enemy, so the loop rides along with it.
    /// CONSTRICT (LoopConstrictTicks): the yoyo spin-walks along the line toward the player (it follows the enemy, which keeps moving)
    /// while the loop shrinks toward the enemy, its tip pulled along by the yoyo. The loop CLOSES when it has shrunk, or at once when the
    /// enemy dies; the yoyo then returns to the hand. It BREAKS (no close) when the enemy gets farther from the hand than the string is long.
    /// A grippable enemy (Snarable.CanGrip: not a boss, not immovable, not a worm segment, not huge) is HELD in place for the whole loop
    /// (NpcHold); anything else is looped but keeps moving.
    /// MULTIPLAYER: the owner decides the target, the start, the close and the break; the data travels in ArteryProjectile's extra sync.
    /// Every machine runs the same path from it (and remembers its own flight path), refreshes the hold on its own copy of the enemy, and
    /// draws the string itself (visual only). Patch A1 has no damage change: the shred hits and the blood explosion come in A2.
    /// </summary>
    public class ManOnTrapezeTrick : ForwardPassTrick
    {
        int loopTick = -1;        // -1 = the arc is flying; 0 and up = the loop is running
        bool ended;               // the loop is over (closed or broken): the yoyo is on its way back
        int targetIndex = -1;
        int targetType;
        bool snared;
        float baseRadius;         // how big the loop is meant to be (half the enemy's size plus a pad)
        float shrink = 1f;        // how much of its size the loop has left (1 = just formed)
        float startAngle;         // where the catch began, seen from the enemy
        float startRadius;
        float sweepSign = 1f;     // which way round the enemy the yoyo swings
        Vector2 loopStart;
        Vector2 loopStartVel;
        Vector2 lastEnemy;        // where the enemy was last (the loop closes there when the enemy is gone)
        Vector2 lastHand;
        Vector2 lastCenter;
        bool haveLast;
        readonly List<Vector2> trail = new List<Vector2>();    // the yoyo's flight path since the catch, relative to the enemy
        readonly List<Vector2> shape = new List<Vector2>();    // scratch: the loop's points in the world

        public ManOnTrapezeTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        ArteryTuning A => (ArteryTuning)T;

        public override string Name => "Man on Trapeze";

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            base.Begin(yoyo, authoritative);
            loopTick = -1;
            ended = false;
            targetIndex = -1;
            haveLast = false;
            shrink = 1f;
            trail.Clear();
            if (authoritative)
                (yoyo as ArteryProjectile)?.ClearLoop();
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            ArteryProjectile artery = yoyo as ArteryProjectile;
            if (artery == null)
            {
                base.Tick(yoyo);
                return;
            }

            // Another machine: the owner started a loop.
            if (loopTick < 0 && !ended && !yoyo.IsOwner && artery.PendingStart && ValidNpc(artery.LoopTarget) != null)
            {
                artery.TakePendingStart();
                EnterLoop(yoyo, artery.LoopTarget, artery.LoopStart, artery.LoopStartVel);
            }

            if (loopTick >= 0)
            {
                if (!ended)
                    TickLoop(yoyo, artery);
                return;
            }

            base.Tick(yoyo);
            if (yoyo.IsOwner && !stopped && yoyo.State == YoyoState.Out)
                DetectLasso(yoyo, artery);
        }

        // ---- the arc: does the string touch an enemy? (owner's client) ----

        void DetectLasso(YoyoProjectile yoyo, ArteryProjectile artery)
        {
            Vector2 hand = yoyo.Hand;
            Vector2 center = yoyo.Projectile.Center;
            if (Vector2.Distance(hand, center) >= A.LassoMinReachPx)
            {
                float thickness = A.LassoTouchPaddingPx;
                Rectangle yoyoBox = yoyo.Projectile.Hitbox;
                int best = -1;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!Snarable.CanBite(npc))
                        continue;
                    Rectangle box = npc.Hitbox;
                    bool touch = box.Intersects(yoyoBox)
                        || (haveLast ? SegmentHit.Sweep(box, lastHand, lastCenter, hand, center, thickness, 3) : SegmentHit.Overlaps(box, hand, center, thickness));
                    if (!touch)
                        continue;
                    float distance = Vector2.DistanceSquared(npc.Center, center);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = i;
                    }
                }
                if (best >= 0)
                {
                    artery.StartLoop(best, center, yoyo.Vel);
                    EnterLoop(yoyo, best, center, yoyo.Vel);
                    return;
                }
            }
            lastHand = hand;
            lastCenter = center;
            haveLast = true;
        }

        // ---- the loop ----

        static NPC ValidNpc(int index)
        {
            return index >= 0 && index < Main.maxNPCs && Main.npc[index].active ? Main.npc[index] : null;
        }

        // The enemy the loop is on, or null when it is gone (dead, or the slot belongs to another enemy now).
        NPC FindTarget()
        {
            NPC npc = ValidNpc(targetIndex);
            return npc != null && npc.type == targetType && npc.life > 0 ? npc : null;
        }

        void EnterLoop(YoyoProjectile yoyo, int index, Vector2 start, Vector2 startVel)
        {
            NPC npc = ValidNpc(index);
            if (npc == null)
                return;
            loopTick = 0;
            ended = false;
            targetIndex = index;
            targetType = npc.type;
            lastEnemy = npc.Center;
            snared = Snarable.CanGrip(npc, A.SnareMaxSizePx);
            baseRadius = MathHelper.Clamp(Math.Max(npc.width, npc.height) * 0.5f + A.LoopRadiusPadPx, A.LoopMinRadiusPx, A.LoopMaxRadiusPx);
            shrink = 1f;
            loopStart = start;
            loopStartVel = startVel;
            if (!yoyo.IsOwner)
                yoyo.Projectile.Center = start;   // the owner's position, where the swing began

            // The swing goes round the enemy the way the yoyo is already turning (the side its speed points to), so it does not reverse.
            Vector2 rel = start - lastEnemy;
            startRadius = Math.Max(1f, rel.Length());
            startAngle = (float)Math.Atan2(rel.Y, rel.X);
            float cross = rel.X * startVel.Y - rel.Y * startVel.X;
            sweepSign = Math.Abs(cross) > 1f ? Math.Sign(cross) : yoyo.Side;
            trail.Clear();
            trail.Add(rel);
            (yoyo.Fx as ArteryFx)?.LoopCatch(start);
        }

        void TickLoop(YoyoProjectile yoyo, ArteryProjectile artery)
        {
            NPC npc = FindTarget();
            if (npc != null)
                lastEnemy = npc.Center;
            Vector2 hand = yoyo.Hand;

            if (yoyo.IsOwner)
            {
                if (npc == null)
                {
                    Finish(yoyo, artery, true);   // the enemy died: the loop closes at once, where it was
                    return;
                }
                if (Vector2.Distance(hand, lastEnemy) > T.RopeLength())
                {
                    Finish(yoyo, artery, false);  // out of reach: the string would be too long
                    return;
                }
                if (loopTick >= Math.Max(1, A.LoopCatchTicks) + Math.Max(1, A.LoopConstrictTicks))
                {
                    Finish(yoyo, artery, true);
                    return;
                }
            }

            if (npc != null && snared)
                NpcHold.Hold(npc);   // every machine renews its own copy of the hold

            loopTick++;
            Place(yoyo, hand, loopTick);
        }

        // The loop is over (owner's client): closed on its enemy, or broken. The yoyo goes back to the hand either way.
        void Finish(YoyoProjectile yoyo, ArteryProjectile artery, bool closed)
        {
            ended = true;
            if (closed)
                artery.CloseLoop(lastEnemy);
            else
                artery.BreakLoop();
            yoyo.BeginReturn();
        }

        // Where the yoyo is at loop tick t. Everything follows the enemy and the hand as they move now.
        void Place(YoyoProjectile yoyo, Vector2 hand, int t)
        {
            Vector2 toEnemy = lastEnemy - hand;
            float handDistance = toEnemy.Length();
            Vector2 along = handDistance > 1f ? toEnemy / handDistance : new Vector2(yoyo.Side, 0f);   // from the hand toward the enemy
            int catchTicks = Math.Max(1, A.LoopCatchTicks);
            Vector2 point;
            if (t <= catchTicks)
            {
                point = SwingPoint(t / (float)catchTicks, along, handDistance, catchTicks);
            }
            else
            {
                int walked = t - catchTicks;
                float u = MathHelper.Clamp(walked / (float)Math.Max(1, A.LoopConstrictTicks), 0f, 1f);
                shrink = MathHelper.Lerp(1f, A.LoopCloseScale, u * u);   // it closes gently at first and quickly at the end
                float distance = LoopDistance(baseRadius + A.LoopGapPx + A.LoopWalkSpeedPx * walked, baseRadius * shrink, handDistance);
                point = lastEnemy - along * distance;
            }
            yoyo.Move(point - yoyo.Projectile.Center, true);
            if (t <= catchTicks)
                trail.Add(yoyo.Projectile.Center - lastEnemy);   // the flight path the string will lie on
        }

        // The swing round the enemy (u from 0 to 1): a spiral about the enemy from where the catch began to the string line on the player's
        // side of it. It bulges out to at least the loop's size so the string really goes round the enemy, and a correction that is zero at
        // both ends makes it start with the speed the yoyo had (so the turn is smooth) and end exactly on the line.
        Vector2 SwingPoint(float u, Vector2 along, float handDistance, int catchTicks)
        {
            float endRadius = LoopDistance(baseRadius + A.LoopGapPx, baseRadius, handDistance);
            float endAngle = (float)Math.Atan2(-along.Y, -along.X);   // from the enemy toward the hand
            float sweep = SweepAngle(endAngle);
            float bulge = Math.Max(0f, baseRadius + A.LoopRadiusPadPx - 0.5f * (startRadius + endRadius));

            float radius = MathHelper.Lerp(startRadius, endRadius, u) + bulge * (float)Math.Sin(Math.PI * u);
            float angle = startAngle + sweep * u;
            Vector2 radial = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 point = lastEnemy + radial * radius;

            // The speed the spiral itself starts with, per tick, against the speed the yoyo really had.
            Vector2 radial0 = new Vector2((float)Math.Cos(startAngle), (float)Math.Sin(startAngle));
            Vector2 around0 = new Vector2(-radial0.Y, radial0.X);
            float radiusSpeed = (endRadius - startRadius) + bulge * (float)Math.PI;
            Vector2 spiralStart = (radial0 * radiusSpeed + around0 * (startRadius * sweep)) / catchTicks;
            point += (loopStartVel - spiralStart) * (catchTicks * u * (1f - u) * (1f - u));
            return point;
        }

        // The whole angle the swing turns through (signed), the way the yoyo is already turning, never less than LoopMinSweepDeg.
        float SweepAngle(float endAngle)
        {
            float raw = MathHelper.WrapAngle(endAngle - startAngle);
            float sweep = sweepSign > 0f ? (raw >= 0f ? raw : raw + MathHelper.TwoPi) : (raw <= 0f ? raw : raw - MathHelper.TwoPi);
            if (Math.Abs(sweep) < MathHelper.ToRadians(A.LoopMinSweepDeg))
                sweep += sweepSign * MathHelper.TwoPi;
            return sweep;
        }

        // How far from the enemy the yoyo is on the string line: not closer to the hand than allowed, not inside the loop.
        float LoopDistance(float wanted, float loopRadius, float handDistance)
        {
            float distance = Math.Min(wanted, handDistance - A.LoopMinHandDistancePx);
            return Math.Max(distance, loopRadius + 4f);
        }

        // ---- the string (visual) ----

        // While the loop runs the string is drawn by hand (see YoyoTrick.StringPath). CATCH: the line from the hand to where the swing began,
        // then the yoyo's own flight path. After it: the straight line from the hand to the yoyo, and the loop (the line back to where the
        // swing began and the flight path) hanging from the yoyo, shrinking toward the enemy.
        public override bool StringPath(YoyoProjectile yoyo, List<Vector2> points)
        {
            if (Main.dedServ || loopTick < 0 || ended || trail.Count == 0)
                return false;
            points.Clear();
            Vector2 hand = yoyo.Hand;
            float step = A.LoopStringStepPx;
            int catchTicks = Math.Max(1, A.LoopCatchTicks);

            shape.Clear();
            if (loopTick < catchTicks)
            {
                for (int i = 0; i < trail.Count; i++)
                    shape.Add(lastEnemy + trail[i]);
                ArteryLoop.AddLine(points, hand, shape[0], step);
                ArteryLoop.AddSpline(points, shape, A.LoopSplineSteps);
                return true;
            }

            Vector2 apex = yoyo.Projectile.Center;
            for (int i = 0; i < trail.Count; i++)
                shape.Add(lastEnemy + trail[i] * shrink);
            ArteryLoop.AddLine(points, hand, apex, step);
            ArteryLoop.AddLine(points, apex, shape[0], step);
            ArteryLoop.AddSpline(points, shape, A.LoopSplineSteps);
            ArteryLoop.AddLine(points, shape[shape.Count - 1], apex, step);
            return true;
        }

        public override float SpinStep(YoyoProjectile yoyo)
        {
            if (loopTick < 0 || ended)
                return base.SpinStep(yoyo);
            float walking = loopTick > Math.Max(1, A.LoopCatchTicks) ? A.LoopSpinSpeed : 0f;   // the spin-walk
            return yoyo.Side * Math.Max(yoyo.Vel.Length() * T.SpinPerPixel, walking);
        }

        public override bool RopeTaut(YoyoProjectile yoyo)
        {
            return loopTick >= 0 || base.RopeTaut(yoyo);
        }
    }
}