using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework.Graphics;
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
    /// The vines the Blade of Grass plants (right click, see BladeOfGrassPlantSwingProjectile): they come OUT OF THE GROUND where the sword was driven in, the
    /// way the Amazon's vine buds do (a pop of dirt and leaves, then PlantGrowTicks of growing out). There is no sword in the ground, only the vines. It hosts a
    /// BladeOfGrassVineSet rooted at the ground point (the same searching, looping and break-offs as the player's vines; they search only ABOVE the ground,
    /// there is no distance limit), and its vines GROW SLOWLY BY THEMSELVES (PlantedGrowPerTick, up to PlantedMaxLength). Once fully grown it stands
    /// PlantedLifeTicks, then it UNGROWS: the vines shrink back into the ground and shrivel (brown) over PlantRetractTicks, and the projectile ends.
    /// Only one set exists per player: planting again makes the old one ungrow the same way. While growing out or ungrowing the vines cannot attack.
    /// ai[2] = 3 means "ungrow now" (the owner sets it, with netUpdate). The projectile has no other synced state.
    /// MULTIPLAYER: the owner's client decides (the plant, the targets, the break-offs, the damage of the coils, the growth, the replacement). The vine lengths
    /// are set right after the spawn (netUpdate) and then travel through SendExtraAI together with each vine's target (sent when a target or a length changes
    /// and every PlantedSyncTicks while it grows). The grow-out, the standing time, the ungrowing, the rope physics and all effects are timers and visuals each
    /// machine runs on its own copy. It never damages itself: its coils (BladeOfGrassSpineProjectile) are normal projectiles spawned by the owner. No custom packets.
    /// </summary>
    public class BladeOfGrassPlantedVinesProjectile : ModProjectile
    {
        // Nothing is drawn from this sprite; the vine set draws itself.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.IvyWhip;

        bool IsOwner => Projectile.owner == Main.myPlayer;
        bool UngrowOrdered => Projectile.ai[2] >= 3f;

        int age;               // ticks since this machine saw the projectile
        int standTicks;        // ticks fully grown
        int retractTicks = -1; // -1 = standing; else ticks since the ungrowing began
        int sinceSync;
        float[] lengths;       // owner only: the target lengths of the vines
        Func<int, Vector2> rootFunction;
        readonly BladeOfGrassVineSet vines = new BladeOfGrassVineSet();

        // The planted sword stands upright with its tip PlantSinkPx below the surface: Center is the middle of the blade, the hilt is the top.
        Vector2 HiltPoint => new Vector2(Projectile.Center.X, Projectile.Top.Y);
        Vector2 GroundPoint => new Vector2(Projectile.Center.X, Projectile.Bottom.Y - BladeOfGrassTuning.PlantSinkPx);
        int Facing => Projectile.ai[1] >= 0f ? 1 : -1;
        float swordSink;       // 0..1: the sword sinks into the ground while the set ungrows
        // The vines grow from the middle of the blade and are drawn behind it.
        Vector2 Root => HiltPoint + new Vector2(0f, Projectile.height * BladeOfGrassTuning.PlantedRootAlongBladeFraction);

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
            Projectile.timeLeft = 10000; // refreshed by the AI; it ends itself
            Projectile.hide = true; // drawn by DrawBehind
        }

        public override bool? CanDamage()
        {
            return false;
        }

        // Owner only, at the swing's impact: plants the vines the player has grown on the ground point; an older set of this player ungrows.
        public static void Plant(Player player, Vector2 ground, IEntitySource source, int damage, float knockback)
        {
            int type = ModContent.ProjectileType<BladeOfGrassPlantedVinesProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile old = Main.projectile[i];
                if (old.active && old.owner == player.whoAmI && old.type == type && old.ai[2] < 3f)
                {
                    old.ai[2] = 3f; // only one planted set: the old one ungrows and shrivels away
                    old.netUpdate = true;
                }
            }

            float[] taken = BladeOfGrassPlayer.TakeLengths(player);
            float height = BladeOfGrassTuning.BladeLength * BladeOfGrassTuning.SwordScale;
            Vector2 center = new Vector2(ground.X, ground.Y + BladeOfGrassTuning.PlantSinkPx - height * 0.5f);
            int index = Projectile.NewProjectile(source, center, Vector2.Zero, type, damage, knockback, player.whoAmI, 0f, player.direction >= 0 ? 1f : -1f);
            if (index < 0 || index >= Main.maxProjectiles)
                return;
            if (Main.projectile[index].ModProjectile is BladeOfGrassPlantedVinesProjectile planted)
            {
                planted.lengths = taken;
                Main.projectile[index].netUpdate = true; // the lengths go out right away
            }
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
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 10000;
            Projectile.velocity = Vector2.Zero;

            if (age == 0)
                BladeOfGrassFx.Emerge(GroundPoint);

            // Growing out of the ground, standing, ungrowing: one number (LengthScale) scales every vine's length; the vines cannot attack while it is not 1.
            float grow = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(age / (float)Math.Max(1, BladeOfGrassTuning.PlantGrowTicks), 0f, 1f));
            bool grown = age >= BladeOfGrassTuning.PlantGrowTicks;
            if (grown && retractTicks < 0)
            {
                standTicks++;
                if (UngrowOrdered || standTicks >= BladeOfGrassTuning.PlantedLifeTicks)
                {
                    retractTicks = 0;
                    BladeOfGrassFx.Wither(Projectile.Center);
                }
            }
            else if (!grown && UngrowOrdered && retractTicks < 0)
            {
                retractTicks = 0; // replaced before it was out: it goes back in from where it is
                BladeOfGrassFx.Wither(Projectile.Center);
            }

            float retract = 0f;
            if (retractTicks >= 0)
            {
                retract = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(retractTicks / (float)Math.Max(1, BladeOfGrassTuning.PlantRetractTicks), 0f, 1f));
                retractTicks++;
                if (retractTicks >= BladeOfGrassTuning.PlantRetractTicks)
                {
                    Projectile.Kill();
                    return;
                }
            }
            vines.LengthScale = grow * (1f - retract);
            swordSink = retract;
            vines.Disabled = !grown || retractTicks >= 0;
            vines.DrawTint = Color.Lerp(Color.White, new Color(170, 130, 80), retract);

            // The planted vines grow slowly by themselves (owner decides; the others hear the lengths through SendExtraAI).
            if (IsOwner && !vines.Disabled && lengths != null)
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
                if (!vines.Disabled && Main.rand.NextFloat() < BladeOfGrassTuning.PlantedLeafChance)
                    BladeOfGrassFx.PlantedLeaf(Root);
            }
            age++;
        }

        public override void OnKill(int timeLeft)
        {
            vines.Release();
        }

        // ---------------------------------------------------------------- drawing

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (BladeOfGrassTuning.PlantedDrawBehind)
                behindNPCsAndTiles.Add(index); // the tile hides the part of the sword that sank in
            else
                behindProjectiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            vines.DrawNow(); // first: the vines are behind the sword, they grow from its middle

            Main.instance.LoadItem(ItemID.BladeofGrass);
            Texture2D texture = TextureAssets.Item[ItemID.BladeofGrass].Value;
            HeldSwordPose.Sprite(texture, Facing, out Vector2 origin, out float rotationOffset, out SpriteEffects effects);
            Vector2 position = HiltPoint + new Vector2(0f, swordSink * (Projectile.height + BladeOfGrassTuning.PlantSinkPx));
            Color color = Color.Lerp(lightColor, Color.White, 0.3f);
            Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, color, MathHelper.PiOver2 + rotationOffset, origin, BladeOfGrassTuning.SwordScale, effects, 0);
            return false;
        }
    }
}