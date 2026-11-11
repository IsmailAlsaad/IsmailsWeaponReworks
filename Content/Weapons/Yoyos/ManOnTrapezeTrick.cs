using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos
{
    /// <summary>
    /// Man on Trapeze (pattern; the Artery's left click and the Malaise's fallback): the ForwardPassTrick pattern, but the string tries to LOOP an enemy.
    /// ARC: the pass flies as a Forward Pass. Every tick the owner's client checks whether the string (the line hand to yoyo) or the yoyo
    /// touches an enemy that can be damaged (Snarable.CanBite); the first one found is the target (one loop at a time).
    /// CATCH (LoopCatchTicks): the yoyo leaves the ellipse and follows a cubic curve that starts with the speed it had (no harsh turn from the
    /// straight line to sideways), bends around the enemy and arrives on the string line, on the player's side of the enemy. The noose
    /// (YoyoLoop.AddNoose) grows around the enemy meanwhile.
    /// CONSTRICT (LoopConstrictTicks): the yoyo spin-walks along the line toward the player (it follows the enemy, which keeps moving)
    /// while the noose shrinks. The loop CLOSES when the noose is shut, or at once when the enemy dies; the yoyo then returns to the hand.
    /// It BREAKS (no close) when the enemy gets farther from the hand than the string is long.
    /// A grippable enemy (Snarable.CanGrip: not a boss, not immovable, not a worm segment, not huge) is HELD in place for the whole loop
    /// (NpcHold); anything else is looped but keeps moving.
    /// MULTIPLAYER: the owner decides the target, the start, the close and the break; the data travels in LoopYoyoProjectile's extra sync.
    /// Every machine runs the curve from it, refreshes the hold on its own copy of the enemy, and draws the string and the noose
    /// itself (visual only). The shred hits are done here; what happens when the loop ENDS is the yoyo's own subclass (OnLoopEnd: the
    /// Artery's blood explosion, the Malaise's confusion) and its fx.
    /// </summary>
    public class ManOnTrapezeTrick : ForwardPassTrick
    {
        int loopTick = -1;        // -1 = the arc is flying; 0 and up = the loop is running
        bool ended;               // the loop is over (closed or broken): the yoyo is on its way back
        int targetIndex = -1;
        int targetType;
        bool snared;
        float baseRadius;         // the noose's radius when it is fully grown
        float radius;             // the noose's radius now
        Vector2 loopStart;
        Vector2 loopStartVel;
        Vector2 lastEnemy;        // where the enemy was last (the loop closes there when the enemy is gone)
        Vector2 lastHand;
        Vector2 lastCenter;
        bool haveLast;

        public ManOnTrapezeTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        ILoopTuning A => (ILoopTuning)T;

        public override string Name => "Man on Trapeze";

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            base.Begin(yoyo, authoritative);
            loopTick = -1;
            ended = false;
            targetIndex = -1;
            haveLast = false;
            radius = 0f;
            if (authoritative)
                (yoyo as LoopYoyoProjectile)?.ClearLoop();
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            LoopYoyoProjectile loop = yoyo as LoopYoyoProjectile;
            if (loop == null)
            {
                base.Tick(yoyo);
                return;
            }

            // Another machine: the owner started a loop.
            if (loopTick < 0 && !ended && !yoyo.IsOwner && loop.PendingStart && ValidNpc(loop.LoopTarget) != null)
            {
                loop.TakePendingStart();
                EnterLoop(yoyo, loop.LoopTarget, loop.LoopStart, loop.LoopStartVel);
            }

            if (loopTick >= 0)
            {
                if (!ended)
                    TickLoop(yoyo, loop);
                return;
            }

            base.Tick(yoyo);
            if (yoyo.IsOwner && !stopped && yoyo.State == YoyoState.Out)
                DetectLasso(yoyo, loop);
        }

        // ---- the arc: does the string touch an enemy? (owner's client) ----

        void DetectLasso(YoyoProjectile yoyo, LoopYoyoProjectile loop)
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
                    if (BeginLoop(yoyo, loop, best, center))
                        return;   // the yoyo's own trick took the catch over (Rock the Baby)
                    loop.StartLoop(best, center, yoyo.Vel);
                    EnterLoop(yoyo, best, center, yoyo.Vel);
                    return;
                }
            }
            lastHand = hand;
            lastCenter = center;
            haveLast = true;
        }

        // ---- the loop ----

        protected static NPC ValidNpc(int index)
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
            radius = 0f;
            loopStart = start;
            loopStartVel = startVel;
            if (!yoyo.IsOwner)
                yoyo.Projectile.Center = start;   // the owner's position, where the curve began
            OnLoopStart(yoyo);
            (yoyo.Fx as LoopYoyoFx)?.LoopCatch(start);
        }

        // ---- hooks for the yoyo's own subclass ----

        // The string just caught enemy 'index' at 'center' (the owner's client, before anything starts). A yoyo that does something else
        // than the noose (the Malaise's Rock the Baby) starts it here, tells the other machines through the loop's synced data and returns
        // true; false (the default) lets the Man on Trapeze noose start.
        protected virtual bool BeginLoop(YoyoProjectile yoyo, LoopYoyoProjectile loop, int index, Vector2 center)
        {
            return false;
        }

        // A loop starts (every machine, before the catch fx plays): a yoyo can hand its fx numbers over here.
        protected virtual void OnLoopStart(YoyoProjectile yoyo)
        {
        }

        // The loop ended (the owner's client only): 'closed' = it closed on its enemy (or the enemy died inside it), false = it broke (the
        // enemy got out of reach). 'target' is the looped enemy when it is still alive (null when it died), 'at' where the loop was.
        // The synced close/break flag and the fx have already been sent; this is for the yoyo's own damage and debuffs.
        protected virtual void OnLoopEnd(YoyoProjectile yoyo, NPC target, Vector2 at, bool closed)
        {
        }

        void TickLoop(YoyoProjectile yoyo, LoopYoyoProjectile loop)
        {
            NPC npc = FindTarget();
            if (npc != null)
                lastEnemy = npc.Center;
            Vector2 hand = yoyo.Hand;

            if (yoyo.IsOwner)
            {
                if (npc == null)
                {
                    Finish(yoyo, loop, true);   // the enemy died: the loop closes at once, where it was
                    return;
                }
                if (Vector2.Distance(hand, lastEnemy) > T.RopeLength())
                {
                    Finish(yoyo, loop, false);  // out of reach: the string would be too long
                    return;
                }
                if (loopTick >= Math.Max(1, A.LoopCatchTicks) + Math.Max(1, A.LoopConstrictTicks))
                {
                    Finish(yoyo, loop, true);
                    return;
                }
            }

            if (npc != null && snared)
                NpcHold.Hold(npc);   // every machine renews its own copy of the hold

            Shred(yoyo, npc);
            loopTick++;
            Place(yoyo, hand, loopTick);
        }

        // ---- damage: the shred ----

        // While the noose shrinks: a shred hit every ShredIntervalTicks. Every machine counts the same ticks and plays the blood; only the
        // owner's client strikes (SimpleStrikeNPC syncs it). Called before loopTick goes up, so the first hit comes one interval after the catch ends.
        void Shred(YoyoProjectile yoyo, NPC npc)
        {
            int catchTicks = Math.Max(1, A.LoopCatchTicks);
            if (npc == null || loopTick <= catchTicks)
                return;
            if ((loopTick - catchTicks) % Math.Max(1, A.ShredIntervalTicks) != 0)
                return;
            (yoyo.Fx as LoopYoyoFx)?.LoopShred(npc.Center, radius);
            if (!yoyo.IsOwner)
                return;
            int damage = Math.Max(1, (int)(yoyo.Projectile.damage * A.ShredDamageFraction));
            npc.SimpleStrikeNPC(damage, npc.Center.X >= yoyo.Hand.X ? 1 : -1, false, 0f, DamageClass.Melee);
            YoyoShake.Hit(npc.Center, A.ShredShake);
        }

        // The loop is over (owner's client): closed on its enemy, or broken. The yoyo goes back to the hand either way.
        void Finish(YoyoProjectile yoyo, LoopYoyoProjectile loop, bool closed)
        {
            ended = true;
            NPC target = FindTarget();   // null when the enemy died
            if (closed)
                loop.CloseLoop(lastEnemy);
            else
                loop.BreakLoop();
            OnLoopEnd(yoyo, target, lastEnemy, closed);
            yoyo.BeginReturn();
        }

        // Where the yoyo is at loop tick t, and how big the noose is. Everything follows the enemy and the hand as they move now.
        void Place(YoyoProjectile yoyo, Vector2 hand, int t)
        {
            Vector2 toEnemy = lastEnemy - hand;
            float handDistance = toEnemy.Length();
            Vector2 along = handDistance > 1f ? toEnemy / handDistance : new Vector2(yoyo.Side, 0f);   // from the hand toward the enemy
            int catchTicks = Math.Max(1, A.LoopCatchTicks);
            Vector2 point;
            if (t <= catchTicks)
            {
                // A cubic curve: it starts with the speed the yoyo had and ends on the string line moving toward the player.
                float u = t / (float)catchTicks;
                radius = baseRadius * YoyoLoop.Smooth(u);
                float distance = LoopDistance(baseRadius + A.LoopGapPx, baseRadius, handDistance);
                Vector2 end = lastEnemy - along * distance;
                Vector2 c1 = loopStart + loopStartVel * (catchTicks / 3f);
                Vector2 c2 = end + along * (A.LoopWalkSpeedPx * catchTicks / 3f);
                point = YoyoLoop.Bezier(loopStart, c1, c2, end, u);
            }
            else
            {
                int walked = t - catchTicks;
                float u = MathHelper.Clamp(walked / (float)Math.Max(1, A.LoopConstrictTicks), 0f, 1f);
                radius = MathHelper.Lerp(baseRadius, A.LoopCloseRadiusPx, u * u);   // it closes gently at first and quickly at the end
                float distance = LoopDistance(baseRadius + A.LoopGapPx + A.LoopWalkSpeedPx * walked, radius, handDistance);
                point = lastEnemy - along * distance;
            }
            yoyo.Move(point - yoyo.Projectile.Center, true);
        }

        // How far from the enemy the yoyo is on the string line: not closer to the hand than allowed, not inside the noose.
        float LoopDistance(float wanted, float noose, float handDistance)
        {
            float distance = Math.Min(wanted, handDistance - A.LoopMinHandDistancePx);
            return Math.Max(distance, noose + 4f);
        }

        // ---- the string (visual) ----

        // While the loop runs the string is the straight line hand to yoyo plus the noose (see YoyoTrick.StringPath).
        public override bool StringPath(YoyoProjectile yoyo, List<Vector2> points)
        {
            if (Main.dedServ || loopTick < 0 || ended)
                return false;
            points.Clear();
            Vector2 yoyoCenter = yoyo.Projectile.Center;
            YoyoLoop.AddLine(points, yoyo.Hand, yoyoCenter, A.LoopStringStepPx);
            YoyoLoop.AddNoose(points, yoyoCenter, lastEnemy, radius, A.LoopStringStepPx, A.LoopArcSamples);
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