using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Rally
{
    /// <summary>
    /// A piece of the Rally's broken shell. It is thrown outward by the break, falls under gravity (toward the owner's feet, so flipped
    /// gravity works), bounces subtly off tiles and settles on them. It hurts every enemy it touches (each enemy once) and breaks after
    /// RallyTuning.ShardMaxHits of them (vanilla piercing); a shard that hit nothing fades after ShardLifeTicks. A settled shard keeps
    /// its pierces. Drawn from Rally/RallyShellShard.png; until that exists it is one quarter of the vanilla yoyo sprite (ai[0] picks the quarter).
    /// ai[0] = piece number (0 to 3, picks the quarter), ai[1] = spin direction (+1 or -1), localAI[0] = age in ticks.
    /// MULTIPLAYER: the owner's client spawns the shards (normal spawned projectiles, so the position and the launch velocity are
    /// synced) and vanilla counts the pierces and removes the shard on every machine; the fall, the bounce and the fade are
    /// deterministic from the launch, each machine simulates its own copy. Other machines add the hit effects themselves
    /// (RemoteHitTracker, visual only).
    /// </summary>
    public class RallyShellShardProjectile : ModProjectile
    {
        // The vanilla Rally art as the fallback (a quarter of it is drawn).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Rally;

        static Asset<Texture2D> art;

        readonly RallyTuning T = new RallyTuning();
        readonly RallyFx fx = new RallyFx();
        RemoteHitTracker hitTracker;   // other machines only
        bool resting;                  // it landed last tick

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>(new RallyTuning().ShardTexturePath, out art);
        }

        public override void Unload()
        {
            art = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = T.ShardSize;
            Projectile.height = T.ShardSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = T.ShardMaxHits;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = T.ShardLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;   // each enemy is hit once by a shard
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }
            int g = owner.gravDir >= 0f ? 1 : -1;
            bool wasResting = resting;
            resting = false;
            Projectile.localAI[0]++;

            Projectile.velocity.Y += T.ShardGravity * g;
            Projectile.velocity.Y = g > 0 ? Math.Min(Projectile.velocity.Y, T.ShardMaxFallSpeed) : Math.Max(Projectile.velocity.Y, -T.ShardMaxFallSpeed);
            Projectile.velocity.X *= T.ShardAirDrag;

            float speed = Projectile.velocity.Length();
            if (!wasResting)
                Projectile.rotation += (T.ShardSpin + 0.03f * speed) * (Projectile.ai[1] >= 0f ? 1f : -1f);
            if (speed > 3f && Main.rand.NextBool(3))
                fx.ShardTrail(Projectile.Center, Projectile.velocity);

            if (Projectile.owner != Main.myPlayer && !Main.dedServ)
            {
                hitTracker ??= new RemoteHitTracker();
                hitTracker.Update(Projectile, 0, npc => fx.ShardHit(npc.Center));   // one segment: each enemy once, like the owner's hits
            }
        }

        // A tile: bounce subtly, or settle when it lands softly. Returning false keeps the shard alive.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            int g = Main.player[Projectile.owner].gravDir >= 0f ? 1 : -1;
            Vector2 velocity = Projectile.velocity;
            if (Projectile.velocity.X != oldVelocity.X)
                velocity.X = -oldVelocity.X * T.ShardBounceDamp;
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                if (Math.Abs(oldVelocity.Y) < T.ShardRestSpeed)
                {
                    velocity.Y = 0f;
                    if (oldVelocity.Y * g > 0f)
                    {
                        resting = true;   // landed on the feet side: it slides to a stop
                        velocity.X = oldVelocity.X * T.ShardGroundFriction;
                    }
                }
                else
                {
                    velocity.Y = -oldVelocity.Y * T.ShardBounceDamp;
                    fx.ShardBounce(Projectile.Center, Math.Abs(oldVelocity.Y));
                }
            }
            Projectile.velocity = velocity;
            return false;
        }

        // Owner's client only.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            fx.ShardHit(target.Center);
        }

        // Every machine: the last pierce, the end of its time, or the owner leaving.
        public override void OnKill(int timeLeft)
        {
            fx.ShardBreak(Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)Math.Max(1, T.ShardFadeTicks), 0f, 1f);
            Color color = lightColor * fade;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Texture2D own = art != null && art.IsLoaded ? art.Value : null;
            if (own != null)
            {
                Main.EntitySpriteDraw(own, position, null, color, Projectile.rotation, own.Size() * 0.5f, 1f, SpriteEffects.None, 0);
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int halfW = texture.Width / 2;
            int halfH = texture.Height / 2;
            int quarter = (int)Projectile.ai[0] & 3;
            Rectangle source = new Rectangle((quarter & 1) * halfW, (quarter >> 1) * halfH, halfW, halfH);
            Main.EntitySpriteDraw(texture, position, source, color, Projectile.rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}