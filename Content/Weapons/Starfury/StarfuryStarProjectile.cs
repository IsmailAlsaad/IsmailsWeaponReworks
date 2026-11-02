using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Starfury
{
    /// <summary>
    /// The star of the Starfury. A called star flies a straight line at a constant speed (no gravity) and does small damage to the first
    /// enemy it touches: the weak baseline for a star nobody bats. A swing that touches it bats it (StarfurySwingProjectile): it doubles,
    /// becomes the next generation (its own color) and flies fast toward where the owner decided. A launched star that hits an enemy
    /// arcs back to the player on a solved parabola so it can be batted again; after the last bat it ends on its first hit instead. Every star ends
    /// with the same burst of sparks (OnKill, which runs on every machine).
    /// ai[0] = generation (0 = a star straight from a call, +1 per bat). ai[1] = ticks until a called star may touch blocks: it flies
    /// through everything on its way to the cursor point (like the vanilla falling stars, so it also works underground).
    /// ai[2] = mode: 0 Straight (called, no gravity), 1 Launched (batted, straight and fast), 2 Parabolic (a small pop with gravity: the star
    /// born at a melee hit), 3 Arc (the bounce back to the player: gravity without the usual fall-speed cap, so the solved arc stays exact).
    /// MULTIPLAYER: stars are spawned by the owner (so tModLoader syncs them) and every machine moves them the same way. The owner's
    /// client decides everything else (the bat, its direction, the doubling, the bounce angle, the end) and syncs it through ai[] and
    /// velocity with netUpdate; every machine notices a changed generation or mode on its own and plays the bat or bounce effect, so
    /// no packets are needed. WeaponDamage and WeaponKnockback exist on the owner's machine only (nothing else needs them).
    /// </summary>
    public class StarfuryStarProjectile : ModProjectile
    {
        // The vanilla falling-star projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Starfury;

        public enum Mode { Straight = 0, Launched = 1, Parabolic = 2, Arc = 3 }

        public int Generation => (int)Projectile.ai[0];
        public Mode CurrentMode => (Mode)(int)Projectile.ai[2];
        public bool CanBeBatted => CurrentMode != Mode.Launched && noBatTicks <= 0;

        // Owner's machine only (never synced): the weapon's full damage and knockback, the base this star's damage is derived from.
        public int WeaponDamage;
        public float WeaponKnockback;

        int noBatTicks;          // owner only: a star that just bounced cannot be batted again right away
        int hitGraceTicks;       // owner only: a star just born at a melee hit cannot hit an enemy yet (it would end inside the one it came out of)
        int lastGeneration = -1; // per machine: lets every machine notice a bat or a bounce the owner decided
        int lastMode = -1;
        AfterimageTrail afterimages; // visual only, never synced

        float Look => 1f + StarfuryTuning.GenerationGrowth * Generation;

        // Owner only: creates a star and returns its projectile index. A launched star's velocity must already include its speed.
        public static int Spawn(IEntitySource source, Vector2 position, Vector2 velocity, int generation, Mode mode, float blockDelayTicks, int weaponDamage, float weaponKnockback, int owner)
        {
            bool launched = mode == Mode.Launched;
            int damage = Math.Max(1, (int)(weaponDamage * (launched ? StarfuryTuning.LaunchDamageMultiplier(generation) : StarfuryTuning.CalledStarDamageMultiplier)));
            float knockback = weaponKnockback * (launched ? StarfuryTuning.LaunchKnockbackMultiplier : StarfuryTuning.CalledStarKnockbackMultiplier);
            int index = Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<StarfuryStarProjectile>(), damage, knockback, owner,
                generation, blockDelayTicks, (float)mode);
            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is StarfuryStarProjectile star)
            {
                star.WeaponDamage = weaponDamage;
                star.WeaponKnockback = weaponKnockback;
                if (launched)
                    Main.projectile[index].timeLeft = StarfuryTuning.LaunchLifeTicks;
            }
            return index;
        }

        public static int CountOwned(int owner)
        {
            int type = ModContent.ProjectileType<StarfuryStarProjectile>();
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].owner == owner && Main.projectile[i].type == type)
                    count++;
            }
            return count;
        }

        // Owner only: this star was batted. It becomes the given generation and flies toward direction at that generation's speed.
        public void Launch(Vector2 direction, int generation, int weaponDamage, float weaponKnockback)
        {
            WeaponDamage = weaponDamage;
            WeaponKnockback = weaponKnockback;
            Projectile.ai[0] = generation;
            Projectile.ai[1] = 0f;
            Projectile.ai[2] = (float)Mode.Launched;
            Projectile.velocity = direction * StarfuryTuning.LaunchSpeed(generation);
            Projectile.damage = Math.Max(1, (int)(weaponDamage * StarfuryTuning.LaunchDamageMultiplier(generation)));
            Projectile.knockBack = weaponKnockback * StarfuryTuning.LaunchKnockbackMultiplier;
            Projectile.timeLeft = StarfuryTuning.LaunchLifeTicks;
            Projectile.tileCollide = true;
            Projectile.ResetLocalNPCHitImmunity(); // an enemy hit earlier may be hit again by the new launch
            Projectile.netUpdate = true;
        }

        // Owner only: the launch velocity that carries this star from where it is to a point in front of the player (within sword reach),
        // aimed where the player will be when it arrives. Gravity is added before the move every tick, so after n ticks the star has moved
        // x = vx * n and y = vy * n + g * n * (n + 1) / 2; solving that for the target gives the velocity. The flight time n comes from the
        // distance, clamped. Mode.Arc has no fall-speed cap, so the arc is exact (no correction in flight, no extra network traffic).
        Vector2 SolveArc(Player owner)
        {
            int facing = owner.direction >= 0 ? 1 : -1;
            Vector2 front = owner.MountedCenter + new Vector2(facing * StarfuryTuning.BounceFrontDistance, StarfuryTuning.BounceTargetHeight);
            float distance = Vector2.Distance(Projectile.Center, front);
            int n = (int)MathHelper.Clamp(distance / Math.Max(0.1f, StarfuryTuning.BounceSpeedTarget), StarfuryTuning.BounceMinTicks, StarfuryTuning.BounceMaxTicks);
            Vector2 delta = front + owner.velocity * (n * StarfuryTuning.BounceLead) - Projectile.Center;
            return new Vector2(delta.X / n, delta.Y / n - StarfuryTuning.Gravity * (n + 1) * 0.5f);
        }

        // Owner only: a launched star hit an enemy: it arcs back to the player (SolveArc) so the player can bat it again.
        void Bounce()
        {
            Projectile.velocity = SolveArc(Main.player[Projectile.owner]);
            Projectile.ai[2] = (float)Mode.Arc;
            Projectile.damage = Math.Max(1, (int)(WeaponDamage * StarfuryTuning.CalledStarDamageMultiplier));
            Projectile.knockBack = WeaponKnockback * StarfuryTuning.CalledStarKnockbackMultiplier;
            Projectile.timeLeft = StarfuryTuning.BouncedLifeTicks;
            noBatTicks = StarfuryTuning.BatGraceTicks;
            Projectile.netUpdate = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = StarfuryTuning.StarSize;
            Projectile.height = StarfuryTuning.StarSize;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1; // the star decides itself when a hit ends it (OnHitNPC)
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = StarfuryTuning.BounceImmunityTicks;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = StarfuryTuning.StarLifeTicks;
        }

        public override void AI()
        {
            // Notice a bat or a bounce the owner decided (it arrives through ai[] and netUpdate), so every machine plays the same effect.
            if (lastGeneration < 0)
            {
                lastGeneration = Generation;
                lastMode = (int)CurrentMode;
            }
            else
            {
                if (Generation != lastGeneration)
                {
                    lastGeneration = Generation;
                    StarfuryFx.BatBurst(Projectile.Center, Generation);
                    StarfuryFx.PlayBat(Projectile.Center, Generation);
                }
                if ((int)CurrentMode != lastMode)
                {
                    if (lastMode == (int)Mode.Launched && (CurrentMode == Mode.Parabolic || CurrentMode == Mode.Arc))
                    {
                        StarfuryFx.BounceBurst(Projectile.Center, Generation);
                        StarfuryFx.PlayBounce(Projectile.Center);
                    }
                    lastMode = (int)CurrentMode;
                }
            }
            if (noBatTicks > 0)
                noBatTicks--;
            if (hitGraceTicks > 0)
                hitGraceTicks--;

            switch (CurrentMode)
            {
                case Mode.Straight:
                    if (Projectile.ai[1] > 0f)
                        Projectile.ai[1]--; // every machine counts the same, so every machine switches block collision on the same tick
                    Projectile.tileCollide = Projectile.ai[1] <= 0f;
                    break;
                case Mode.Parabolic:
                    Projectile.tileCollide = true;
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + StarfuryTuning.Gravity, StarfuryTuning.MaxFallSpeed);
                    break;
                case Mode.Arc:
                    Projectile.tileCollide = true;
                    Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + StarfuryTuning.Gravity, StarfuryTuning.ArcMaxFallSpeed);
                    break;
                default:
                    Projectile.tileCollide = true;
                    break;
            }

            Projectile.rotation += StarfuryTuning.StarSpin * (Projectile.velocity.X >= 0f ? 1f : -1f);
            StarfuryFx.StarTrail(Projectile.Center, Projectile.velocity, Generation);
            Lighting.AddLight(Projectile.Center, StarfuryTuning.GenerationColor(Generation).ToVector3() * 0.9f);
        }

        // Owner only: a star born at a melee hit. It cannot be batted for a moment (so the same swing does not bat it at once), it cannot hit
        // any enemy for a few ticks, and it cannot hit the enemy it came out of for longer: it is born inside that enemy's hitbox, and any
        // hit ends a star that is not launched, so without this it ended on its first tick.
        public void StartGrace(NPC origin)
        {
            noBatTicks = StarfuryTuning.BatGraceTicks;
            hitGraceTicks = StarfuryTuning.MeleeStarHitGraceTicks;
            if (origin != null && origin.whoAmI >= 0 && origin.whoAmI < Projectile.localNPCImmunity.Length)
                Projectile.localNPCImmunity[origin.whoAmI] = StarfuryTuning.MeleeStarOriginImmunityTicks;
        }

        public override bool? CanHitNPC(NPC target)
        {
            return hitGraceTicks > 0 ? (bool?)false : null;
        }

        // Owner only (OnHitNPC only runs on the owner's machine). A launched star bounces, unless it has had its last bat: that one
        // explodes on the enemy (the finisher). Every other star just ends.
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (CurrentMode == Mode.Launched && Generation < StarfuryTuning.MaxBats)
            {
                Bounce();
                return;
            }
            if (CurrentMode == Mode.Launched)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<StarfuryBlastProjectile>(),
                    Math.Max(1, (int)(WeaponDamage * StarfuryTuning.BlastDamageMultiplier)), WeaponKnockback * StarfuryTuning.BlastKnockbackMultiplier, Projectile.owner);
            }
            Projectile.Kill();
        }

        // One afterimage per tick (visual only); OnKill hands the trail to DetachedTrails, so the leftovers fade out on their own.
        public override void PostAI()
        {
            if (Main.dedServ || !Projectile.active)
                return;
            afterimages ??= AfterimageTrail.Sprite(TextureAssets.Projectile[Type].Value, StarfuryTuning.AfterimageCount, 0.5f);
            afterimages.Tick();
            afterimages.Add(Projectile.Center, Projectile.rotation, Projectile.scale * Look, StarfuryTuning.GenerationColor(Generation) * 0.7f);
        }

        public override void OnKill(int timeLeft)
        {
            StarfuryFx.StarImpact(Projectile.Center, Generation);
            StarfuryFx.PlayStarImpact(Projectile.Center);
            DetachedTrails.Hand(afterimages);
            afterimages = null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            afterimages?.Draw();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f; // a single frame
            Vector2 position = Projectile.Center - Main.screenPosition;
            Color generationColor = StarfuryTuning.GenerationColor(Generation);
            Color body = Color.Lerp(Color.White, new Color(generationColor.R, generationColor.G, generationColor.B, 255), 0.5f);
            float scale = Projectile.scale * Look;
            Main.EntitySpriteDraw(texture, position, null, generationColor * 0.6f, Projectile.rotation, origin, scale * 1.35f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, null, body, Projectile.rotation, origin, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}