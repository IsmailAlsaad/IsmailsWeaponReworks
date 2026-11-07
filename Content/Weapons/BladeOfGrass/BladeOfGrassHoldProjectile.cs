using IsmailsWeaponReworks.Common;
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

namespace IsmailsWeaponReworks.Content.Weapons.BladeOfGrass
{
    /// <summary>
    /// The held Blade of Grass. While the button is down it points the blade at the cursor, shakes more and more, and grows three
    /// whip-like vines (verlet ropes with a lively sideways wave) from the blade's edge toward the cursor; the longer the hold, the longer
    /// the vines. A vine locks onto an enemy near the cursor (the three prefer different enemies; with only one in reach they all take
    /// it), its tip circles the enemy, and after LoopTicks the looped section breaks off as a BladeOfGrassSpineProjectile and the vine gets
    /// shorter (the closer the enemy, the more of the vine breaks off). The vines then regrow while the button stays down.
    /// MULTIPLAYER: the owner's client decides everything (aim, targets, when a section breaks off, the damage). It sends through
    /// SendExtraAI: the aim, the cursor distance, and per vine the target enemy and the length. Every other machine builds the same
    /// ropes from that and only DRAWS; the rope physics itself is visual, never synced. A section that breaks off is a normal projectile
    /// spawned by the owner (its enemy, length and coil phase travel in its ai[]). Nothing needs a custom packet.
    /// </summary>
    public class BladeOfGrassHoldProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.BladeofGrass;

        class Vine
        {
            public Vector2[] P;
            public Vector2[] Old;
            public bool Init;
            public float Length;      // current length in pixels
            public float SyncLength;  // other machines: the owner's length, which Length eases toward
            public int Target = -1;   // enemy index, -1 = none
            public int LastTarget = -1;
            public int WrapTicks;     // owner: ticks the tip has circled the enemy
            public int Cooldown;      // owner: ticks before this vine may target again
            public float LoopAngle;
            public float LoopDir = 1f;
            public float Seed;
            public float Shown;       // length of the vine actually on show (shorter than Length while it reaches for an enemy)
            public int LoopClock;     // ticks since this machine saw the vine lock on (visual only, so every machine has it)
            // Per-vine character, rolled once per held blade from its synced identity (so every machine gives the same vine the same traits;
            // visual, never synced). 1 = the tuning value.
            public float WaveAmp = 1f, WaveSpeed = 1f, WaveLen = 1f, StiffnessFactor = 1f, SpeedFactor = 1f, LengthFactor = 1f, GrowFactor = 1f;
            public float SpreadJitterDeg;
            public int StartJitter;
        }

        Player Owner => Main.player[Projectile.owner];

        Vine[] vines;
        int holdTicks;
        float charge;           // 0..1, how shaken the blade is
        float aimShown;         // aim in the player's own frame
        float syncAim;          // other machines: the owner's latest aim
        bool gotAim;
        bool retracting;        // the button was let go or the weapon swapped: the vines shrink away, bit by bit
        bool handLocked;
        Vector2 handOffset;     // where the blade's hand was relative to the player when the retreat began (the roots stay there)
        bool keepHeld = true;   // while the vines retract the player keeps holding the blade; false for good once the weapon was swapped away

        public bool Retracting => retracting;
        public bool KeepsHolding => retracting && keepHeld; // retracting, but the blade is still in the player's hand
        float cursorDist;
        int syncTimer;
        int searchTimer;
        Vector2 hand;
        Vector2 bladeDir;
        int[] targetCounts;
        float[] coilTotals;     // owner: per enemy, the length of this player's coils on it (the enemy cap counts against it)
        bool traitsRolled;
        bool releaseSeen;       // owner: the button was let go since the retreat began (pressing it again then lets the vines grow again)
        Asset<Texture2D> chainAsset;
        Asset<Texture2D> headAsset;
        readonly List<Vector2> samplePositions = new List<Vector2>();
        readonly List<float> sampleAngles = new List<float>();

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.friendly = false; // it never hurts anything itself: the broken-off sections do
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        void EnsureVines()
        {
            int count = Math.Max(1, BladeOfGrassTuning.VineCount);
            if (vines != null && vines.Length == count)
                return;
            vines = new Vine[count];
            for (int i = 0; i < count; i++)
                vines[i] = new Vine { Seed = i * 2.1f };
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            EnsureVines();
            syncAim = Projectile.ai[0];
            aimShown = Projectile.ai[0];
            gotAim = true;
        }

