using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Malaise
{
    /// <summary>
    /// Rock the Baby (the Malaise's left click): the Man on Trapeze lasso (ManOnTrapezeTrick) that, when it catches a first enemy and a
    /// second one is in range, does something else than the noose. When there is no second enemy it is the Man on Trapeze (the base).
    /// SEEK: the string bends over the first enemy (A, a small pole circle around its center, the string leaves the hand, goes round A
    /// and on to the yoyo) and the yoyo homes on the second enemy (B).
    /// HOOK: the yoyo reaches B, the string bends over B too, and the yoyo curves back to the player. The string is now the cradle: a
    /// triangle hand - A - B with the player as the pointy top.
    /// SWING: the yoyo hangs from the hand on a second string and swings like a grandfather clock pendulum between A and B (the arc around
    /// the hand, from A's center to B's center). The swing speeds up. It hurts whatever it crosses (the normal yoyo hits). It ends when
    /// the button is let go (after PendulumMinTicks), after MaxHoldTicks, or when a side of the triangle gets longer than the string.
    /// Every enemy that was looped is Confused when it ends. Nothing here moves or holds an enemy (HoldEnemies is a knob, off).
    /// MULTIPLAYER (rule 8): the owner's client decides everything: A, B (and a new B if B is lost while seeking), the touch that starts
    /// the hook, the swing, the end, the damage and the Confused buff. What travels: the existing LoopYoyoProjectile extra sync (mode,
    /// A, B, the turn side, the phase, the start position and speed). Every machine then runs the same geometry itself: the homing from
    /// the synced start, the hook curve from B and the hand, and the swing as a pure function of the two enemy centers, the hand and the
    /// tick count since the swing began. The string and all fx are local visuals.
    /// </summary>
    public class MalaiseRockTrick : ManOnTrapezeTrick
    {
        const int PhaseSeek = 0;
        const int PhaseHook = 1;
        const int PhaseSwing = 2;
        const int PhaseDone = 3;

        bool rock;                // Rock the Baby is running on this machine
        bool rockDone;            // it ended: nothing else runs until the next throw
        int phase;
        int phaseTick;            // ticks in this phase
        int rockTick;             // ticks since it began
        int hookStartTick;        // rockTick when the hook began
        int aIndex = -1;
        int aType;
        int bIndex = -1;
        int bType = -1;
        int turn = 1;             // which way the string turns round the corners (the triangle's orientation), +1 or -1
        float baseA;              // the pole circle's radius around A and B when fully grown
        float baseB;
        bool reachedB;            // the yoyo has hooked B (B counts as looped)
        Vector2 lastA;            // where each enemy was last (the string and the swing keep using it when the enemy is gone)
        Vector2 lastB;
        Vector2 hookOffset;       // the yoyo's place relative to B when the hook began
        Vector2 hookVel;
        int swingTick;
        float pendulumPhase;
        float prevCos;

        public MalaiseRockTrick(YoyoTuning tuning) : base(tuning)
        {
        }

        MalaiseTuning Tune => (MalaiseTuning)T;

        public override string Name => "Rock the Baby";

        public override void Begin(YoyoProjectile yoyo, bool authoritative)
        {
            base.Begin(yoyo, authoritative);
            rock = false;
            rockDone = false;
            phase = PhaseSeek;
            aIndex = -1;
            bIndex = -1;
            bType = -1;
            reachedB = false;
        }

        public override void Tick(YoyoProjectile yoyo)
        {
            LoopYoyoProjectile loop = yoyo as LoopYoyoProjectile;
            if (loop == null)
            {
                base.Tick(yoyo);
                return;
            }

            // Another machine: the owner started Rock the Baby.
            if (!rock && !rockDone && !yoyo.IsOwner && loop.PendingStart && loop.LoopMode == 1 && ValidNpc(loop.LoopTarget) != null)
            {
                loop.TakePendingStart();
                EnterRock(yoyo, loop.LoopTarget, loop.LoopStart, loop.LoopStartVel, loop.LoopTarget2, loop.LoopTurn);
            }

            if (rock)
            {
                TickRock(yoyo, loop);
                return;
            }
            if (rockDone)
                return;   // over: the yoyo waits for the return (it must not fly the rest of the ellipse)
            base.Tick(yoyo);
        }

        // ---- the start (owner's client, called from the lasso) ----

        protected override bool BeginLoop(YoyoProjectile yoyo, LoopYoyoProjectile loop, int index, Vector2 center)
        {
            NPC a = ValidNpc(index);
            if (a == null)
                return false;
            int second = FindSecond(yoyo, a, -1);
            if (second < 0)
                return false;   // no second enemy in range: the Man on Trapeze fallback
            int side = TurnFor(yoyo.Hand, a.Center, Main.npc[second].Center);
            loop.StartLoop(index, center, yoyo.Vel, 1, second, side);
            EnterRock(yoyo, index, center, yoyo.Vel, second, side);
            return true;
        }

        // The second corner: the FURTHEST enemy from A that can be damaged, at least SeekMinRadiusFraction of SeekRadiusPx and at most
        // SeekRadiusPx away (closer than that it is no triangle: Man on Trapeze), and close enough to the player that both
        // sides of the triangle fit the string. Not another part of A's own body (a worm). 'skip' is an enemy to leave out (a lost B).
        int FindSecond(YoyoProjectile yoyo, NPC a, int skip)
        {
            float reach = T.RopeLength() - Tune.SeekReachMarginPx;
            Vector2 hand = yoyo.Hand;
            int best = -1;
            float bestDistance = -1f;
            float minimum = Tune.SeekRadiusPx * Tune.SeekMinRadiusFraction;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (i == skip || !Snarable.CanBite(npc) || npc.life <= 0 || SameBody(a, npc))
                    continue;
                float fromA = Vector2.Distance(npc.Center, a.Center);
                if (fromA < minimum || fromA > Tune.SeekRadiusPx || fromA > reach || Vector2.Distance(npc.Center, hand) > reach)
                    continue;
                if (fromA > bestDistance)
                {
                    bestDistance = fromA;
                    best = i;
                }
            }
            return best;
        }

        static bool SameBody(NPC a, NPC b)
        {
            if (a.whoAmI == b.whoAmI)
                return true;
            int bodyA = a.realLife >= 0 ? a.realLife : a.whoAmI;
            int bodyB = b.realLife >= 0 ? b.realLife : b.whoAmI;
            return bodyA == bodyB;
        }

        // Which way the string turns at the corners of the triangle hand - A - B: the sign of the turn at A.
        static int TurnFor(Vector2 hand, Vector2 a, Vector2 b)
        {
            Vector2 first = a - hand;
            Vector2 second = b - a;
            return first.X * second.Y - first.Y * second.X >= 0f ? 1 : -1;
        }

        float PoleRadius(NPC npc)
        {
            return MathHelper.Clamp(Math.Max(npc.width, npc.height) * 0.5f * Tune.WrapRadiusScale, Tune.WrapRadiusMinPx, Tune.WrapRadiusMaxPx);
        }

        void EnterRock(YoyoProjectile yoyo, int a, Vector2 start, Vector2 startVel, int b, int side)
        {
            NPC npc = ValidNpc(a);
            if (npc == null)
                return;
            rock = true;
            rockDone = false;
            phase = PhaseSeek;
            phaseTick = 0;
            rockTick = 0;
            aIndex = a;
            aType = npc.type;
            bIndex = b;
            NPC second = ValidNpc(b);
            bType = second != null ? second.type : -1;
            turn = side >= 0 ? 1 : -1;
            baseA = PoleRadius(npc);
            baseB = second != null ? PoleRadius(second) : 10f;
            reachedB = false;
            lastA = npc.Center;
            lastB = second != null ? second.Center : npc.Center;
            yoyo.Vel = startVel;
            if (!yoyo.IsOwner)
                yoyo.Projectile.Center = start;   // the owner's position, where the string caught A
            (yoyo.Fx as LoopYoyoFx)?.LoopCatch(start);
        }

        // ---- the phases ----

        NPC FindA()
        {
            NPC npc = ValidNpc(aIndex);
            return npc != null && npc.type == aType && npc.life > 0 ? npc : null;
        }

        NPC FindB()
        {
            NPC npc = ValidNpc(bIndex);
            if (npc == null || npc.life <= 0)
                return null;
            if (bType < 0)
                bType = npc.type;   // another machine: take the type of the enemy the owner named
            return npc.type == bType ? npc : null;
        }

        static bool ButtonHeld(YoyoProjectile yoyo)
        {
            Player owner = yoyo.Owner;
            return yoyo.UsingRight ? owner.controlUseTile : owner.controlUseItem;
        }

        void TickRock(YoyoProjectile yoyo, LoopYoyoProjectile loop)
        {
            Vector2 hand = yoyo.Hand;
            bool owner = yoyo.IsOwner;
            if (!owner)
            {
                // Another machine: it follows what the owner decided (a new B, the phase).
                turn = loop.LoopTurn >= 0 ? 1 : -1;
                if (loop.LoopTarget2 != bIndex)
                {
                    bIndex = loop.LoopTarget2;
                    bType = -1;
                }
                int wanted = Math.Min(loop.LoopPhase, PhaseDone);
                while (rock && phase < wanted)
                    EnterPhase(yoyo, phase + 1);
                if (!rock)
                    return;
            }

            FaceYoyo(yoyo);

            NPC a = FindA();
            NPC b = FindB();
            if (a != null)
                lastA = a.Center;
            if (b != null)
                lastB = b.Center;

            if (owner)
            {
                float reach = T.RopeLength() * Tune.BreakStretch;
                bool hooked = phase >= PhaseHook;
                if (a == null || (hooked && b == null)
                    || Vector2.Distance(hand, lastA) > reach
                    || (hooked && (Vector2.Distance(hand, lastB) > reach || Vector2.Distance(lastA, lastB) > reach)))
                {
                    EndRock(yoyo, loop, true);   // a looped enemy is gone, or a side of the triangle is longer than the string
                    return;
                }
                if (phase == PhaseSeek && b == null)
                {
                    // B was lost on the way: the nearest other enemy takes its place, or it ends.
                    int next = FindSecond(yoyo, a, bIndex);
                    if (next < 0)
                    {
                        EndRock(yoyo, loop, true);
                        return;
                    }
                    bIndex = next;
                    b = Main.npc[next];
                    bType = b.type;
                    baseB = PoleRadius(b);
                    lastB = b.Center;
                    turn = TurnFor(hand, lastA, lastB);
                    loop.SetSecond(next, turn);
                }
            }

            if (Tune.HoldEnemies)
            {
                Hold(a);
                if (phase >= PhaseHook)
                    Hold(b);
            }

            rockTick++;
            phaseTick++;
            switch (phase)
            {
                case PhaseSeek:
                    TickSeek(yoyo, loop, b);
                    break;
                case PhaseHook:
                    TickHook(yoyo, loop, hand);
                    break;
                default:
                    TickSwing(yoyo, loop, hand);
                    break;
            }
        }

        // Rock the Baby: the player always faces the side the yoyo is on, in every phase. The owner's client decides (past FaceDeadzonePx, so the
        // player does not flicker when the yoyo swings over the head) and publishes it through Side, which is already in the yoyo's sync; every
        // machine then turns the player (and the arm, the spin and the hit direction, which all read Side) the same way.
        void FaceYoyo(YoyoProjectile yoyo)
        {
            if (yoyo.IsOwner)
            {
                float dx = yoyo.Projectile.Center.X - yoyo.Owner.Center.X;
                int wanted = dx > Tune.FaceDeadzonePx ? 1 : dx < -Tune.FaceDeadzonePx ? -1 : yoyo.Side;
                if (wanted != yoyo.Side)
                {
                    yoyo.Side = wanted;
                    yoyo.Projectile.netUpdate = true;
                }
            }
            yoyo.Owner.ChangeDir(yoyo.Side);   // the yoyo's TickOut already did this with the old Side this tick
        }

        void Hold(NPC npc)
        {
            if (npc != null && Snarable.CanGrip(npc, Tune.SnareMaxSizePx))
                NpcHold.Hold(npc);   // renewed on every machine, as the Artery does
        }

        // The yoyo homes on B: it keeps its speed from the lasso, turns toward B at a limited rate (more when it is close) and speeds up.
        void TickSeek(YoyoProjectile yoyo, LoopYoyoProjectile loop, NPC b)
        {
            Vector2 position = yoyo.Projectile.Center;
            Vector2 target = b != null ? b.Center : lastB;
            Vector2 toTarget = target - position;
            float distance = toTarget.Length();
            float speed = Math.Max(yoyo.Vel.Length(), Tune.SeekStartSpeedPx);
            speed = Math.Min(speed + Tune.SeekAccelPx, Tune.SeekSpeedPx);
            float heading = yoyo.Vel.LengthSquared() > 0.25f ? yoyo.Vel.ToRotation() : toTarget.ToRotation();
            float near = 1f - MathHelper.Clamp(distance / 80f, 0f, 1f);
            float turnRate = Tune.SeekTurnRate * (1f + 2f * near);
            heading += MathHelper.Clamp(MathHelper.WrapAngle(toTarget.ToRotation() - heading), -turnRate, turnRate);
            MoveYoyo(yoyo, heading.ToRotationVector2() * speed);

            if (!yoyo.IsOwner)
                return;
            if (b != null)
            {
                Rectangle box = b.Hitbox;
                box.Inflate((int)Tune.SeekTouchPadPx, (int)Tune.SeekTouchPadPx);
                if (SegmentHit.Overlaps(box, position, yoyo.Projectile.Center, Tune.SeekTouchPadPx + yoyo.Projectile.width))
                {
                    loop.SetPhase(PhaseHook);
                    EnterPhase(yoyo, PhaseHook);
                    return;
                }
            }
            if (phaseTick > Tune.SeekTicks)
                EndRock(yoyo, loop, true);   // it never reached B
        }

        // The yoyo hooked B: it curves on from B (with the speed it had) and back to the hand, where the triangle is complete.
        void TickHook(YoyoProjectile yoyo, LoopYoyoProjectile loop, Vector2 hand)
        {
            int ticks = Math.Max(1, Tune.HookTicks);
            float u = MathHelper.Clamp(phaseTick / (float)ticks, 0f, 1f);
            Vector2 start = lastB + hookOffset;   // the curve starts where the yoyo was, and follows B
            Vector2 toHand = hand - lastB;
            float distance = toHand.Length();
            Vector2 back = distance > 1f ? toHand / distance : new Vector2(yoyo.Side, 0f);   // from B toward the hand
            Vector2 c1 = start + hookVel * (ticks / 3f);
            Vector2 c2 = hand - back * (distance * Tune.HookArriveScale);   // it arrives from B's side
            Vector2 point = YoyoLoop.Bezier(start, c1, c2, hand, u);
            MoveYoyo(yoyo, point - yoyo.Projectile.Center);
            if (yoyo.IsOwner && phaseTick >= ticks)
            {
                loop.SetPhase(PhaseSwing);
                EnterPhase(yoyo, PhaseSwing);
            }
        }

        // The pendulum: the yoyo hangs from the hand and swings along the arc from A's center to B's center at a steady speed and
        // bounces off each end (no slowing down and speeding up), a little faster every swing. A pure function of the two centers, the hand and the tick count.
        void TickSwing(YoyoProjectile yoyo, LoopYoyoProjectile loop, Vector2 hand)
        {
            swingTick++;
            float speedUp = YoyoLoop.Smooth(swingTick / (float)Math.Max(1, Tune.PendulumSpeedUpTicks));
            float period = Math.Max(4f, MathHelper.Lerp(Tune.PendulumStartTicks, Tune.PendulumEndTicks, speedUp));
            pendulumPhase += MathHelper.TwoPi / period;
            float bounce = (float)(2.0 / Math.PI * Math.Asin(Math.Sin(pendulumPhase)));   // a triangle wave from -1 to 1: constant speed, turns round at once at the ends
            float u = 0.5f + 0.5f * Tune.PendulumSwing * bounce;
            Vector2 toA = lastA - hand;
            Vector2 toB = lastB - hand;
            float angleA = (float)Math.Atan2(toA.Y, toA.X);
            float angleB = (float)Math.Atan2(toB.Y, toB.X);
            float angle = angleA + MathHelper.WrapAngle(angleB - angleA) * u;   // the short way round the hand
            float length = MathHelper.Lerp(toA.Length(), toB.Length(), u) * YoyoLoop.Smooth(swingTick / (float)Math.Max(1, Tune.PendulumRampInTicks));
            Vector2 target = hand + angle.ToRotationVector2() * length;
            MoveYoyo(yoyo, target - yoyo.Projectile.Center);

            MalaiseFx fx = yoyo.Fx as MalaiseFx;
            float cosine = (float)Math.Cos(pendulumPhase);
            if (swingTick > 1 && prevCos * cosine <= 0f)
                fx?.RockSwingEnd(yoyo.Projectile.Center, speedUp);   // the yoyo reached one end of the swing
            prevCos = cosine;
            if (swingTick % 2 == 0)
                fx?.RockSwingTrail(yoyo.Projectile.Center, yoyo.Vel, speedUp);

            if (yoyo.IsOwner && (swingTick >= Tune.MaxHoldTicks || (swingTick >= Tune.PendulumMinTicks && !ButtonHeld(yoyo))))
                EndRock(yoyo, loop, true);   // let go, or held long enough
        }

        // Moves the yoyo by 'delta'. By default it flies through blocks (the string does too); Vel becomes the move (spin and afterimages).
        void MoveYoyo(YoyoProjectile yoyo, Vector2 delta)
        {
            if (Tune.FliesThroughBlocks)
            {
                yoyo.Projectile.Center += delta;
                yoyo.Vel = delta;
            }
            else
                yoyo.Move(delta, true);
        }

        void EnterPhase(YoyoProjectile yoyo, int next)
        {
            phase = next;
            phaseTick = 0;
            switch (next)
            {
                case PhaseHook:
                    reachedB = true;
                    hookStartTick = rockTick;
                    hookOffset = yoyo.Projectile.Center - lastB;
                    hookVel = yoyo.Vel;
                    (yoyo.Fx as LoopYoyoFx)?.LoopCatch(lastB);   // the string bends over B
                    break;
                case PhaseSwing:
                    swingTick = 0;
                    pendulumPhase = 0f;
                    prevCos = 1f;
                    (yoyo.Fx as MalaiseFx)?.RockTriangle(yoyo.Hand, lastA, lastB);
                    break;
                case PhaseDone:
                    FinishLocal(yoyo);
                    break;
            }
        }

        // ---- the end ----

        // Owner's client: Rock the Baby is over (let go, held long enough, broken, an enemy gone, or the yoyo was pulled back by something
        // else: OnRetract). Every looped enemy that is still alive is Confused; the phase goes to the others in the next sync.
        void EndRock(YoyoProjectile yoyo, LoopYoyoProjectile loop, bool returnNow)
        {
            if (!rock)
                return;
            Confuse(FindA());
            if (reachedB)
                Confuse(FindB());
            loop.SetPhase(PhaseDone);
            FinishLocal(yoyo);
            if (returnNow)
                yoyo.BeginReturn();
        }

        // Every machine: it is over here; the release ring (visual).
        void FinishLocal(YoyoProjectile yoyo)
        {
            if (!rock)
                return;
            rock = false;
            rockDone = true;
            phase = PhaseDone;
            (yoyo.Fx as MalaiseFx)?.RockRelease(lastA, lastB, reachedB);
        }

        public override void OnRetract(YoyoProjectile yoyo)
        {
            LoopYoyoProjectile loop = yoyo as LoopYoyoProjectile;
            if (rock && loop != null && yoyo.IsOwner)
                EndRock(yoyo, loop, false);   // the yoyo is on its way back anyway
        }

        // The owner's client: Confused for ConfusedTicks (AddBuff does it here, the message tells the server and everyone else).
        protected void Confuse(NPC npc)
        {
            if (npc == null || !npc.active || npc.life <= 0)
                return;
            int ticks = Tune.ConfusedTicks;
            npc.AddBuff(BuffID.Confused, ticks);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.AddNPCBuff, -1, -1, null, npc.whoAmI, BuffID.Confused, ticks);
        }

        // ---- the string (visual) ----

        // The string is hand-drawn while it runs: seek = hand, round A, yoyo; hook = hand, round A, round B, yoyo; swing = the closed
        // triangle (hand, round A, round B, hand) plus the pendulum string from the hand to the yoyo.
        public override bool StringPath(YoyoProjectile yoyo, List<Vector2> points)
        {
            if (!rock)
                return base.StringPath(yoyo, points);
            if (Main.dedServ)
                return false;
            points.Clear();
            Vector2 hand = yoyo.Hand;
            Vector2 center = yoyo.Projectile.Center;
            float grow = Math.Max(1f, Tune.WrapGrowTicks);
            float radiusA = baseA * YoyoLoop.Smooth(rockTick / grow);
            float radiusB = reachedB ? baseB * YoyoLoop.Smooth((rockTick - hookStartTick) / grow) : 0f;
            switch (phase)
            {
                case PhaseSeek:
                    YoyoLoop.AddWrapped(points, new[] { hand, lastA, center }, new[] { 0f, radiusA, 0f }, turn, Tune.LoopStringStepPx, Tune.LoopArcSamples);
                    break;
                case PhaseHook:
                    YoyoLoop.AddWrapped(points, new[] { hand, lastA, lastB, center }, new[] { 0f, radiusA, radiusB, 0f }, turn, Tune.LoopStringStepPx, Tune.LoopArcSamples);
                    break;
                default:
                    YoyoLoop.AddWrapped(points, new[] { hand, lastA, lastB, hand }, new[] { 0f, radiusA, radiusB, 0f }, turn, Tune.LoopStringStepPx, Tune.LoopArcSamples);
                    YoyoLoop.AddLine(points, hand, center, Tune.LoopStringStepPx);
                    break;
            }
            return true;
        }

        public override float SpinStep(YoyoProjectile yoyo)
        {
            if (!rock)
                return base.SpinStep(yoyo);
            return yoyo.Side * Math.Max(yoyo.Vel.Length() * T.SpinPerPixel, Tune.RockSpinSpeed);
        }

        public override bool RopeTaut(YoyoProjectile yoyo)
        {
            return rock || base.RopeTaut(yoyo);
        }
    }
}