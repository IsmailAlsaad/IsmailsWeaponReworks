using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.WoodenBoomerang
{
    /// <summary>
    /// One half of a broken Wooden Boomerang. It tumbles away, bounces off tiles, comes to rest and lingers for a while, hurting every
    /// enemy that touches it (each enemy at most once per ShardHitCooldownTicks), then fades out. Drawn as one half of the boomerang sprite.
    /// ai[0] = which half of the sprite (0 = left, 1 = right), ai[1] = spin direction (+1 or -1), localAI[0] = age in ticks.
    /// MULTIPLAYER: the owner's client spawns both shards (normal spawned projectiles) and registers their hits. The motion (gravity,
    /// bounce, friction) and the lifetime run identically on every machine's copy; other machines add the hit sparks themselves
    /// (RemoteHitTracker, visual only).
    /// </summary>
    public class WoodenBoomerangShardProjectile : ModProjectile
    {
        // The vanilla Wooden Boomerang art (it is cut in half when drawn; PreDraw uses WoodenBoomerangProjectile.Art()).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenBoomerang;

        RemoteHitTracker hitTracker; // other machines only

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = WoodenBoomerangTuning.ShardLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = WoodenBoomerangTuning.ShardHitCooldownTicks;
        }

        // Hurts only while it is clearly there (not in the last few ticks of the fade).
        public override bool? CanDamage()
        {
            return Projectile.timeLeft > 8 ? (bool?)null : false;
        }

        public override void AI()
        {
            if (!Main.player[Projectile.owner].active)
            {
                Projectile.Kill();
                return;
            }

            float age = Projectile.localAI[0]++;
            if (age == 0f && Projectile.ai[0] == 0f)
            {
                // The two shards spawn together; only one of them plays the break effects.
                WoodenBoomerangFx.PlayBreak(Projectile.Center);
                WoodenBoomerangFx.Splinters(Projectile.Center, 14, 4f, 1.2f);
            }

            // Tumbling: gravity, a little air drag, friction while it rests on something.
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + WoodenBoomerangTuning.ShardGravity, WoodenBoomerangTuning.ShardMaxFallSpeed);
            Projectile.velocity.X *= WoodenBoomerangTuning.ShardAirDrag;
            if (Projectile.velocity.Y == 0f || (Projectile.velocity.Y > 0f && Projectile.velocity.Y < WoodenBoomerangTuning.ShardGravity * 1.5f))
                Projectile.velocity.X *= WoodenBoomerangTuning.ShardGroundFriction;

            // The spin follows the movement: fast while flying, a slow wobble once it has come to rest.
            Projectile.rotation += (0.05f + 0.04f * Projectile.velocity.Length()) * (Projectile.ai[1] >= 0f ? 1f : -1f);

            if (Projectile.velocity.LengthSquared() > 4f && Main.rand.NextBool(3))
                WoodenBoomerangFx.Trail(Projectile.Center, Projectile.velocity, 0.8f);

            if (Projectile.owner != Main.myPlayer && !Main.dedServ)
            {
                hitTracker ??= new RemoteHitTracker();
                // The segment id changes every cooldown, so an enemy that stays in contact sparks again as often as the owner's hits land.
                hitTracker.Update(Projectile, (int)(age / WoodenBoomerangTuning.ShardHitCooldownTicks), npc => WoodenBoomerangFx.HitSparks(npc.Center));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * WoodenBoomerangTuning.ShardBounceX;
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y * WoodenBoomerangTuning.ShardBounceY;
                if (Math.Abs(Projectile.velocity.Y) < 0.6f)
                    Projectile.velocity.Y = 0f; // too small to bounce: it rests
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            WoodenBoomerangFx.HitSparks(target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            WoodenBoomerangFx.Splinters(Projectile.Center, 4, 2f, 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = WoodenBoomerangProjectile.Art();
            int half = texture.Width / 2;
            Rectangle source = Projectile.ai[0] >= 1f ? new Rectangle(half, 0, texture.Width - half, texture.Height) : new Rectangle(0, 0, half, texture.Height);
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)WoodenBoomerangTuning.ShardFadeTicks, 0f, 1f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, source, lightColor * fade, Projectile.rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}