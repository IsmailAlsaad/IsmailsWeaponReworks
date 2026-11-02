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
    /// shot outward as an impulse, flies straight out for a moment, then homes on the nearest enemy that is NOT the one that triggered
    /// it (it never targets or damages that one). If no other enemy is near it falls back to an arc: a pull toward the player
    /// (BoomerangFlight.StepForceReturn) bends the impulse into one smooth curve back, and reaching the player ends it. It passes
    /// through tiles and pierces. After 3 hits, or at the end of the arc, it explodes, which is only a visual.
    /// ai[0] = the enemy it must never hit (NPC index), ai[1] = target NPC index (-1 none, -2 dying), ai[2] = spin speed in radians per
    /// tick; localAI[0] = age. MULTIPLAYER: spawned by the owner's client (so it is synced like any projectile, with the launch velocity
    /// and the first target in the spawn); the owner re-picks targets, counts the hits and decides the end, and syncs target and "dying"
    /// through ai[1] and netUpdate. Every machine steers toward the synced target (or flies the same arc) and plays the explosion when
    /// it sees "dying", so no kill packet is needed.
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
        AfterimageTrail afterimages; // visual only, never synced

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

            // Owner only: keep the target while it is valid, otherwise look for another one every few ticks.
            if (isOwner && (Age == 1f || Age % TrimarangTuning.EtherealRetargetTicks == 0f))
                ChooseTarget();

            int target = (int)Projectile.ai[1];
            if (target >= 0 && target < Main.maxNPCs && Main.npc[target].active)
            {
                if (Age >= TrimarangTuning.EtherealLaunchTicks) // flies straight out first, then homes
                {
                    Vector2 desired = Vector2.Normalize(Main.npc[target].Center - Projectile.Center) * TrimarangTuning.EtherealHomingSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, TrimarangTuning.EtherealTurn);
                }
                return;
            }

            // No other enemy near: the launch impulse (the velocity it was spawned with) is bent into an arc by the pull toward the
            // player. Reaching the player ends it with the same harmless explosion as the last hit (the owner decides, everyone else
            // sees "dying").
            bool reached = BoomerangFlight.StepForceReturn(Projectile, owner, TrimarangTuning.EtherealReturnAcceleration, TrimarangTuning.EtherealReturnMaxSpeed, TrimarangTuning.EtherealReturnDrag, TrimarangTuning.CatchDistance);
            if (reached && isOwner)
                BeginDying();
        }

        // Owner only: keep the current target while it is valid, otherwise look for the nearest other enemy.
        void ChooseTarget()
        {
            int current = (int)Projectile.ai[1];
            if (current >= 0 && current < Main.maxNPCs && Main.npc[current].active && Main.npc[current].CanBeChasedBy(Projectile))
                return;
            if (current >= 0)
            {
                Projectile.ai[1] = -1f;
                Projectile.netUpdate = true;
            }
            if (Age % TrimarangTuning.EtherealRetargetTicks != 0f)
                return;

            int best = FindTarget(Projectile.Center, (int)Projectile.ai[0], Projectile);
            if (best >= 0)
            {
                Projectile.ai[1] = best;
                Projectile.netUpdate = true;
            }
        }

        // The nearest enemy within the search range that is not 'excluded' (the one that triggered the ghost), or -1. The main
        // Trimarang uses it too, to give every new ghost its first target in the spawn itself.
        public static int FindTarget(Vector2 from, int excluded, Projectile projectile)
        {
            int best = -1;
            float bestDistance = TrimarangTuning.EtherealSearchRange * TrimarangTuning.EtherealSearchRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (i == excluded || !npc.CanBeChasedBy(projectile))
                    continue;
                float distance = Vector2.DistanceSquared(from, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
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

        // One afterimage per tick (visual only, never synced). Each carries its own lifetime: OnKill hands the trail to DetachedTrails,
        // so when this projectile dies the leftover afterimages fade out on their own instead of vanishing with it.
        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active)
                return;
            afterimages ??= AfterimageTrail.Sprite(art?.Value ?? TextureAssets.Projectile[Type].Value, 8, 0.7f);
            afterimages.Tick();
            if (!Dying)
                afterimages.Add(Projectile.Center, Projectile.rotation, 1f, TrimarangTuning.EtherealColor * 0.5f);
        }

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(afterimages);
            afterimages = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = art?.Value ?? TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = TrimarangTuning.EtherealColor;
            float opacity = Dying ? 0.3f : 1f;

            afterimages?.Draw();

            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, position, null, glow * (0.45f * opacity), Projectile.rotation, origin, 1.3f, SpriteEffects.None, 0); // halo
            Main.EntitySpriteDraw(texture, position, null, Color.White * (TrimarangTuning.EtherealOpacity * opacity), Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}