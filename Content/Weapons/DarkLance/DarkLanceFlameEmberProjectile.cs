using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.DarkLance
{
    /// <summary>The trail of a falling flame ember: the orb's trail, thinner.</summary>
    public class DarkLanceEmberTrailStyle : DarkLanceOrbTrailStyle
    {
        public override float GetWidth(int layer, float age)
        {
            return base.GetWidth(layer, age) * DarkLanceTuning.FlameEmberTrailScale;
        }
    }

    /// <summary>
    /// A flame ember thrown out of the enemy by the full-ring explosion: a small dark flame blob that flies up in a fan, is pulled down by
    /// gravity and explodes in a small dark flame blast (a DarkLanceBlastProjectile at a fraction of the size) when it hits a tile, touches an
    /// enemy, or runs out of time. It deals no damage itself; only its blast does.
    /// SPAWN VALUES: none (the velocity is the launch).
    /// MULTIPLAYER: spawned by the owner's client, so it is synced like any projectile; every machine runs the same gravity and shows the same
    /// flame (visual only). Only the owner's client decides the explosion (enemy touch, and it is the one whose Kill spawns the blast), the
    /// blast is a normal synced projectile whose visuals every machine plays on its first tick.
    /// </summary>
    public class DarkLanceFlameEmberProjectile : ModProjectile
    {
        // Never drawn as a sprite, so any existing vanilla texture will do.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        bool IsOwner => Projectile.owner == Main.myPlayer;

        int age;
        PixelTrail trail;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;   // it never damages anything by itself
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true; // it explodes on the ground
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Math.Max(10, DarkLanceTuning.FlameEmberLifeTicks);
        }

        public override void AI()
        {
            // gravity and a little air drag
            Projectile.velocity.X *= DarkLanceTuning.FlameEmberDrag;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + DarkLanceTuning.FlameEmberGravity, DarkLanceTuning.FlameEmberMaxFall);
            Projectile.rotation = Projectile.velocity.ToRotation();

            // The owner decides: an enemy touching the ember (after a short arming time, so it does not blow up inside the one that threw it).
            if (IsOwner && age >= DarkLanceTuning.FlameEmberArmTicks)
            {
                Rectangle zone = Utils.CenteredRectangle(Projectile.Center, new Vector2(DarkLanceTuning.FlameEmberTriggerSize));
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && npc.CanBeChasedBy() && npc.Hitbox.Intersects(zone))
                    {
                        Projectile.Kill();
                        return;
                    }
                }
            }

            if (!Main.dedServ)
            {
                DarkLanceEmbers.Ball(Projectile.Center, DarkLanceTuning.FlameEmberBallScale, age / 60f, Projectile.whoAmI % 64);   // a small living flame blob
                if (age % 2 == 0)
                    DarkLanceEmbers.Shed(Projectile.Center, Projectile.velocity * 0.1f);
                Lighting.AddLight(Projectile.Center, DarkLanceTuning.Palette.Mid.ToVector3() * 0.6f);
            }
            age++;
        }

        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active)
                return;
            trail ??= new PixelTrail(new DarkLanceEmberTrailStyle(), IsOwner);
            trail.Add(Projectile.Center);
            trail.Tick();
        }

        // Landing on a tile ends it (and so explodes it).
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(trail); // the line keeps fading on its own
            trail = null;
            if (IsOwner)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DarkLanceBlastProjectile>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, DarkLanceTuning.FlameEmberBlastScale);   // ai[1] = the size, as a fraction of the full blast
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail?.Draw();
            // The body is a metaball (DarkLanceEmbers.Ball); only the soft halo is drawn here.
            float haloRadius = (DarkLanceTuning.HaloReach * DarkLanceTuning.OrbUnitPx + DarkLanceTuning.BallRadius) * DarkLanceTuning.FlameEmberBallScale;
            DarkLanceFx.DrawGlow(Projectile.Center, haloRadius, DarkLanceTuning.HaloStrength * 0.8f);
            return false;
        }
    }
}