using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Mace
{
    /// <summary>
    /// A fractured piece of the Mace's head, thrown out when the head explodes. It falls under gravity (toward the owner's feet, so flipped
    /// gravity works), bounces subtly off tiles and settles on them, like the Rally's shell shards. It hurts every enemy it touches (each
    /// enemy once) and breaks after MaceTuning.ShardPierce of them (vanilla piercing); a piece that did not break fades after ShardLifeTicks.
    /// Drawn as one cell of a ShardColumns x ShardRows grid cut from the vanilla Mace head sprite (ai[1] picks the cell); a
    /// MaceShrapnelProjectile.png next to this file replaces the cut piece with one picture.
    /// ai[0] = the enemy the head exploded on (+1, 0 = none): the piece cannot hit it for ShardGraceTicks. ai[1] = piece number (0 also
    /// plays the explosion effect). localAI[0] = age in ticks.
    /// MULTIPLAYER: the owner's client spawns the pieces (normal spawned projectiles, so the position and the launch velocity are synced)
    /// and vanilla counts the pierces and removes the piece on every machine; the fall, the bounce and the fade are deterministic from the
    /// launch, each machine simulates its own copy. Other machines add the hit sparks themselves (RemoteHitTracker, visual only).
    /// </summary>
    public class MaceShrapnelProjectile : ModProjectile
    {
        // The vanilla Mace head: the piece is cut out of it.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Mace;

        static Asset<Texture2D> art;
        static MaceTuning Tuning => MaceTuning.Instance;

        float Age => Projectile.localAI[0];
        int Piece => (int)Projectile.ai[1];

        PixelTrail trail;
        RemoteHitTracker hitTracker; // other machines only
        bool resting;                // it landed last tick

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Mace/MaceShrapnelProjectile", out art);
        }

        public override void Unload()
        {
            art = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)Tuning.ShardBoxPx;
            Projectile.height = (int)Tuning.ShardBoxPx;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = Tuning.ShardPierce;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Tuning.ShardLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // every enemy once
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (Age < Tuning.ShardGraceTicks && target.whoAmI == (int)Projectile.ai[0] - 1)
                return false;
            return null;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.localAI[0] == 0f)
            {
                if (Piece == 0)
                    MaceFx.Instance.Explosion(Projectile.Center);
                if (!Main.dedServ)
                    trail = new PixelTrail(new MaceShardTrailStyle(), Projectile.owner == Main.myPlayer);
            }
            Projectile.localAI[0]++;

            int g = owner.gravDir >= 0f ? 1 : -1;
            bool wasResting = resting;
            resting = false;

            Projectile.velocity.Y += Tuning.ShardGravity * g;
            Projectile.velocity.Y = g > 0 ? Math.Min(Projectile.velocity.Y, Tuning.ShardMaxFallSpeed) : Math.Max(Projectile.velocity.Y, -Tuning.ShardMaxFallSpeed);
            Projectile.velocity.X *= Tuning.ShardAirDrag;

            float speed = Projectile.velocity.Length();
            if (!wasResting)
                Projectile.rotation += (Tuning.ShardSpin + 0.03f * speed) * (Piece % 2 == 0 ? 1f : -1f);
            if (speed >= Tuning.ShardTrailMinSpeed)
                trail?.Add(Projectile.Center);
            trail?.Tick();
            if (speed > 3f)
                MaceFx.Instance.ShardFlight(Projectile.Center, Projectile.velocity);
            Lighting.AddLight(Projectile.Center, 0.3f, 0.33f, 0.4f);

            if (Projectile.owner != Main.myPlayer && !Main.dedServ)
            {
                hitTracker ??= new RemoteHitTracker();
                hitTracker.Update(Projectile, 0, npc =>
                {
                    if (Age < Tuning.ShardGraceTicks && npc.whoAmI == (int)Projectile.ai[0] - 1)
                        return;
                    MaceFx.Instance.ShardHit(npc.Center);
                });
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            MaceFx.Instance.ShardHit(target.Center); // owner's client; the penetrate count breaks the shard after its last enemy
        }

        // A tile: bounce subtly, or settle when it lands softly. Returning false keeps the piece alive.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            int g = Main.player[Projectile.owner].gravDir >= 0f ? 1 : -1;
            Vector2 velocity = Projectile.velocity;
            if (Projectile.velocity.X != oldVelocity.X)
                velocity.X = -oldVelocity.X * Tuning.ShardBounceDamp;
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                if (Math.Abs(oldVelocity.Y) < Tuning.ShardRestSpeed)
                {
                    velocity.Y = 0f;
                    if (oldVelocity.Y * g > 0f)
                    {
                        resting = true;   // landed on the feet side: it slides to a stop
                        velocity.X = oldVelocity.X * Tuning.ShardGroundFriction;
                    }
                }
                else
                {
                    velocity.Y = -oldVelocity.Y * Tuning.ShardBounceDamp;
                    MaceFx.Instance.ShardBounce(Projectile.Center, Math.Abs(oldVelocity.Y));
                }
            }
            Projectile.velocity = velocity;
            return false;
        }

        // Every machine: the last pierce, the end of its time, or the owner leaving (OnKill plays the break effect).
        public override void OnKill(int timeLeft)
        {
            MaceFx.Instance.ShardBreak(Projectile.Center);
            DetachedTrails.Hand(trail);
            trail = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();
            Vector2 position = Projectile.Center - Main.screenPosition;
            Texture2D own = art != null && art.IsLoaded ? art.Value : null;
            if (own != null)
            {
                Main.EntitySpriteDraw(own, position, null, lightColor, Projectile.rotation, own.Size() * 0.5f, 1f, SpriteEffects.None, 0);
                return false;
            }

            // One cell of the vanilla head, cut from a ShardColumns x ShardRows grid.
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int columns = Math.Max(1, Tuning.ShardColumns);
            int rows = Math.Max(1, Tuning.ShardRows);
            int cellW = Math.Max(1, texture.Width / columns);
            int cellH = Math.Max(1, texture.Height / rows);
            int piece = Math.Max(0, Piece) % (columns * rows);
            Rectangle source = new Rectangle((piece % columns) * cellW, (piece / columns) * cellH, cellW, cellH);
            Main.EntitySpriteDraw(texture, position, source, lightColor, Projectile.rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            return false;
        }
    }

    /// <summary>The thin steel streak behind a shard (a PixelTrail): a glow under a hot core, tapering to a point.</summary>
    public class MaceShardTrailStyle : PixelTrailStyle
    {
        public override float Grid => 2f;
        public override int LifeTicks => MaceTuning.Instance.ShardTrailLifeTicks;
        public override int Layers => 2;

        public override float GetWidth(int layer, float age)
        {
            return MaceTuning.Instance.ShardTrailWidth * (layer == 0 ? 1f : 0.5f) * (1f - age);
        }

        public override Color GetColor(int layer, float age)
        {
            Color color = layer == 0 ? MaceTuning.Instance.GlowColor(3) : MaceTuning.Instance.HotColor(3);
            return color * ((1f - age * age) * (layer == 0 ? 0.5f : 0.9f)); // alpha stays 0: additive
        }
    }
}