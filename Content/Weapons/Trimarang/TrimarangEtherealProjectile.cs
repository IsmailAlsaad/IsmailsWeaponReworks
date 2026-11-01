using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Trimarang
{
    /// <summary>
    /// The ethereal form the Trimarang sheds on every Nth hit: a see-through copy that spins as fast as the main boomerang did. It is
    /// shot outward as an impulse (no homing at all) and a pull toward the player (BoomerangFlight.StepForceReturn) bends that into one
    /// smooth arc back; it never hits (or damages) the enemy that triggered it, passes through tiles and pierces. After 8 hits, or when
    /// it reaches the player, it explodes, which is only a visual.
    /// ai[0] = the enemy it must never hit (NPC index), ai[1] = -1 alive / -2 dying, ai[2] = spin speed in radians per tick;
    /// localAI[0] = age. MULTIPLAYER: spawned by the owner's client (so it is synced like any projectile, launch velocity included); the
    /// owner counts the hits and decides the end (8 hits, reaching the player, the safety limit) and syncs "dying" through ai[1] and
    /// netUpdate. Every machine flies the same force-based arc and plays the explosion when it sees "dying", so no kill packet is needed.
    /// </summary>
    public class TrimarangEtherealProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Trimarang;

        static Asset<Texture2D> art;

        bool Dying => Projectile.ai[1] == -2f;
        float Age => Projectile.localAI[0];

        int hits;                  // owner only
        bool exploded;             // every machine plays the explosion once
        int dyingTicks;
        RemoteHitTracker hitTracker; // other machines only

        public override void Load()
        {
            if (Main.dedServ)
                return;
            ModContent.RequestIfExists<Texture2D>("IsmailsWeaponReworks/Content/Weapons/Trimarang/TrimarangEtherealProjectile", out art);
        }

        public override void Unload()
        {
            art = null;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // passes through tiles
            Projectile.ignoreWater = true;
            Projectile.timeLeft = TrimarangTuning.EtherealLifeTicks + 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = TrimarangTuning.EtherealHitCooldownTicks;
        }

        // Owner only: start the harmless explosion (synced through ai[1]).
        public void BeginDying()
        {
            if (Dying)
                return;
            Projectile.ai[1] = -2f;
            Projectile.netUpdate = true;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active)
            {
                Projectile.Kill();
                return;
            }
            bool isOwner = Projectile.owner == Main.myPlayer;

            if (Projectile.localAI[0] == 0f)
                TrimarangFx.EtherealSpawn(Projectile.Center, Vector2.Normalize(Projectile.velocity));
            Projectile.localAI[0]++;

            if (Dying)
            {
                if (!exploded)
                {
                    exploded = true;
                    TrimarangFx.EtherealExplode(Projectile.Center);
                }
                Projectile.velocity = Vector2.Zero;
                if (++dyingTicks >= 2)
                    Projectile.Kill();
                return;
            }

            if (!isOwner)
                RemoteHitSparks();

            Projectile.rotation += Projectile.ai[2]; // the same spin speed the main boomerang had
            Lighting.AddLight(Projectile.Center, 0.3f, 0.6f, 0.9f);
            TrimarangFx.EtherealTrail(Projectile.Center, Projectile.velocity);

            if (isOwner && Age > TrimarangTuning.EtherealLifeTicks)
            {
                BeginDying(); // safety net: it has not reached the player in time, so it fizzles out
                return;
            }

            // No homing: the launch impulse (the velocity it was spawned with) is bent into an arc by the pull toward the player.
            // Reaching the player ends it with the same harmless explosion as the 8th hit (the owner decides, everyone else sees "dying").
            bool reached = BoomerangFlight.StepForceReturn(Projectile, owner, TrimarangTuning.EtherealReturnAcceleration, TrimarangTuning.EtherealReturnMaxSpeed, TrimarangTuning.EtherealReturnDrag, TrimarangTuning.CatchDistance);
            if (reached && isOwner)
                BeginDying();
        }

        // ---- hits ----

        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == (int)Projectile.ai[0] ? false : (bool?)null; // never the shredded one
        }

        public override bool? CanDamage()
        {
            return Dying ? false : (bool?)null;
        }

        void RemoteHitSparks()
        {
            if (Main.dedServ)
                return;
            hitTracker ??= new RemoteHitTracker();
            hitTracker.Update(Projectile, 0, npc =>
            {
                if (npc.whoAmI != (int)Projectile.ai[0])
                    TrimarangFx.HitSparks(npc.Center);
            });
        }

        // Only runs on the owner's client, which counts the hits.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            TrimarangFx.HitSparks(target.Center);
            if (++hits >= TrimarangTuning.EtherealHitsToExplode)
                BeginDying();
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = TrimarangTuning.EtherealColor;
            float opacity = Dying ? 0.3f : 1f;

            int length = ProjectileID.Sets.TrailCacheLength[Type];
            for (int i = length - 1; i >= 1; i--)
            {
                Vector2 old = Projectile.oldPos[i];
                if (old == Vector2.Zero)
                    continue;
                float fade = 1f - i / (float)length;
                Main.EntitySpriteDraw(texture, old + Projectile.Size * 0.5f - Main.screenPosition, null, glow * (0.5f * fade * opacity), Projectile.oldRot[i], origin, 0.7f + 0.3f * fade, SpriteEffects.None, 0);
            }

            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, position, null, glow * (0.45f * opacity), Projectile.rotation, origin, 1.3f, SpriteEffects.None, 0); // halo
            Main.EntitySpriteDraw(texture, position, null, Color.White * (TrimarangTuning.EtherealOpacity * opacity), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}