using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using IsmailsWeaponReworks.Common;

namespace IsmailsWeaponReworks.Content.Weapons.Melee.PreHardmode.Broadsword.WoodenSwords
{
    /// <summary>
    /// A splinter of a wooden sword, lodged in an enemy. It follows the enemy, draws one 12x12 cell of Splinters.png (column = the wood, row =
    /// the style, both picked at random by the owner when it lodges, plus a random rotation) and hurts the enemy for WoodenSwordTuning.DamagePerHit
    /// every HitIntervalTicks until its life (per wood) runs out, the enemy dies or the owner pushes it out (max splinters per enemy).
    /// ai[0] = the enemy's index (-1 = pushed out, every machine removes it), ai[1] = column * StyleCount + style, ai[2] = rotation,
    /// velocity = the lodging point relative to the enemy's center (the projectile never moves by itself, so the velocity is just a number
    /// that rides along in the spawn packet and is never changed afterwards).
    /// MULTIPLAYER: the owner's client alone decides (the lodging, the cap, the damage: SimpleStrikeNPC syncs itself). Every machine computes
    /// the same lifetime from the wood, the same position from the enemy and the same look from ai. No custom packets.
    /// </summary>
    public class SplinterProjectile : ModProjectile
    {
        // A placeholder so the mod loads; the art is Splinters.png (see ArtPath), drawn from PreDraw.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.WoodenArrowFriendly;

        const string ArtPath = "IsmailsWeaponReworks/Content/Weapons/Melee/PreHardmode/Broadsword/WoodenSwords/Splinters";

        Asset<Texture2D> art;
        bool artTried;

        bool started;
        int npcType = -1;
        int waited;
        int lifeTicks;

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        Vector2 Offset => Projectile.velocity;

        int Column => Math.Max(0, (int)Projectile.ai[1] / Math.Max(1, WoodenSwordTuning.StyleCount));
        int Style => Math.Max(0, (int)Projectile.ai[1] % Math.Max(1, WoodenSwordTuning.StyleCount));

        // Owner's client: lodge a splinter at 'point' on 'target'. The oldest splinter of this player on this enemy is pushed out first while
        // the wood's cap is full.
        public static void Attach(IEntitySource source, Player owner, NPC target, Vector2 point, int column)
        {
            WoodSpec spec = WoodenSwordWoods.Spec(column);
            int type = ModContent.ProjectileType<SplinterProjectile>();

            while (true)
            {
                int count = 0;
                Projectile oldest = null;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || p.type != type || p.owner != owner.whoAmI || (int)p.ai[0] != target.whoAmI)
                        continue;
                    count++;
                    if (oldest == null || p.timeLeft < oldest.timeLeft)
                        oldest = p;
                }
                if (count < spec.MaxSplinters || oldest == null)
                    break;
                oldest.ai[0] = -1f; // pushed out: every machine sees it and removes it
                oldest.netUpdate = true;
            }

            int style = Main.rand.Next(Math.Max(1, WoodenSwordTuning.StyleCount));
            float rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 offset = point - target.Center;
            Projectile.NewProjectile(source, point, offset, type, 0, 0f, owner.whoAmI, target.whoAmI, column * Math.Max(1, WoodenSwordTuning.StyleCount) + style, rotation);
        }

        // Every machine, the first tick: the same lifetime, the sound and the chips of the lodging.
        void Start()
        {
            started = true;
            lifeTicks = WoodenSwordWoods.Spec(Column).LifeTicks + 1;
            Projectile.timeLeft = lifeTicks;
            int index = (int)Projectile.ai[0];
            if (index >= 0 && index < Main.maxNPCs && Main.npc[index].active)
                npcType = Main.npc[index].type;
            Projectile.Center = Anchor(index);
            WoodenSwordFx.PlayStick(Projectile.Center);
            WoodenSwordFx.Chips(Projectile.Center, Column, WoodenSwordTuning.StickDustCount, 2f);
            if (Projectile.owner == Main.myPlayer)
                CameraShake.Punch(Projectile.Center, WoodenSwordTuning.StickShake, 6);
        }

        Vector2 Anchor(int index)
        {
            if (index < 0 || index >= Main.maxNPCs)
                return Projectile.Center;
            NPC npc = Main.npc[index];
            return npc.Center + Offset + new Vector2(0f, npc.gfxOffY);
        }

        public override void AI()
        {
            if (!started)
                Start();

            int index = (int)Projectile.ai[0];
            if (index < 0 || index >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC npc = Main.npc[index];
            if (npcType < 0)
            {
                // The enemy had not reached this machine yet: wait for it a moment.
                if (npc.active)
                    npcType = npc.type;
                else if (++waited > WoodenSwordTuning.WaitForNpcTicks)
                    Projectile.Kill();
                return;
            }
            if (!npc.active || npc.type != npcType)
            {
                Projectile.Kill(); // the enemy died (or its slot was taken by another)
                return;
            }

            Projectile.Center = Anchor(index);

            int age = lifeTicks - Projectile.timeLeft;
            int interval = Math.Max(1, WoodenSwordTuning.HitIntervalTicks);
            if (age > 0 && age % interval == 0)
            {
                WoodenSwordFx.Chips(Projectile.Center, Column, WoodenSwordTuning.TickDustCount, 1.2f);
                if (Projectile.owner == Main.myPlayer && !npc.dontTakeDamage && !npc.immortal)
                {
                    int direction = npc.Center.X > Main.player[Projectile.owner].Center.X ? 1 : -1;
                    npc.SimpleStrikeNPC(WoodenSwordTuning.DamagePerHit, direction, false, 0f, DamageClass.Melee);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (started)
                WoodenSwordFx.Break(Projectile.Center, Column, WoodenSwordTuning.BreakDustCount, WoodenSwordTuning.BreakDustSpeed);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!started)
                return false;

            Color color = Lighting.GetColor(Projectile.Center.ToTileCoordinates());
            Vector2 position = (Projectile.Center - Main.screenPosition).Floor();
            float rotation = Projectile.ai[2];
            float scale = WoodenSwordTuning.SplinterScale;

            if (!artTried)
            {
                artTried = true;
                ModContent.RequestIfExists(ArtPath, out art);
            }

            if (art != null && art.IsLoaded)
            {
                int cell = Math.Max(1, WoodenSwordTuning.CellSize);
                Rectangle source = new Rectangle(Column * cell, Style * cell, cell, cell);
                if (source.Right <= art.Value.Width && source.Bottom <= art.Value.Height)
                {
                    Main.EntitySpriteDraw(art.Value, position, source, color, rotation, new Vector2(cell * 0.5f), scale, SpriteEffects.None, 0);
                    return false;
                }
            }

            // No art yet: a small stick in the wood's color.
            Color stick = WoodenSwordWoods.Spec(Column).Mid;
            stick.A = 255;
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, position, new Rectangle(0, 0, 1, 1), stick.MultiplyRGBA(color), rotation, new Vector2(0.5f), new Vector2(5f, 2f) * scale, SpriteEffects.None, 0);
            return false;
        }
    }
}