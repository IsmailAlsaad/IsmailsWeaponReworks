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
    /// A Phaseblade driven into the ground (right click). It stands upright where it was planted and stays until it is pulled back, or its owner
    /// dies or gets very far away. Every enemy that touches it takes a burn hit (a per-enemy cooldown) and is set on fire; embers rise from it in the
    /// blade's color. It draws the held item's sprite, so each color looks right.
    /// MULTIPLAYER: the owner's client spawns it (from the plant swing's impact tick) and the normal projectile sync carries it: ai[0] = the Phaseblade's
    /// item type (the color), ai[1] = the facing (+1 / -1), ai[2] = 1 once the owner pulls it back (set with netUpdate; every machine then removes
    /// its copy, so the removal never depends on a kill packet). Damage and the burn debuff come from the owner's client alone (OnHitNPC); the other
    /// machines get the hit sparks from RemoteHitTracker. Embers, light and glow are per machine. No custom packets.
    /// </summary>
    public class PhasebladePlantedProjectile : ModProjectile
    {
        // The projectile's own sprite is only a placeholder: the blade draws the item sprite of the color that was planted.
        public override string Texture => "Terraria/Images/Item_" + ItemID.BluePhaseblade;

        int ItemType => (int)Projectile.ai[0];
        int Facing => Projectile.ai[1] >= 0f ? 1 : -1;
        bool Retracting => Projectile.ai[2] >= 1f;
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;
        Color BladeColor => PhasebladeTuning.ColorOf(ItemType);

        int ticks;
        float flare; // 1 right after a hit, fades to 0
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

        // Where the ground is below x, starting a little above the player's feet: false when there is no solid tile (or platform) within reach.
        public static bool FindGround(Player player, float x, out Vector2 ground)
        {
            float top = player.Bottom.Y - 10f;
            float bottom = player.Bottom.Y + PhasebladeTuning.PlantMaxDropPx;
            for (float y = top; y <= bottom; y += 2f)
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

        // Owner only (item use): every planted blade of the player goes back to the hand. Patch 3 turns this into a flight; for now it is instant.
        public static void PullBack(Player player)
        {
            int type = ModContent.ProjectileType<PhasebladePlantedProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == player.whoAmI && other.type == type && other.ai[2] < 1f)
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
            if (Retracting || !owner.active || owner.dead || Vector2.DistanceSquared(owner.Center, Projectile.Center) > PhasebladeTuning.PlantedMaxDistancePx * PhasebladeTuning.PlantedMaxDistancePx)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 10000;
            Projectile.rotation = MathHelper.PiOver2; // the blade points down
            Projectile.spriteDirection = Facing;

            Color color = BladeColor;
            Vector2 hilt = HiltPoint;
            if (Main.rand.NextFloat() < PhasebladeTuning.PlantedEmberChance)
                PhasebladeFx.PlantEmber(Vector2.Lerp(hilt, Projectile.Bottom, Main.rand.NextFloat()), color);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * (0.6f + 0.4f * flare));

            if (!IsOwner)
                remoteHits.Update(Projectile, ticks / Math.Max(1, PhasebladeTuning.PlantedHitCooldownTicks), npc => PhasebladeFx.HitSpark(npc.Center, color));

            flare = Math.Max(0f, flare - 0.08f);
            ticks++;
        }

        // The top of the blade (the hilt): the sprite is drawn from here downward.
        Vector2 HiltPoint => new Vector2(Projectile.Center.X, Projectile.Top.Y);

        // ---- hits ----

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X > Projectile.Center.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Owner only (OnHitNPC): the debuff is applied here and the game syncs it.
            target.AddBuff(BuffID.OnFire3, PhasebladeTuning.PlantedBurnTicks);
            PhasebladeFx.HitSpark(target.Center, BladeColor);
            flare = 1f;
        }

        public override void OnKill(int timeLeft)
        {
            PhasebladeFx.Retract(Projectile.Center, BladeColor);
        }

        // ---- drawing ----

        // Behind the tiles: the part of the blade that sank into the ground is covered by the tile; the rest stands in the air in front of the walls.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!PhasebladeTuning.IsPhaseblade(ItemType))
                return false;
            Main.instance.LoadItem(ItemType);
            Texture2D texture = TextureAssets.Item[ItemType].Value;
            HeldSwordPose.Sprite(texture, Facing, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);

            Vector2 position = HiltPoint - Main.screenPosition;
            float rotation = Projectile.rotation + rotationOffset;
            float scale = PhasebladeTuning.SwordScale;
            Color color = BladeColor;

            // A pulsing fire glow around the blade, drawn around the CENTER of the sprite (not the hilt) so it swells outward evenly.
            float pulse = 0.75f + 0.25f * (float)Math.Sin(ticks * 0.2f);
            float glow = (0.35f + 0.5f * flare) * pulse;
            Vector2 hiltToCenter = new Vector2(Facing > 0 ? texture.Width * 0.5f : -texture.Width * 0.5f, -texture.Height * 0.5f);
            Vector2 centerPosition = position + (hiltToCenter * scale).RotatedBy(rotation);
            Vector2 centerOrigin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, centerPosition, null, color * (0.5f * glow), rotation, centerOrigin, scale * (1.3f + 0.15f * flare), effects, 0);
            Main.EntitySpriteDraw(texture, centerPosition, null, color * (0.6f * glow), rotation, centerOrigin, scale * 1.12f, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.7f), rotation, origin, scale, effects, 0);
            Main.EntitySpriteDraw(texture, position, null, PhasebladeTuning.CoreOf(color) * (0.25f * pulse + 0.5f * flare), rotation, origin, scale, effects, 0);
            return false;
        }
    }
}