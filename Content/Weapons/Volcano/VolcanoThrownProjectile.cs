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
    /// shorter, while a pool of lava swells at the contact point; the sword shakes and brightens. The fuse ends with the ERUPTION (patch 3):
    /// a shockwave, a fan of lava drops (VolcanoLavaDropProjectile) and the handle, thrown out of the contact point, spinning and bouncing
    /// home to the player (phase Returning). The heat is reset by the eruption, so the caught handle becomes a cold held sword again.
    /// If the enemy dies during the fuse it ends at once; a sword that finds nothing in reach erupts in the air.
    /// Geometry: Center is the TIP while flying and the CONTACT POINT once stuck; the sword extends back from it along the flight angle.
    /// ai[0] = phase (0 flying, 1 stuck, 2 returning), ai[1] = index of the enemy it sits on (-1 = a tile, -2 = it burst in the air),
    /// ai[2] = the sword's angle (the flight direction, radians). SendExtraAI: where on the enemy it sits (offset from the enemy's center),
    /// how long the visible blade was when it stuck, and the sword's scale.
    /// MULTIPLAYER: the owner's client decides everything (the contact, the target, the end of the fuse, the eruption and its drops). The phase, the enemy and the
    /// contact travel in ai / SendExtraAI through netUpdate; every machine follows the enemy and draws the melt itself (visual only, each
    /// machine feeds its own metaball layers). The impact hit is the owner's alone. The handle's way home is flown by every machine from the
    /// same rules (kick, arc, homing on the owner), and every machine removes its own copy when it reaches the player. No custom packets.
    /// </summary>
    public class VolcanoThrownProjectile : ModProjectile
    {
        // The vanilla item sprite is the stand-in handle until VolcanoHandle.png exists (the held blade loads the art).
        public override string Texture => "Terraria/Images/Item_" + ItemID.FieryGreatsword;

        enum Phase { Flying = 0, Stuck = 1, Returning = 2 }

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
        int returnTicks;
        bool impactPending;         // owner: the impact hit on the enemy is still to be dealt
        int impactTicks;
        bool stuckFxPlayed;
        bool returnStarted;         // the eruption was played and the handle is on its way home (this machine)
        float handleAngle;          // the handle's angle while it spins home
        float spinSpeed;            // radians per tick
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
        float HandleMid => VolcanoTuning.HandleSpinOffset * swordScale;   // the handle spins around the point this far back from the joint

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
                    AiReturn();
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
                    Erupt(); // nothing in reach: it erupts in the air
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
                    Erupt(); // the enemy died: the fuse ends at once
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
                Erupt();
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

        // Owner's client: the end of the fuse, or the air burst when nothing was in reach. The stored lava erupts: a shockwave that hurts
        // everything around (VolcanoBlastProjectile, silent: every machine plays the eruption itself from the phase), a fan of lava drops
        // that rain down and linger (VolcanoLavaDropProjectile) and the handle is thrown out of the contact point and bounces home. The heat
        // is reset here, so the sword comes back cold. The phase is synced; every machine then plays the eruption and flies the handle.
        void Erupt()
        {
            if (CurrentPhase == Phase.Returning)
                return;
            bool inAir = CurrentPhase == Phase.Flying;
            Vector2 point = StuckPoint();
            Vector2 axis = (new Vector2(0f, -1f) - Direction * 0.5f).SafeNormalize(-Vector2.UnitY); // up, leaning away from the way the sword came in

            int blastDamage = (int)(Projectile.damage * VolcanoTuning.EruptionDamageMultiplier);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), point, Vector2.Zero, ModContent.ProjectileType<VolcanoBlastProjectile>(), blastDamage, Projectile.knockBack,
                Projectile.owner, VolcanoTuning.EruptionSize / (float)VolcanoTuning.BlastSize, 1f);
            VolcanoLavaDropProjectile.Rain(Projectile, point, axis);

            if (inAir)
                Projectile.ai[1] = -2f;
            Projectile.ai[0] = (float)Phase.Returning;
            Projectile.velocity = Vector2.Zero;
            impactPending = false;
            VolcanoPlayer.ResetHeat(Owner);
            Projectile.netUpdate = true;
        }

        // Every machine. The handle is thrown out of the contact point, spins, arcs freely for a moment (the bounce) and then homes in on the
        // player, faster and faster. Each machine flies it from the same rules and removes its own copy when it reaches the player.
        void AiReturn()
        {
            if (!returnStarted)
                StartReturn();

            returnTicks++;
            handleAngle += spinSpeed;

            if (returnTicks <= VolcanoTuning.BounceArcTicks)
            {
                Projectile.velocity.Y += VolcanoTuning.BounceGravity;
                Projectile.velocity *= VolcanoTuning.BounceDrag;
            }
            else
            {
                spinSpeed *= 0.96f;
                Vector2 toHand = Owner.MountedCenter - Projectile.Center;
                Vector2 home = toHand.SafeNormalize(Vector2.UnitY);
                float speed = Math.Min(VolcanoTuning.ReturnSpeedMax, Projectile.velocity.Length() + VolcanoTuning.ReturnAccel);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, home * speed, VolcanoTuning.ReturnTurn).SafeNormalize(home) * speed;
                if (toHand.Length() < VolcanoTuning.CatchDistance)
                {
                    Catch();
                    return;
                }
            }

            if (returnTicks > VolcanoTuning.ReturnMaxTicks)
            {
                Catch();
                return;
            }

            if (!Main.dedServ)
                ReturnVisuals();
        }

        // Every machine, once: plays the eruption at the contact point and puts the handle where it was when the lava ran out (its joint has
        // sunk SinkDepth past the contact point; in the air it is just the held sword's handle), then kicks it back out the way it came in.
        void StartReturn()
        {
            returnStarted = true;
            Vector2 direction = Direction;
            Vector2 point = StuckPoint();
            Vector2 joint = StuckNpc == -2 ? point - direction * BladeLen : point + direction * (VolcanoTuning.SinkDepth * swordScale);
            handleAngle = SwordAngle;
            Projectile.Center = joint - direction * HandleMid;
            Projectile.velocity = -direction * VolcanoTuning.BounceKickSpeed + new Vector2(0f, -VolcanoTuning.BounceKickUp);
            spinSpeed = MathHelper.ToRadians(VolcanoTuning.BounceSpinDeg) * (direction.X >= 0f ? -1f : 1f);
            if (!Main.dedServ)
                VolcanoFx.Eruption(point);
        }

        // The handle reached the player (or ran out of time): sparks and a clank, and every machine removes its own copy. The owner's
        // VolcanoPlayer then forms the held blade again (cold: the heat was reset by the eruption).
        void Catch()
        {
            if (!Main.dedServ)
                VolcanoFx.Catch(Owner.MountedCenter);
            Projectile.Kill();
        }

        // The handle is white-hot after the eruption and drips lava and throws embers while it cools down.
        void ReturnVisuals()
        {
            float hot = 1f - MathHelper.Clamp(returnTicks / (float)Math.Max(1, VolcanoTuning.ReturnCoolTicks), 0f, 1f);
            if (hot <= 0f)
                return;
            if (Main.rand.NextFloat() < 0.7f * hot + 0.05f)
                MetaballLayers.Of<VolcanoDripStyle>().Spawn(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextVector2Circular(0.7f, 0.7f),
                    Main.rand.Next(24, 46), Main.rand.NextFloat(VolcanoTuning.DripSizeMin, VolcanoTuning.DripSizeMax) * swordScale);
            if (Main.rand.NextFloat() < 0.35f * hot)
                VolcanoFx.Ember(Projectile.Center, Projectile.velocity);
            Lighting.AddLight(Projectile.Center, 1f * hot, 0.45f * hot, 0.1f * hot);
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
            // The lava (blade, pool, flames, drips, droplets) in front of enemies: the layers ask to be drawn from here, once per frame.
            VolcanoFx.DrawLayers();

            if (CurrentPhase == Phase.Returning)
            {
                // The handle on its way home: it spins around its middle (Center); the original throw direction fixes which side the sprite faces.
                if (returnStarted)
                {
                    Vector2 spin = handleAngle.ToRotationVector2();
                    Vector2 spinJoint = Projectile.Center + spin * HandleMid;
                    float cool = 1f - MathHelper.Clamp(returnTicks / (float)Math.Max(1, VolcanoTuning.ReturnCoolTicks), 0f, 1f);
                    VolcanoBlade.DrawHandle(VolcanoBladeProjectile.HandleArt, TextureAssets.Projectile[Type].Value, spinJoint - spin * HandleLen, spinJoint, handleAngle,
                        Direction.X >= 0f ? 1 : -1, swordScale, lightColor, cool, cool);
                }
                return false;
            }

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