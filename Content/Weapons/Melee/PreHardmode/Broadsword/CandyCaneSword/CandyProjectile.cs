using System;
using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.CandyCaneSword
{
    /// <summary>
    /// One candy (all kinds are this one projectile). ai[0] = the kind (CandyKind), ai[1] = the style (0..9: row * 5 + column inside the kind's two rows
    /// of Candy.png), ai[2] = 0 normal, 1 = "break now" (the owner pushed it out because of the cap; every machine sees it and breaks it).
    /// Red: falls and goes off in a small AoE (CandyBlastProjectile) when it touches a block. Green: waits on the ground, drifts to a player and heals
    /// him 5. Yellow: the same, but gives Sugar Rush (+2 s each, max 10 s). Every candy breaks when its lifetime runs out (it blinks first). Blue comes in patch 2.
    /// MULTIPLAYER: the owner's client decides what spawns (kind, style, velocity, the cap). Every machine runs the same physics and the same timers
    /// (a bounce or settle is corrected by the owner's netUpdate) and draws the same look from ai. ANY player can pick up a green or yellow candy:
    /// that player's client asks the server (CandyPickup.Request), the server claims the candy once and tells everybody (CandyPickup grant packet), and only
    /// the picker's machine applies the heal or the buff. The red explosion's damage is a projectile only the owner's client spawns.
    /// </summary>
    public class CandyProjectile : ModProjectile
    {
        // A placeholder so the mod loads; the art is Candy.png (see CandyCaneSwordFx.SheetPath), drawn from PreDraw.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

        Asset<Texture2D> sheet;
        bool sheetTried;
        bool started;
        float spin;
        int lastRequestAge = -1000;

        /// <summary>Ticks this machine has seen the candy alive (the owner's copy is the reference for "oldest").</summary>
        public int Age;

        /// <summary>The server (or singleplayer) has given this candy to a player; nobody else can take it.</summary>
        public bool Claimed;

        public CandyKind Kind => (CandyKind)Math.Clamp((int)Projectile.ai[0], 0, 3);

        int Style => Math.Max(0, (int)Projectile.ai[1]) % Math.Max(1, CandyCaneSwordTuning.StyleCount);

        // A number from 0 to 1 that is the same on every machine for this candy (the start rotation and spin are drawn from it).
        float Seed => ((Projectile.identity * 7919 + Projectile.owner * 104729) & 1023) / 1023f;

        public override void SetDefaults()
        {
            Projectile.width = CandyCaneSwordTuning.HitboxSize;
            Projectile.height = CandyCaneSwordTuning.HitboxSize;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 3600;
            Projectile.DamageType = DamageClass.Melee;
        }

        // ---- spawning (owner's client) ----

        // The owner has too many candies: the oldest ones break so that 'extra' more fit.
        public static void EnforceCap(Player owner, int extra)
        {
            int type = ModContent.ProjectileType<CandyProjectile>();
            while (true)
            {
                int count = 0;
                Projectile oldest = null;
                int oldestAge = -1;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || p.type != type || p.owner != owner.whoAmI || p.ai[2] != 0f)
                        continue;
                    count++;
                    int age = p.ModProjectile is CandyProjectile candy ? candy.Age : 0;
                    if (age > oldestAge)
                    {
                        oldestAge = age;
                        oldest = p;
                    }
                }
                if (count + extra <= CandyCaneSwordTuning.MaxCandies || oldest == null)
                    break;
                oldest.ai[2] = 1f; // pushed out: every machine sees it and breaks it
                oldest.netUpdate = true;
            }
        }

        static void Spawn(IEntitySource source, Player owner, Vector2 position, Vector2 velocity, int damage)
        {
            CandyKind kind = CandyKinds.Pick();
            int style = Main.rand.Next(Math.Max(1, CandyCaneSwordTuning.StyleCount));
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<CandyProjectile>(), damage, 0f, owner.whoAmI, (float)kind, style, 0f);
        }

        // Owner's client: one candy pops up out of the enemy.
        public static void SpawnOne(IEntitySource source, Player owner, Vector2 position, int damage)
        {
            EnforceCap(owner, 1);
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-CandyCaneSwordTuning.PopSideSpeed, CandyCaneSwordTuning.PopSideSpeed),
                -Main.rand.NextFloat(CandyCaneSwordTuning.PopSpeedMin, CandyCaneSwordTuning.PopSpeedMax));
            Spawn(source, owner, position, velocity, damage);
        }

        // Owner's client: the enemy died to the sword; a fan of candies shoots upward.
        public static void SpawnBurst(IEntitySource source, Player owner, Vector2 position, int damage)
        {
            int count = Main.rand.Next(CandyCaneSwordTuning.BurstMin, Math.Max(CandyCaneSwordTuning.BurstMin, CandyCaneSwordTuning.BurstMax) + 1);
            EnforceCap(owner, count);
            float spread = MathHelper.ToRadians(CandyCaneSwordTuning.BurstSpreadDeg);
            for (int i = 0; i < count; i++)
            {
                float angle = -MathHelper.PiOver2 + Main.rand.NextFloat(-spread, spread);
                float speed = Main.rand.NextFloat(CandyCaneSwordTuning.BurstSpeedMin, CandyCaneSwordTuning.BurstSpeedMax);
                Spawn(source, owner, position, angle.ToRotationVector2() * speed, damage);
            }
            CandyCaneSwordFx.KillBurst(position);
        }

        // ---- pickup rules (every machine; the server asks them again before it believes a request) ----

        public static bool CanPick(CandyKind kind, Player player)
        {
            if (player == null || !player.active || player.dead)
                return false;
            if (kind == CandyKind.Yellow)
                return true;
            return kind == CandyKind.Green && (!CandyCaneSwordTuning.HealOnlyWhenHurt || player.statLife < player.statLifeMax2);
        }

        // The server asks this before it claims a candy for a player.
        public bool CanBeClaimedBy(Player player)
        {
            return !Claimed && Projectile.ai[2] == 0f && CandyKinds.IsPickup(Kind) && CanPick(Kind, player);
        }

        Player NearestPicker(CandyKind kind)
        {
            Player best = null;
            float bestDistance = CandyCaneSwordTuning.MagnetRange;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!CanPick(kind, player))
                    continue;
                float distance = Vector2.Distance(player.Center, Projectile.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = player;
                }
            }
            return best;
        }

        // The local player's client: touching a green or yellow candy asks the server for it (repeats now and then until the answer comes).
        void TryRequest(CandyKind kind)
        {
            Player me = Main.LocalPlayer;
            if (!CanPick(kind, me) || Age - lastRequestAge < CandyCaneSwordTuning.RequestRetryTicks)
                return;
            Rectangle box = me.Hitbox;
            box.Inflate(CandyCaneSwordTuning.PickupRange, CandyCaneSwordTuning.PickupRange);
            if (!box.Intersects(Projectile.Hitbox))
                return;
            lastRequestAge = Age;
            CandyPickup.Request(Projectile);
        }

        // ---- every machine ----

        void Start()
        {
            started = true;
            Projectile.timeLeft = CandyKinds.LifeTicks(Kind) + 120; // our own timer (Age) ends it first
            float seed = Seed;
            Projectile.rotation = seed * MathHelper.TwoPi;
            float fraction = (seed * 7.3f) % 1f;
            spin = MathHelper.Lerp(CandyCaneSwordTuning.SpinMin, CandyCaneSwordTuning.SpinMax, fraction) * (seed < 0.5f ? -1f : 1f);
            CandyCaneSwordFx.Spawn(Projectile.Center, Kind);
        }

        public override void AI()
        {
            if (!started)
                Start();
            Age++;
            CandyKind kind = Kind;
            if (Projectile.ai[2] != 0f || Age >= CandyKinds.LifeTicks(kind))
            {
                CandyCaneSwordFx.Break(Projectile.Center, kind);
                Projectile.Kill();
                return;
            }

            bool magnet = false;
            if (CandyKinds.IsPickup(kind) && Age >= CandyCaneSwordTuning.PickupDelayTicks && !Claimed)
            {
                Player nearest = NearestPicker(kind);
                if (nearest != null)
                {
                    magnet = true;
                    Vector2 to = nearest.Center - Projectile.Center;
                    float distance = to.Length();
                    if (distance > 1f)
                        Projectile.velocity += to / distance * CandyCaneSwordTuning.MagnetAccel;
                    Projectile.velocity *= CandyCaneSwordTuning.MagnetDamp;
                    if (Projectile.velocity.Length() > CandyCaneSwordTuning.MagnetMaxSpeed)
                        Projectile.velocity = Vector2.Normalize(Projectile.velocity) * CandyCaneSwordTuning.MagnetMaxSpeed;
                }
                if (Main.netMode != NetmodeID.Server)
                    TryRequest(kind);
            }

            if (!magnet)
            {
                Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + CandyCaneSwordTuning.Gravity, CandyCaneSwordTuning.MaxFallSpeed);
                Projectile.velocity.X *= CandyCaneSwordTuning.AirDrag;
            }

            bool onGround = Collision.SolidCollision(Projectile.position + new Vector2(0f, Projectile.height), Projectile.width, 2);
            if (onGround && !magnet)
            {
                Projectile.velocity.X *= CandyCaneSwordTuning.GroundFriction;
                if (Math.Abs(Projectile.velocity.X) < 0.05f)
                    Projectile.velocity.X = 0f;
                spin *= CandyCaneSwordTuning.GroundSpinKeep;
            }
            Projectile.rotation += spin;

            if (!Main.dedServ)
            {
                Color tint = CandyKinds.Tint(kind);
                Lighting.AddLight(Projectile.Center, tint.ToVector3() * CandyCaneSwordTuning.LightStrength);
                if (Main.rand.NextBool(24))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), CandyKinds.DustOf(kind), Vector2.Zero, 0, default, 0.7f);
                    dust.noGravity = true;
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Kind == CandyKind.Red)
            {
                Explode();
                return false;
            }

            bool bounced = false;
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * CandyCaneSwordTuning.BounceKeep;
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y * CandyCaneSwordTuning.BounceKeep;
                if (Math.Abs(Projectile.velocity.Y) < CandyCaneSwordTuning.SettleSpeed)
                    Projectile.velocity.Y = 0f;
                else
                    bounced = true;
            }
            if (bounced && Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true; // the other machines catch up with the real position
            return false;
        }

        // Every machine plays the explosion; only the owner's client spawns the damage.
        void Explode()
        {
            Vector2 center = Projectile.Center;
            CandyCaneSwordFx.RedExplosion(center);
            if (Projectile.owner == Main.myPlayer)
            {
                int damage = (int)(Projectile.damage * CandyCaneSwordTuning.RedBlastDamageMultiplier);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero, ModContent.ProjectileType<CandyBlastProjectile>(),
                    damage, CandyCaneSwordTuning.RedBlastKnockback, Projectile.owner);
            }
            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CandyKind kind = Kind;
            int left = CandyKinds.LifeTicks(kind) - Age;
            float alpha = 1f;
            if (left > 0 && left < CandyCaneSwordTuning.BlinkTicks && (left / 4) % 2 == 0)
                alpha = 0.35f; // about to break
            float grow = Math.Min(1f, (Age + 1) / (float)Math.Max(1, CandyCaneSwordTuning.PopInTicks));
            float scale = 0.5f + 0.5f * grow;
            Vector2 position = Projectile.Center - Main.screenPosition;

            if (!sheetTried)
            {
                sheetTried = true;
                ModContent.RequestIfExists<Texture2D>(CandyCaneSwordFx.SheetPath, out sheet, AssetRequestMode.ImmediateLoad);
            }

            if (sheet != null && sheet.IsLoaded)
            {
                int cell = CandyCaneSwordTuning.CellSize;
                int style = Style;
                int column = style % Math.Max(1, CandyCaneSwordTuning.Columns);
                int row = (int)kind * 2 + style / Math.Max(1, CandyCaneSwordTuning.Columns);
                Rectangle source = new Rectangle(column * cell, row * cell, cell, cell);
                Color color = Color.Lerp(lightColor, Color.White, 0.6f) * alpha;
                Main.spriteBatch.Draw(sheet.Value, position, source, color, Projectile.rotation, source.Size() / 2f, CandyCaneSwordTuning.DrawScale * scale, SpriteEffects.None, 0f);
            }
            else
            {
                float size = CandyCaneSwordTuning.FallbackSize * scale;
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, position, new Rectangle(0, 0, 1, 1), CandyKinds.Tint(kind) * alpha, Projectile.rotation,
                    new Vector2(0.5f), new Vector2(size), SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}