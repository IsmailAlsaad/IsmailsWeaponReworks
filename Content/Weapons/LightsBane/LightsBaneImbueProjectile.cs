using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// The imbue (patch 2): spawned by the owner at the moment the blade is buried in the chest (ai[0] = how many ticks it lasts). It follows
    /// the player for that long and shows the imbue to EVERY machine: the purple dust trail at the feet and the purple light. It draws
    /// nothing itself yet (patch 3 adds the player outline and the afterimages here). It is VISUAL ONLY: the speed boost lives in
    /// LightsBanePlayer on the owner's machine (vanilla syncs the player's position, so nobody else needs the speed), so a machine that
    /// misses this projectile (a late joiner) only misses some dust.
    /// </summary>
    public class LightsBaneImbueProjectile : ModProjectile
    {
        // Never drawn (PreDraw returns false); the texture only has to exist.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        bool started;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600; // replaced on the first tick by ai[0]
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            // ai[0] arrives with the spawn on every machine, so the duration is read on the first tick rather than in SetDefaults.
            if (!started)
            {
                started = true;
                Projectile.timeLeft = Math.Max(2, (int)Projectile.ai[0]);
            }

            Projectile.Center = owner.Center;
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)Math.Max(1, LightsBaneTuning.ImbueFadeTicks), 0f, 1f);
            LightsBaneFx.ImbueTrail(owner, fade);
            Lighting.AddLight(owner.Center, 0.45f * fade, 0.15f * fade, 0.9f * fade);
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}