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
        }

        Player Owner => Main.player[Projectile.owner];

        Vine[] vines;
        int holdTicks;
        float charge;           // 0..1, how shaken the blade is
        float aimShown;         // aim in the player's own frame
        float syncAim;          // other machines: the owner's latest aim
        bool gotAim;
        float cursorDist;
        int syncTimer;
        int searchTimer;
        Vector2 hand;
        Vector2 bladeDir;
        int[] targetCounts;
        Asset<Texture2D> chainAsset;
        Asset<Texture2D> whipAsset;
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
            // The owner's client ends it when the button is let go, the item is swapped, or the player is crowd-controlled; the kill is synced.
            if (isOwner && (!owner.channel || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.BladeofGrass))
            {
                Projectile.Kill();
                return;
            }

            EnsureVines();
            Projectile.timeLeft = 2;
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            Projectile.scale = BladeOfGrassTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);
            holdTicks++;
            charge = MathHelper.Clamp(holdTicks / (float)Math.Max(1, BladeOfGrassTuning.ChargeTicks), 0f, 1f);

            Vector2 cursorWorld;
            if (isOwner)
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

            ApplyPose(owner);

            if (isOwner && ++searchTimer >= Math.Max(1, BladeOfGrassTuning.RetargetInterval))
            {
                searchTimer = 0;
                AssignTargets(cursorWorld);
            }

            for (int i = 0; i < vines.Length; i++)
                StepVine(i, vines[i], cursorWorld, isOwner);

            // Leaves fly off the shaking blade.
            if (charge > 0.2f && Main.rand.NextFloat() < charge * 6f / Math.Max(1, BladeOfGrassTuning.ChargeDustInterval) * 0.2f)
                BladeOfGrassFx.ChargeLeaf(hand + bladeDir * (BladeOfGrassTuning.BladeLength * Projectile.scale * Main.rand.NextFloat()), charge);
            Lighting.AddLight(hand + bladeDir * (BladeOfGrassTuning.BladeLength * 0.5f), 0.1f + 0.2f * charge, 0.3f + 0.3f * charge, 0.08f);
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

            // 1) Keep the targets that are still good (a vine in the middle of a loop always keeps its enemy while it is valid).
            for (int i = 0; i < vines.Length; i++)
            {
                Vine vine = vines[i];
                if (vine.Target < 0)
                    continue;
                NPC npc = Main.npc[vine.Target];
                bool keep = Valid(npc);
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
                    if (!Valid(npc))
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

        // ---------------------------------------------------------------- the rope

        void StepVine(int index, Vine vine, Vector2 cursorWorld, bool isOwner)
        {
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
                float start = BladeOfGrassTuning.StartTicks + index * BladeOfGrassTuning.StaggerTicks;
                float desired = holdTicks < start ? 0f
                    : MathHelper.Lerp(BladeOfGrassTuning.MinLength, BladeOfGrassTuning.MaxLength, MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((holdTicks - start) / (float)Math.Max(1, BladeOfGrassTuning.GrowTicks), 0f, 1f)));
                if (vine.Length < desired)
                    vine.Length = Math.Min(desired, vine.Length + BladeOfGrassTuning.GrowSpeed);
                else if (vine.Length > desired)
                    vine.Length = desired;
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
                return;
            }

            float segment = vine.Length / (n - 1);
            Vector2 cursorDirection = (cursorWorld - root).SafeNormalize(bladeDir);
            float spread = (index - (vines.Length - 1) * 0.5f) * MathHelper.ToRadians(BladeOfGrassTuning.SpreadDeg);
            Vector2 freeDirection = cursorDirection.RotatedBy(spread);

            if (!vine.Init)
            {
                for (int k = 0; k < n; k++)
                {
                    vine.P[k] = root + freeDirection * (segment * k);
                    vine.Old[k] = vine.P[k];
                }
                vine.Init = true;
            }

            // Where the tip wants to be: a point on the circle around the target, or straight out toward the cursor.
            bool hasTarget = vine.Target >= 0 && vine.Target < Main.maxNPCs && Valid(Main.npc[vine.Target]);
            if (!hasTarget && vine.Target >= 0)
            {
                vine.Target = -1;
                vine.WrapTicks = 0;
                if (isOwner)
                    Projectile.netUpdate = true;
            }
            Vector2 goal;
            NPC targetNpc = null;
            if (hasTarget)
            {
                targetNpc = Main.npc[vine.Target];
                if (vine.LastTarget != vine.Target)
                {
                    vine.LoopAngle = (vine.P[n - 1] - targetNpc.Center).ToRotation(); // start circling from where the tip is
                    if (!isOwner)
                        vine.LoopDir = Main.rand.NextBool() ? 1f : -1f;
                    if (Vector2.Distance(vine.P[n - 1], targetNpc.Center) < 4000f)
                        BladeOfGrassFx.Lock(targetNpc.Center);
                }
                float progress = MathHelper.Clamp(vine.WrapTicks / (float)Math.Max(1, BladeOfGrassTuning.LoopTicks), 0f, 1f);
                float radius = (Math.Max(targetNpc.width, targetNpc.height) * BladeOfGrassTuning.WrapRadiusScale + BladeOfGrassTuning.WrapRadiusPad) * (1f - BladeOfGrassTuning.WrapTighten * progress);
                vine.LoopAngle += BladeOfGrassTuning.LoopSpeed * vine.LoopDir;
                goal = targetNpc.Center + vine.LoopAngle.ToRotationVector2() * radius;
            }
            else
                goal = root + freeDirection * vine.Length;
            vine.LastTarget = hasTarget ? vine.Target : -1;

            Vector2 toGoal = goal - root;
            float goalDistance = toGoal.Length();
            Vector2 direction = goalDistance > 0.01f ? toGoal / goalDistance : freeDirection;
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
            float reach = Math.Min(vine.Length, goalDistance);
            float wiggle = BladeOfGrassTuning.WrigglePx * (hasTarget ? BladeOfGrassTuning.WriggleTargetFactor : 1f) * (1f + BladeOfGrassTuning.WriggleChargeBoost * charge);

            for (int k = 1; k < n; k++)
            {
                float along = k / (float)(n - 1);
                Vector2 velocity = (vine.P[k] - vine.Old[k]) * BladeOfGrassTuning.Damping;
                if (velocity.Length() > BladeOfGrassTuning.MaxPointSpeed)
                    velocity = velocity.SafeNormalize(Vector2.Zero) * BladeOfGrassTuning.MaxPointSpeed;
                velocity.Y += BladeOfGrassTuning.Gravity;
                vine.Old[k] = vine.P[k];
                Vector2 point = vine.P[k] + velocity;

                float envelope = 4f * along * (1f - along) + 0.25f * along; // the wave is biggest in the middle, a little lash at the tip
                float wave = (float)Math.Sin(holdTicks * BladeOfGrassTuning.WriggleSpeed + along * BladeOfGrassTuning.WriggleWaves + vine.Seed);
                Vector2 wish = root + direction * (reach * along) + perpendicular * (wave * wiggle * envelope);
                point += (wish - point) * (BladeOfGrassTuning.Stiffness * (0.5f + 0.5f * along));
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
            if (wrapped < 8f)
            {
                vine.WrapTicks = 0; // the vine is too short to give anything: keep circling
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
            whipAsset ??= ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + BladeOfGrassTuning.SpineProjectileId);
            Texture2D chain = chainAsset.Value;
            Texture2D whip = whipAsset.Value;

            // The vines go behind the sword.
            for (int i = 0; i < vines.Length; i++)
                DrawVine(vines[i], chain, whip);

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, Projectile.spriteDirection, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            Vector2 position = Projectile.Center + HeldSwordPose.GripShift(Projectile.rotation, BladeOfGrassTuning.GripOffset, Projectile.scale) - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = Projectile.scale;

            // A green glow behind the blade that grows with the charge (drawn around the CENTER of the sprite so it swells outward evenly).
            if (charge > 0.05f)
            {
                Vector2 hiltToCenter = new Vector2(Projectile.spriteDirection > 0 ? texture.Width * 0.5f : -texture.Width * 0.5f, -texture.Height * 0.5f);
                Vector2 centerPosition = position + (hiltToCenter * scale).RotatedBy(rotation);
                Main.EntitySpriteDraw(texture, centerPosition, null, BladeOfGrassTuning.GlowColor * (0.5f * charge), rotation, texture.Size() * 0.5f, scale * (1f + 0.15f * charge), effects, 0);
            }
            Main.EntitySpriteDraw(texture, position, null, lightColor, rotation, origin, scale, effects, 0);
            return false;
        }

        void DrawVine(Vine vine, Texture2D chain, Texture2D whip)
        {
            if (!vine.Init || vine.P == null || vine.Length < 2f)
                return;
            int n = vine.P.Length;
            BladeOfGrassVineDraw.Resample(vine.P, n, chain.Height * BladeOfGrassTuning.ChainSpacing * BladeOfGrassTuning.VineScale, samplePositions, sampleAngles);
            int count = samplePositions.Count;
            Vector2 chainOrigin = chain.Size() * 0.5f;
            for (int s = 0; s < count; s++)
            {
                float along = count <= 1 ? 0f : s / (float)(count - 1);
                float scale = BladeOfGrassTuning.VineScale * MathHelper.Lerp(1f, BladeOfGrassTuning.VineTipScale, along);
                Vector2 world = samplePositions[s];
                Color color = Lighting.GetColor(world.ToTileCoordinates());
                Main.EntitySpriteDraw(chain, world - Main.screenPosition, null, color, sampleAngles[s] - MathHelper.PiOver2, chainOrigin, scale, SpriteEffects.None, 0);
            }

            // The stinger: the tip frame of the Snapthorn whip at the end of the vine, pointing along its last stretch.
            int frames = Math.Max(1, BladeOfGrassTuning.SpineSheetFrames);
            int frameHeight = whip.Height / frames;
            Rectangle tip = new Rectangle(0, (frames - 1) * frameHeight, whip.Width, frameHeight);
            Vector2 end = vine.P[n - 1];
            float angle = (end - vine.P[n - 2]).ToRotation();
            Color tipColor = Lighting.GetColor(end.ToTileCoordinates());
            Main.EntitySpriteDraw(whip, end - Main.screenPosition, tip, tipColor, angle - MathHelper.PiOver2, new Vector2(tip.Width * 0.5f, 0f), BladeOfGrassTuning.StingerScale, SpriteEffects.None, 0);
        }
    }
}