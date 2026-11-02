using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.WoodenBoomerang
{
    /// <summary>
    /// One half of a broken Wooden Boomerang. It flies away from the break, slows down, sinks slowly and lingers, hurting every enemy that
    /// touches it (each enemy at most once per ShardHitCooldownTicks). It breaks after ShardMaxHits hits, when it touches a block, or when
    /// its time is up. It does not fade out. Drawn from WoodenBoomerangShard.png (the second shard mirrored); until that file exists it is
    /// one half of the boomerang sprite.
    /// ai[0] = which half (0 = left, 1 = right), ai[1] = spin direction (+1 or -1), ai[2] = 1 once it has used up its hits (synced),
    /// localAI[0] = age in ticks.
    /// MULTIPLAYER: the owner's client spawns both shards (normal spawned projectiles) and registers their hits and counts them; after the
    /// last hit it flags ai[2] (netUpdate) and every machine removes its own copy. The motion and the lifetime are deterministic, and a block
    /// breaks each machine's own copy; other machines add the hit sparks themselves (RemoteHitTracker, visual only).
    /// </summary>
    public class WoodenBoomerangShardProjectile : ModProjectile
    {
        // The vanilla Wooden Boomerang art as the fallback (it is cut in half when drawn).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenBoomerang;

        static Asset<Texture2D> art;

        RemoteHitTracker hitTracker; // other machines only
        int hits;                    // owner only: hits dealt so far

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/WoodenBoomerang/WoodenBoomerangShard", out art);
        }

        public override void Unload()
        {
            art = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1; // the hit count is kept by hand (synced), so every machine removes its copy
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = WoodenBoomerangTuning.ShardLifeTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = WoodenBoomerangTuning.ShardHitCooldownTicks;
        }

        // A shard that has used up its hits is only waiting for every machine to remove its copy.
        public override bool? CanDamage()
        {
            return Projectile.ai[2] == 1f ? false : (bool?)null;
        }

        public override void AI()
        {
            if (!Main.player[Projectile.owner].active || Projectile.ai[2] == 1f)
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

            // Slows down and hovers, sinking slowly.
            Projectile.velocity *= WoodenBoomerangTuning.ShardDrag;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + WoodenBoomerangTuning.ShardGravity, WoodenBoomerangTuning.ShardMaxFallSpeed);

            // The spin follows the movement: fast while flying, a slow wobble once it hovers.
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

        // A block breaks it (OnKill puffs the splinters). Every machine's copy meets the block the same way.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        // Owner's client only.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            WoodenBoomerangFx.HitSparks(target.Center);
            if (++hits >= WoodenBoomerangTuning.ShardMaxHits)
            {
                Projectile.ai[2] = 1f; // used up: flagged and synced, every machine removes its own copy next tick
                Projectile.netUpdate = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            WoodenBoomerangFx.Splinters(Projectile.Center, 8, 3.5f, 1f);
            WoodenBoomerangFx.PlayShardBreak(Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 position = Projectile.Center - Main.screenPosition;
            Texture2D own = art?.Value;
            if (own != null)
            {
                // Your own shard art: both shards use it, the second one mirrored.
                SpriteEffects effects = Projectile.ai[0] >= 1f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Main.EntitySpriteDraw(own, position, null, lightColor, Projectile.rotation, own.Size() * 0.5f, 1f, effects, 0);
                return false;
            }

            Texture2D texture = WoodenBoomerangProjectile.Art();
            int half = texture.Width / 2;
            Rectangle source = Projectile.ai[0] >= 1f ? new Rectangle(half, 0, texture.Width - half, texture.Height) : new Rectangle(0, 0, half, texture.Height);
            Main.EntitySpriteDraw(texture, position, source, lightColor, Projectile.rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}