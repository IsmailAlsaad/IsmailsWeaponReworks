using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Volcano
{
    /// <summary>
    /// The thrown Volcano (patch 2): the WHOLE sword (handle and lava blade) flies tip first toward the cursor, straight, and ends where it
    /// first touches something. A tile: it sticks. An enemy: it latches onto the contact point and follows the enemy (the Thorn Chakram's
    /// method). Then a fuse (FuseTicks): the lava is pumped into what it touched, so the handle slowly pushes in and the visible blade gets
    /// shorter, while a pool of lava swells at the contact point; the sword shakes and brightens. The fuse ends with a placeholder pop
    /// (the eruption is patch 3) that also resets the heat. If the enemy dies during the fuse it ends at once.
    /// Geometry: Center is the TIP while flying and the CONTACT POINT once stuck; the sword extends back from it along the flight angle.
    /// ai[0] = phase (0 flying, 1 stuck, 2 spent), ai[1] = index of the enemy it sits on (-1 = a tile or in the air),
    /// ai[2] = the sword's angle (the flight direction, radians). SendExtraAI: where on the enemy it sits (offset from the enemy's center),
    /// how long the visible blade was when it stuck, and the sword's scale.
    /// MULTIPLAYER: the owner's client decides everything (the contact, the target, the end of the fuse, the pop). The phase, the enemy and the
    /// contact travel in ai / SendExtraAI through netUpdate; every machine follows the enemy and draws the melt itself (visual only, each
    /// machine feeds its own metaball layers). The impact hit is the owner's alone. No custom packets.
    /// </summary>
    public class VolcanoThrownProjectile : ModProjectile
    {
        // The vanilla item sprite is the stand-in handle until VolcanoHandle.png exists (the held blade loads the art).
        public override string Texture => "Terraria/Images/Item_" + ItemID.FieryGreatsword;

        enum Phase { Flying = 0, Stuck = 1, Spent = 2 }

        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        float SwordAngle => Projectile.ai[2];
        Vector2 Direction => SwordAngle.ToRotationVector2();
        int StuckNpc => (int)Projectile.ai[1];
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // ---- synced (SendExtraAI / ReceiveExtraAI) ----
        Vector2 contactOffset;      // where it sits on the enemy, from the enemy's center
        float meltLength;           // the visible blade length when it stuck (a full blade, or less when the contact was close to the hand)
        float swordScale = VolcanoTuning.SwordScale;

        // ---- per machine ----
        int ticks;
        int flightTicks;            // owner: how long it has flown
        int stuckTicks;             // ticks since this machine saw the sword stuck (the owner's copy decides the end of the fuse)
        int spentTicks;
        bool impactPending;         // owner: the impact hit on the enemy is still to be dealt
        int impactTicks;
        bool stuckFxPlayed;
        bool popFxPlayed;
        Vector2 shake;              // the sword shakes harder as the pressure builds (drawing only)

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // the contact is tested by hand (TryContact), so the sword stops exactly at the surface
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        // Owner's client, right after it spawns the sword: the scale of the held sword (reforges), so the thrown one is the same size.
        public void Setup(float scale)
        {
            swordScale = scale;
            Projectile.netUpdate = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(contactOffset.X);
            writer.Write(contactOffset.Y);
            writer.Write(meltLength);
            writer.Write(swordScale);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            contactOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            meltLength = reader.ReadSingle();
            swordScale = reader.ReadSingle();
        }

        // ---- geometry ----

        float HandleLen => VolcanoTuning.HandleLength * swordScale;
        float BladeLen => VolcanoTuning.BladeLength * swordScale;
        float SwordLen => HandleLen + BladeLen;

        float FuseProgress => CurrentPhase == Phase.Stuck ? MathHelper.Clamp(stuckTicks / (float)Math.Max(1, VolcanoTuning.FuseTicks), 0f, 1f) : 0f;
        float Melt => (float)Math.Pow(FuseProgress, Math.Max(0.1f, VolcanoTuning.MeltPower));

        // Where the tip touches while it is stuck: on an enemy it moves along with it.
        Vector2 StuckPoint()
        {
            int index = StuckNpc;
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].active)
                return Main.npc[index].Center + contactOffset;
            return Projectile.Center;
        }

        // The joint (where the lava blade starts), the visible blade length and the tip. While it melts the joint travels toward the
        // contact point and on past it by SinkDepth (the handle pushes in) and the blade is only what is left in front of the joint.
        void Pose(out Vector2 joint, out float bladeLength, out Vector2 tip)
        {
            Vector2 direction = Direction;
            if (CurrentPhase == Phase.Stuck)
            {
                tip = StuckPoint();
                float travel = (meltLength + VolcanoTuning.SinkDepth * swordScale) * Melt;
                joint = tip - direction * meltLength + direction * travel;
                bladeLength = Math.Max(0f, meltLength - travel);
            }
            else
            {
                tip = Projectile.Center;
                joint = tip - direction * BladeLen;
                bladeLength = BladeLen;
            }
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (ticks < VolcanoTuning.ThrowArmTicks)
                PoseArm(owner);

            switch (CurrentPhase)
            {
                case Phase.Flying:
                    AiFlying();
                    break;
                case Phase.Stuck:
                    AiStuck();
                    break;
                default:
                    AiSpent();
                    break;
            }

            // The impact hit never came (the enemy was immune, say): do not wait for it forever.
            if (IsOwner && impactPending && ++impactTicks > 3)
                impactPending = false;

            ticks++;
        }

        // Every machine: the throwing arm points along the sword for a few ticks, then the hand is empty.
        void PoseArm(Player owner)
        {
            float arm = owner.gravDir == -1f ? -SwordAngle : SwordAngle;
            if (IsOwner)
                owner.ChangeDir(Math.Cos(SwordAngle) >= 0.0 ? 1 : -1);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, arm - MathHelper.PiOver2);
        }

        void AiFlying()
        {
            Vector2 direction = Direction;
            shake = Vector2.Zero;

            if (ticks == 0 && !Main.dedServ)
                VolcanoFx.Throw(Projectile.Center - direction * SwordLen, direction);

            if (IsOwner)
            {
                // The first tick also tests the whole sword as it was held, so an enemy or a wall right in front of the hand is caught at once.
                Vector2 from = ticks == 0 ? Projectile.Center - direction * SwordLen : Projectile.Center;
                if (TryContact(from, Projectile.Center + Projectile.velocity, direction))
                    return;
                if (++flightTicks > VolcanoTuning.ThrowMaxTicks)
                {
                    Pop(); // nothing in reach: it bursts in the air
                    return;
                }
            }

            if (!Main.dedServ)
            {
                // Fed where the sword will be once it has moved this tick, so the lava stays on the handle (the sprite is drawn after the move).
                Vector2 tipNext = Projectile.Center + Projectile.velocity;
                Vector2 joint = tipNext - direction * BladeLen;
                VolcanoBlade.Feed(joint, direction, BladeLen, swordScale, 1f, ticks / 60f, Projectile.owner, Vector2.Zero);
                FlightDrips(joint, direction);
                Lighting.AddLight(tipNext - direction * (BladeLen * 0.5f), 1f, 0.45f, 0.1f);
            }
        }

        // Owner's client. Tests the path of the tip between 'from' and 'to': the first solid tile and the nearest enemy on the way, whichever
        // comes first. Returns true when the sword stuck (it is then at the contact point and its phase has changed).
        bool TryContact(Vector2 from, Vector2 to, Vector2 direction)
        {
            Vector2 delta = to - from;
            float length = delta.Length();
            if (length < 0.5f)
                return false;
            Vector2 unit = delta / length;
            Vector2 half = new Vector2(1f, 1f);

            // The first solid tile: coarse steps, then a few halvings to find the surface.
            float tileAt = float.MaxValue;
            float previous = 0f;
            for (float at = 0f; ; at += 4f)
            {
                float distance = Math.Min(at, length);
                if (Collision.SolidCollision(from + unit * distance - half, 2, 2))
                {
                    float low = previous;
                    float high = distance;
                    for (int i = 0; i < 3; i++)
                    {
                        float middle = (low + high) * 0.5f;
                        if (Collision.SolidCollision(from + unit * middle - half, 2, 2))
                            high = middle;
                        else
                            low = middle;
                    }
                    tileAt = low;
                    break;
                }
                previous = distance;
                if (distance >= length)
                    break;
            }

            // The nearest enemy on the way (only if it comes before the tile).
            int bestNpc = -1;
            float bestAt = tileAt;
            Vector2 bestPoint = Vector2.Zero;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;
                if (!npc.CanBeChasedBy() && npc.type != NPCID.TargetDummy)
                    continue;
                Rectangle box = npc.Hitbox;
                float point = 0f;
                if (!Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), from, to, VolcanoTuning.ThrowHitWidth * swordScale, ref point))
                    continue;
                Vector2 entry = Contact.EntryPoint(from, to, box);
                float at = Vector2.Dot(entry - from, unit);
                if (at < bestAt)
                {
                    bestAt = at;
                    bestNpc = i;
                    bestPoint = entry;
                }
            }

            if (bestNpc >= 0)
            {
                Stick(bestPoint, bestNpc, direction);
                return true;
            }
            if (tileAt < float.MaxValue)
            {
                Stick(from + unit * tileAt, -1, direction);
                return true;
            }
            return false;
        }

        // Owner's client: the sword stops at 'point'. The new phase, the enemy and the contact are synced through netUpdate; the impact hit
        // (on an enemy) is dealt by the normal hit pipeline this tick, because the sword's box now sits on the contact point.
        void Stick(Vector2 point, int npcIndex, Vector2 direction)
        {
            Vector2 jointNow = Projectile.Center - direction * BladeLen;
            meltLength = MathHelper.Clamp(Vector2.Dot(point - jointNow, direction), 0f, BladeLen);
            contactOffset = npcIndex >= 0 ? point - Main.npc[npcIndex].Center : Vector2.Zero;
            Projectile.ai[0] = (float)Phase.Stuck;
            Projectile.ai[1] = npcIndex;
            Projectile.Center = point;
            Projectile.velocity = Vector2.Zero;
            impactPending = npcIndex >= 0;
            impactTicks = 0;
            Projectile.netUpdate = true;
        }

        void AiStuck()
        {
            Vector2 direction = Direction;
            Projectile.velocity = Vector2.Zero;

            int index = StuckNpc;
            bool onEnemy = index >= 0 && index < Main.maxNPCs;
            if (onEnemy)
            {
                NPC npc = Main.npc[index];
                if (npc.active)
                    Projectile.Center = npc.Center + contactOffset;
                else if (IsOwner)
                {
                    Pop(); // the enemy died: the fuse ends at once
                    return;
                }
            }
            Vector2 point = Projectile.Center;

            if (!stuckFxPlayed)
            {
                stuckFxPlayed = true;
                if (!Main.dedServ)
                    VolcanoFx.Stick(point, direction, onEnemy);
            }

            stuckTicks++;
            if (!Main.dedServ)
                MeltVisuals(point, direction);

            if (IsOwner && stuckTicks >= VolcanoTuning.FuseTicks)
                Pop();
        }

        // The pumping: the blade (shorter every tick, thicker and brighter) feeds the lava layer, a pool swells at the contact point and
        // merges with it, drips fall from the pool and the sword shakes.
        void MeltVisuals(Vector2 point, Vector2 direction)
        {
            float s = FuseProgress;
            float pressure = s * s;
            shake = Main.rand.NextVector2Circular(1f, 1f) * VolcanoTuning.FuseShake * swordScale * pressure;

            Pose(out Vector2 joint, out float bladeLength, out _);
            joint += shake;
            Vector2 contact = point + shake;
            Vector2 normal = new Vector2(-direction.Y, direction.X);
            MetaballLayer body = MetaballLayers.Of<VolcanoBlobStyle>();
            MetaballLayer drips = MetaballLayers.Of<VolcanoDripStyle>();

            if (bladeLength > 2f)
                VolcanoBlade.Feed(joint, direction, bladeLength, swordScale, 1f, ticks / 60f, Projectile.owner, Vector2.Zero,
                    1f + VolcanoTuning.FuseSwell * pressure, VolcanoTuning.FuseBrighten * pressure);

            // the pool of lava at the contact point, spread sideways along the surface
            float poolRadius = MathHelper.Lerp(VolcanoTuning.PoolRadiusStart, VolcanoTuning.PoolRadiusEnd, Melt) * swordScale;
            float poolHeat = MathHelper.Clamp(0.6f + 0.4f * s, 0f, 1f);
            for (int i = -1; i <= 1; i++)
            {
                float wobble = (float)Math.Sin(ticks * 0.35f + i * 2f) * 0.12f;
                float radius = poolRadius * (i == 0 ? 1f : 0.7f) * (1f + wobble);
                body.AddBlob(contact + normal * (i * poolRadius * 0.9f) - direction * (poolRadius * 0.25f), radius, poolHeat);
            }

            // lava drips off the pool, more and more of them
            float rate = VolcanoTuning.FuseDripRate * pressure + 0.05f;
            int count = (int)rate + (Main.rand.NextFloat() < rate - (int)rate ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                Vector2 position = contact + normal * Main.rand.NextFloat(-1f, 1f) * poolRadius;
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-0.6f, 0.4f));
                drips.Spawn(position, velocity, Main.rand.Next(30, 60), Main.rand.NextFloat(VolcanoTuning.DripSizeMin, VolcanoTuning.DripSizeMax) * swordScale);
            }

            if (Main.rand.NextFloat() < 0.2f + 0.5f * s)
                VolcanoFx.Ember(contact, Vector2.Zero);
            if (stuckTicks % Math.Max(1, VolcanoTuning.SizzleTicks) == 1)
                VolcanoFx.PlaySizzle(contact, s);
            float light = 0.5f + 1.5f * pressure;
            Lighting.AddLight(contact, 1f * light, 0.45f * light, 0.1f * light);
        }

        // Falling lava the flying sword leaves behind.
        void FlightDrips(Vector2 joint, Vector2 direction)
        {
            MetaballLayer drips = MetaballLayers.Of<VolcanoDripStyle>();
            float rate = VolcanoTuning.FlightDripRate;
            int count = (int)rate + (Main.rand.NextFloat() < rate - (int)rate ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                Vector2 position = joint + direction * (BladeLen * Main.rand.NextFloat(0.1f, 1f));
                Vector2 velocity = Main.rand.NextVector2Circular(0.6f, 0.6f) - direction * 0.3f;
                drips.Spawn(position, velocity, Main.rand.Next(30, 60), Main.rand.NextFloat(VolcanoTuning.DripSizeMin, VolcanoTuning.DripSizeMax) * swordScale);
            }
        }

        // Owner's client: the end of the sword (patch 3 turns this into the eruption). The sword is flagged Spent (synced) instead of being
        // killed on the spot, so every machine plays the pop before it removes its own copy. The heat is reset here.
        void Pop()
        {
            if (CurrentPhase == Phase.Spent)
                return;
            Projectile.ai[0] = (float)Phase.Spent;
            Projectile.velocity = Vector2.Zero;
            impactPending = false;
            VolcanoPlayer.ResetHeat(Owner);
            Projectile.netUpdate = true;
        }

        void AiSpent()
        {
            Projectile.velocity = Vector2.Zero;
            if (!popFxPlayed)
            {
                popFxPlayed = true;
                if (!Main.dedServ)
                    VolcanoFx.Pop(Projectile.Center);
            }
            if (++spentTicks > VolcanoTuning.SpentLingerTicks)
                Projectile.Kill(); // every machine removes its own copy
        }

        // ---- hits: only the impact on an enemy ----

        public override bool? CanDamage()
        {
            return impactPending ? (bool?)null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = Direction.X >= 0f ? 1 : -1;
            modifiers.SourceDamage *= VolcanoTuning.ThrowDamageMultiplier;
            modifiers.Knockback *= VolcanoTuning.ThrowKnockbackMultiplier;
        }

        // Owner's client only.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            impactPending = false;
            target.AddBuff(BuffID.OnFire, VolcanoTuning.OnFireTicks + VolcanoTuning.OnFireHeatBonusTicks);
            VolcanoFx.HitSpark(Projectile.Center, 1f);
            CameraShake.Punch(Projectile.Center, VolcanoTuning.HitShake * 2f, 10);
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            if (CurrentPhase == Phase.Spent)
                return false;

            // The lava blade (and the pool and flames) in front of enemies: the layer asks to be drawn from here, once per frame.
            MetaballLayers.Of<VolcanoBlobStyle>().DrawInBatch();

            Vector2 direction = Direction;
            Pose(out Vector2 joint, out _, out _);
            joint += shake;
            Vector2 hand = joint - direction * HandleLen;
            int spriteDirection = direction.X >= 0f ? 1 : -1;
            float pressure = FuseProgress * FuseProgress;
            VolcanoBlade.DrawHandle(VolcanoBladeProjectile.HandleArt, TextureAssets.Projectile[Type].Value, hand, joint, SwordAngle, spriteDirection, swordScale, lightColor, 1f, pressure);
            return false;
        }
    }
}