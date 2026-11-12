using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Phaseblade
{
    /// <summary>
    /// A Phaseblade driven into the ground (right click). Three states, in ai[2]: FALLING (0: shot straight down from the air until its tip meets a tile,
    /// then it lands), PLANTED (2: it stands upright, its tip PlantSinkPx into the tile, and stays until it is pulled back, or its owner dies or gets very
    /// far away) and RETURNING (1: it pops out, spins and flies to the hand like a boomerang, hitting what it crosses, and is caught). Every enemy that
    /// touches it takes a hit (a per-enemy cooldown); embers rise from it in the blade's color. It draws the held item's sprite, so each color looks right.
    /// MULTIPLAYER: the owner's client spawns it (from the plant swing's impact tick) and the normal projectile sync carries it: ai[0] = the Phaseblade's
    /// item type (the color), ai[1] = the facing (+1 / -1), ai[2] = the state (the owner changes it with netUpdate: landing sets 2, a click sets 1).
    /// Every machine runs the same fall, landing detection and return flight on its own copy (the tiles and the owner's position are synced), so no
    /// removal or catch depends on a kill packet. Damage comes from the owner's client alone (OnHitNPC); the other machines get the hit sparks from
    /// RemoteHitTracker. Embers, light, glow and the landing and catch effects are per machine. No custom packets.
    /// </summary>
    public class PhasebladePlantedProjectile : ModProjectile
    {
        // The projectile's own sprite is only a placeholder: the blade draws the item sprite of the color that was planted.
        public override string Texture => "Terraria/Images/Item_" + ItemID.BluePhaseblade;

        int ItemType => (int)Projectile.ai[0];
        int Facing => Projectile.ai[1] >= 0f ? 1 : -1;
        int State => (int)Projectile.ai[2];   // 0 = falling, 1 = flying back to the hand, 2 = planted
        bool Returning => State == 1;
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        Color BladeColor => PhasebladeTuning.ColorOf(ItemType);

        int ticks;
        float flare; // 1 right after a hit, fades to 0
        bool fell;       // this blade was shot from the air (so its landing plays the impact effects)
        bool landed;     // this machine has seen the blade stand in the ground
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
            for (float y = fromY; y <= fromY + distance; y += 2f)
            {
                int tileX = (int)(x / 16f);
                int tileY = (int)(y / 16f);
                if (!WorldGen.InWorld(tileX, tileY, 10))
                    break;
                Tile tile = Main.tile[tileX, tileY];
                if (tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                {
                    ground = new Vector2(x, y);
                    return true;
                }
            }
            ground = default;
            return false;
        }

        // Standing: the ground below x, from a little above the player's feet down 'maxDrop' pixels below them.
        public static bool FindGround(Player player, float x, out Vector2 ground, float maxDrop)
        {
            return FindTileBelow(x, player.Bottom.Y - 10f, 10f + maxDrop, out ground);
        }

        // True when the player has a planted or falling blade of this Phaseblade type (one per color: the colors are planted separately).
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

        // Owner only (item use): every blade of the player that is planted or falling leaves for the hand. A LEFT click (thenSwing) also starts the
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
                fell = State == 0;
            if (!owner.active || owner.dead || (State == 2 && Vector2.DistanceSquared(owner.Center, Projectile.Center) > PhasebladeTuning.PlantedMaxDistancePx * PhasebladeTuning.PlantedMaxDistancePx))
            {
                Projectile.Kill();
                return;
            }

            Projectile.spriteDirection = Facing;
            Projectile.localNPCHitCooldown = Returning ? PhasebladeTuning.ReturnHitCooldownTicks : PhasebladeTuning.PlantedHitCooldownTicks;
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
                    Fall();
                }
                if (Main.rand.NextFloat() < PhasebladeTuning.PlantedEmberChance * (landed ? 1f : 2f))
                    PhasebladeFx.PlantEmber(Vector2.Lerp(HiltPoint, Projectile.Bottom, Main.rand.NextFloat()), color);
            }

            Lighting.AddLight(Projectile.Center, color.ToVector3() * (0.6f + 0.4f * flare));

            if (!IsOwner)
                remoteHits.Update(Projectile, ticks / Math.Max(1, Projectile.localNPCHitCooldown), npc => PhasebladeFx.HitSpark(npc.Center, color));

            flare = Math.Max(0f, flare - 0.08f);
            ticks++;
        }

        // FALLING: straight down at PlantDropSpeed until the tip would meet a tile within this tick's step; there it lands.
        void Fall()
        {
            Projectile.rotation = MathHelper.PiOver2;
            Projectile.velocity = new Vector2(0f, PhasebladeTuning.PlantDropSpeed);
            if (FindTileBelow(Projectile.Center.X, Projectile.Bottom.Y, PhasebladeTuning.PlantDropSpeed, out Vector2 ground))
                Land(ground, true);
            else if (Projectile.Center.Y > Main.maxTilesY * 16f)
                Projectile.Kill(); // fell out of the world
        }

        // The blade stands in the ground: its tip PlantSinkPx below the surface point 'ground'. The owner marks it planted for everyone (netUpdate).
        void Land(Vector2 ground, bool snap)
        {
            landed = true;
            Projectile.velocity = Vector2.Zero;
            if (snap)
                Projectile.Center = new Vector2(ground.X, ground.Y + PhasebladeTuning.PlantSinkPx - Projectile.height * 0.5f);
            if (fell)
            {
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

        // ---- hits ----

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
            if (Returning)
                modifiers.SourceDamage *= PhasebladeTuning.ReturnDamageFactor;
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
            if (Returning)
                overPlayers.Add(index); // flying back it is a normal projectile in front of everything
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

            bool flying = Returning;
            Vector2 position = (flying ? Projectile.Center : HiltPoint) - Main.screenPosition; // flying, the sprite spins about its middle
            float rotation = Projectile.rotation + rotationOffset;
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