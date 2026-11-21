using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.BladeOfGrass
{
    /// <summary>
    /// The Blade of Grass driven into the ground (right click): a sword standing in the earth with the vines the player had grown planted on it. It hosts a
    /// BladeOfGrassVineSet rooted at the sword's hilt (the same searching, looping and break-offs as the player's vines, no distance limit), and its vines
    /// GROW SLOWLY BY THEMSELVES (PlantedGrowPerTick, up to PlantedMaxLength). It lives PlantedLifeTicks after landing. Only one exists per player: planting
    /// again withers the old one. The player's own sword is hidden (BladeOfGrassBladeProjectile.HideBlade) from the throw until PlantRegrowTicks after landing.
    /// States in ai[2]: FALLING (0: the sword drops from the hand until its tip meets a tile), PLANTED (2) and WITHERING (3: fades out and goes). ai[1] = the facing (+1 / -1).
    /// MULTIPLAYER: the owner's client decides (the plant, the ground, the targets, the break-offs, the damage of the coils, the growth). The spawn packet carries
    /// the facing; the owner sets the lengths right after the spawn and the normal projectile sync (SendExtraAI) carries the target and length of every vine, sent when a
    /// target or a length changes and every PlantedSyncTicks while it grows. Every machine runs the same fall and landing on its own copy (tiles are synced), the
    /// regrow and the wither timers, the rope physics and all effects locally. The planted projectile itself never damages: its coils (BladeOfGrassSpineProjectile)
    /// are normal projectiles spawned by the owner. No custom packets.
    /// </summary>
    public class BladeOfGrassPlantedProjectile : ModProjectile
    {
        // The sword is drawn from the item sprite (see PreDraw); this is only a placeholder.
        public override string Texture => "Terraria/Images/Item_" + ItemID.BladeofGrass;

        enum PlantState { Falling = 0, Planted = 2, Withering = 3 }

        PlantState State => (PlantState)(int)Projectile.ai[2];
        int Facing => Projectile.ai[1] >= 0f ? 1 : -1;
        Player Owner => Main.player[Projectile.owner];
        bool IsOwner => Projectile.owner == Main.myPlayer;

        int ticks;
        bool fell;            // dropped from the air (so its landing plays the impact)
        bool landed;          // this machine has seen the sword stand in the ground
        int sinceLanded;
        bool regrown;         // this machine has played the regrow: the owner's hand has its sword back
        bool witherStarted;
        int sinceWither;
        int sinceSync;
        float[] lengths;      // owner only: the planted vines' lengths
        Func<int, Vector2> rootFunction;
        readonly BladeOfGrassVineSet vines = new BladeOfGrassVineSet();

        // The top of the sword (the hilt); the sprite is drawn from here downward.
        Vector2 HiltPoint => new Vector2(Projectile.Center.X, Projectile.Top.Y);
        Vector2 Root => HiltPoint + new Vector2(0f, BladeOfGrassTuning.PlantedRootDownPx);

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = (int)(BladeOfGrassTuning.BladeLength * BladeOfGrassTuning.SwordScale); // the sword standing upright
            Projectile.aiStyle = -1;
            Projectile.friendly = false; // never hurts by itself; the coils do
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 10000;
            Projectile.hide = true; // drawn by DrawBehind
        }

        public override bool? CanDamage()
        {
            return false;
        }

        // ---------------------------------------------------------------- helpers

        // True while this player's sword is out of the hand (thrown, in the ground, not grown back yet). Every machine.
        public static bool HidesSword(int owner)
        {
            int type = ModContent.ProjectileType<BladeOfGrassPlantedProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != owner || other.type != type)
                    continue;
                if (other.ModProjectile is BladeOfGrassPlantedProjectile planted && planted.State != PlantState.Withering && !planted.regrown)
                    return true;
            }
            return false;
        }

        // Owner only: plant the sword toward the cursor. 'currentLengths' are the vines the player has grown (they are copied onto the planted set); the caller
        // resets its own. False when there is no ground within PlantMaxDropPx (nothing happens).
        public static bool TryPlant(Player player, float[] currentLengths, IEntitySource source, int damage, float knockback)
        {
            float dx = Main.MouseWorld.X - player.Center.X;
            int side = dx >= 0f ? 1 : -1;
            float reach = MathHelper.Clamp(Math.Abs(dx), BladeOfGrassTuning.PlantMinReachPx, BladeOfGrassTuning.PlantMaxReachPx);
            float x = player.Center.X + side * reach;
            float startY = player.MountedCenter.Y;
            if (!TileProbe.FindTileBelow(x, startY, BladeOfGrassTuning.PlantMaxDropPx, out Vector2 ground) || ground.Y - startY < 8f)
                return false; // no ground, or a wall at the sword's height

            int type = ModContent.ProjectileType<BladeOfGrassPlantedProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile old = Main.projectile[i];
                if (old.active && old.owner == player.whoAmI && old.type == type && old.ai[2] != (float)PlantState.Withering)
                {
                    old.ai[2] = (float)PlantState.Withering; // only one planted set: the old one withers
                    old.netUpdate = true;
                }
            }

            int height = (int)(BladeOfGrassTuning.BladeLength * BladeOfGrassTuning.SwordScale);
            Vector2 center = new Vector2(x, startY - height * 0.5f); // the tip starts at the hand's height
            int index = Projectile.NewProjectile(source, center, new Vector2(0f, BladeOfGrassTuning.PlantDropSpeed), type, damage, knockback, player.whoAmI, 0f, side, (float)PlantState.Falling);
            if (index < 0 || index >= Main.maxProjectiles)
                return false;
            if (Main.projectile[index].ModProjectile is BladeOfGrassPlantedProjectile planted)
            {
                planted.lengths = (float[])currentLengths.Clone();
                Main.projectile[index].netUpdate = true; // the lengths go out right away
            }
            return true;
        }

        // ---------------------------------------------------------------- sync

        public override void SendExtraAI(BinaryWriter writer)
        {
            vines.Write(writer, IsOwner ? lengths : null);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            vines.Read(reader);
        }

        // ---------------------------------------------------------------- AI

        public override void AI()
        {
            Player owner = Owner;
            if (ticks == 0)
            {
                fell = State == PlantState.Falling;
                BladeOfGrassFx.PlantStart(Projectile.Center);
            }
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.spriteDirection = Facing;
            Projectile.rotation = MathHelper.PiOver2; // the sword points down

            if (State == PlantState.Withering)
            {
                Projectile.velocity = Vector2.Zero;
                if (!witherStarted)
                {
                    witherStarted = true;
                    BladeOfGrassFx.Wither(Projectile.Center);
                }
                vines.Disabled = true;
                vines.DrawFade = 1f - sinceWither / (float)Math.Max(1, BladeOfGrassTuning.WitherTicks);
                sinceWither++;
                if (sinceWither >= BladeOfGrassTuning.WitherTicks)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else if (State == PlantState.Planted || landed)
            {
                if (!landed)
                    Land(new Vector2(Projectile.Center.X, Projectile.Bottom.Y - BladeOfGrassTuning.PlantSinkPx), false); // the owner's sync says it is planted
                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.timeLeft = 10000;
                Fall();
            }

            // The owner's hand gets its sword back a moment after the landing.
            if (landed && !regrown)
            {
                sinceLanded++;
                if (sinceLanded >= BladeOfGrassTuning.PlantRegrowTicks)
                {
                    regrown = true;
                    BladeOfGrassFx.Regrow(owner.MountedCenter);
                }
            }

            // The planted vines grow slowly by themselves (owner decides; the others hear the lengths through SendExtraAI).
            if (IsOwner && landed && State == PlantState.Planted && lengths != null)
            {
                bool grew = false;
                float cap = BladeOfGrassTuning.PlantedMaxLength;
                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] < cap)
                    {
                        lengths[i] = Math.Min(cap, lengths[i] + BladeOfGrassTuning.PlantedGrowPerTick);
                        grew = true;
                    }
                }
                if (grew && ++sinceSync >= Math.Max(1, BladeOfGrassTuning.PlantedSyncTicks))
                {
                    sinceSync = 0;
                    Projectile.netUpdate = true;
                }
            }

            if (!Main.dedServ)
            {
                rootFunction ??= _ => Root;
                vines.StartTrimPx = BladeOfGrassTuning.PlantedChainStartTrimPx;
                vines.SearchAbove = BladeOfGrassTuning.PlantedSearchAbove;
                vines.Tick(Projectile, Root, rootFunction, 0f, IsOwner ? lengths : null);
                if (IsOwner && vines.TakeSync())
                    Projectile.netUpdate = true;

                Lighting.AddLight(Root, 0.1f, 0.3f, 0.1f);
                if (landed && State == PlantState.Planted && Main.rand.NextFloat() < BladeOfGrassTuning.PlantedLeafChance)
                    BladeOfGrassFx.PlantedLeaf(Vector2.Lerp(HiltPoint, Projectile.Bottom, Main.rand.NextFloat()));
            }
            ticks++;
        }

        // FALLING: straight down until the tip would meet a tile within this tick's step; there it lands.
        void Fall()
        {
            Projectile.velocity = new Vector2(0f, BladeOfGrassTuning.PlantDropSpeed);
            if (TileProbe.FindTileBelow(Projectile.Center.X, Projectile.Bottom.Y, BladeOfGrassTuning.PlantDropSpeed, out Vector2 ground))
                Land(ground, true);
            else if (Projectile.Center.Y > Main.maxTilesY * 16f)
                Projectile.Kill(); // fell out of the world
        }

        // The sword stands in the ground, its tip PlantSinkPx below the surface point 'ground'. The owner marks it planted for everyone (netUpdate).
        void Land(Vector2 ground, bool snap)
        {
            landed = true;
            sinceLanded = 0;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = BladeOfGrassTuning.PlantedLifeTicks;
            if (snap)
                Projectile.Center = new Vector2(ground.X, ground.Y + BladeOfGrassTuning.PlantSinkPx - Projectile.height * 0.5f);
            if (fell)
            {
                BladeOfGrassFx.PlantImpact(ground);
                CameraShake.Punch(ground, BladeOfGrassTuning.PlantShake, 10);
            }
            if (snap && IsOwner)
            {
                Projectile.ai[2] = (float)PlantState.Planted;
                Projectile.netUpdate = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            vines.Release();
            if (landed)
                BladeOfGrassFx.Unwind(Projectile.Center, Projectile.width, Projectile.height);
        }

        // ---------------------------------------------------------------- drawing

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (BladeOfGrassTuning.PlantedDrawBehind)
                behindNPCsAndTiles.Add(index); // the tile hides the part that sank in
            else
                behindProjectiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            vines.DrawNow(); // first, so the sword covers the vine roots

            Main.instance.LoadItem(ItemID.BladeofGrass);
            Texture2D texture = TextureAssets.Item[ItemID.BladeofGrass].Value;
            HeldSwordPose.Sprite(texture, Facing, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            Color color = Color.Lerp(lightColor, Color.White, 0.3f) * vines.DrawFade;
            Main.EntitySpriteDraw(texture, HiltPoint - Main.screenPosition, null, color, Projectile.rotation + rotationOffset, origin, BladeOfGrassTuning.SwordScale, effects, 0);
            return false;
        }
    }
}