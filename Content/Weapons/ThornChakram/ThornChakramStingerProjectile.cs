using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.ThornChakram
{
    /// <summary>
    /// A stinger of the Thorn Chakram: either one of the rings it spits while it flies (hits one enemy and is gone) or one of the
    /// eight it explodes into (pierces). Spawned by the chakram's owner, so its damage goes through the normal owner-client hit path.
    /// MULTIPLAYER: normal spawned projectile (synced by tModLoader); the straight flight is deterministic on every machine.
    /// ai[0]: 1 = piercing (set at spawn). ai[1]: life in ticks (set at spawn). ai[2]: 1 = plays the burst sound (one stinger per ring).
    /// localAI[0]: age in ticks.
    /// </summary>
    public class ThornChakramStingerProjectile : ModProjectile
    {
        // The vanilla stinger art.
        public override string Texture => "Terraria/Images/Projectile_" + ThornChakramTuning.StingerSpriteId;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1; // non-piercing until the first tick says otherwise
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 30;
        }

        public override void AI()
        {
            if (Projectile.localAI[0]++ == 0f)
            {
                if (Projectile.ai[1] > 0f)
                    Projectile.timeLeft = (int)Projectile.ai[1];
                if (Projectile.ai[0] == 1f)
                {
                    Projectile.penetrate = -1;
                    Projectile.usesLocalNPCImmunity = true;
                    Projectile.localNPCHitCooldown = ThornChakramTuning.ExplosionHitCooldownTicks;
                }
                ThornChakramFx.Puff(Projectile.Center, 2, 1.5f, 0.8f);
                if (Projectile.ai[2] == 1f)
                    ThornChakramFx.PlayBurst(Projectile.Center);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + ThornChakramTuning.StingerSpriteRotationOffset;
            Lighting.AddLight(Projectile.Center, 0.05f, 0.2f, 0.05f);
            if (Main.rand.NextBool(4))
                ThornChakramFx.Trail(Projectile.Center, Projectile.velocity, 0.7f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ThornChakramFx.Puff(target.Center, 3, 2.5f, 0.9f);
        }

        public override void OnKill(int timeLeft)
        {
            ThornChakramFx.Puff(Projectile.Center, 3, 2f, 0.8f);
        }
    }
}