        // ---------------------------------------------------------------- sync

        public override void SendExtraAI(BinaryWriter writer)
        {
            EnsureVines();
            writer.Write(aimShown);
            writer.Write(cursorDist);
            writer.Write((byte)vines.Length);
            for (int i = 0; i < vines.Length; i++)
            {
                writer.Write((short)vines[i].Target);
                writer.Write((short)Math.Round(vines[i].Length));
            }
            writer.Write(retracting);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            EnsureVines();
            syncAim = reader.ReadSingle();
            cursorDist = reader.ReadSingle();
            if (!gotAim)
            {
                aimShown = syncAim;
                gotAim = true;
            }
            int count = reader.ReadByte();
            for (int i = 0; i < count; i++)
            {
                short target = reader.ReadInt16();
                short length = reader.ReadInt16();
                if (i < vines.Length)
                {
                    vines[i].Target = target;
                    vines[i].SyncLength = length;
                }
            }
            retracting = reader.ReadBoolean(); // false again when the owner pressed the button during the retreat
        }

        // ---------------------------------------------------------------- AI

        public override void AI()
        {
            Player owner = Owner;
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            // The owner's client starts the retreat when the button is let go, the item is swapped, or the player is crowd-controlled;
            // the flag is synced and every machine then shrinks its own copy of the vines. The owner ends the projectile when they are gone.
            if (isOwner && !retracting && (!owner.channel || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.BladeofGrass))
            {
                EnsureVines();
                retracting = true;
                releaseSeen = !owner.controlUseItem;
                syncAim = aimShown;
                for (int i = 0; i < vines.Length; i++)
                {
                    vines[i].Target = -1;
                    vines[i].WrapTicks = 0;
                }
                Projectile.netUpdate = true;
            }

            EnsureVines();
            Projectile.timeLeft = 2;
            // Pressing the button again while the vines shrink away (and the blade is still in the hand) lets them grow again from where they are.
            // The owner decides; the flag travels through SendExtraAI like the start of the retreat.
            if (isOwner && retracting)
            {
                if (!owner.controlUseItem)
                    releaseSeen = true;
                else if (releaseSeen && keepHeld && owner.HeldItem.type == ItemID.BladeofGrass && !owner.noItems && !owner.CCed)
                {
                    retracting = false;
                    releaseSeen = false;
                    owner.channel = true; // a normal hold ends the blade as soon as channel is false
                    Projectile.netUpdate = true;
                }
            }
            // While the vines retract the player keeps holding the blade (and cannot switch items) until the last vine is gone.
            // Swapping away anyway ends that for good: the blade is then no longer drawn and the roots stay where the hand was.
            if (retracting && keepHeld && owner.HeldItem.type != ItemID.BladeofGrass)
            {
                keepHeld = false;
                syncAim = aimShown;
            }
            bool heldPose = !retracting || keepHeld;
            if (heldPose)
            {
                owner.heldProj = Projectile.whoAmI;
                owner.itemTime = 2;
                owner.itemAnimation = 2;
            }
            Projectile.scale = BladeOfGrassTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);
            holdTicks++;
            if (retracting)
                charge = Math.Max(0f, charge - 0.03f);
            else // rises at most as fast as it falls, so a hold that continues after a retreat does not snap back to the old shake
                charge = Math.Min(MathHelper.Clamp(holdTicks / (float)Math.Max(1, BladeOfGrassTuning.ChargeTicks), 0f, 1f), charge + 0.03f);

