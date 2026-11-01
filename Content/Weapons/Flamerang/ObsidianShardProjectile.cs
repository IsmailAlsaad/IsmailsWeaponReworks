using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Flamerang
{
    /// <summary>
    /// A shard of obsidian thrown out by the Flamerang's contact explosion. It falls, lands on the first tile it touches and lingers
    /// there, cutting any enemy that touches it. It glows like lava at first and cools to dark obsidian. It BREAKS (a shatter effect)
    /// after ShardMaxHits hits, when its time runs out, or when newer shards push it out (at most ShardMaxAlive per player, oldest first).
    /// ai[0] = 0 falling, 1 landed (synced by the owner with netUpdate). ai[1] = -2 breaking (set by the owner, synced with netUpdate).
    /// localAI[0] = age.
    /// MULTIPLAYER: spawned by the owner's client (so synced); every machine simulates the fall from the spawn velocity, the owner
    /// decides where it lands, counts the hits (OnHitNPC only runs on the owner), picks which shards break and syncs that through ai[1];
    /// every machine plays the shatter in OnKill, also when the plain lifetime countdown it runs itself ends.
    /// </summary>
    public class ObsidianShardProjectile : ModProjectile
    {
        // The vanilla crystal shard sprite, tinted. A PNG named ObsidianShardProjectile.png next to this file replaces it.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        static Asset<Texture2D> art;

        bool Landed => Projectile.ai[0] == 1f;
        float Age => Projectile.localAI[0];

        bool Breaking => Projectile.ai[1] == -2f;

        bool landEffectPlayed;
        int hits; // owner only
        RemoteHitTracker hitTracker; // other machines only

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Flamerang/ObsidianShardProjectile", out art);
        }

        public override void Unload()
        {
            art = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = FlamerangTuning.ShardLingerTicks;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = FlamerangTuning.ShardHitCooldownTicks;
        }

        // Owner only: start breaking (synced through ai[1]); every machine then ends its copy next tick and shatters it in OnKill.
        public void Break()
        {
            if (Breaking)
                return;
            Projectile.ai[1] = -2f;
            Projectile.netUpdate = true;
        }

        // Owner only: before a burst of 'incoming' new shards, break the oldest of this player's shards so that no more than ShardMaxAlive exist.
        public static void MakeRoom(int owner, int incoming)
        {
            int type = ModContent.ProjectileType<ObsidianShardProjectile>();
            List<Projectile> alive = new List<Projectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.type == type && other.owner == owner && other.ai[1] != -2f)
                    alive.Add(other);
            }

            int excess = alive.Count + incoming - FlamerangTuning.ShardMaxAlive;
            if (excess <= 0)
                return;
            alive.Sort((a, b) => a.timeLeft.CompareTo(b.timeLeft)); // least time left = oldest
            for (int i = 0; i < excess && i < alive.Count; i++)
                (alive[i].ModProjectile as ObsidianShardProjectile)?.Break();
        }

        public override bool? CanHitNPC(NPC target)
        {
            return Breaking ? false : (bool?)null;
        }

        public override void OnKill(int timeLeft)
        {
            FlamerangFx.ShardShatter(Projectile.Center); // every machine: 4 hits, end of life and being pushed out all end here
        }

        public override void AI()
        {
            if (Breaking)
            {
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0]++;
            float heat = MathHelper.Clamp(1f - Age / Math.Max(1, FlamerangTuning.ShardCoolTicks), 0f, 1f);

            if (Landed)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                if (!landEffectPlayed)
                {
                    landEffectPlayed = true;
                    FlamerangFx.ShardLand(Projectile.Center);
                }
            }
            else
            {
                Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + FlamerangTuning.ShardGravity, FlamerangTuning.ShardMaxFallSpeed);
                Projectile.velocity.X *= 0.99f;
                Projectile.rotation += 0.25f * (Projectile.velocity.X >= 0f ? 1f : -1f);
            }

            FlamerangFx.ShardGlow(Projectile.Center, heat);
            Lighting.AddLight(Projectile.Center, 0.8f * heat, 0.4f * heat, 0.1f * heat);

            if (Projectile.owner != Main.myPlayer && !Main.dedServ)
            {
                hitTracker ??= new RemoteHitTracker();
                hitTracker.Update(Projectile, 0, npc => FlamerangFx.HitSparks(npc.Center));
            }
        }

        // Owner only decides the landing (and syncs it); other machines are simply stopped by the tile on their own.
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Break();
            //if (Projectile.owner == Main.myPlayer && !Landed)
            //{
            //    Projectile.ai[0] = 1f;
            //    Projectile.velocity = Vector2.Zero;
            //    Projectile.netUpdate = true;
            //}
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            FlamerangFx.HitSparks(target.Center);
            if (++hits >= FlamerangTuning.ShardMaxHits)
                Break(); // OnHitNPC only runs on the owner's machine
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float heat = MathHelper.Clamp(1f - Age / Math.Max(1, FlamerangTuning.ShardCoolTicks), 0f, 1f);
            Vector2 position = Projectile.Center - Main.screenPosition;

            if (heat > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, FlamerangTuning.FireColor * (0.8f * heat), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            //Color body = Color.Lerp(FlamerangTuning.ObsidianColor, new Color(255, 190, 90), heat);
            Main.EntitySpriteDraw(texture, position, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}