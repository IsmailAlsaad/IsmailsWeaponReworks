using IsmailsWeaponReworks.Common;
using IsmailsWeaponReworks.Common.Flail;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Meatball
{
    /// <summary>
    /// The ball that tears loose from the Meatball at the apex of the tier 3 special. Three phases:
    /// FLYING: it flies straight on in the direction of the throw (BallSpeed) for BallRangePx, then falls with gravity; the first enemy or solid
    ///   tile on its path stops it (a tile in front of an enemy wins). Still flying after BallMaxLifeTicks it goes off where it is.
    /// ATTACHED: it sticks to that enemy (and follows it) or tile, the enemy takes an impact hit, and a fuse of FuseTicks runs: the ball swells
    ///   and pulses faster and faster (a heartbeat thump per pulse). When the fuse ends, or the enemy it sits on is gone, or a newer ball
    ///   pushes it out (MakeRoom), it goes off.
    /// SPENT: it exploded (a blood explosion: every enemy in BlastRadius is hit once and bleeds); every machine plays the explosion on the
    ///   first tick it sees this phase and removes its own copy SpentLingerTicks later.
    /// MULTIPLAYER: the owner's client decides everything (contact, target, fuse end, eviction, all damage and the Bleeding buff) and flips
    /// the phase through ai[0] with netUpdate. ai[1] / ai[2] while attached (the Thorn Chakram's encoding): ai[1] = enemy index plus
    /// (distance of the contact point from the enemy's center) / 1024, or -1 for a tile; ai[2] = angle of the contact point seen from the
    /// enemy's center. The flight is the same straight line (then the same fall) on every machine; the owner syncs the velocity every few ticks
    /// while it falls. The pulse, the swell and the sounds are pure functions of the ticks each machine has run since the phase began. The
    /// projectile is not friendly: it never damages through a vanilla collision, the owner's code strikes the enemies (SimpleStrikeNPC and
    /// AddBuff sync themselves). No custom packets.
    /// </summary>
    public class MeatballHeadProjectile : ModProjectile
    {
        // Vanilla art until a MeatballHeadProjectile.png exists next to this file.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.TheMeatball;

        enum Phase { Flying = 0, Attached = 1, Spent = 2 }

        static MeatballTuning Tune => MeatballTuning.Instance;
        Phase CurrentPhase => (Phase)(int)Projectile.ai[0];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        // Per-machine state: counters and visuals, never synced.
        int lastPhase = -1;
        int age;
        int fuseTicks;
        int spentTicks;
        float beatPhase;
        int beatCount;
        float beatFlash;
        PixelTrail trail;
        Asset<Texture2D> art;
        bool artTried;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;   // all damage is dealt by the owner's code, never by a vanilla collision
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000;   // ended by the AI, never by this countdown
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }

            Phase phase = CurrentPhase;
            if ((int)phase != lastPhase)
            {
                EnterPhase(phase);
                lastPhase = (int)phase;
            }
            age++;

            switch (phase)
            {
                case Phase.Flying:
                    UpdateFlying();
                    break;
                case Phase.Attached:
                    UpdateAttached();
                    break;
                default:
                    spentTicks++;
                    if (spentTicks >= Tune.SpentLingerTicks)
                        Projectile.Kill();
                    break;
            }
            if (!Projectile.active)
                return;

            beatFlash = Math.Max(0f, beatFlash - 0.1f);
            trail?.Tick();
            if (age > Tune.BallMaxLifeTicks + Tune.FuseTicks + 600)
                Projectile.Kill();   // safety: a copy that never saw the end
        }

        // Every machine, once per phase.
        void EnterPhase(Phase phase)
        {
            switch (phase)
            {
                case Phase.Flying:
                    if (!Main.dedServ)
                    {
                        trail = new PixelTrail(new FlailTrailStyle(Tune, 3), IsOwner);
                        MeatballFx.Instance.Detach(Projectile.Center, Projectile.velocity);
                    }
                    break;
                case Phase.Attached:
                    fuseTicks = 0;
                    beatPhase = 0f;
                    beatCount = 0;
                    Projectile.velocity = Vector2.Zero;
                    DetachedTrails.Hand(trail);
                    trail = null;
                    MeatballFx.Instance.Attach(Projectile.Center);
                    break;
                case Phase.Spent:
                    spentTicks = 0;
                    Projectile.velocity = Vector2.Zero;
                    DetachedTrails.Hand(trail);
                    trail = null;
                    MeatballFx.Instance.Explode(Projectile.Center, Tune.BlastRadius);
                    break;
            }
        }

        // ---- flying ----

        void UpdateFlying()
        {
            int flightTicks = Math.Max(1, (int)(Tune.BallRangePx / Math.Max(1f, Tune.BallSpeed)));
            if (age > flightTicks)
            {
                // The range is used up: the ball falls. The owner keeps the other machines' copies in step while it does.
                Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + Tune.BallGravity, Tune.BallMaxFall);
                if (IsOwner && age % 5 == 0)
                    Projectile.netUpdate = true;
            }

            // The move itself is vanilla's (position += velocity right after this AI), so 'to' is where the ball will be this tick.
            Vector2 from = Projectile.Center;
            Vector2 to = from + Projectile.velocity;
            if (IsOwner && TryContact(from, to))
                return;

            Projectile.rotation += 0.45f;
            trail?.Add(to);
            MeatballFx.Instance.BallFlight(to, Projectile.velocity);
            Lighting.AddLight(to, 0.9f, 0.1f, 0.12f);

            if (IsOwner && age > Tune.BallMaxLifeTicks)
                Detonate();
        }

        // Owner only: the nearest thing on this tick's path stops the ball: an enemy, or a solid tile (a tile in front of an enemy wins).
        bool TryContact(Vector2 from, Vector2 to)
        {
            float enemyDistance = float.MaxValue;
            NPC enemy = null;
            Vector2 enemyPoint = to;
            float width = Tune.BallHitRadius * 2f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.CanBeChasedBy())
                    continue;
                float unused = 0f;
                if (!Collision.CheckAABBvLineCollision(npc.Hitbox.TopLeft(), npc.Hitbox.Size(), from, to, width, ref unused))
                    continue;
                Vector2 point = Contact.EntryPoint(from, to, npc.Hitbox);
                float distance = Vector2.DistanceSquared(from, point);
                if (distance < enemyDistance)
                {
                    enemyDistance = distance;
                    enemy = npc;
                    enemyPoint = point;
                }
            }

            // Tiles: steps of 4 px along the path; the ball stops on the last free step.
            int steps = Math.Max(1, (int)Math.Ceiling(Vector2.Distance(from, to) / 4f));
            Vector2 previous = from;
            for (int s = 1; s <= steps; s++)
            {
                Vector2 point = Vector2.Lerp(from, to, s / (float)steps);
                if (Collision.SolidCollision(point - new Vector2(4f), 8, 8))
                {
                    if (enemy == null || Vector2.DistanceSquared(from, previous) <= enemyDistance)
                    {
                        AttachToTile(previous);
                        return true;
                    }
                    break;
                }
                previous = point;
            }

            if (enemy == null)
                return false;
            AttachToNpc(enemy, enemyPoint);
            return true;
        }

        // Owner only. The new phase is flagged (synced) and every machine then plays the splat and follows the enemy itself.
        void AttachToNpc(NPC npc, Vector2 contact)
        {
            int direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Vector2 offset = contact - npc.Center;
            float length = Math.Min(offset.Length(), 1000f);
            Projectile.Center = contact;
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[0] = (float)Phase.Attached;
            Projectile.ai[1] = npc.whoAmI + length / 1024f;
            Projectile.ai[2] = length > 0.01f ? offset.ToRotation() : 0f;
            Projectile.netUpdate = true;

            // The impact hit (SimpleStrikeNPC syncs itself).
            int damage = Math.Max(1, (int)(Projectile.damage * Tune.ImpactDamageMultiplier));
            npc.SimpleStrikeNPC(damage, direction, false, Projectile.knockBack * Tune.ImpactKnockbackMultiplier, DamageClass.Melee);
        }

        void AttachToTile(Vector2 contact)
        {
            Projectile.Center = contact;
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[0] = (float)Phase.Attached;
            Projectile.ai[1] = -1f;
            Projectile.ai[2] = 0f;
            Projectile.netUpdate = true;
        }

        // ---- attached: the fuse ----

        void UpdateAttached()
        {
            Projectile.velocity = Vector2.Zero;   // it never moves by itself while stuck (vanilla would add the velocity)
            fuseTicks++;
            float t = MathHelper.Clamp(fuseTicks / (float)Math.Max(1, Tune.FuseTicks), 0f, 1f);

            if (Projectile.ai[1] >= 0f)
            {
                int index = (int)Projectile.ai[1];
                NPC npc = index >= 0 && index < Main.maxNPCs ? Main.npc[index] : null;
                if (npc == null || !npc.active)
                {
                    if (IsOwner)
                        Detonate();   // the enemy is gone: no reason to wait
                    return;
                }
                float length = (Projectile.ai[1] - index) * 1024f;
                Projectile.Center = npc.Center + Projectile.ai[2].ToRotationVector2() * length;
            }

            // The heartbeat: the time between beats shrinks from PulsePeriodStart to PulsePeriodEnd; a beat peaks every time the phase passes x.5.
            float period = MathHelper.Lerp(Tune.PulsePeriodStart, Tune.PulsePeriodEnd, (float)Math.Pow(t, Tune.PulseCurve));
            beatPhase += 1f / Math.Max(2f, period);
            int beats = (int)(beatPhase + 0.5f);
            if (beats > beatCount)
            {
                beatCount = beats;
                beatFlash = 1f;
                MeatballFx.Instance.Beat(Projectile.Center, t);
            }
            int dripChance = Math.Max(1, (int)MathHelper.Lerp(Tune.DripChanceStart, Tune.DripChanceEnd, t));
            if (Main.rand.NextBool(dripChance))
                MeatballFx.Instance.Drip(Projectile.Center);
            Lighting.AddLight(Projectile.Center, 0.5f + 0.9f * t + 0.5f * beatFlash, 0.05f, 0.08f);

            if (IsOwner && fuseTicks >= Tune.FuseTicks)
                Detonate();
        }

        // ---- the explosion ----

        // Owner's client only: the blood explosion hits every enemy in the circle once and makes it bleed; the ball is flagged Spent (synced)
        // and every machine plays the explosion and removes its own copy. Also called when the ball is pushed out by a newer one.
        void Detonate()
        {
            if (!IsOwner || CurrentPhase == Phase.Spent)
                return;
            Vector2 at = Projectile.Center;
            int damage = Math.Max(1, (int)(Projectile.damage * Tune.BlastDamageMultiplier));
            AreaEffect.ForEachNpc(at, Tune.BlastRadius, npc =>
            {
                npc.SimpleStrikeNPC(damage, npc.Center.X >= at.X ? 1 : -1, false, Tune.BlastKnockback, DamageClass.Melee);
                if (npc.active)   // the hit may have killed it
                    npc.AddBuff(BuffID.Bleeding, Tune.BleedTicks);   // AddBuff syncs itself
            });
            Projectile.ai[0] = (float)Phase.Spent;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }

        // Owner's client only (the flail calls it right before it spawns a new ball): if the player already has 'maxBalls' balls, the oldest
        // stuck one goes off now.
        public static void MakeRoom(int owner, int maxBalls)
        {
            int type = ModContent.ProjectileType<MeatballHeadProjectile>();
            int count = 0;
            MeatballHeadProjectile oldest = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.type != type || other.owner != owner || !(other.ModProjectile is MeatballHeadProjectile ball))
                    continue;
                if (ball.CurrentPhase == Phase.Spent)
                    continue;
                count++;
                if (ball.CurrentPhase == Phase.Attached && (oldest == null || ball.fuseTicks > oldest.fuseTicks))
                    oldest = ball;
            }
            if (count >= maxBalls && oldest != null)
                oldest.Detonate();
        }

        // ---- drawing ----

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail);
            trail = null;
        }

        Texture2D HeadTexture()
        {
            if (!artTried)
            {
                artTried = true;
                ModContent.RequestIfExists<Texture2D>(GetType().FullName.Replace('.', '/'), out art); // the weapon's own art when its PNG exists
            }
            return art != null && art.IsLoaded ? art.Value : TextureAssets.Projectile[Type].Value;
        }

        // The pulse of the heartbeat: 0 at rest, 1 at the peak of a beat.
        float Pulse
        {
            get
            {
                float p = 0.5f - 0.5f * (float)Math.Cos(beatPhase * MathHelper.TwoPi);
                return p * p;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();
            MeatballFx.Instance.DrawLayers();

            Phase phase = CurrentPhase;
            if (phase == Phase.Spent)
                return false;
            bool attached = phase == Phase.Attached;

            float t = attached ? MathHelper.Clamp(fuseTicks / (float)Math.Max(1, Tune.FuseTicks), 0f, 1f) : 0f;
            float pulse = attached ? Pulse : 0f;
            float scale = 1f + Tune.SwellMax * t + Tune.PulseSwell * (0.35f + 0.65f * t) * pulse + 0.2f * beatFlash;
            Vector2 shake = attached ? Main.rand.NextVector2Circular(1f, 1f) * (Tune.ShakePx * t * t) : Vector2.Zero;
            Vector2 center = Projectile.Center + shake;
            Vector2 position = center - Main.screenPosition;
            float glow = attached ? 0.3f + 0.7f * t + 0.5f * beatFlash : 0.8f;
            Color body = Color.Lerp(lightColor, new Color(255, 70, 70), MathHelper.Clamp(0.5f * t + 0.3f * pulse * t, 0f, 1f));

            Texture2D texture = HeadTexture();
            Vector2 origin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, position, null, Tune.GlowColor(3) * (0.6f * glow), Projectile.rotation, origin, scale * 1.3f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, Tune.HotColor(3) * (0.45f * glow), Projectile.rotation, origin, scale * 1.12f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, body, Projectile.rotation, origin, scale, SpriteEffects.None, 0);

            // A ring of pixels leaves the ball with every beat.
            if (attached && beatFlash > 0.02f)
            {
                float grid = Tune.PixelSize;
                float u = 1f - beatFlash;
                float ringRadius = 14f * scale + (20f + 28f * t) * u;
                int count = Math.Max(10, (int)(MathHelper.TwoPi * ringRadius / (grid * 3f)));
                for (int i = 0; i < count; i++)
                    IceFx.DrawPixel(center + (MathHelper.TwoPi * i / count).ToRotationVector2() * ringRadius, grid * 2f, Tune.HotColor(3) * (0.8f * beatFlash), grid);
            }
            return false;
        }
    }
}