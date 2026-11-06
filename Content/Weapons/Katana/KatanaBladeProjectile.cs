using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Katana
{
    /// <summary>
    /// The thrown Katana blade. The owner presses the button and the blade flies out along the aim line (captured at the press: no
    /// steering) while the button stays down; the longer it is held the farther it goes, up to MaxRangeTiles after MaxChargeTicks.
    /// The dash starts when the button comes up, when the blade reaches the max range, or the moment it hits a tile (it sticks
    /// edge-first for a moment): the player flies toward the blade at DashSpeed through the shared PlayerDash (invincible while he
    /// travels, stopped or slid along by a tile in his way), the cooldown starts, a KatanaSlashProjectile cuts the line he travels, and a
    /// new stance starts.
    /// A blade that bit into a tile stays in it for EmbedStayTicks. If the tile is mined away (every machine checks the tile it sees)
    /// the blade pops loose, tumbles, falls and bounces on whatever it lands on, then fades: a purely visual drop, computed on every
    /// machine by itself, nothing synced. The owner's client ends the projectile when the show is over. Throwing again ends it too.
    /// MULTIPLAYER: the owner's client decides everything and spawns it with ai[0] = the aim (player's own frame), ai[1] = how far the
    /// blade can fly at most (the tile raycast, capped at the max range), ai[2] = -1 while flying in open air, -2 while its reach ends
    /// in a tile, and the distance it stopped at once released (that number is the one thing that changes, with netUpdate). Every
    /// machine places the blade as a pure function of the ticks it has run it, so no position is synced. The dash runs on the owner's
    /// client only (PlayerDash); vanilla syncs the player's position and velocity. Damage comes from the slash projectile.
    /// </summary>
    public class KatanaBladeProjectile : ModProjectile
    {
        // The vanilla item sprite is the projectile sprite (no art needed).
        public override string Texture => "Terraria/Images/Item_" + ItemID.Katana;

        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        float Aim => Projectile.ai[0];
        float Reach => Math.Max(0f, Projectile.ai[1]);
        bool Released => Projectile.ai[2] >= 0f;
        int Facing => HeldAim.Facing(Aim);
        Vector2 Direction => haveDirection ? cachedDirection : HeldAim.ToWorldDirection(Owner, Aim); // cached on the first tick, so a flipped gravity cannot turn a stuck blade

        // Per-machine state: counters and visuals, never synced.
        bool initialized;
        bool haveDirection;
        Vector2 cachedDirection;
        int ticks;
        Vector2 origin;            // where the throw started (the player's center at the press)
        bool endsInTile;
        bool embedded;
        float embedShake;
        float lastDistance;
        bool scaleSet;
        int stayTicks;             // ticks the blade has sat in its tile
        bool dropped;              // the tile is gone: the blade falls (a visual simulation, per machine)
        Vector2 dropCenter;
        Vector2 dropVelocity;
        float dropAngle;
        float dropSpin;
        int dropTicks;
        int dropBounces;
        int landFade = -1;         // -1 = still falling, otherwise ticks since it lay still
        bool releaseSeen;
        int releaseTicks;
        AfterimageTrail afterimages;
        readonly Vector2[] guidePoints = new Vector2[2];
        readonly float[] guideTs = { 0f, 1f };

        // Called on the owner's client from KatanaGlobalItem.Shoot: captures the aim, measures how far the blade can fly and throws it.
        public static void Throw(Player player, IEntitySource source)
        {
            float aim = HeldAim.Capture(player);
            player.ChangeDir(HeldAim.Facing(aim));
            Vector2 origin = player.MountedCenter;
            float reach = MeasureReach(origin, HeldAim.ToWorldDirection(player, aim), out bool hitsTile);

            KatanaPlayer.KillReleasedBlades(player.whoAmI);
            Projectile stance = KatanaPlayer.Find(player.whoAmI, ModContent.ProjectileType<KatanaStanceProjectile>());
            stance?.Kill();
            Projectile.NewProjectile(source, origin, Vector2.Zero, ModContent.ProjectileType<KatanaBladeProjectile>(), 0, 0f, player.whoAmI, aim, reach, hitsTile ? -2f : -1f);
        }

        // The distance along the line to the first solid tile, or the max range when the way is free. Coarse steps, then a few halvings.
        static float MeasureReach(Vector2 origin, Vector2 direction, out bool hitsTile)
        {
            float max = KatanaTuning.MaxRangeTiles * 16f;
            float step = Math.Max(1f, KatanaTuning.StepPixels);
            Vector2 half = new Vector2(1f, 1f);
            float previous = 0f;
            hitsTile = false;
            for (float at = 0f; ; at += step)
            {
                float distance = Math.Min(at, max);
                if (Collision.SolidCollision(origin + direction * distance - half, 2, 2))
                {
                    float low = previous;
                    float high = distance;
                    for (int i = 0; i < 4; i++)
                    {
                        float middle = (low + high) * 0.5f;
                        if (Collision.SolidCollision(origin + direction * middle - half, 2, 2))
                            high = middle;
                        else
                            low = middle;
                    }
                    hitsTile = true;
                    return low;
                }
                previous = distance;
                if (distance >= max)
                    break;
            }
            return max;
        }


        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = false; // the slash does the damage, never the blade itself
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // ended by the AI, never by this countdown
        }

        // How far the blade has flown after 'heldTicks' ticks of holding: the free flight grows from MinRangePixels to the max range, and
        // a tile (or the reach) stops it.
        float DistanceAt(int heldTicks)
        {
            float t = MathHelper.Clamp(heldTicks / (float)Math.Max(1, KatanaTuning.MaxChargeTicks), 0f, 1f);
            float eased = (float)Math.Pow(t, Math.Max(0.05f, KatanaTuning.ReachEasePower));
            float free = MathHelper.Lerp(KatanaTuning.MinRangePixels, KatanaTuning.MaxRangeTiles * 16f, eased);
            return Math.Min(free, Reach);
        }

        public override void AI()
        {
            Player owner = Owner;
            if (!owner.active || owner.dead || ((owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.Katana) && !Released))
            {
                Projectile.Kill(); // nothing happens: no teleport, no cooldown
                return;
            }

            if (!initialized)
            {
                initialized = true;
                cachedDirection = HeldAim.ToWorldDirection(Owner, Aim);
                haveDirection = true;
                origin = Projectile.Center;
                endsInTile = Projectile.ai[2] <= -1.5f;
                KatanaFx.Throw(origin, Direction);
            }
            if (!scaleSet || !Released) // a released blade keeps its size even when another item is held
            {
                Projectile.scale = KatanaTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);
                scaleSet = true;
            }
            afterimages?.Tick();

            bool released = Released;
            float distance;
            if (!released)
            {
                distance = DistanceAt(ticks + 1);
                if (IsOwner)
                {
                    owner.ChangeDir(Facing);
                    // The button came up, or the blade reached the end of its reach (the max range or a tile): dash.
                    if ((ticks >= 1 && !owner.controlUseItem) || distance >= Reach - 0.01f)
                    {
                        Release(owner, distance);
                        released = true;
                    }
                }
            }
            else
                distance = Projectile.ai[2];

            Vector2 head = origin + Direction * distance;
            Projectile.Center = head;

            if (!released)
            {
                // The arm points along the line for as long as the blade is out.
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Aim - MathHelper.PiOver2);

                if (distance > lastDistance + 0.1f)
                {
                    KatanaFx.FlightDust(origin + Direction * lastDistance, head);
                    if (!Main.dedServ)
                    {
                        EnsureAfterimages();
                        float scale = Projectile.scale;
                        Vector2 hilt = head - Direction * KatanaTuning.BladeLength * scale;
                        afterimages.Add(hilt, Direction.ToRotation(), scale, KatanaTuning.GlowColor);
                    }
                }
                Lighting.AddLight(head, 0.5f, 0.55f, 0.7f);
            }
            else
            {
                if (!releaseSeen)
                {
                    releaseSeen = true;
                    if (endsInTile) // the blade bit into a tile: it stays there, trembling, while it fades (the player is already flying toward it)
                    {
                        embedded = true;
                        embedShake = KatanaTuning.EmbedShake;
                        KatanaFx.Embed(head, Direction);
                        CameraShake.Punch(head, KatanaTuning.EmbedCameraShake, 8);
                    }
                }
                releaseTicks++;
                if (embedded)
                {
                    if (UpdateStuck(head))
                        return;
                }
                else if (IsOwner && releaseTicks >= Math.Max(1, KatanaTuning.VanishTicks))
                {
                    Projectile.Kill();
                    return;
                }
            }
            embedShake *= KatanaTuning.EmbedShakeDecay;

            lastDistance = distance;
            ticks++;
        }

        // A blade stuck in a tile. Every machine looks at the tile it can see: once it is gone the blade drops. The owner's client ends the
        // projectile after the stay (or after the fall). Returns true when the projectile was killed.
        bool UpdateStuck(Vector2 head)
        {
            float scale = Projectile.scale;
            if (!dropped)
            {
                stayTicks++;
                // The probe sits just inside the tile the blade bit into (the contact test of the raycast was the same 2x2 box).
                if (releaseTicks > 1 && !Collision.SolidCollision(head + Direction * 2f - Vector2.One, 2, 2))
                {
                    BeginDrop(head, scale);
                    return false;
                }
                if (IsOwner && stayTicks >= Math.Max(1, KatanaTuning.EmbedStayTicks))
                {
                    Projectile.Kill();
                    return true;
                }
                return false;
            }

            // Falling: gravity, a tumble, and bounces on whatever solid it meets (tested at its lowest point and at its center).
            dropTicks++;
            Vector2 half = new Vector2(2f, 2f);
            if (landFade < 0)
            {
                dropVelocity.Y = Math.Min(dropVelocity.Y + KatanaTuning.DropGravity, KatanaTuning.DropMaxFallSpeed);
                dropAngle += dropSpin;

                Vector2 nextY = dropCenter + new Vector2(0f, dropVelocity.Y);
                if (Collision.SolidCollision(LowPoint(nextY, scale) - Vector2.One, 2, 2))
                {
                    float impact = Math.Abs(dropVelocity.Y);
                    dropBounces++;
                    dropVelocity.Y = -dropVelocity.Y * KatanaTuning.DropBounceKeep;
                    dropVelocity.X *= 0.6f;
                    dropSpin *= -0.5f;
                    KatanaFx.DropLand(LowPoint(dropCenter, scale), impact);
                    if (dropBounces >= KatanaTuning.DropMaxBounces || Math.Abs(dropVelocity.Y) < 1.5f)
                        landFade = 0;
                }
                else
                    dropCenter = nextY;

                Vector2 nextX = dropCenter + new Vector2(dropVelocity.X, 0f);
                if (Collision.SolidCollision(nextX - half, 4, 4))
                    dropVelocity.X = -dropVelocity.X * KatanaTuning.DropBounceKeep;
                else
                    dropCenter = nextX;

                if (dropTicks >= KatanaTuning.DropMaxTicks)
                    landFade = 0;
            }
            else
            {
                dropVelocity = Vector2.Zero;
                landFade++;
                if (IsOwner && landFade >= Math.Max(1, KatanaTuning.DropFadeTicks))
                {
                    Projectile.Kill();
                    return true;
                }
            }
            Projectile.Center = dropCenter;
            return false;
        }

        // Where the blade lets go: from the middle of the sprite as drawn, popping out of the wall backward, spinning away.
        void BeginDrop(Vector2 head, float scale)
        {
            dropped = true;
            Vector2 direction = Direction;
            Vector2 tip = head + direction * (KatanaTuning.SinkDepth * scale);
            dropCenter = tip - direction * (KatanaTuning.BladeLength * scale * 0.5f);
            dropVelocity = -direction * KatanaTuning.DropKick;
            dropAngle = direction.ToRotation();
            dropSpin = KatanaTuning.DropSpin * (Facing >= 0 ? 1f : -1f) * Main.rand.NextFloat(0.8f, 1.2f);
            embedShake = 0f;
            KatanaFx.Drop(tip, direction);
        }

        // The lowest end of the blade (the one that touches the ground first).
        Vector2 LowPoint(Vector2 center, float scale)
        {
            Vector2 offset = dropAngle.ToRotationVector2() * (KatanaTuning.BladeLength * scale * 0.5f);
            return center.Y + offset.Y > center.Y - offset.Y ? center + offset : center - offset;
        }

        // 1 while the stuck blade is solid, fading out at the end of its stay, or after it has landed.
        float StuckFade()
        {
            if (dropped)
                return landFade >= 0 ? 1f - MathHelper.Clamp(landFade / (float)Math.Max(1, KatanaTuning.DropFadeTicks), 0f, 1f) : 1f;
            int fadeTicks = Math.Max(1, KatanaTuning.EmbedFadeTicks);
            return MathHelper.Clamp((KatanaTuning.EmbedStayTicks - stayTicks) / (float)fadeTicks, 0f, 1f);
        }

        void EnsureAfterimages()
        {
            if (afterimages != null)
                return;
            afterimages = new AfterimageTrail(KatanaTuning.AfterimageTicks, (image, age) =>
            {
                Texture2D texture = TextureAssets.Projectile[Type].Value;
                HeldSwordPose.Sprite(texture, 1, out Vector2 spriteOrigin, out float rotationOffset, out SpriteEffects effects);
                Main.EntitySpriteDraw(texture, image.Position - Main.screenPosition, null, image.Color * (1f - age), image.Rotation + rotationOffset, spriteOrigin, image.Scale, effects, 0);
            });
        }

        // Owner's client: the dash. The blade's state is synced, then the player starts flying toward the blade (PlayerDash: speed set
        // every tick, no gravity, no fall damage, invincible for the whole trip and a short while after), the cooldown starts, and the
        // slash that cuts the line he travels is spawned.
        void Release(Player owner, float distance)
        {
            Vector2 start = owner.Center;
            Vector2 travel = origin + Direction * distance - start;
            float length = travel.Length();

            Projectile.ai[2] = distance; // released: the one synced change of this projectile
            Projectile.netUpdate = true;

            Item item = owner.HeldItem;
            IEntitySource source = owner.GetSource_ItemUse(item);

            if (length >= Math.Max(1f, KatanaTuning.MinDashPixels))
            {
                float speed = Math.Max(1f, KatanaTuning.DashSpeed);
                int dashTicks = Math.Max(1, (int)Math.Ceiling(length / speed));
                DashSettings settings = new DashSettings(speed, dashTicks, false, 0f, 0, 0, dashTicks, KatanaTuning.DashGraze, 0.88f, KatanaTuning.ImmuneTailTicks);
                PlayerDash.Begin(owner, travel / length, settings);
                owner.GetModPlayer<KatanaPlayer>().CooldownTicks = KatanaTuning.CooldownTicks;

                int damage = (int)(owner.GetWeaponDamage(item) * KatanaTuning.SlashDamageMultiplier);
                Projectile.NewProjectile(source, start, Vector2.Zero, ModContent.ProjectileType<KatanaSlashProjectile>(), damage,
                    owner.GetWeaponKnockback(item, item.knockBack), owner.whoAmI, travel.X, travel.Y, dashTicks);
            }

            // The sword is back in the hand: the stance starts from the throw pose and settles into the sheathed one.
            KatanaStanceProjectile.Spawn(source, owner, HeldSwordPose.ToWorld(Facing, Aim), 0f, owner.GetModPlayer<KatanaPlayer>().CooldownTicks);
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            DetachedTrails.Hand(afterimages);
            afterimages = null;
        }

        // ---- drawing ----

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ || !initialized)
                return false;
            afterimages?.Draw();

            Vector2 head = Projectile.Center;
            bool released = Released;
            float vanish = released ? MathHelper.Clamp(releaseTicks / (float)Math.Max(1, KatanaTuning.VanishTicks), 0f, 1f) : 0f;

            // The guide line from where the throw started to the blade: faint glow under a thin bright core, brighter toward the blade.
            float grid = KatanaTuning.PixelSize;
            float fade = (1f - vanish) * (1f - vanish);
            guidePoints[0] = origin;
            guidePoints[1] = head;
            PixelLine.Draw(guidePoints, guideTs, 2, grid, t => grid * 5f, t => KatanaTuning.EdgeColor * (0.25f * fade));
            PixelLine.Draw(guidePoints, guideTs, 2, grid, t => grid, t => KatanaTuning.CoreColor * (0.7f * fade * (0.4f + 0.6f * t)));

            if (released && !embedded)
                return false;

            // The blade, tip first: the tip sits on the head (sunk a little deeper once it is stuck in a tile) and trembles after it bit.
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            HeldSwordPose.Sprite(texture, 1, out Vector2 spriteOrigin, out float rotationOffset, out SpriteEffects effects);
            float scale = Projectile.scale;
            Vector2 direction = dropped ? dropAngle.ToRotationVector2() : Direction;
            float length = KatanaTuning.BladeLength * scale;
            Vector2 hilt;
            if (dropped) // tumbling: the angle turns around the middle of the sword
                hilt = dropCenter - direction * (length * 0.5f) - Main.screenPosition;
            else
            {
                Vector2 tip = head + (embedded ? direction * (KatanaTuning.SinkDepth * scale) : Vector2.Zero) + Main.rand.NextVector2Circular(1f, 1f) * embedShake;
                hilt = tip - direction * length - Main.screenPosition;
            }
            float rotation = direction.ToRotation() + rotationOffset;

            float swordFade = embedded ? StuckFade() : 1f - vanish;
            Main.EntitySpriteDraw(texture, hilt, null, KatanaTuning.EdgeColor * (0.6f * swordFade), rotation, spriteOrigin, scale * 1.12f, effects, 0);
            Main.EntitySpriteDraw(texture, hilt, null, Color.Lerp(lightColor, Color.White, 0.7f) * swordFade, rotation, spriteOrigin, scale, effects, 0);
            return false;
        }
    }
}