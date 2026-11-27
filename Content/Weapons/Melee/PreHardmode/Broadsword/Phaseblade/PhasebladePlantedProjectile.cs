using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.Phaseblade
{
    /// <summary>
    /// A Phaseblade thrown with the right click. Three states, in ai[2]: FLYING (0: it leaves the hand on an arc aimed at the cursor, spins with a
    /// speed that follows its own speed, falls by gravity and sticks in the first ground it reaches), PLANTED (2: it stands upright, its tip
    /// PlantSinkPx into the tile, rocks about its tip for a moment and stays until it is pulled back, or its owner dies or gets very far away)
    /// and RETURNING (1: it pops out, spins and flies to the hand like a boomerang, hitting what it crosses, and is caught; a click while it is
    /// still flying sends it back too). Enemies it touches take a hit (a per-enemy cooldown), flying harder than planted; embers rise from it in
    /// the blade's color. It draws the held item's sprite, so each color looks right.
    /// MULTIPLAYER: the owner's client spawns it (from the throw swing's release tick, with the launch velocity in the spawn packet) and the normal
    /// projectile sync carries it: ai[0] = the Phaseblade's item type (the color), ai[1] = the facing (+1 / -1), ai[2] = the state (the owner
    /// changes it with netUpdate: landing sets 2, a click sets 1). Every machine runs the same flight (gravity, spin, walls, landing detection) on
    /// its own copy from the spawn values (the tiles and the owner's position are synced), so no removal or catch depends on a kill packet, and the
    /// owner's landing packet puts every copy on the same spot. Damage comes from the owner's client alone (OnHitNPC); the other machines get the
    /// hit sparks from RemoteHitTracker. Embers, light, glow, the rocking and the landing and catch effects are per machine. No custom packets.
    /// </summary>
    /// </summary>
    public class PhasebladePlantedProjectile : ModProjectile
    {
        // The projectile's own sprite is only a placeholder: the blade draws the item sprite of the color that was planted.
        public override string Texture => "Terraria/Images/Item_" + ItemID.BluePhaseblade;

        int ItemType => (int)Projectile.ai[0];
        int Facing => Projectile.ai[1] >= 0f ? 1 : -1;
        int State => (int)Projectile.ai[2];   // 0 = falling, 1 = flying back to the hand, 2 = planted
        bool Returning => State == 1;
        bool Thrown => State == 0 && !landed; // in the air after the throw, not stuck yet
        float BladeLengthPx => PhasebladeTuning.BladeLength * PhasebladeTuning.SwordScale; // the flying hitbox is a square, so the blade's length is not Projectile.height there
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        Color BladeColor => PhasebladeTuning.ColorOf(ItemType);

        int ticks;
        float flare; // 1 right after a hit, fades to 0
        bool fell;       // this blade was thrown (so its landing plays the impact effects and it rocks)
        bool landed;     // this machine has seen the blade stand in the ground
        int landedAt;    // the tick it landed on this machine (the rocking counts from it)
        bool launched;   // this machine has started the return flight
        bool caught;     // this machine's copy reached the hand
        readonly RemoteHitTracker remoteHits = new RemoteHitTracker();

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = (int)(PhasebladeTuning.BladeLength * PhasebladeTuning.SwordScale); // the blade standing upright
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000; // refreshed by the AI: it stays until it is pulled back
            Projectile.hide = true;      // drawn by DrawBehind: behind the tiles, so the tile hides the part that sank in
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = PhasebladeTuning.PlantedHitCooldownTicks;
        }

        // ---- helpers (the plant swing and the item use them too) ----

        // The first solid tile (or platform) below the point (x, fromY), looking down 'distance' pixels: false when there is none. The standing plant
        // and the blade that is shot down from the air both use it.
        public static bool FindTileBelow(float x, float fromY, float distance, out Vector2 ground)
        {
            return TileProbe.FindTileBelow(x, fromY, distance, out ground);
        }

        // True when the player has a thrown blade (flying or planted) of this Phaseblade type (one per color: the colors are planted separately).
        public static bool Has(Player player, int itemType)
        {
            int type = ModContent.ProjectileType<PhasebladePlantedProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == player.whoAmI && other.type == type && (int)other.ai[0] == itemType && other.ai[2] != 1f)
                    return true;
            }
            return false;
        }

        // True while a blade of this Phaseblade type is flying back to the hand (not caught yet).
        public static bool IsReturning(Player player, int itemType)
        {
            int type = ModContent.ProjectileType<PhasebladePlantedProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == player.whoAmI && other.type == type && (int)other.ai[0] == itemType && other.ai[2] == 1f)
                    return true;
            }
            return false;
        }

        // Owner only (item use): every blade of the player that is flying or planted leaves for the hand. A LEFT click (thenSwing) also starts the
        // combo as soon as the blade is caught.
        public static void PullBack(Player player, int itemType, bool thenSwing)
        {
            if (thenSwing)
                player.GetModPlayer<PhasebladePlayer>().SwingAfterCatch = true;
            int type = ModContent.ProjectileType<PhasebladePlantedProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == player.whoAmI && other.type == type && (int)other.ai[0] == itemType && other.ai[2] != 1f)
                {
                    other.ai[2] = 1f;
                    other.netUpdate = true;
                }
            }
        }

        // ---- AI ----

        public override void AI()
        {
            Player owner = Owner;
            if (ticks == 0)
            {
                fell = State == 0;
                if (fell)
                    StartThrow();
            }
            if (!owner.active || owner.dead || (State == 2 && Vector2.DistanceSquared(owner.Center, Projectile.Center) > PhasebladeTuning.PlantedMaxDistancePx * PhasebladeTuning.PlantedMaxDistancePx))
            {
                Projectile.Kill();
                return;
            }

            Projectile.spriteDirection = Facing;
            Projectile.localNPCHitCooldown = Returning || Thrown ? PhasebladeTuning.ReturnHitCooldownTicks : PhasebladeTuning.PlantedHitCooldownTicks;
            Color color = BladeColor;

            if (Returning)
            {
                if (!launched)
                    Launch();
                if (Fly(owner))
                    return;
            }
            else
            {
                Projectile.timeLeft = 10000; // it stays until it is pulled back
                if (State == 2 || landed)
                {
                    if (!landed)
                        Land(new Vector2(Projectile.Center.X, Projectile.Bottom.Y - PhasebladeTuning.PlantSinkPx), false); // the owner's sync says it is planted
                    Projectile.velocity = Vector2.Zero;
                    Projectile.rotation = MathHelper.PiOver2; // the blade points down
                }
                else
                {
                    Throw();
                }
                if (Main.rand.NextFloat() < PhasebladeTuning.PlantedEmberChance * (landed ? 1f : 2f))
                {
                    Vector2 along = landed ? Vector2.Lerp(HiltPoint, Projectile.Bottom, Main.rand.NextFloat())
                        : Projectile.Center + Projectile.rotation.ToRotationVector2() * ((Main.rand.NextFloat() - 0.5f) * BladeLengthPx);
                    PhasebladeFx.PlantEmber(along, color);
                }
            }

            Lighting.AddLight(Projectile.Center, color.ToVector3() * (0.6f + 0.4f * flare));

            if (!IsOwner)
                remoteHits.Update(Projectile, ticks / Math.Max(1, Projectile.localNPCHitCooldown), npc => PhasebladeFx.HitSpark(npc.Center, color));

            flare = Math.Max(0f, flare - 0.08f);
            ticks++;
        }

        // FLYING, first tick: the rotation starts along the velocity, and the hitbox is the flying blade's square.
        void StartThrow()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Resize(PhasebladeTuning.ReturnHitboxPx, PhasebladeTuning.ReturnHitboxPx);
        }

        // FLYING: gravity pulls it down, it spins with its speed (so the spin is slowest at the top of the arc and fastest on the way down), a wall
        // or a ceiling stops the sideways or upward part (the blade keeps falling), and the first ground the lowest end of the blade reaches this tick
        // is where it sticks (Land).
        void Throw()
        {
            Vector2 velocity = Projectile.velocity;
            if (velocity.Y < PhasebladeTuning.ThrowMaxFallSpeed)
                velocity.Y = Math.Min(velocity.Y + PhasebladeTuning.ThrowGravity, PhasebladeTuning.ThrowMaxFallSpeed);

            Vector2 center = Projectile.Center;
            if (velocity.X != 0f && !Collision.CanHitLine(center, 1, 1, center + new Vector2(velocity.X, 0f), 1, 1))
                velocity.X *= -0.15f; // a wall: it loses its sideways speed and drops
            if (velocity.Y < 0f && !Collision.CanHitLine(center, 1, 1, center + new Vector2(0f, velocity.Y), 1, 1))
                velocity.Y = 0f;      // a ceiling

            if (velocity.Y > 0f)
            {
                // The lowest point of the spinning blade is half its length times |sin| below its middle.
                float reach = BladeLengthPx * 0.5f * Math.Abs((float)Math.Sin(Projectile.rotation));
                if (FindTileBelow(center.X, center.Y, reach + velocity.Y, out Vector2 ground))
                {
                    Land(ground, true);
                    return;
                }
            }

            Projectile.velocity = velocity;
            float speed = velocity.Length();
            float spin = PhasebladeTuning.ThrowSpinPerSpeed * speed;
            Projectile.rotation += spin * Facing;

            // Sparks off the tips of the spinning blade.
            float half = BladeLengthPx * 0.5f;
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2 * Facing) * (spin * half);
            PhasebladeFx.SpinDust(center + direction * half, velocity + tangent, BladeColor);
            PhasebladeFx.SpinDust(center - direction * half, velocity - tangent, BladeColor);

            if (Projectile.Center.Y > Main.maxTilesY * 16f)
                Projectile.Kill(); // fell out of the world
        }

        // The blade stands in the ground: its tip PlantSinkPx below the surface point 'ground'. The owner marks it planted for everyone (netUpdate).
        void Land(Vector2 ground, bool snap)
        {
            landed = true;
            landedAt = ticks;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = MathHelper.PiOver2; // the blade points down
            Projectile.Resize(22, (int)(PhasebladeTuning.BladeLength * PhasebladeTuning.SwordScale)); // the blade standing upright
            if (snap)
                Projectile.Center = new Vector2(ground.X, ground.Y + PhasebladeTuning.PlantSinkPx - Projectile.height * 0.5f);
            else
                ground = new Vector2(Projectile.Center.X, Projectile.Bottom.Y - PhasebladeTuning.PlantSinkPx); // the owner's packet put it there: the surface is its tip's depth above
            if (fell)
            {
                flare = 1f;
                PhasebladeFx.PlayPlant(ground);
                PhasebladeFx.PlantImpact(ground, BladeColor);
            }
            if (snap && IsOwner)
            {
                Projectile.ai[2] = 2f;
                Projectile.netUpdate = true;
            }
        }

        // RETURNING, first tick: a square hitbox around the middle of the blade, a pop up out of the ground, a timeout in case it is never caught.
        void Launch()
        {
            launched = true;
            Projectile.Resize(PhasebladeTuning.ReturnHitboxPx, PhasebladeTuning.ReturnHitboxPx);
            Projectile.velocity = new Vector2(0f, -PhasebladeTuning.ReturnPopSpeed);
            Projectile.timeLeft = PhasebladeTuning.ReturnTimeoutTicks;
            PhasebladeFx.PullOut(Projectile.Center, BladeColor);
        }

        // RETURNING: spins and is pulled toward the player's current position (BoomerangFlight.StepForceReturn); true when it was caught.
        bool Fly(Player owner)
        {
            Projectile.rotation += PhasebladeTuning.ReturnSpinSpeed * Facing;
            if (Main.rand.NextBool(2))
                PhasebladeFx.PlantEmber(Projectile.Center, BladeColor);

            // A straight line: the velocity always points exactly at the player's current position. The speed starts at ReturnPopSpeed and grows by
            // ReturnAcceleration per tick up to ReturnMaxSpeed. Caught when the blade is within a step or ReturnCatchDistance of the player.
            Vector2 toOwner = owner.Center - Projectile.Center;
            float distance = toOwner.Length();
            float speed = Math.Min(PhasebladeTuning.ReturnMaxSpeed, Math.Max(Projectile.velocity.Length(), PhasebladeTuning.ReturnPopSpeed) + PhasebladeTuning.ReturnAcceleration);
            if (distance <= Math.Max(speed, PhasebladeTuning.ReturnCatchDistance))
            {
                caught = true;
                Projectile.Kill();
                return true;
            }
            Projectile.velocity = toOwner / distance * speed;
            return false;
        }

        // The top of the blade (the hilt): the sprite is drawn from here downward.
        Vector2 HiltPoint => new Vector2(Projectile.Center.X, Projectile.Top.Y);

        // How far the standing blade is rocked about its tip right now (radians): it leans the way it was thrown, swings back and forth and dies out.
        // Only a blade that landed in front of this machine rocks (not one that was already standing when it arrived).
        float Wobble()
        {
            int age = ticks - landedAt;
            if (!fell || !landed || age < 0 || age >= PhasebladeTuning.PlantVibrateTicks)
                return 0f;
            float left = 1f - age / (float)Math.Max(1, PhasebladeTuning.PlantVibrateTicks);
            return Facing * MathHelper.ToRadians(PhasebladeTuning.PlantVibrateDeg) * left * left * (float)Math.Cos(age * PhasebladeTuning.PlantVibrateSpeed);
        }

        // ---- hits ----

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
            if (Returning)
                modifiers.SourceDamage *= PhasebladeTuning.ReturnDamageFactor;
            else if (Thrown)
                modifiers.SourceDamage *= PhasebladeTuning.ThrowDamageFactor;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            PhasebladeFx.HitSpark(target.Center, BladeColor);
            flare = 1f;
        }

        // Caught: a catch effect, and when a LEFT click pulled it back the combo starts now. Anything else (the owner died, flew too far, fell out of the
        // world): the blade just goes, and a pending swing is forgotten.
        public override void OnKill(int timeLeft)
        {
            if (caught)
                PhasebladeFx.Catch(Projectile.Center, BladeColor);
            else
                PhasebladeFx.Retract(Projectile.Center, BladeColor);

            if (!IsOwner)
                return;
            PhasebladePlayer state = Owner.GetModPlayer<PhasebladePlayer>();
            bool swing = caught && state.SwingAfterCatch && !Owner.dead && Owner.HeldItem.type == ItemType;
            state.SwingAfterCatch = false;
            if (swing)
                PhasebladeGlobalItem.StartCombo(Owner, Owner.GetSource_ItemUse(Owner.HeldItem), Owner.GetWeaponDamage(Owner.HeldItem), Owner.GetWeaponKnockback(Owner.HeldItem, Owner.HeldItem.knockBack));
        }

        // ---- drawing ----

        // Behind the tiles: the part of the blade that sank into the ground is covered by the tile; the rest stands in the air in front of the walls.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Returning || Thrown)
                overPlayers.Add(index); // flying (out or back) it is a normal projectile in front of everything
            else
                behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!PhasebladeTuning.IsPhaseblade(ItemType))
                return false;
            Main.instance.LoadItem(ItemType);
            Texture2D texture = TextureAssets.Item[ItemType].Value;
            HeldSwordPose.Sprite(texture, Facing, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);

            bool flying = Returning || Thrown;
            float wobble = flying ? 0f : Wobble();
            Vector2 hilt = HiltPoint;
            if (wobble != 0f) // the tip stays where it is, the blade rocks about it
                hilt = new Vector2(Projectile.Center.X, Projectile.Bottom.Y) - (MathHelper.PiOver2 + wobble).ToRotationVector2() * Projectile.height;
            Vector2 position = (flying ? Projectile.Center : hilt) - Main.screenPosition; // flying, the sprite spins about its middle
            float rotation = Projectile.rotation + wobble + rotationOffset;
            float scale = PhasebladeTuning.SwordScale;
            Color color = BladeColor;

            // A pulsing fire glow around the blade, drawn around the CENTER of the sprite (not the hilt) so it swells outward evenly.
            float pulse = 0.75f + 0.25f * (float)Math.Sin(ticks * 0.2f);
            float glow = (0.35f + 0.5f * flare) * pulse;
            Vector2 hiltToCenter = new Vector2(Facing > 0 ? texture.Width * 0.5f : -texture.Width * 0.5f, -texture.Height * 0.5f);
            Vector2 centerPosition = flying ? position : position + (hiltToCenter * scale).RotatedBy(rotation);
            Vector2 spriteOrigin = flying ? texture.Size() * 0.5f : origin;
            Vector2 centerOrigin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, centerPosition, null, color * (0.5f * glow), rotation, centerOrigin, scale * (1.3f + 0.15f * flare), effects, 0);
            Main.EntitySpriteDraw(texture, centerPosition, null, color * (0.6f * glow), rotation, centerOrigin, scale * 1.12f, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.7f), rotation, spriteOrigin, scale, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, PhasebladeTuning.CoreOf(color) * (0.25f * pulse + 0.5f * flare), rotation, spriteOrigin, scale, effects, 0);
            return false;
        }
    }
}