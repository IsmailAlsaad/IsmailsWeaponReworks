using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// A set of whip-like vines (verlet ropes with a lively sideways wave) that grow from roots somebody else moves (the held blade's edge now,
    /// a planted sword later). Plain class, owned by a projectile that calls Tick every tick and Draw from its PreDraw.
    /// SEARCHING: a free vine floats around the set's center, its tip drifting along a slow looping path (a sweep and a breathing reach around a
    /// slowly turning direction, a different one for each vine) so the vines look like they search the area. A vine that has an enemy within
    /// reach locks on (the three prefer different enemies; with only one in reach they all take it), its tip circles the enemy, and after
    /// LoopTicks the looped section breaks off as a BladeOfGrassSpineProjectile and the vine gets shorter FOR GOOD (the closer the enemy,
    /// the more of the vine breaks off). The length of every vine is owned from outside (the owner's BladeOfGrassPlayer): the owner's copy reads
    /// it every tick and writes back what a break-off took; other machines follow the synced length.
    /// MULTIPLAYER: the owner's client decides the targets and the break-offs. Write/Read carry the target enemy and the length per vine
    /// (the host projectile sends them through SendExtraAI when TakeSync says something changed). The searching motion is computed on every
    /// machine from the host's identity and a local clock, and the rope physics is visual: neither is ever synced.
    /// </summary>
    public class BladeOfGrassVineSet
    {
        public class Vine
        {
            public Vector2[] P;
            public Vector2[] Old;
            public bool Init;
            public float Length;      // current length in pixels
            public float SyncLength;  // the owner's length (the others ease toward it)
            public int Target = -1;   // enemy index, -1 = none
            public int LastTarget = -1;
            public int WrapTicks;     // owner: ticks the tip has circled the enemy
            public int Cooldown;      // owner: ticks before this vine may target again
            public float LoopAngle;
            public float LoopDir = 1f;
            public float Seed;
            public float Shown;       // length of the vine actually on show (shorter than Length while it reaches for something)
            public int LoopClock;     // ticks since this machine saw the vine lock on (visual only)
            public Vector2 AimPos, AimVel; // the homing point the free tip reaches for (visual only)
            // Per-vine character, rolled once from the host's identity (every machine gives the same vine the same traits; never synced). 1 = the tuning value.
            public float WaveAmp = 1f, WaveSpeed = 1f, WaveLen = 1f, StiffnessFactor = 1f, SpeedFactor = 1f;
            public float Phase;       // where around the center this vine searches first (radians)
            public float OrbitDir = 1f;
        }

        public Vine[] Vines;

        Projectile host;
        Vector2 center;
        Func<int, Vector2> rootOf;
        float charge;
        float[] ownerLengths;
        bool isOwner;
        int clock;
        bool traitsRolled;
        bool needsSync;
        int[] targetCounts;
        float[] coilTotals;
        Asset<Texture2D> chainAsset;
        Asset<Texture2D> headAsset;

        void Ensure()
        {
            int count = Math.Max(1, BladeOfGrassTuning.VineCount);
            if (Vines != null && Vines.Length == count)
                return;
            Vines = new Vine[count];
            for (int i = 0; i < count; i++)
                Vines[i] = new Vine { Seed = i * 2.1f };
            traitsRolled = false;
        }

        /// <summary>The host calls this once per tick after it has put the roots in place. 'roots' gives the root of vine i (world position); 'lengths' is the owner's length array (null on the other machines).</summary>
        public void Tick(Projectile projectile, Vector2 centerWorld, Func<int, Vector2> roots, float chargeNow, float[] lengths)
        {
            host = projectile;
            center = centerWorld;
            rootOf = roots;
            charge = chargeNow;
            isOwner = projectile.owner == Main.myPlayer;
            ownerLengths = isOwner ? lengths : null;
            Ensure();
            if (!traitsRolled)
                RollTraits();
            clock++;

            if (isOwner && clock % Math.Max(1, BladeOfGrassTuning.RetargetInterval) == 0)
                AssignTargets();
            for (int i = 0; i < Vines.Length; i++)
                StepVine(i, Vines[i]);
        }

        /// <summary>True once after the owner's targets or lengths changed: the host then sets netUpdate.</summary>
        public bool TakeSync()
        {
            bool value = needsSync;
            needsSync = false;
            return value;
        }

        // ---------------------------------------------------------------- sync

        public void Write(BinaryWriter writer, float[] lengths)
        {
            Ensure();
            writer.Write((byte)Vines.Length);
            for (int i = 0; i < Vines.Length; i++)
            {
                float length = lengths != null && i < lengths.Length ? lengths[i] : Vines[i].SyncLength;
                writer.Write((short)Vines[i].Target);
                writer.Write((short)Math.Round(length));
            }
        }

        public void Read(BinaryReader reader)
        {
            Ensure();
            int count = reader.ReadByte();
            for (int i = 0; i < count; i++)
            {
                short target = reader.ReadInt16();
                short length = reader.ReadInt16();
                if (i < Vines.Length)
                {
                    Vines[i].Target = target;
                    Vines[i].SyncLength = length;
                }
            }
        }

        // ---------------------------------------------------------------- targets (owner only)

        bool Valid(NPC npc)
        {
            return npc.active && !npc.friendly && !npc.dontTakeDamage && npc.CanBeChasedBy(host);
        }

        void AssignTargets()
        {
            if (targetCounts == null || targetCounts.Length != Main.maxNPCs)
                targetCounts = new int[Main.maxNPCs];
            Array.Clear(targetCounts, 0, targetCounts.Length);
            FillCoilTotals();

            // 1) Keep the targets that are still good (a vine in the middle of a loop always keeps its enemy while it is valid).
            for (int i = 0; i < Vines.Length; i++)
            {
                Vine vine = Vines[i];
                if (vine.Target < 0)
                    continue;
                NPC npc = Main.npc[vine.Target];
                bool keep = Valid(npc) && HasRoom(vine.Target); // an enemy that is full of vine is left alone, also in the middle of a loop
                if (keep && vine.WrapTicks <= 0)
                    keep = Vector2.Distance(rootOf(i), npc.Center) <= vine.Length + BladeOfGrassTuning.ReachSlack * 2f;
                if (keep)
                    targetCounts[vine.Target]++;
                else
                {
                    vine.Target = -1;
                    vine.WrapTicks = 0;
                    needsSync = true;
                }
            }

            // 2) Free vines pick the nearest enemy in reach that no other vine has; only when there is nothing else, one that is taken.
            for (int i = 0; i < Vines.Length; i++)
            {
                Vine vine = Vines[i];
                if (vine.Target >= 0 || vine.Cooldown > 0 || vine.Length < BladeOfGrassTuning.MinWrapPixels)
                    continue;
                Vector2 root = rootOf(i);
                int best = -1;
                float bestScore = float.MaxValue;
                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    NPC npc = Main.npc[n];
                    if (!Valid(npc) || !HasRoom(n))
                        continue;
                    float distance = Vector2.Distance(root, npc.Center);
                    if (distance > vine.Length + BladeOfGrassTuning.ReachSlack)
                        continue;
                    float score = distance + targetCounts[n] * BladeOfGrassTuning.SamePenalty;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = n;
                    }
                }
                if (best >= 0)
                {
                    vine.Target = best;
                    vine.WrapTicks = 0;
                    vine.LoopDir = Main.rand.NextBool() ? 1f : -1f;
                    targetCounts[best]++;
                    needsSync = true;
                }
            }
        }

        // Slightly different wave, stiffness, speed and search path for each vine so the three do not look like copies. The projectile's identity
        // is the same on every machine, so everyone rolls the same traits. BladeOfGrassTuning.VineVariation scales how different they are (0 = identical).
        void RollTraits()
        {
            traitsRolled = true;
            var random = new Random(host.identity * 7919 + host.owner * 104729 + 17);
            float Spread(float scale) => 1f + ((float)random.NextDouble() * 2f - 1f) * BladeOfGrassTuning.VineVariation * scale;

            for (int i = 0; i < Vines.Length; i++)
            {
                Vine vine = Vines[i];
                vine.Seed = i * 2.1f + (float)random.NextDouble() * 2f;
                vine.WaveAmp = Spread(1.3f);
                vine.WaveSpeed = Spread(1f);
                vine.WaveLen = Spread(1f);
                vine.StiffnessFactor = Spread(0.6f);
                vine.SpeedFactor = Spread(1.2f);
                // The vines start spread evenly around the center; every other one turns the opposite way.
                vine.Phase = MathHelper.TwoPi * i / Vines.Length + ((float)random.NextDouble() * 2f - 1f) * 0.5f;
                vine.OrbitDir = i % 2 == 0 ? 1f : -1f;
            }
        }

        // Where a free vine's tip is heading now: around the center, a different slow looping path for each vine.
        Vector2 SearchGoal(Vine vine)
        {
            float t = clock;
            float angle = vine.Phase + vine.OrbitDir * t * BladeOfGrassTuning.SearchOrbitSpeed * vine.SpeedFactor
                + (float)Math.Sin(t * BladeOfGrassTuning.SearchSweepSpeed * vine.WaveSpeed + vine.Seed) * MathHelper.ToRadians(BladeOfGrassTuning.SearchSweepDeg);
            float breath = 0.5f + 0.5f * (float)Math.Sin(t * BladeOfGrassTuning.SearchBreathSpeed * vine.WaveSpeed + vine.Seed * 1.7f);
            float reach = vine.Length * MathHelper.Lerp(BladeOfGrassTuning.SearchReachMin, BladeOfGrassTuning.SearchReachMax, breath);
            return center + angle.ToRotationVector2() * reach;
        }

        // ---------------------------------------------------------------- the rope

        void StepVine(int index, Vine vine)
        {
            int n = Math.Max(4, BladeOfGrassTuning.PointsPerVine);
            if (vine.P == null || vine.P.Length != n)
            {
                vine.P = new Vector2[n];
                vine.Old = new Vector2[n];
                vine.Init = false;
            }
            Vector2 root = rootOf(index);

            // Length: the owner's copy reads the number it owns (hits made it longer, break-offs shorter); everyone else follows the synced length.
            if (isOwner && ownerLengths != null && index < ownerLengths.Length)
            {
                vine.Length = ownerLengths[index];
                vine.SyncLength = vine.Length;
                if (vine.Cooldown > 0)
                    vine.Cooldown--;
            }
            else
                vine.Length += (vine.SyncLength - vine.Length) * 0.35f;

            if (vine.Length < 2f)
            {
                vine.Init = false;
                vine.Target = isOwner ? -1 : vine.Target;
                vine.Shown = 0f;
                return;
            }

            // The vine lives in the WORLD: its tip heads for a goal that floats around the center. Turning the blade only moves the root; the
            // rest follows through the rope constraints (so that pull fades toward the tip).
            Vector2 goal = SearchGoal(vine);
            Vector2 freeDirection = (goal - root).SafeNormalize(Vector2.UnitX);

            bool hasTarget = vine.Target >= 0 && vine.Target < Main.maxNPCs && Valid(Main.npc[vine.Target]);
            if (!hasTarget && vine.Target >= 0)
            {
                vine.Target = -1;
                vine.WrapTicks = 0;
                if (isOwner)
                    needsSync = true;
            }
            NPC targetNpc = hasTarget ? Main.npc[vine.Target] : null;

            // The loop around the enemy: the tip circles and the end of the rope trails behind it on the same circle, a real coil.
            float radius = 0f;
            float wrapSpan = 0f;
            float arc = 0f;
            if (hasTarget)
            {
                if (vine.LastTarget != vine.Target)
                {
                    vine.LoopClock = 0;
                    vine.LoopAngle = (root - targetNpc.Center).ToRotation(); // start on the side facing the root: the rope arrives, then circles
                    if (!isOwner)
                        vine.LoopDir = Main.rand.NextBool() ? 1f : -1f;
                    if (Vector2.Distance(root, targetNpc.Center) < 4000f)
                        BladeOfGrassFx.Lock(targetNpc.Center);
                }
                vine.LoopClock++;
                float progress = MathHelper.Clamp(vine.LoopClock / (float)Math.Max(1, BladeOfGrassTuning.LoopTicks), 0f, 1f);
                radius = (Math.Max(targetNpc.width, targetNpc.height) * BladeOfGrassTuning.WrapRadiusScale + BladeOfGrassTuning.WrapRadiusPad) * (1f - BladeOfGrassTuning.WrapTighten * progress);
                wrapSpan = MathHelper.TwoPi * BladeOfGrassTuning.WrapTurns * MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(vine.LoopClock / (float)Math.Max(1, BladeOfGrassTuning.WrapSpanTicks), 0f, 1f));
                arc = radius * wrapSpan;
                vine.LoopAngle += BladeOfGrassTuning.LoopSpeed * vine.SpeedFactor * vine.LoopDir;
            }
            else
                vine.LoopClock = 0;
            vine.LastTarget = hasTarget ? vine.Target : -1;

            // How much of the vine is on show: while searching only what it needs to put its tip on its goal (plus a little slack to wave
            // in; the rest stays coiled up, so the vine never flaps around with spare rope); while reaching for an enemy only what it needs to get there and wrap it.
            float shownGoal;
            if (hasTarget)
                shownGoal = Math.Min(vine.Length, Math.Max(0f, Vector2.Distance(root, targetNpc.Center) - radius) + arc + BladeOfGrassTuning.WrapSlackPx);
            else
                shownGoal = Math.Min(vine.Length, Vector2.Distance(root, goal) + BladeOfGrassTuning.FreeSlackPx);
            if (vine.Shown <= 0f)
                vine.Shown = shownGoal;
            else
                vine.Shown += (shownGoal - vine.Shown) * BladeOfGrassTuning.ShownEase;
            vine.Shown = Math.Min(vine.Shown, vine.Length);
            float segment = Math.Max(1f, vine.Shown) / (n - 1);

            if (!vine.Init)
            {
                for (int k = 0; k < n; k++)
                {
                    vine.P[k] = root + freeDirection * (segment * k);
                    vine.Old[k] = vine.P[k];
                }
                vine.AimPos = vine.P[n - 1];
                vine.AimVel = Vector2.Zero;
                vine.Init = true;
            }

            // Points 0..bodyCount-1 are the free body; the points from bodyCount to the tip are the coil (only while looping).
            int tail = hasTarget ? Math.Clamp((int)Math.Round(arc / segment), 2, n - 2) : 0;
            int bodyCount = n - 1 - tail;
            // The tip heads for the search goal, but never farther than the vine shows.
            Vector2 fromCenter = goal - center;
            Vector2 tipGoal = center + fromCenter.SafeNormalize(freeDirection) * Math.Min(fromCenter.Length(), vine.Shown + Vector2.Distance(center, root));

            Vector2 CoilWish(int k)
            {
                float t = (k - bodyCount) / (float)tail;
                float angle = vine.LoopAngle - vine.LoopDir * (1f - t) * wrapSpan;
                float ringRadius = radius * (1f + BladeOfGrassTuning.WrapSpiral * (1f - t));
                return targetNpc.Center + angle.ToRotationVector2() * ringRadius;
            }

            // The free tip homes on its goal: it accelerates toward it (limited acceleration = inertia), eases in near it and never exceeds a
            // top speed. While wrapping an enemy it just rides along with the rope so letting go is smooth.
            if (hasTarget)
            {
                vine.AimPos = vine.P[n - 1];
                vine.AimVel = Vector2.Zero;
            }
            else
            {
                Vector2 toGoal = tipGoal - vine.AimPos;
                float goalDistance = toGoal.Length();
                float topSpeed = BladeOfGrassTuning.TipMaxSpeed * vine.SpeedFactor;
                Vector2 desiredVelocity = toGoal.SafeNormalize(Vector2.Zero) * Math.Min(topSpeed, goalDistance * BladeOfGrassTuning.TipArrive);
                Vector2 steer = desiredVelocity - vine.AimVel;
                float maxSteer = BladeOfGrassTuning.TipAccel * vine.SpeedFactor;
                if (steer.Length() > maxSteer)
                    steer = steer.SafeNormalize(Vector2.Zero) * maxSteer;
                vine.AimVel += steer;
                vine.AimPos += vine.AimVel;

                // Never farther from the root than the vine actually shows.
                Vector2 fromRoot = vine.AimPos - root;
                if (fromRoot.Length() > vine.Shown)
                    vine.AimPos = root + fromRoot.SafeNormalize(freeDirection) * vine.Shown;
            }

            Vector2 bodyEnd = hasTarget ? CoilWish(bodyCount) : vine.AimPos;
            Vector2 toEnd = bodyEnd - root;
            Vector2 direction = toEnd.SafeNormalize(freeDirection);
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
            float wiggle = BladeOfGrassTuning.WrigglePx * vine.WaveAmp * (hasTarget ? BladeOfGrassTuning.WriggleTargetFactor : 1f) * (1f + BladeOfGrassTuning.WriggleChargeBoost * charge);

            for (int k = 1; k < n; k++)
            {
                Vector2 velocity = (vine.P[k] - vine.Old[k]) * BladeOfGrassTuning.Damping;
                if (velocity.Length() > BladeOfGrassTuning.MaxPointSpeed)
                    velocity = velocity.SafeNormalize(Vector2.Zero) * BladeOfGrassTuning.MaxPointSpeed;
                velocity.Y += BladeOfGrassTuning.Gravity;
                vine.Old[k] = vine.P[k];
                Vector2 point = vine.P[k] + velocity;

                Vector2 wish;
                float weight;
                if (hasTarget && k >= bodyCount)
                {
                    wish = CoilWish(k);
                    weight = BladeOfGrassTuning.CoilStiffness;
                }
                else
                {
                    // The wish line runs from the root to a goal that is fixed in the world, so a point far from the root hardly feels the root move.
                    float along = k / (float)Math.Max(1, bodyCount);
                    float envelope = 4f * along * (1f - along) + 0.25f * along; // the wave is biggest in the middle, a little lash at the tip
                    float wave = (float)Math.Sin(clock * BladeOfGrassTuning.WriggleSpeed * vine.WaveSpeed + along * BladeOfGrassTuning.WriggleWaves * vine.WaveLen + vine.Seed);
                    wish = root + toEnd * along + perpendicular * (wave * wiggle * envelope);
                    float influence = BladeOfGrassTuning.RootInfluenceFloor + (1f - BladeOfGrassTuning.RootInfluenceFloor) * (float)Math.Pow(along, Math.Max(0.1f, BladeOfGrassTuning.TipInfluencePower));
                    weight = BladeOfGrassTuning.Stiffness * vine.StiffnessFactor * (hasTarget ? BladeOfGrassTuning.TargetStiffnessBoost : 1f) * influence;
                }
                point += (wish - point) * weight;
                vine.P[k] = point;
            }
            vine.Old[0] = root;
            for (int pass = 0; pass < Math.Max(1, BladeOfGrassTuning.ConstraintPasses); pass++)
            {
                vine.P[0] = root;
                for (int k = 1; k < n; k++)
                {
                    Vector2 delta = vine.P[k] - vine.P[k - 1];
                    float length = delta.Length();
                    delta = length > 0.001f ? delta / length : direction;
                    vine.P[k] = vine.P[k - 1] + delta * segment;
                }
            }

            // The owner decides when the looped section breaks off.
            if (isOwner && hasTarget)
            {
                float rootDistance = Vector2.Distance(root, targetNpc.Center);
                if (rootDistance <= vine.Length + BladeOfGrassTuning.ReachSlack)
                    vine.WrapTicks++;
                else
                    vine.WrapTicks = Math.Max(0, vine.WrapTicks - 1);
                if (vine.WrapTicks >= BladeOfGrassTuning.LoopTicks)
                    BreakOff(index, vine, targetNpc, rootDistance);
            }
        }

        // Owner only: the looped section leaves the vine and sticks to the enemy. The vine stays that much shorter (nothing gives it back
        // except more hits).
        void BreakOff(int index, Vine vine, NPC npc, float rootDistance)
        {
            float distanceFraction = MathHelper.Clamp(rootDistance / Math.Max(1f, BladeOfGrassTuning.MaxLength), 0f, 1f);
            float wrapFraction = MathHelper.Lerp(BladeOfGrassTuning.MaxWrapFraction, BladeOfGrassTuning.MinWrapFraction, distanceFraction);
            float wrapped = vine.Length * wrapFraction;
            wrapped = Math.Max(wrapped, BladeOfGrassTuning.MinWrapPixels);
            wrapped = Math.Min(wrapped, vine.Length - BladeOfGrassTuning.MinRemainPixels);
            // The enemy only has room for so much vine: what is already on it counts against the cap and the new section is cut to what is left.
            FillCoilTotals();
            wrapped = Math.Min(wrapped, EnemyCap - coilTotals[npc.whoAmI]);
            if (wrapped < 8f)
            {
                vine.WrapTicks = 0; // the vine is too short (or the enemy is full) to give anything: keep circling until the targets are refreshed
                return;
            }

            EnforceSegmentCap();
            float lengthFactor = MathHelper.Lerp(BladeOfGrassTuning.SegmentMinLengthFactor, BladeOfGrassTuning.SegmentMaxLengthFactor, MathHelper.Clamp(wrapped / Math.Max(1f, BladeOfGrassTuning.MaxLength), 0f, 1f));
            int damage = Math.Max(1, (int)(host.damage * BladeOfGrassTuning.SegmentDamageScale * lengthFactor));
            Projectile.NewProjectile(host.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BladeOfGrassSpineProjectile>(),
                damage, 0f, host.owner, npc.whoAmI, wrapped, Main.rand.NextFloat(MathHelper.TwoPi));

            vine.Length -= wrapped;
            vine.SyncLength = vine.Length;
            if (ownerLengths != null && index < ownerLengths.Length)
                ownerLengths[index] = vine.Length;
            BladeOfGrassFx.BreakOff(npc.Center, wrapped / Math.Max(1f, BladeOfGrassTuning.MaxLength));
            vine.Target = -1;
            vine.WrapTicks = 0;
            vine.Cooldown = BladeOfGrassTuning.WrapCooldownTicks;
            needsSync = true;
        }

        float EnemyCap => BladeOfGrassTuning.MaxLength * BladeOfGrassTuning.EnemyCapFraction;

        // Owner only: coilTotals[i] = the length of all this player's coils that are on enemy i right now (they count against the enemy cap).
        void FillCoilTotals()
        {
            if (coilTotals == null || coilTotals.Length != Main.maxNPCs)
                coilTotals = new float[Main.maxNPCs];
            Array.Clear(coilTotals, 0, coilTotals.Length);
            int type = ModContent.ProjectileType<BladeOfGrassSpineProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != host.owner || projectile.type != type)
                    continue;
                int enemy = (int)projectile.ai[0];
                if (enemy >= 0 && enemy < Main.maxNPCs)
                    coilTotals[enemy] += projectile.ai[1];
            }
        }

        // An enemy that already carries a full vine's worth of coils (less than MinEnemyRoomPx room left) is left alone until some of them expire.
        bool HasRoom(int enemy)
        {
            return EnemyCap - coilTotals[enemy] >= BladeOfGrassTuning.MinEnemyRoomPx;
        }

        void EnforceSegmentCap()
        {
            int type = ModContent.ProjectileType<BladeOfGrassSpineProjectile>();
            while (true)
            {
                int count = 0;
                int oldest = -1;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (!projectile.active || projectile.owner != host.owner || projectile.type != type)
                        continue;
                    count++;
                    if (oldest < 0 || projectile.timeLeft < Main.projectile[oldest].timeLeft)
                        oldest = i;
                }
                if (count < BladeOfGrassTuning.MaxSegments || oldest < 0)
                    break;
                Main.projectile[oldest].Kill();
            }
        }

        // The host ended (the sword was put away, the player died): the vines wither where they are.
        public void Release()
        {
            if (Main.dedServ || Vines == null)
                return;
            for (int i = 0; i < Vines.Length; i++)
            {
                Vine vine = Vines[i];
                if (vine.Init && vine.P != null && vine.Length > 2f)
                    BladeOfGrassFx.Release(vine.P[vine.P.Length - 1]);
            }
        }

        // ---------------------------------------------------------------- drawing

        public void Draw()
        {
            if (Main.dedServ || Vines == null)
                return;
            chainAsset ??= ModContent.Request<Texture2D>("Terraria/Images/" + BladeOfGrassTuning.VineChainName);
            headAsset ??= ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.IvyWhip);
            Texture2D chain = chainAsset.Value;
            Texture2D head = headAsset.Value;

            // All the chains first (as one mesh each, see VineDraw.DrawStrip), then the heads.
            VineDraw.BeginMesh();
            for (int i = 0; i < Vines.Length; i++)
                DrawVineBody(Vines[i], chain);
            VineDraw.EndMesh();
            for (int i = 0; i < Vines.Length; i++)
                DrawVineHead(Vines[i], head);
        }

        void DrawVineBody(Vine vine, Texture2D chain)
        {
            if (!vine.Init || vine.P == null || vine.Length < 2f)
                return;
            VineDraw.DrawStrip(chain, vine.P, vine.P.Length, BladeOfGrassTuning.VineScale, BladeOfGrassTuning.VineScale * BladeOfGrassTuning.VineTipScale, 1f, 0f, BladeOfGrassTuning.ChainEndTrimPx);
        }

        void DrawVineHead(Vine vine, Texture2D head)
        {
            if (!vine.Init || vine.P == null || vine.Length < 2f)
                return;
            int n = vine.P.Length;
            // The Ivy Whip head over the end of the vine; its back covers the last links, so there is no gap.
            Vector2 end = vine.P[n - 1];
            float angle = (end - vine.P[Math.Max(0, n - 3)]).ToRotation();
            VineDraw.DrawHead(head, end, angle, BladeOfGrassTuning.HeadScale, Lighting.GetColor(end.ToTileCoordinates()), BladeOfGrassTuning.HeadForwardPx);
        }
    }
}