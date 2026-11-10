using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Rally
{
    /// <summary>
    /// The Rally's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and trick and adds
    /// the cracks. Every enemy hit (owner's client) calls Crack: the stage goes 0, 1, 2 and the third hit breaks the outer shell into
    /// RallyShellShardProjectile pieces and starts over at 0. The yoyo keeps flying through all of it. The stage is kept across passes
    /// and catches.
    /// SPRITE: Rally/RallyProjectile.png (the intact yoyo; Tuning.TexturePath), RallyProjectileCrack1.png and RallyProjectileCrack2.png
    /// for the two cracked looks; whatever is missing falls back to the vanilla sprite, with cracks drawn by code (PixelLine).
    /// MULTIPLAYER: the owner's client decides the hits, the stage and the break. The stage travels in the extra sync (one byte added
    /// after the base's), so every machine draws the right sprite and plays the crack or break effects when the stage changes; the shards
    /// are spawned by the owner (normal projectile spawn sync).
    /// </summary>
    public class RallyProjectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Rally;

        public override int YoyoItemType => ItemID.Rally;

        public int Cracks;                    // 0 = intact, 1 and 2 = cracked (synced)

        RallyTuning Tune => (RallyTuning)Tuning;
        RallyFx RFx => (RallyFx)Fx;

        bool cracksSynced;                    // this machine has received the stage once
        bool pendingCrackFx;
        bool pendingBreakFx;
        int pendingStage;
        bool crackArtChecked;
        Asset<Texture2D> crack1;
        Asset<Texture2D> crack2;
        readonly Vector2[] crackPoints = new Vector2[8];
        readonly float[] crackT = new float[8];

        protected override YoyoTuning CreateTuning()
        {
            return new RallyTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new RallyFx();
        }

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new RallyTrick(tuning);
        }

        // ---- net ----

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)Cracks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            int cracks = reader.ReadByte();
            if (!cracksSynced)
            {
                cracksSynced = true;   // a late joiner takes the stage as it is, no effects
                Cracks = cracks;
                return;
            }
            if (cracks == Cracks)
                return;
            if (cracks < Cracks)
                pendingBreakFx = true;   // the stage went back to 0: the shell broke
            else
            {
                pendingCrackFx = true;
                pendingStage = cracks;
            }
            Cracks = cracks;
        }

        // ---- cracks ----

        // Owner's client only (from RallyTrick.OnHit): one more crack, and the shell breaks on the last.
        public void Crack()
        {
            Cracks++;
            if (Cracks >= Math.Max(1, Tune.CrackHitsToBreak))
            {
                Cracks = 0;
                BreakShell();
                RFx.Break(Projectile.Center);
                YoyoShake.Hit(Projectile.Center, Tune.BreakShake);
            }
            else
                RFx.Crack(Projectile.Center, Cracks);
            Projectile.netUpdate = true;
        }

        // The shell flies apart: shards outward and a little away from the feet side, then they fall.
        void BreakShell()
        {
            int count = Math.Max(1, Tune.ShellShardCount);
            int damage = Math.Max(1, (int)(Projectile.damage * Tune.ShardDamageFactor));
            int type = ModContent.ProjectileType<RallyShellShardProjectile>();
            float start = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < count; i++)
            {
                float angle = start + MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.3f, 0.3f);
                float speed = Main.rand.NextFloat(Tune.ShardLaunchSpeedMin, Tune.ShardLaunchSpeedMax);
                Vector2 velocity = angle.ToRotationVector2() * speed + new Vector2(0f, -Tune.ShardLaunchUp * GravDir) + Vel * Tune.ShardInheritVelocity;
                int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, type, damage, Projectile.knockBack,
                    Projectile.owner, i, i % 2 == 0 ? 1f : -1f);
                if (index >= 0 && index < Main.maxProjectiles)
                    Main.projectile[index].CritChance = Projectile.CritChance;
            }
        }

        public override void AI()
        {
            base.AI();
            if (Main.dedServ)
                return;
            // The crack and break effects of a hit the owner decided (the other machines learn about it from the sync).
            if (pendingBreakFx)
            {
                pendingBreakFx = false;
                pendingCrackFx = false;
                RFx.Break(Projectile.Center);
            }
            else if (pendingCrackFx)
            {
                pendingCrackFx = false;
                RFx.Crack(Projectile.Center, pendingStage);
            }
        }

        // ---- drawing ----

        void CheckCrackArt()
        {
            if (crackArtChecked)
                return;
            crackArtChecked = true;
            ModContent.RequestIfExists<Texture2D>(Tune.Crack1TexturePath, out crack1);
            ModContent.RequestIfExists<Texture2D>(Tune.Crack2TexturePath, out crack2);
        }

        Texture2D CrackArt()
        {
            CheckCrackArt();
            Asset<Texture2D> art = Cracks == 1 ? crack1 : Cracks == 2 ? crack2 : null;
            return art != null && art.IsLoaded ? art.Value : null;
        }

        // The sprite of the current stage (afterimages keep the sprite they were made with).
        protected override Texture2D GetTexture()
        {
            Texture2D cracked = Cracks > 0 ? CrackArt() : null;
            return cracked ?? base.GetTexture();
        }

        // No cracked art yet: cracks drawn over the yoyo with the shared pixel line (they turn with the yoyo).
        public override void PostDraw(Color lightColor)
        {
            if (Main.dedServ || Cracks <= 0 || CrackArt() != null)
                return;
            Texture2D texture = base.GetTexture();
            float radius = texture.Width * 0.45f;
            Color color = new Color(lightColor.ToVector3() * 0.22f);
            int shapes = Cracks == 1 ? 1 : 3;
            for (int s = 0; s < shapes; s++)
            {
                Vector2[] shape = CrackShape(s);
                for (int i = 0; i < shape.Length; i++)
                {
                    crackPoints[i] = Projectile.Center + shape[i].RotatedBy(SpinAngle) * radius;
                    crackT[i] = i / (float)Math.Max(1, shape.Length - 1);
                }
                PixelLine.Draw(crackPoints, crackT, shape.Length, 1f, t => 1f, t => color);
            }
        }

        // Crack lines in the yoyo's own frame, -1 to 1 across the sprite.
        Vector2[] CrackShape(int index)
        {
            switch (index)
            {
                case 0:
                    return new[] { new Vector2(-0.15f, -1f), new Vector2(0.1f, -0.55f), new Vector2(-0.1f, -0.15f), new Vector2(0.2f, 0.3f), new Vector2(0.05f, 0.75f) };
                case 1:
                    return new[] { new Vector2(0.95f, -0.25f), new Vector2(0.5f, -0.1f), new Vector2(-0.1f, -0.15f) };
                default:
                    return new[] { new Vector2(-0.1f, -0.15f), new Vector2(-0.55f, -0.5f), new Vector2(-0.8f, -0.8f) };
            }
        }
    }
}