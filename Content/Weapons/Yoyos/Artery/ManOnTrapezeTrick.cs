using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Artery
{
    public enum LoopPhase : byte
    {
        Arc,        // the Forward Pass ellipse, looking for an enemy the string or the yoyo touches
        Circle,     // a quick circle around the enemy, ending on the line hand to enemy
        Constrict,  // the yoyo spin-walks along that line toward the hand while the loop shrinks onto the enemy
        Done,       // the loop closed (or broke): the normal return
    }

    /// <summary>
    /// Man on trapeze (the Artery's left click): the Forward Pass ellipse, but the string tries to LOOP an enemy. The first enemy that the
    /// string line or the yoyo touches is lassoed: it is held still (CarriedNPC, when it can be carried; a boss or a big one is looped but
    /// not held), the yoyo leaves the ellipse and makes a quick circle around it that ends on the line hand to enemy (the knot), then walks
    /// slowly along that line toward the hand while the loop shrinks onto the enemy, until it closes. One enemy at a time.
    /// The string is drawn as a lollipop: hand, knot, the ring around the enemy, back to the knot, the yoyo on the line.
    /// MULTIPLAYER: the owner's client decides the lasso (which enemy) and the end; LoopPhase and the enemy's index travel in the yoyo's
    /// extra sync (ArteryProjectile). Every machine then moves the yoyo from the synced enemy (the circle and the walk are computed from the
    /// enemy's own position on that machine) and holds it (CarriedNPC.Hold runs on every machine, the server included); the ring and the
    /// string are local visuals. Damage is unchanged in this step (the shred hits and the explosion come in the next one).
    /// </summary>
    public class ManOnTrapezeTrick : ForwardPassTrick
    {
        readonly ArteryTuning A;
        LoopPhase seen;                 // the phase this machine has set up for
        int phaseTicks;                 // ticks in the current phase (local)
        Vector2 startOffset;            // where the yoyo was, relative to the enemy, when the circle began
        float travelSign = 1f;          // which way around the yoyo circles
        Vector2 lastCenter;
        bool ringActive;                // the string lies on a ring this tick (read by RopePath)
        Vector2 ringCenter;
        float ringRadius;
        float ringStart;                // the angle of the knot
        float ringSweep;                // how far around the ring the string goes (signed)

        public ManOnTrapezeTrick(YoyoTuning tuning) : base(tuning)
        {
            A = (ArteryTuning)tuning;
        }

        public override string Name => "Man on trapeze";

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            base.Begin(yoyo, authoritative);
            ArteryProjectile art = (ArteryProjectile)yoyo;
            if (authoritative)
            {
                art.Phase = LoopPhase.Arc;
                art.LoopNpc = -1;
            }
            seen = LoopPhase.Arc;
            phaseTicks = 0;
            ringActive = false;
            lastCenter = yoyo.Projectile.Center;
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            ArteryProjectile art = (ArteryProjectile)yoyo;
            Vector2 before = yoyo.Projectile.Center;
            NPC npc = art.LoopNpc >= 0 && art.LoopNpc < Main.maxNPCs ? Main.npc[art.LoopNpc] : null;
            bool valid = npc != null && npc.active && npc.life > 0;

            if (art.Phase != seen)
            {
                seen = art.Phase;
                phaseTicks = 0;
                if (seen == LoopPhase.Circle && valid)
                {
                    startOffset = before - npc.Center;
                    Vector2 travel = before - lastCenter;
                    travelSign = startOffset.X * travel.Y - startOffset.Y * travel.X >= 0f ? 1f : -1f;
                }
                if (seen == LoopPhase.Done && npc != null)
                    CarriedNPC.Release(npc);
            }
            phaseTicks++;
            ringActive = false;

            if (art.Phase == LoopPhase.Arc)
            {
                base.Tick(yoyo);
                if (yoyo.IsOwner)
                    CheckLasso(yoyo, art);
            }
            else if (art.Phase != LoopPhase.Done)
            {
                if (!valid)
                {
                    if (yoyo.IsOwner)
                        Finish(yoyo, art, npc);   // the enemy is gone: the loop ends (the next step makes it close with the blast)
                }
                else
                    TickLoop(yoyo, art, npc);
            }
            lastCenter = before;
        }

        // Owner's client: the first enemy (nearest to the hand) that the string line or the yoyo touches is lassoed.
        void CheckLasso(YoyoProjectile yoyo, ArteryProjectile art)
        {
            Vector2 hand = yoyo.Owner.MountedCenter;
            Vector2 center = yoyo.Projectile.Center;
            Rectangle box = yoyo.Projectile.Hitbox;
            float best = float.MaxValue;
            int found = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.life <= 0 || npc.realLife >= 0)
                    continue;
                if (!npc.Hitbox.Intersects(box) && !SegmentHit.Overlaps(npc.Hitbox, hand, center, A.LassoTouchPaddingPx))
                    continue;
                float distance = Vector2.DistanceSquared(npc.Center, hand);
                if (distance < best)
                {
                    best = distance;
                    found = i;
                }
            }
            if (found < 0)
                return;
            art.LoopNpc = found;
            art.Phase = LoopPhase.Circle;
            yoyo.Projectile.netUpdate = true;
        }

        void TickLoop(YoyoProjectile yoyo, ArteryProjectile art, NPC npc)
        {
            if (CarriedNPC.CanCarry(npc, A.SnareMaxSizePx))
                CarriedNPC.Hold(npc, npc.Center);   // snared: held still on every machine

            Vector2 enemy = npc.Center;
            Vector2 toHand = yoyo.Owner.MountedCenter - enemy;
            float lineLength = toHand.Length();
            float dirAngle = lineLength > 1f ? toHand.ToRotation() : -MathHelper.PiOver2;
            Vector2 dir = dirAngle.ToRotationVector2();
            float baseRadius = MathHelper.Clamp(Math.Max(npc.width, npc.height) * 0.5f + A.LoopStartRadiusPad, 16f, 80f);
            Vector2 target;

            if (art.Phase == LoopPhase.Circle)
            {
                float u = MathHelper.Clamp(phaseTicks / (float)Math.Max(1, A.LoopCircleTicks), 0f, 1f);
                float ease = MathHelper.SmoothStep(0f, 1f, u);
                float a0 = startOffset.ToRotation();
                float delta = travelSign * WrapTurn(travelSign * (dirAngle - a0));
                if (Math.Abs(delta) < MathHelper.PiOver2)
                    delta += travelSign * MathHelper.TwoPi;   // at least a good part of a circle
                float angle = a0 + delta * ease;
                float radius = MathHelper.Lerp(Math.Max(startOffset.Length(), 8f), baseRadius, ease);
                target = enemy + angle.ToRotationVector2() * radius;
                ringActive = true;
                ringCenter = enemy;
                ringRadius = radius;
                ringStart = dirAngle;
                ringSweep = travelSign * (MathHelper.TwoPi - (1f - ease) * Math.Abs(delta));   // the string follows the yoyo around: a full ring at the end
                if (u >= 1f && yoyo.IsOwner)
                {
                    art.Phase = LoopPhase.Constrict;
                    yoyo.Projectile.netUpdate = true;
                }
            }
            else
            {
                float u = MathHelper.Clamp(phaseTicks / (float)Math.Max(1, A.ConstrictTicks), 0f, 1f);
                float ease = MathHelper.SmoothStep(0f, 1f, u);
                float radius = MathHelper.Lerp(baseRadius, A.LoopMinRadiusPx, ease);
                float walk = Math.Min(A.WalkMaxPx, Math.Max(0f, lineLength - baseRadius - A.WalkHandGapPx));
                target = enemy + dir * (baseRadius + walk * ease);
                ringActive = true;
                ringCenter = enemy;
                ringRadius = radius;
                ringStart = dirAngle;
                ringSweep = travelSign * MathHelper.TwoPi;
                if (yoyo.IsOwner && (u >= 1f || lineLength > T.RopeLength()))
                    Finish(yoyo, art, npc);   // the loop closed (or the enemy got too far from the hand)
            }

            yoyo.Move(target - yoyo.Projectile.Center, true);
        }

        // Owner's client: the enemy is let go and the yoyo is pulled back to the hand.
        void Finish(YoyoProjectile yoyo, ArteryProjectile art, NPC npc)
        {
            if (npc != null)
                CarriedNPC.Release(npc);
            art.Phase = LoopPhase.Done;
            yoyo.Projectile.netUpdate = true;
            yoyo.BeginReturn();
        }

        static float WrapTurn(float angle)
        {
            angle %= MathHelper.TwoPi;
            return angle < 0f ? angle + MathHelper.TwoPi : angle;
        }

        // The string while the loop is on: knot, then the ring around the enemy (the yoyo is the end of the string).
        public override bool RopePath(YoyoProjectile yoyo, List<Vector2> path)
        {
            path.Clear();
            if (!ringActive)
                return false;
            int n = Math.Max(6, A.RingPoints);
            path.Add(ringCenter + ringStart.ToRotationVector2() * ringRadius);
            for (int k = 1; k <= n; k++)
            {
                float angle = ringStart + ringSweep * (k / (float)n);
                path.Add(ringCenter + angle.ToRotationVector2() * ringRadius);
            }
            return true;
        }

        public override float SpinStep(YoyoProjectile yoyo)
        {
            return ringActive ? yoyo.Side * A.LoopSpinSpeed : base.SpinStep(yoyo);
        }
    }
}