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
    /// The dash happens when the button comes up, when the blade reaches the max range, or the moment it hits a tile (it sticks
    /// edge-first for a moment): the player is moved to the blade (the landing is searched in growing circles around it, first toward
    /// the surface normal, until his hitbox fits; if nothing fits within the throw distance he stays where he is), keeps his velocity
    /// plus a push, gets i-frames, the cooldown starts, a KatanaSlashProjectile cuts the line from where he stood to where he landed,
    /// and a new stance starts.
    /// MULTIPLAYER: the owner's client decides everything and spawns it with ai[0] = the aim (player's own frame), ai[1] = how far the
    /// blade can fly at most (the tile raycast, capped at the max range), ai[2] = -1 while flying in open air, -2 while its reach ends
    /// in a tile, and the distance it stopped at once released (that number is the one thing that changes, with netUpdate). Every
    /// machine places the blade as a pure function of the ticks it has run it, so no position is synced. The move itself is the owner
    /// setting his position (no vanilla teleport effects) plus a PlayerControls message. Damage comes from the slash projectile.
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
        Vector2 Direction => HeldAim.ToWorldDirection(Owner, Aim);

        // Per-machine state: counters and visuals, never synced.
        bool initialized;
        int ticks;
        Vector2 origin;            // where the throw started (the player's center at the press)
        bool endsInTile;
        bool embedded;
        float embedShake;
        float lastDistance;
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

        // The direction pointing away from the surface the blade is stuck in: the average direction of the free spots on a small ring
        // around the contact point. Falls back to straight back along the throw.
        static Vector2 SurfaceNormal(Vector2 head, Vector2 direction)
        {
            Vector2 probe = head + direction * 4f;
            Vector2 half = new Vector2(1f, 1f);
            Vector2 sum = Vector2.Zero;
            int samples = 16;
            for (int i = 0; i < samples; i++)
            {
                Vector2 around = (MathHelper.TwoPi * i / samples).ToRotationVector2();
                if (!Collision.SolidCollision(probe + around * 10f - half, 2, 2))
                    sum += around;
            }
            return sum.LengthSquared() > 0.01f ? Vector2.Normalize(sum) : -direction;
        }

        // Where the player can land: the first spot where his hitbox fits, searched in circles around the contact point. Every circle
        // is tried starting toward 'preferred' (the surface normal) and fanning out to both sides; the circle grows until the throw
        // distance. Returns false when nothing fits (the player then stays where he is).
        static bool FindLanding(Player owner, Vector2 head, Vector2 preferred, out Vector2 landing)
        {
            float maxRadius = KatanaTuning.MaxRangeTiles * 16f;
            float step = Math.Max(1f, KatanaTuning.LandingStep);
            int angles = Math.Max(4, KatanaTuning.LandingAngles);
            float baseAngle = preferred.ToRotation();
            for (float radius = 0f; radius <= maxRadius; radius += step)
            {
                for (int a = 0; a < (radius <= 0f ? 1 : angles); a++)
                {
                    int k = (a + 1) / 2;
                    float turn = (a % 2 == 1 ? k : -k) * (MathHelper.TwoPi / angles);
                    Vector2 center = head + (baseAngle + turn).ToRotationVector2() * radius;
                    if (!WorldGen.InWorld((int)(center.X / 16f), (int)(center.Y / 16f), 10))
                        continue;
                    if (!Collision.SolidCollision(center - owner.Size * 0.5f, owner.width, owner.height))
                    {
                        landing = center;
                        return true;
                    }
                }
            }
            landing = head;
            return false;
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
            if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.type != ItemID.Katana)
            {
                Projectile.Kill(); // nothing happens: no teleport, no cooldown
                return;
            }

            if (!initialized)
            {
                initialized = true;
                origin = Projectile.Center;
                endsInTile = Projectile.ai[2] <= -1.5f;
                KatanaFx.Throw(origin, Direction);
            }
            Projectile.scale = KatanaTuning.SwordScale * owner.GetAdjustedItemScale(owner.HeldItem);
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
                    if (endsInTile) // the blade bit into a tile: it stays there, trembling, while it fades
                    {
                        embedded = true;
                        embedShake = KatanaTuning.EmbedShake;
                        KatanaFx.Embed(head, Direction);
                        CameraShake.Punch(head, KatanaTuning.EmbedCameraShake, 8);
                    }
                }
                releaseTicks++;
                if (IsOwner && releaseTicks >= Math.Max(1, KatanaTuning.VanishTicks))
                {
                    Projectile.Kill();
                    return;
                }
            }
            embedShake *= KatanaTuning.EmbedShakeDecay;

            lastDistance = distance;
            ticks++;
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

        // Owner's client: the dash. Everything that decides it happens here, in this order: the blade's state is synced, the landing is
        // found, the player is moved, then the slash that cuts the line is spawned (so the damage happens after he arrived).
        void Release(Player owner, float distance)
        {
            Vector2 start = owner.Center;
            Vector2 head = origin + Direction * distance;
            Vector2 preferred = endsInTile ? SurfaceNormal(head, Direction) : -Direction;
            bool found = FindLanding(owner, head, preferred, out Vector2 landing);

            Projectile.ai[2] = distance; // released: the one synced change of this projectile
            Projectile.netUpdate = true;

            Item item = owner.HeldItem;
            IEntitySource source = owner.GetSource_ItemUse(item);

            if (found)
            {
                // The move: the position is set directly (Player.Teleport would add vanilla teleport dust and sound), and the other
                // machines get the new position with a PlayerControls message right away.
                owner.position = landing - owner.Size * 0.5f;
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, owner.whoAmI);

                // He keeps his speed and gets a push toward where he went; no fall damage from the old height; i-frames; the cooldown starts.
                Vector2 travel = landing - start;
                if (travel.LengthSquared() > 1f)
                    owner.velocity += travel.SafeNormalize(Vector2.Zero) * KatanaTuning.PushSpeed;
                owner.fallStart = (int)(owner.position.Y / 16f);
                Immunity.Grant(owner, KatanaTuning.ImmuneTicks);
                owner.GetModPlayer<KatanaPlayer>().CooldownTicks = KatanaTuning.CooldownTicks;

                // The cut: from where he stood to where he landed.
                if (travel.LengthSquared() >= 4f)
                {
                    int damage = (int)(owner.GetWeaponDamage(item) * KatanaTuning.SlashDamageMultiplier);
                    Projectile.NewProjectile(source, start, Vector2.Zero, ModContent.ProjectileType<KatanaSlashProjectile>(), damage,
                        owner.GetWeaponKnockback(item, item.knockBack), owner.whoAmI, travel.X, travel.Y);
                }
            }

            // The sword is back in the hand: the stance starts from the throw pose and settles into the sheathed one.
            KatanaStanceProjectile.Spawn(source, owner, HeldSwordPose.ToWorld(Facing, Aim), 0f);
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
            Vector2 direction = Direction;
            Vector2 tip = head + (embedded ? direction * (KatanaTuning.SinkDepth * scale) : Vector2.Zero) + Main.rand.NextVector2Circular(1f, 1f) * embedShake;
            Vector2 hilt = tip - direction * KatanaTuning.BladeLength * scale - Main.screenPosition;
            float rotation = direction.ToRotation() + rotationOffset;

            float swordFade = 1f - vanish;
            Main.EntitySpriteDraw(texture, hilt, null, KatanaTuning.EdgeColor * (0.6f * swordFade), rotation, spriteOrigin, scale * 1.12f, effects, 0);
            Main.EntitySpriteDraw(texture, hilt, null, Color.Lerp(lightColor, Color.White, 0.7f) * swordFade, rotation, spriteOrigin, scale, effects, 0);
            return false;
        }
    }
}