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
    /// of Candy.png), ai[2] = 0 normal, 1 = "break now" (the owner pushed it out because of the cap or the enemy it hung on was pushed out; every machine sees it and breaks it), 2 = blue stuck to a block, 3 + n = blue stuck to enemy n (the velocity then holds the offset from the enemy's center).
    /// Red: falls and goes off in a small AoE (CandyBlastProjectile) when it touches a block. Green: waits on the ground, drifts to a player and heals
    /// him 5. Yellow: the same, but gives Sugar Rush (+2 s each, max 10 s). Blue: sticks to the first enemy it touches (after a short delay) or to a block, and an enemy that touches a block-stuck one takes it; stuck to an enemy it hurts it every BlueHitIntervalTicks for BlueDamageMultiplier of the sword's damage (up to BlueMaxPerEnemy per player on one enemy, the oldest comes off). Every candy breaks when its lifetime runs out.
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
        int stuckNpcType = -1;
        int stuckWait;
        int hurtTimer;
        bool stuckSeen;

        /// <summary>Ticks this machine has seen the candy alive (the owner's copy is the reference for "oldest").</summary>
        public int Age;

        /// <summary>The server (or singleplayer) has given this candy to a player; nobody else can take it.</summary>
        public bool Claimed;

        /// <summary>Owner's machine only: the enemy this candy popped out of (-1 = none). A blue candy ignores it for BlueSourceIgnoreTicks so it does not stick right back.</summary>
        public int SourceNpc = -1;

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
                    if (!p.active || p.type != type || p.owner != owner.whoAmI || p.ai[2] == 1f)
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

        static void Spawn(IEntitySource source, Player owner, Vector2 position, Vector2 velocity, int damage, CandyKind kind, int sourceNpc = -1)
        {
            int style = Main.rand.Next(Math.Max(1, CandyCaneSwordTuning.StyleCount));
            int index = Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<CandyProjectile>(), damage, 0f, owner.whoAmI, (float)kind, style, 0f);
            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is CandyProjectile candy)
                candy.SourceNpc = sourceNpc;
        }

        // Owner's client: one candy of color 'kind' pops up out of the enemy.
        public static void SpawnOne(IEntitySource source, Player owner, Vector2 position, int damage, CandyKind kind, int sourceNpc)
        {
            EnforceCap(owner, 1);
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-CandyCaneSwordTuning.PopSideSpeed, CandyCaneSwordTuning.PopSideSpeed),
                -Main.rand.NextFloat(CandyCaneSwordTuning.PopSpeedMin, CandyCaneSwordTuning.PopSpeedMax));
            Spawn(source, owner, position, velocity, damage, kind, sourceNpc);
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
                Spawn(source, owner, position, angle.ToRotationVector2() * speed, damage, CandyKinds.Pick(owner));
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

        // ---- blue candy ----

        int State => (int)Projectile.ai[2];

        // Stuck to a block (2) or to an enemy (3 and up): the candy does not move by itself any more.
        bool Stuck => Projectile.ai[2] >= 2f;

        public override bool ShouldUpdatePosition()
        {
            return !Stuck;
        }

        void BreakNow()
        {
            CandyCaneSwordFx.Break(Projectile.Center, Kind);
            Projectile.Kill();
        }

        // Every machine, every tick of a blue candy. True when this tick is handled (stuck, or gone), false for the normal flight.
        bool StepBlue()
        {
            int state = State;
            if (state < 2)
            {
                // Free flight: the owner's client sticks it to an enemy it touches once the pop is over (a block sticks it from OnTileCollide).
                if (Projectile.owner == Main.myPlayer && Age >= CandyCaneSwordTuning.BlueStickDelayTicks)
                    TryAttach();
                return false;
            }

            Projectile.tileCollide = false;
            if (!stuckSeen)
            {
                stuckSeen = true;
                hurtTimer = Math.Max(1, CandyCaneSwordTuning.BlueHitIntervalTicks);
                CandyCaneSwordFx.BlueStick(Projectile.Center);
            }

            if (state >= 3)
            {
                int index = state - 3;
                NPC npc = index < Main.maxNPCs ? Main.npc[index] : null;
                if (npc == null)
                {
                    BreakNow();
                    return true;
                }
                if (stuckNpcType < 0)
                {
                    // The enemy had not reached this machine yet: wait for it a moment.
                    if (npc.active)
                        stuckNpcType = npc.type;
                    else if (++stuckWait > 30)
                        BreakNow();
                    return true;
                }
                if (!npc.active || npc.life <= 0 || npc.type != stuckNpcType)
                {
                    BreakNow(); // the enemy died (or its slot was taken by another)
                    return true;
                }

                Projectile.Center = npc.Center + Projectile.velocity + new Vector2(0f, npc.gfxOffY);
                Tick(npc);
            }
            else if (Projectile.owner == Main.myPlayer)
            {
                TryAttach(); // an enemy that touches a block-stuck candy takes it
            }

            StuckGlow();
            return true;
        }

        // Every machine counts the interval and shows the tick; only the owner's client strikes (SimpleStrikeNPC syncs itself).
        void Tick(NPC npc)
        {
            if (--hurtTimer > 0)
                return;
            hurtTimer = Math.Max(1, CandyCaneSwordTuning.BlueHitIntervalTicks);
            CandyCaneSwordFx.BlueTick(Projectile.Center);
            if (Projectile.owner != Main.myPlayer || npc.dontTakeDamage) // immortal enemies (target dummies) are hit too
                return;
            int damage = Math.Max(1, (int)(Projectile.damage * CandyCaneSwordTuning.BlueDamageMultiplier));
            int direction = npc.Center.X > Main.player[Projectile.owner].Center.X ? 1 : -1;
            npc.SimpleStrikeNPC(damage, direction, false, 0f, DamageClass.Melee);
        }

        void StuckGlow()
        {
            if (Main.dedServ)
                return;
            Color tint = CandyKinds.Tint(CandyKind.Blue);
            Lighting.AddLight(Projectile.Center, tint.ToVector3() * CandyCaneSwordTuning.LightStrength);
            if (Main.rand.NextBool(24))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), CandyKinds.DustOf(CandyKind.Blue), Vector2.Zero, 0, default, 0.7f);
                dust.noGravity = true;
            }
        }

        // Owner's client: a free or block-stuck blue candy touches an enemy and sticks to it.
        void TryAttach()
        {
            int reach = (int)CandyCaneSwordTuning.BlueStickReach;
            Rectangle box = Projectile.Hitbox;
            box.Inflate(reach, reach);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (i == SourceNpc && Age < CandyCaneSwordTuning.BlueSourceIgnoreTicks)
                    continue; // still ignoring the enemy it popped out of
                if (!npc.active || npc.life <= 0 || npc.friendly || (!npc.CanBeChasedBy() && npc.type != NPCID.TargetDummy) || !box.Intersects(npc.Hitbox))
                    continue;
                AttachTo(npc);
                return;
            }
        }

        // Owner's client: hang on 'npc'. The owner may only have BlueMaxPerEnemy on one enemy: the oldest comes off first.
        void AttachTo(NPC npc)
        {
            int type = ModContent.ProjectileType<CandyProjectile>();
            int attached = 3 + npc.whoAmI;
            while (true)
            {
                int count = 0;
                Projectile oldest = null;
                int oldestAge = -1;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || p.type != type || p.owner != Projectile.owner || (int)p.ai[2] != attached)
                        continue;
                    count++;
                    int age = p.ModProjectile is CandyProjectile candy ? candy.Age : 0;
                    if (age > oldestAge)
                    {
                        oldestAge = age;
                        oldest = p;
                    }
                }
                if (count < CandyCaneSwordTuning.BlueMaxPerEnemy || oldest == null)
                    break;
                oldest.ai[2] = 1f; // pushed off: every machine sees it and breaks it
                oldest.netUpdate = true;
            }

            Projectile.ai[2] = attached;
            Projectile.velocity = Projectile.Center - npc.Center; // the offset rides along; the candy never moves by itself again
            stuckNpcType = npc.type;
            stuckWait = 0;
            hurtTimer = Math.Max(1, CandyCaneSwordTuning.BlueHitIntervalTicks);
            if (!stuckSeen)
            {
                stuckSeen = true;
                CandyCaneSwordFx.BlueStick(Projectile.Center);
            }
            Projectile.netUpdate = true;
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
            if (Projectile.ai[2] == 1f || Age >= CandyKinds.LifeTicks(kind))
            {
                CandyCaneSwordFx.Break(Projectile.Center, kind);
                Projectile.Kill();
                return;
            }
            if (kind == CandyKind.Blue && StepBlue())
                return;

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
            if (Kind == CandyKind.Blue)
            {
                SnapToTile(oldVelocity);
                Projectile.ai[2] = 2f; // stuck to a block (every machine does the same; the owner's netUpdate settles the exact spot)
                Projectile.velocity = Vector2.Zero;
                if (Projectile.owner == Main.myPlayer)
                    Projectile.netUpdate = true;
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

        // The tick a blue candy touches a block it can still be a few pixels away from it: a falling candy covers only the part of its speed the block let
        // through, and zeroing the speed when it sticks would throw that part away (the candy would hang in the air). So close the gap exactly: along every
        // axis the block stopped, move by what Collision.TileCollision allows from where the candy is now (nothing if it already touches the block).
        void SnapToTile(Vector2 oldVelocity)
        {
            int width = Projectile.width;
            int height = Projectile.height;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.position += Collision.TileCollision(Projectile.position, new Vector2(0f, oldVelocity.Y), width, height, false, false, 1);
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.position += Collision.TileCollision(Projectile.position, new Vector2(oldVelocity.X, 0f), width, height, false, false, 1);
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
            float alpha = 1f;
            float grow = Math.Min(1f, (Age + 1) / (float)Math.Max(1, CandyCaneSwordTuning.PopInTicks));
            float scale = 0.5f + 0.5f * grow;
            Vector2 position = Projectile.Center - Main.screenPosition;

            if (!sheetTried)
            {
                sheetTried = true;
                ModContent.RequestIfExists<Texture2D>(CandyCaneSwordFx.SheetPath, out sheet, AssetRequestMode.ImmediateLoad);
            }

            Texture2D purple = CandyCaneSwordPurple.CandySheet();   // the purple seed: same kind and behavior, other art
            if (purple != null)
            {
                Rectangle purpleSource = CandyCaneSwordPurple.Cell(kind, Style);
                Color purpleColor = Color.Lerp(lightColor, Color.White, 0.6f) * alpha;
                Main.spriteBatch.Draw(purple, position, purpleSource, purpleColor, Projectile.rotation, purpleSource.Size() / 2f, CandyCaneSwordTuning.DrawScale * scale, SpriteEffects.None, 0f);
            }
            else if (sheet != null && sheet.IsLoaded)
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
            if (CandyCaneSwordTuning.ShowHitboxDebug)
                DrawHitboxDebug(position);
            return false;
        }

        // CandyCaneSwordTuning.ShowHitboxDebug: a magenta circle on the hitbox (radius = half of its size) and a dot on the center.
        void DrawHitboxDebug(Vector2 center)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float radius = Projectile.width * 0.5f;
            int dots = Math.Max(16, (int)(radius * 1.5f));
            for (int i = 0; i < dots; i++)
            {
                Vector2 point = center + (MathHelper.TwoPi * i / dots).ToRotationVector2() * radius;
                Main.spriteBatch.Draw(pixel, point, new Rectangle(0, 0, 1, 1), Color.Magenta, 0f, new Vector2(0.5f), new Vector2(2f), SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(pixel, center, new Rectangle(0, 0, 1, 1), Color.Magenta, 0f, new Vector2(0.5f), new Vector2(3f), SpriteEffects.None, 0f);
        }
    }
}