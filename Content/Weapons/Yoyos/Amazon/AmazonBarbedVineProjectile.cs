using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// A tiny barbed vine the Amazon plants in the ground while it rolls (right click). It is the end portion of a Blade of Grass vine: the
    /// spiny chain tapering to the Thorn Hook head, only VineLengthPx long, standing up from the surface against gravity (it hangs down
    /// from a ceiling when gravity is flipped) and swaying a little. An enemy that walks into it is held in place for SnareTicks (the vine
    /// reaches out and loops around it); then the vine digs back into the ground. It does no damage. An enemy that touches it in the air
    /// keeps falling until it lands, and is held then (not when it lands more than SnareReachPx from the vine). At most MaxVines of a player exist at once: planting
    /// one more makes the oldest dig away. A vine nobody touches digs away after VineLifeTicks, and a boss is never held.
    /// ai[0] = the sway seed (negative when gravity was flipped at the planting), ai[1] = the held enemy's index plus 1 (0 = none),
    /// ai[2] = 0 standing, 1 digging.
    /// MULTIPLAYER: planted by the owner's client (normal spawn sync, the seed is the spawn data). The owner's client alone decides the
    /// snare (the overlap with an enemy), its end, the eviction and the dig, and publishes them through ai[1], ai[2] and netUpdate.
    /// Every machine animates the pop, the sway, the wrap and the dig from those values, and refreshes the hold lease on its copy of the
    /// held enemy (AmazonSnareNPC), so the enemy is held on the server and on every client. A digging vine ends itself on every machine
    /// after DigTicks. Nothing in the visuals is synced.
    /// </summary>
    public class AmazonBarbedVineProjectile : ModProjectile
    {
        // The Thorn Hook head sprite as its texture so nothing is missing; the drawing picks the textures itself.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ThornHook;

        AmazonTuning tune;
        AmazonFx fx;

        public int Age;                    // ticks since this machine saw the vine (on the owner's client: since it was planted)
        int digAge;                        // ticks since the dig began
        int snareAge;                      // owner only: ticks since an enemy was snared
        int pendingNpc = -1;               // owner only: an enemy that touched the vine in the air and is still falling (held when it lands)
        int pendingAge;                    // owner only: ticks it has been falling
        Vector2 loopCenter;                // the held enemy's center and the loop's radius around it, refreshed while it is held (visual only)
        float loopRadius = 20f;
        Vector2 loopEntry, loopControl;    // the stem from the root to the ring: where it meets the ring and how it bends
        float loopStemFrac, loopA0, loopSpan, loopR0;
        float wrap;                        // 0..1, how far the vine has wrapped the enemy
        bool popped;
        bool wasDigging;
        bool wasSnared;
        Vector2[] points;
        Asset<Texture2D> chainAsset;
        Asset<Texture2D> headAsset;

        bool IsOwner => Projectile.owner == Main.myPlayer;
        int GravDir => Projectile.ai[0] < 0f ? -1 : 1;
        float Seed => Math.Abs(Projectile.ai[0]);
        int SnaredIndex => (int)Projectile.ai[1] - 1;
        public bool Digging => Projectile.ai[2] >= 1f;
        float LoopDir => (int)Seed % 2 == 0 ? 1f : -1f;   // which way the tip circles (from the vine's own seed)

        public override void SetDefaults()
        {
            tune = new AmazonTuning();
            fx = new AmazonFx();
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;   // it never damages: the snare is decided by hand
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = tune.VineLifeTicks + tune.DigTicks + 600;   // the owner starts the dig long before this
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        // ---------------------------------------------------------------- planting and eviction (owner's client)

        // Plants a vine at 'surface' (the floor point under the yoyo, or the ceiling point when gravity is flipped). The oldest vine
        // digs away first when the player already has MaxVines.
        public static void Plant(Projectile yoyo, Vector2 surface, int gravDir, AmazonTuning tune)
        {
            MakeRoom(yoyo.owner, tune.MaxVines);
            float seed = Main.rand.NextFloat(1f, 100f) * gravDir;
            Projectile.NewProjectile(yoyo.GetSource_FromThis(), surface, Vector2.Zero, ModContent.ProjectileType<AmazonBarbedVineProjectile>(), 0, 0f, yoyo.owner, seed, 0f, 0f);
        }

        // Makes the oldest standing vines of this player dig away until there is room for one more (fewer than 'max' are left standing).
        static void MakeRoom(int owner, int max)
        {
            int type = ModContent.ProjectileType<AmazonBarbedVineProjectile>();
            while (true)
            {
                int count = 0;
                AmazonBarbedVineProjectile oldest = null;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile other = Main.projectile[i];
                    if (!other.active || other.type != type || other.owner != owner)
                        continue;
                    AmazonBarbedVineProjectile vine = other.ModProjectile as AmazonBarbedVineProjectile;
                    if (vine == null || vine.Digging)
                        continue;
                    count++;
                    if (oldest == null || vine.Age > oldest.Age)
                        oldest = vine;
                }
                if (count < max || oldest == null)
                    return;
                oldest.StartDig();   // sets it digging at once, so the next round counts one less
            }
        }

        // Owner's client: the vine lets go of its enemy (if it holds one) and digs back into the ground.
        void StartDig()
        {
            int index = SnaredIndex;
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].active)
                Main.npc[index].GetGlobalNPC<AmazonSnareNPC>().ResnareTicks = tune.ResnareCooldownTicks;
            Projectile.ai[1] = 0f;
            Projectile.ai[2] = 1f;
            Projectile.netUpdate = true;
        }

        // ---------------------------------------------------------------- AI

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;
            Vector2 up = new Vector2(0f, -GravDir);

            if (!popped)
            {
                popped = true;
                fx.VinePop(Projectile.Center, up);
            }

            if (Digging)
            {
                if (!wasDigging)
                {
                    wasDigging = true;
                    digAge = 0;
                    Projectile.timeLeft = Math.Min(Projectile.timeLeft, tune.DigTicks + 2);   // every machine ends its own copy
                    fx.VineDig(Projectile.Center, up);
                }
                digAge++;
                wrap = Math.Max(0f, wrap - 1f / Math.Max(1, tune.WrapTicks));
                return;
            }

            int index = SnaredIndex;
            if (index >= 0)
            {
                NPC held = index < Main.maxNPCs ? Main.npc[index] : null;
                if (held != null && held.active && held.life > 0)
                {
                    // Every machine keeps the hold alive on its own copy of the enemy (AmazonSnareNPC ends it a moment after the vine stops).
                    held.GetGlobalNPC<AmazonSnareNPC>().HoldTick = (long)Main.GameUpdateCount;
                    loopCenter = held.Center;
                    loopRadius = MathHelper.Clamp(Math.Max(held.width, held.height) * tune.LoopRadiusScale + tune.LoopRadiusPad, 12f, 60f);
                    if (!wasSnared)
                    {
                        wasSnared = true;
                        fx.Snare(held.Center, held.width, held.height);
                    }
                    wrap = Math.Min(1f, wrap + 1f / Math.Max(1, tune.WrapTicks));

                    if (Age % Math.Max(1, tune.SnareDustTicks) == 0)
                        fx.SnareTick(held.Center, held.width, held.height);
                    if (IsOwner)
                    {
                        snareAge++;
                        if (snareAge >= tune.SnareTicks)
                            StartDig();   // the 5 s are over: the enemy walks on, the vine digs away
                    }
                }
                else if (IsOwner)
                    StartDig();           // the enemy is gone: nothing left to hold
            }
            else
            {
                wasSnared = false;
                wrap = Math.Max(0f, wrap - 1f / Math.Max(1, tune.WrapTicks));
                if (IsOwner)
                {
                    if (pendingNpc >= 0)
                        TickPending();   // an enemy is still falling toward the ground: held when it lands
                    else if (Age >= tune.VineGrowTicks)
                        TrySnare();
                    if (Age >= tune.VineLifeTicks)
                        StartDig();       // nobody touched it for a long time
                }
            }
        }

        // An enemy that can be held: any hostile that can be damaged (the target dummy too), but never a boss or a worm's body.
        static bool CanSnare(NPC npc)
        {
            return npc.active && !npc.friendly && !npc.dontTakeDamage && !npc.boss && !NPCID.Sets.ShouldBeCountedAsBoss[npc.type] && npc.realLife < 0;
        }

        // Owner's client: an enemy that touches the standing vine is snared.
        void TrySnare()
        {
            float length = tune.VineLengthPx;
            int width = (int)tune.VineHitWidthPx;
            int x = (int)(Projectile.Center.X - width * 0.5f);
            int y = GravDir > 0 ? (int)(Projectile.Center.Y - length) : (int)Projectile.Center.Y;
            Rectangle box = new Rectangle(x, y, width, (int)length);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!CanSnare(npc) || !npc.Hitbox.Intersects(box))
                    continue;
                AmazonSnareNPC snare = npc.GetGlobalNPC<AmazonSnareNPC>();
                if (snare.ResnareTicks > 0 || snare.Held || snare.Pending)
                    continue;   // snared a moment ago, or another vine of ours already holds it or is waiting for it to land
                if (!npc.noGravity && !OnGround(npc))
                {
                    pendingNpc = npc.whoAmI;   // in the air: it keeps falling and is held when it lands
                    pendingAge = 0;
                    snare.PendingTick = (long)Main.GameUpdateCount;
                    return;
                }
                Hold(npc);
                return;
            }
        }

        // ---------------------------------------------------------------- the enemy that touched the vine (owner's client)

        // The enemy is held: the owner publishes it through ai[1] and every machine takes it from there.
        void Hold(NPC npc)
        {
            Projectile.ai[1] = npc.whoAmI + 1;
            snareAge = 0;
            Projectile.netUpdate = true;
            YoyoShake.Hit(npc.Center, tune.SnareShake);
        }

        // An enemy that touched the vine in the air keeps falling (and moving) as usual. When it lands near enough it is held; landing too far
        // away (SnareReachPx), dying, getting held by another vine or falling for too long (PendingMaxTicks) cancels the snare.
        void TickPending()
        {
            pendingAge++;
            NPC npc = pendingNpc >= 0 && pendingNpc < Main.maxNPCs ? Main.npc[pendingNpc] : null;
            if (npc == null || !CanSnare(npc) || npc.life <= 0 || pendingAge > tune.PendingMaxTicks)
            {
                pendingNpc = -1;
                return;
            }
            AmazonSnareNPC snare = npc.GetGlobalNPC<AmazonSnareNPC>();
            if (snare.Held)
            {
                pendingNpc = -1;
                return;
            }
            snare.PendingTick = (long)Main.GameUpdateCount;
            if (!OnGround(npc))
                return;
            pendingNpc = -1;
            if (Vector2.Distance(npc.Bottom, Projectile.Center) <= tune.SnareReachPx)
                Hold(npc);
        }

        // Standing on a block or a platform (and not on its way up): false while it is in the air.
        static bool OnGround(NPC npc)
        {
            return npc.velocity.Y > -0.5f && Collision.SolidCollision(new Vector2(npc.position.X, npc.Bottom.Y), npc.width, 2, true);
        }

        // ---------------------------------------------------------------- the loop around a held enemy

        // Works out the loop for this frame (visual only): a stem that arches from the root to a ring around the enemy, and the vine circling
        // the enemy about LoopTurns times on that ring (a little wider where it starts, like the Blade of Grass vine that latches on).
        void LoopGeometry(Vector2 root)
        {
            loopA0 = (root - loopCenter).ToRotation();
            loopR0 = loopRadius * (1f + tune.LoopSpiral);
            loopEntry = loopCenter + loopA0.ToRotationVector2() * loopR0;
            float stemLength = Vector2.Distance(root, loopEntry);
            loopControl = (root + loopEntry) * 0.5f + new Vector2(0f, -GravDir) * Math.Min(tune.StemArcPx, stemLength * 0.4f);
            loopSpan = MathHelper.TwoPi * tune.LoopTurns;
            float arcLength = loopRadius * loopSpan * (1f + tune.LoopSpiral * 0.5f);
            loopStemFrac = stemLength / Math.Max(1f, stemLength + arcLength);
        }

        // A point on the loop, f from 0 (the root) to 1 (the tip), with a tiny wiggle.
        Vector2 LoopAt(Vector2 start, float f)
        {
            float clock = Age * tune.LoopWiggleSpeed + Seed;
            if (f < loopStemFrac)
            {
                float t = f / Math.Max(0.0001f, loopStemFrac);
                float s = 1f - t;
                Vector2 stem = start * (s * s) + loopControl * (2f * s * t) + loopEntry * (t * t);
                return stem + new Vector2((float)Math.Sin(clock + t * 5f) * tune.LoopWigglePx * 0.5f * (float)Math.Sin(t * MathHelper.Pi), 0f);
            }
            float q = (f - loopStemFrac) / Math.Max(0.0001f, 1f - loopStemFrac);
            float angle = loopA0 + LoopDir * loopSpan * q;
            float radius = MathHelper.Lerp(loopR0, loopRadius, q) + (float)Math.Sin(clock + q * 9f) * tune.LoopWigglePx;
            return loopCenter + angle.ToRotationVector2() * radius;
        }

        // ---------------------------------------------------------------- drawing

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || !popped)
                return false;

            chainAsset ??= ModContent.Request<Texture2D>("Terraria/Images/" + tune.SpineChainName);
            headAsset ??= ModContent.Request<Texture2D>("Terraria/Images/Projectile_" + ProjectileID.ThornHook);

            // How much of the vine is out of the ground: it grows out over VineGrowTicks and shrinks back in over DigTicks.
            float grow = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(Age / (float)Math.Max(1, tune.VineGrowTicks), 0f, 1f));
            float dig = Digging ? MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(digAge / (float)Math.Max(1, tune.DigTicks), 0f, 1f)) : 0f;
            float visible = grow * (1f - dig);
            if (visible <= 0.02f)
                return false;

            bool looped = wrap > 0.001f;
            int count = Math.Max(4, looped ? Math.Max(tune.VinePoints, tune.LoopPoints) : tune.VinePoints);
            if (points == null || points.Length != count)
                points = new Vector2[count];

            int g = GravDir;
            Vector2 up = new Vector2(0f, -g);
            Vector2 root = Projectile.Center - up * tune.VineRootSinkPx;   // the foot of the vine is a little inside the surface
            float seed = Seed;
            float reach = MathHelper.SmoothStep(0f, 1f, wrap);   // how far along the loop the tip has got, and how much of the standing shape has bent into it
            if (looped)
                LoopGeometry(root);

            for (int k = 0; k < count; k++)
            {
                float u = k / (float)(count - 1);

                // Standing: straight up from the root, swaying more toward the tip (a sine wave along the vine with its own phase).
                float sway = (float)Math.Sin(Age * tune.VineSwaySpeed + seed + u * 3.2f) * tune.VineSwayPx * u;
                Vector2 point = root + up * (tune.VineLengthPx * visible * u) + new Vector2(sway, 0f);

                // Snaring: the vine reaches out to the enemy and loops around it, the root staying where it is.
                if (looped)
                    point = Vector2.Lerp(point, LoopAt(root, u * reach), reach);
                points[k] = point;
            }

            VineDraw.BeginMesh();
            VineDraw.DrawStrip(chainAsset.Value, points, count, tune.VineBaseScale, tune.VineTipScale, 1f, 0f, tune.ChainEndTrimPx);
            VineDraw.EndMesh();

            Vector2 tip = points[count - 1];
            Vector2 along = tip - points[count - 3];
            if (along.LengthSquared() > 0.01f)
                VineDraw.DrawHead(headAsset.Value, tip, along.ToRotation(), tune.VineHeadScale, Lighting.GetColor(tip.ToTileCoordinates()), tune.HeadForwardPx);
            return false;
        }
    }
}