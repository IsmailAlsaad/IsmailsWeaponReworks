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
    /// wraps its feet); then the vine digs back into the ground. It does no damage. At most MaxVines of a player exist at once: planting
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
        int wrapNpc = -1;                  // the enemy the wrap is drawn on (kept while the wrap unwinds)
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
                wrapNpc = index;
                NPC held = index < Main.maxNPCs ? Main.npc[index] : null;
                if (held != null && held.active && held.life > 0)
                {
                    // Every machine keeps the hold alive on its own copy of the enemy (AmazonSnareNPC ends it a moment after the vine stops).
                    held.GetGlobalNPC<AmazonSnareNPC>().HoldTick = (long)Main.GameUpdateCount;
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
                    if (Age >= tune.VineGrowTicks)
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
                if (snare.ResnareTicks > 0 || snare.Held)
                    continue;   // snared a moment ago, or another vine of ours already holds it
                Projectile.ai[1] = npc.whoAmI + 1;
                snareAge = 0;
                Projectile.netUpdate = true;
                YoyoShake.Hit(npc.Center, tune.SnareShake);
                return;
            }
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

            int count = Math.Max(4, tune.VinePoints);
            if (points == null || points.Length != count)
                points = new Vector2[count];

            int g = GravDir;
            Vector2 up = new Vector2(0f, -g);
            Vector2 root = Projectile.Center - up * tune.VineRootSinkPx;   // the foot of the vine is a little inside the surface
            float seed = Seed;

            NPC wrapped = null;
            int wrapIndex = SnaredIndex >= 0 ? SnaredIndex : wrapNpc;
            if (wrap > 0.001f && wrapIndex >= 0 && wrapIndex < Main.maxNPCs && Main.npc[wrapIndex].active)
                wrapped = Main.npc[wrapIndex];

            for (int k = 0; k < count; k++)
            {
                float u = k / (float)(count - 1);

                // Standing: straight up from the root, swaying more toward the tip (a sine wave along the vine with its own phase).
                float sway = (float)Math.Sin(Age * tune.VineSwaySpeed + seed + u * 3.2f) * tune.VineSwayPx * u;
                Vector2 point = root + up * (tune.VineLengthPx * visible * u) + new Vector2(sway, 0f);

                // Snaring: the vine curls around the enemy's feet, the root staying where it is.
                if (wrapped != null)
                {
                    float radius = MathHelper.Clamp(wrapped.width * 0.5f + tune.WrapRadiusPad, 8f, 40f);
                    float climb = MathHelper.Clamp(wrapped.height * tune.WrapHeightFraction, 8f, 28f);
                    float angle = seed + u * tune.WrapLoops * MathHelper.TwoPi;
                    float feetY = g > 0 ? wrapped.Bottom.Y : wrapped.Top.Y;
                    Vector2 coil = new Vector2(wrapped.Center.X + (float)Math.Cos(angle) * radius, feetY - g * (2f + u * climb) + (float)Math.Sin(angle) * radius * 0.3f);
                    float weight = wrap * MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(u / 0.35f, 0f, 1f));
                    point = Vector2.Lerp(point, coil, weight);
                }
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