            Vector2 cursorWorld;
            if (isOwner && heldPose)
            {
                aimShown = HeldAim.Capture(owner);
                cursorWorld = Main.MouseWorld;
                cursorDist = Math.Min(Vector2.Distance(Main.MouseWorld, owner.MountedCenter), 4000f);
                owner.ChangeDir(HeldAim.Facing(aimShown));
                if (++syncTimer >= Math.Max(1, BladeOfGrassTuning.SyncIntervalTicks))
                {
                    syncTimer = 0;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                aimShown += MathHelper.WrapAngle(syncAim - aimShown) * 0.4f;
                cursorWorld = owner.MountedCenter + HeldAim.ToWorldDirection(owner, aimShown) * cursorDist;
            }

            if (heldPose || hand == Vector2.Zero)
                ApplyPose(owner);
            if (!heldPose)
                LockHand(owner);

            if (isOwner && !retracting && ++searchTimer >= Math.Max(1, BladeOfGrassTuning.RetargetInterval))
            {
                searchTimer = 0;
                AssignTargets(cursorWorld);
            }

            for (int i = 0; i < vines.Length; i++)
                StepVine(i, vines[i], cursorWorld, isOwner);

            if (retracting)
            {
                bool any = false;
                for (int i = 0; i < vines.Length; i++)
                {
                    Vine vine = vines[i];
                    if (vine.Length < 2f || !vine.Init)
                        continue;
                    any = true;
                    if (holdTicks % 4 == 0 && !Main.dedServ)
                        BladeOfGrassFx.Release(vine.P[vine.P.Length - 1]);
                }
                if (!any && isOwner)
                {
                    Projectile.Kill();
                    return;
                }
            }

            // Leaves fly off the shaking blade.
            if (!retracting && charge > 0.2f && Main.rand.NextFloat() < charge * 6f / Math.Max(1, BladeOfGrassTuning.ChargeDustInterval) * 0.2f)
                BladeOfGrassFx.ChargeLeaf(hand + bladeDir * (BladeOfGrassTuning.BladeLength * Projectile.scale * Main.rand.NextFloat()), charge);
            Lighting.AddLight(hand + bladeDir * (BladeOfGrassTuning.BladeLength * 0.5f), 0.1f + 0.2f * charge, 0.3f + 0.3f * charge, 0.08f);
        }

        // While the vines retract the blade is no longer held; the roots stay where the hand was, relative to the player.
        void LockHand(Player owner)
        {
            if (!handLocked)
            {
                handOffset = hand - owner.MountedCenter;
                handLocked = true;
            }
            hand = owner.MountedCenter + handOffset;
            Projectile.Center = hand;
        }

        // Smooth shake that is not periodic enough to look mechanical.
        float Noise(float speed, float offset)
        {
            float t = holdTicks * speed + offset;
            return (float)(Math.Sin(t) * 0.6 + Math.Sin(t * 2.3 + 1.7) * 0.4);
        }

