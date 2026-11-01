using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Flamerang
{
    /// <summary>
    /// A shard of obsidian thrown out by the Flamerang's contact explosion. It falls, lands on the first tile it touches and lingers
    /// there, cutting any enemy that touches it, then fades away. It glows like lava at first and cools to dark obsidian.
    /// ai[0] = 0 falling, 1 landed (synced by the owner with netUpdate). localAI[0] = age.
    /// MULTIPLAYER: spawned by the owner's client (so synced); every machine simulates the fall from the spawn velocity, the owner
    /// decides where it lands and does the damage, and the lifetime is a plain countdown that every machine runs itself.
    /// </summary>
    public class ObsidianShardProjectile : ModProjectile
    {
        // The vanilla crystal shard sprite, tinted. A PNG named ObsidianShardProjectile.png next to this file replaces it.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        static Asset<Texture2D> art;

        bool Landed => Projectile.ai[0] == 1f;
        float Age => Projectile.localAI[0];

        bool landEffectPlayed;
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

        public override void AI()
        {
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
            if (Projectile.owner == Main.myPlayer && !Landed)
            {
                Projectile.ai[0] = 1f;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            FlamerangFx.HitSparks(target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float heat = MathHelper.Clamp(1f - Age / Math.Max(1, FlamerangTuning.ShardCoolTicks), 0f, 1f);
            float fade = Projectile.timeLeft < FlamerangTuning.ShardFadeTicks ? Projectile.timeLeft / (float)FlamerangTuning.ShardFadeTicks : 1f;
            Vector2 position = Projectile.Center - Main.screenPosition;

            if (heat > 0.01f)
                Main.EntitySpriteDraw(texture, position, null, FlamerangTuning.FireColor * (0.8f * heat * fade), Projectile.rotation, origin, 1.5f, SpriteEffects.None, 0);
            Color body = Color.Lerp(FlamerangTuning.ObsidianColor, new Color(255, 190, 90), heat) * fade;
            Main.EntitySpriteDraw(texture, position, null, body, Projectile.rotation, origin, 1.25f, SpriteEffects.None, 0);
            return false;
        }
    }
}