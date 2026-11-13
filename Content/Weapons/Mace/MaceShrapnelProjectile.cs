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
    /// A piece of metal shrapnel thrown out when the Mace's head explodes. It flies straight, slowing a little, with a short pixel trail,
    /// and BREAKS after piercing MaceTuning.ShardPierce enemies, on the first tile it touches, or when its life ends.
    /// ai[0] = the enemy the head exploded on (+1, 0 = none): the shard cannot hit it for ShardGraceTicks. ai[1] = 1 for the one shard that
    /// plays the explosion effect. localAI[0] = age.
    /// MULTIPLAYER: spawned by the owner's client (so it is synced); every machine flies it from the spawn velocity, the owner's client does
    /// the damage and the piercing count (Projectile.penetrate), the other machines add the hit sparks themselves (RemoteHitTracker), and
    /// every machine plays the break effect in OnKill.
    /// </summary>
    public class MaceShrapnelProjectile : ModProjectile
    {
        // The vanilla crystal shard sprite, tinted. A MaceShrapnelProjectile.png next to this file replaces it.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        static Asset<Texture2D> art;
        static MaceTuning Tuning => MaceTuning.Instance;

        float Age => Projectile.localAI[0];

        PixelTrail trail;
        RemoteHitTracker hitTracker; // other machines only

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
            if (Projectile.localAI[0] == 0f)
            {
                if (Projectile.ai[1] == 1f)
                    MaceFx.Instance.Explosion(Projectile.Center);
                if (!Main.dedServ)
                    trail = new PixelTrail(new MaceShardTrailStyle(), Projectile.owner == Main.myPlayer);
            }
            Projectile.localAI[0]++;

            Projectile.velocity *= Tuning.ShardDrag;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            trail?.Add(Projectile.Center);
            trail?.Tick();
            MaceFx.Instance.ShardFlight(Projectile.Center, Projectile.velocity);
            Lighting.AddLight(Projectile.Center, 0.35f, 0.38f, 0.45f);

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

        // A tile breaks the shard (the default); OnKill plays the break effect on every machine.
        public override void OnKill(int timeLeft)
        {
            MaceFx.Instance.ShardBreak(Projectile.Center);
            DetachedTrails.Hand(trail);
            trail = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, position, null, Tuning.GlowColor(3) * 0.7f, Projectile.rotation, origin, 1.25f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(new Color(185, 195, 210), lightColor, 0.35f), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
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