        void ApplyPose(Player owner)
        {
            float shake = (float)Math.Pow(charge, Math.Max(0.1f, BladeOfGrassTuning.ShakeCurve));
            float armAngle = aimShown + MathHelper.ToRadians(BladeOfGrassTuning.ShakeArmDeg) * shake * Noise(0.9f, 0f);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle - MathHelper.PiOver2);
            hand = HeldSwordPose.HandWorld(owner, armAngle)
                + new Vector2(Noise(1.3f, 4f), Noise(1.1f, 9f)) * (BladeOfGrassTuning.ShakeHandPx * shake);
            bladeDir = HeldSwordPose.BladeDirectionWorld(owner, armAngle);
            Projectile.rotation = bladeDir.ToRotation();
            Projectile.Center = hand;
            Projectile.spriteDirection = owner.direction >= 0 ? 1 : -1;
        }

        // ---------------------------------------------------------------- targets (owner only)

        bool Valid(NPC npc)
        {
            return npc.active && !npc.friendly && !npc.dontTakeDamage && npc.CanBeChasedBy(Projectile);
        }

        Vector2 RootOf(int index)
        {
            float fraction = vines.Length <= 1 ? BladeOfGrassTuning.RootEnd
                : MathHelper.Lerp(BladeOfGrassTuning.RootStart, BladeOfGrassTuning.RootEnd, index / (float)(vines.Length - 1));
            return hand + bladeDir * (BladeOfGrassTuning.BladeLength * Projectile.scale * fraction);
        }

        void AssignTargets(Vector2 cursorWorld)
        {
            if (targetCounts == null || targetCounts.Length != Main.maxNPCs)
                targetCounts = new int[Main.maxNPCs];
            Array.Clear(targetCounts, 0, targetCounts.Length);
            FillCoilTotals();

            // 1) Keep the targets that are still good (a vine in the middle of a loop always keeps its enemy while it is valid).
            for (int i = 0; i < vines.Length; i++)
            {
                Vine vine = vines[i];
                if (vine.Target < 0)
                    continue;
                NPC npc = Main.npc[vine.Target];
                bool keep = Valid(npc) && HasRoom(vine.Target); // an enemy that is full of vine is left alone, also in the middle of a loop
                if (keep && vine.WrapTicks <= 0)
                {
                    keep = Vector2.Distance(cursorWorld, npc.Center) <= BladeOfGrassTuning.TargetRadius * BladeOfGrassTuning.LoseTargetFactor
                        && Vector2.Distance(RootOf(i), npc.Center) <= vine.Length + BladeOfGrassTuning.ReachSlack * 2f;
                }
                if (keep)
                    targetCounts[vine.Target]++;
                else
                {
                    vine.Target = -1;
                    vine.WrapTicks = 0;
                    Projectile.netUpdate = true;
                }
            }

            // 2) Free vines pick the enemy nearest the cursor that no other vine has; only when there is nothing else, one that is taken.
            for (int i = 0; i < vines.Length; i++)
            {
                Vine vine = vines[i];
                if (vine.Target >= 0 || vine.Cooldown > 0 || vine.Length < BladeOfGrassTuning.MinWrapPixels)
                    continue;
                Vector2 root = RootOf(i);
                int best = -1;
                float bestScore = float.MaxValue;
                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    NPC npc = Main.npc[n];
                    if (!Valid(npc) || !HasRoom(n))
                        continue;
                    float cursorDistance = Vector2.Distance(cursorWorld, npc.Center);
                    if (cursorDistance > BladeOfGrassTuning.TargetRadius || Vector2.Distance(root, npc.Center) > vine.Length + BladeOfGrassTuning.ReachSlack)
                        continue;
                    float score = cursorDistance + targetCounts[n] * BladeOfGrassTuning.SamePenalty;
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
                    Projectile.netUpdate = true;
                }
            }
        }

        // Slightly different wave, stiffness, speed, growth and length for each vine so the three do not look like copies. The projectile's identity
        // is the same on every machine, so everyone rolls the same traits. BladeOfGrassTuning.VineVariation scales how different they are (0 = identical).
        void RollTraits()
        {
            traitsRolled = true;
            var random = new Random(Projectile.identity * 7919 + Projectile.owner * 104729 + 17);
            float Spread(float scale) => 1f + ((float)random.NextDouble() * 2f - 1f) * BladeOfGrassTuning.VineVariation * scale;

            for (int i = 0; i < vines.Length; i++)
            {
                Vine vine = vines[i];
                vine.Seed = i * 2.1f + (float)random.NextDouble() * 2f;
                vine.WaveAmp = Spread(1.3f);
                vine.WaveSpeed = Spread(1f);
                vine.WaveLen = Spread(1f);
                vine.StiffnessFactor = Spread(0.6f);
                vine.SpeedFactor = Spread(1.2f);

                vine.GrowFactor = Spread(0.8f);
                vine.LengthFactor = 1f - (float)random.NextDouble() * BladeOfGrassTuning.VineVariation * 0.5f; // never longer than MaxLength
                vine.SpreadJitterDeg = ((float)random.NextDouble() * 2f - 1f) * BladeOfGrassTuning.VineVariation * 8f;

                vine.StartJitter = random.Next(0, 7);
            }
        }

        // ---------------------------------------------------------------- the rope

        void StepVine(int index, Vine vine, Vector2 cursorWorld, bool isOwner)
        {
            if (!traitsRolled)
                RollTraits();
            int n = Math.Max(4, BladeOfGrassTuning.PointsPerVine);
            if (vine.P == null || vine.P.Length != n)
            {
                vine.P = new Vector2[n];
                vine.Old = new Vector2[n];
                vine.Init = false;
            }
            Vector2 root = RootOf(index);

            // Length: the owner grows it with the hold time (and regrows it after a cut); everyone else follows the synced length.
            if (isOwner)
            {
                float start = BladeOfGrassTuning.StartTicks + index * BladeOfGrassTuning.StaggerTicks + vine.StartJitter;
                float desired = retracting || holdTicks < start ? 0f
                    : MathHelper.Lerp(BladeOfGrassTuning.MinLength, BladeOfGrassTuning.MaxLength * vine.LengthFactor, MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((holdTicks - start) / (float)Math.Max(1, BladeOfGrassTuning.GrowTicks), 0f, 1f)));
                if (vine.Length < desired)
                    vine.Length = Math.Min(desired, vine.Length + BladeOfGrassTuning.GrowSpeed * vine.GrowFactor);
                else if (vine.Length > desired)
                    vine.Length = retracting ? Math.Max(desired, vine.Length - BladeOfGrassTuning.RetractSpeed) : desired;
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

            // The vine lives in the WORLD: its direction comes from the cursor as seen from the player, never from the blade. Turning the
            // blade only moves the root; the rest follows through the rope constraints (so that pull fades toward the tip), and the tip
            // heads for its own goal near the cursor.
            Vector2 center = Owner.MountedCenter;
            Vector2 cursorDirection = (cursorWorld - center).SafeNormalize(bladeDir);
            float spread = (index - (vines.Length - 1) * 0.5f) * MathHelper.ToRadians(BladeOfGrassTuning.SpreadDeg) + MathHelper.ToRadians(vine.SpreadJitterDeg);
            Vector2 freeDirection = cursorDirection.RotatedBy(spread);

            bool hasTarget = vine.Target >= 0 && vine.Target < Main.maxNPCs && Valid(Main.npc[vine.Target]);
            if (!hasTarget && vine.Target >= 0)
            {
                vine.Target = -1;
                vine.WrapTicks = 0;
                if (isOwner)
                    Projectile.netUpdate = true;
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
                    vine.LoopAngle = (root - targetNpc.Center).ToRotation(); // start on the side facing the player: the rope arrives, then circles
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

            // How much of the vine is on show: all of it when free; while reaching for an enemy only what it needs to get there and wrap it
            // (the rest is "coiled up"), so the spare rope does not flap around.
            float shownGoal = vine.Length;
            if (hasTarget)
                shownGoal = Math.Min(vine.Length, Math.Max(0f, Vector2.Distance(root, targetNpc.Center) - radius) + arc + BladeOfGrassTuning.WrapSlackPx);
            else
            {
                // A free vine reaches for the cursor itself: it shows only what it needs to put its tip on the cursor (plus a little slack to
                // wave in), the rest stays coiled up, so it never shoots past a cursor that is nearer than the vine is long.
                Vector2 cursorGoal = center + freeDirection * Vector2.Distance(center, cursorWorld);
                shownGoal = Math.Min(vine.Length, Vector2.Distance(root, cursorGoal) + BladeOfGrassTuning.FreeSlackPx);
            }
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
                vine.Init = true;
            }

            // Points 0..bodyCount-1 are the free body; the points from bodyCount to the tip are the coil (only while looping).
            int tail = hasTarget ? Math.Clamp((int)Math.Round(arc / segment), 2, n - 2) : 0;
            int bodyCount = n - 1 - tail;
            // The tip heads for the cursor itself (at the vine's own fan angle), but never farther than the vine reaches.
            Vector2 tipGoal = center + freeDirection * Math.Min(Vector2.Distance(center, cursorWorld), vine.Shown + Vector2.Distance(center, root));

            Vector2 CoilWish(int k)
            {
                float t = (k - bodyCount) / (float)tail;
                float angle = vine.LoopAngle - vine.LoopDir * (1f - t) * wrapSpan;
                float ringRadius = radius * (1f + BladeOfGrassTuning.WrapSpiral * (1f - t));
                return targetNpc.Center + angle.ToRotationVector2() * ringRadius;
            }

            Vector2 bodyEnd = hasTarget ? CoilWish(bodyCount) : tipGoal;
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
                    float wave = (float)Math.Sin(holdTicks * BladeOfGrassTuning.WriggleSpeed * vine.WaveSpeed + along * BladeOfGrassTuning.WriggleWaves * vine.WaveLen + vine.Seed);
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
                    BreakOff(vine, targetNpc, rootDistance);
            }
        }

        // Owner only: the looped section leaves the vine and sticks to the enemy.
        void BreakOff(Vine vine, NPC npc, float rootDistance)
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
            int damage = Math.Max(1, (int)(Projectile.damage * BladeOfGrassTuning.SegmentDamageScale * lengthFactor));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<BladeOfGrassSpineProjectile>(),
                damage, 0f, Projectile.owner, npc.whoAmI, wrapped, Main.rand.NextFloat(MathHelper.TwoPi));

            vine.Length -= wrapped;
            vine.SyncLength = vine.Length;
            BladeOfGrassFx.BreakOff(npc.Center, wrapped / Math.Max(1f, BladeOfGrassTuning.MaxLength));
            vine.Target = -1;
            vine.WrapTicks = 0;
            vine.Cooldown = BladeOfGrassTuning.WrapCooldownTicks;
            Projectile.netUpdate = true;
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
                if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != type)
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
                    if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != type)
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

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || vines == null)
                return;
            for (int i = 0; i < vines.Length; i++)
            {
                Vine vine = vines[i];
                if (vine.Init && vine.P != null && vine.Length > 2f)
                    BladeOfGrassFx.Release(vine.P[vine.P.Length - 1]);
            }
        }

        // ---------------------------------------------------------------- drawing

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || vines == null)
                return false;
            chainAsset ??= ModContent.Request<Texture2D>("Terraria/Images/" + BladeOfGrassTuning.VineChainName);
            headAsset ??= ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.IvyWhip);
            Texture2D chain = chainAsset.Value;
            Texture2D head = headAsset.Value;

            // The vines go behind the sword: all the chains first (as one mesh each, see DrawStrip), then the heads.
            BladeOfGrassVineDraw.BeginMesh();
            for (int i = 0; i < vines.Length; i++)
                DrawVineBody(vines[i], chain);
            BladeOfGrassVineDraw.EndMesh();
            for (int i = 0; i < vines.Length; i++)
                DrawVineHead(vines[i], head);
            return false; // the sword itself is drawn by BladeOfGrassSwordLayer, so the player's front arm is drawn over its handle
        }

        public bool DrawsSword => !retracting || keepHeld; // false once the weapon was swapped away

        // Called by BladeOfGrassSwordLayer (a player draw layer just before the front arm): adds the blade and its charge glow to the player's
        // draw data, so the hand covers the handle instead of the sword floating in front of the arm.
        public void AddSwordDrawData(ref Terraria.DataStructures.PlayerDrawSet drawInfo)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            Vector2 position = Projectile.Center + HeldSwordPose.GripShift(Projectile.rotation, BladeOfGrassTuning.GripOffset, Projectile.scale) - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;
            Color lightColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates());

            // A green glow behind the blade that grows with the charge (drawn around the CENTER of the sprite so it swells outward evenly).
            if (charge > 0.05f)
            {
                Vector2 hiltToCenter = new Vector2(Projectile.spriteDirection > 0 ? texture.Width * 0.5f : -texture.Width * 0.5f, -texture.Height * 0.5f);
                Vector2 centerPosition = position + (hiltToCenter * scale).RotatedBy(rotation);
                drawInfo.DrawDataCache.Add(new Terraria.DataStructures.DrawData(texture, centerPosition, null, BladeOfGrassTuning.GlowColor * (0.5f * charge), rotation, texture.Size() * 0.5f, scale * (1f + 0.15f * charge), effects, 0));
            }
            drawInfo.DrawDataCache.Add(new Terraria.DataStructures.DrawData(texture, position, null, lightColor, rotation, origin, scale, effects, 0));
        }

        void DrawVineBody(Vine vine, Texture2D chain)
        {
            if (!vine.Init || vine.P == null || vine.Length < 2f)
                return;
            BladeOfGrassVineDraw.DrawStrip(chain, vine.P, vine.P.Length, BladeOfGrassTuning.VineScale, BladeOfGrassTuning.VineScale * BladeOfGrassTuning.VineTipScale, 1f, 0f, BladeOfGrassTuning.ChainEndTrimPx);
        }

        void DrawVineHead(Vine vine, Texture2D head)
        {
            if (!vine.Init || vine.P == null || vine.Length < 2f)
                return;
            int n = vine.P.Length;
            // The Ivy Whip head over the end of the vine; its back covers the last links, so there is no gap.
            Vector2 end = vine.P[n - 1];
            float angle = (end - vine.P[Math.Max(0, n - 3)]).ToRotation();
            BladeOfGrassVineDraw.DrawHead(head, end, angle, BladeOfGrassTuning.HeadScale, Lighting.GetColor(end.ToTileCoordinates()));
        }
    }
}