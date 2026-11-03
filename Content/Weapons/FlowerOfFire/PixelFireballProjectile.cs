using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.FlowerOfFire
{
    /// <summary>
    /// The shader fireball: no sprite, no dust, the whole look is Effects/PixelFireball.fx (see FlowerOfFireFx.DrawFireball).
    /// It flies straight, and on the first enemy, tile or at the end of its range it detonates (the same shader plays a burst phase).
    /// MULTIPLAYER: the owner's client decides the detonation (OnHitNPC, OnTileCollide, range end) and syncs it through ai[0]
    /// (0 = flying, 1 = bursting) with netUpdate; every machine then runs the same burst timer from its own tick count. Damage and the
    /// burn come from the owner's client only; the shader time is local visual state and is never synced.
    /// </summary>
    public class PixelFireballProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.BallofFire;

        bool Bursting => Projectile.ai[0] >= 1f;
        float FlightAge => Projectile.localAI[0];
        float BurstAge => Projectile.localAI[1];

        public override void SetDefaults()
        {
            Projectile.width = (int)FlowerOfFireTuning.HitSize;
            Projectile.height = (int)FlowerOfFireTuning.HitSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = FlowerOfFireTuning.FlightTicks;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        void Detonate()
        {
            if (Projectile.owner != Main.myPlayer || Bursting)
                return;
            Projectile.ai[0] = 1f;
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            Vector3 light = FlowerOfFireTuning.LightColor;

            if (Bursting)
            {
                if (Projectile.localAI[2] == 0f)
                {
                    // First burst tick on this machine (the owner decided it, everyone else noticed it through ai[0]).
                    Projectile.localAI[2] = 1f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.friendly = false;
                    Projectile.tileCollide = false;
                    Projectile.timeLeft = FlowerOfFireTuning.BurstTicks;
                    FlowerOfFireFx.Burst(Projectile.Center);
                }
                Projectile.localAI[1]++;
                float fade = 1f - MathHelper.Clamp(BurstAge / Math.Max(1, FlowerOfFireTuning.BurstTicks), 0f, 1f);
                Lighting.AddLight(Projectile.Center, light.X * (1.6f * fade + 0.1f), light.Y * (1.6f * fade + 0.1f), light.Z * (1.6f * fade));
                return;
            }

            Projectile.localAI[0]++;
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, light.X, light.Y, light.Z);

            // Range end: detonate a little early so the other machines have time to receive the burst before their own timer runs out.
            if (Projectile.timeLeft <= 10)
                Detonate();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Detonate();
            return false; // it stays where it stopped and plays the burst there
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, FlowerOfFireTuning.BurnTicks);
            Detonate();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = (FlightAge + BurstAge) / 60f * FlowerOfFireTuning.FlameSpeed + Projectile.identity * 3.7f;
            float burst = Bursting ? MathHelper.Clamp(BurstAge / Math.Max(1, FlowerOfFireTuning.BurstTicks), 0f, 1f) : 0f;
            FlowerOfFireFx.DrawFireball(Projectile.Center, Projectile.rotation, time, burst);
            return false;
        }
    }
}