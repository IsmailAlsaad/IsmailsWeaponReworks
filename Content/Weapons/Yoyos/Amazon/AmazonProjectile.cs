using IsmailsWeaponReworks.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.Yoyos.Amazon
{
    /// <summary>
    /// The Amazon's held yoyo projectile: the machinery is in YoyoProjectile, this names the yoyo's item, tuning, fx and trick and adds
    /// the barbs. Every enemy hit (owner's client) calls Barb: the stage goes 0, 1, 2 and the third hit releases the barbs as
    /// AmazonBarbedCoilProjectile vines latched onto the enemy that was hit, then starts over at 0. If that enemy already carries as much
    /// latched vine as the cap allows, nothing is released and the barbs wait (the stage stays at its highest) for a hit on an enemy with room.
    /// The yoyo keeps flying through all of it. The stage is kept across passes and catches.
    /// SPRITE: Amazon/AmazonProjectile.png (the smooth yoyo; Tuning.TexturePath), AmazonProjectileBarbed1.png and AmazonProjectileBarbed2.png
    /// for the two barbed looks; whatever is missing falls back to the vanilla sprite, with barbs drawn by code (PixelLine).
    /// MULTIPLAYER: the owner's client decides the hits, the stage and the release. The stage travels in the extra sync (one byte added
    /// after the base's), so every machine draws the right sprite and plays the barb or release effects when the stage changes; the
    /// vines are spawned by the owner (normal projectile spawn sync).
    /// </summary>
    public class AmazonProjectile : YoyoProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Amazon;

        public override int YoyoItemType => ItemID.Amazon;

        public int Barbs;                     // 0 = smooth, 1 and 2 = more and more barbed (synced)

        AmazonTuning Tune => (AmazonTuning)Tuning;
        AmazonFx AFx => (AmazonFx)Fx;

        bool barbsSynced;                     // this machine has received the stage once
        bool pendingBarbFx;
        bool pendingReleaseFx;
        int pendingStage;
        bool barbArtChecked;
        Asset<Texture2D> barbed1;
        Asset<Texture2D> barbed2;
        readonly Vector2[] barbPoints = new Vector2[2];
        readonly float[] barbT = { 0f, 1f };

        protected override YoyoTuning CreateTuning()
        {
            return new AmazonTuning();
        }

        protected override YoyoFx CreateFx()
        {
            return new AmazonFx();
        }

        protected override YoyoTrick CreateTrick(YoyoTuning tuning)
        {
            return new AmazonTrick(tuning);
        }

        // ---- net ----

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)Barbs);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            int barbs = reader.ReadByte();
            if (!barbsSynced)
            {
                barbsSynced = true;   // a late joiner takes the stage as it is, no effects
                Barbs = barbs;
                return;
            }
            if (barbs == Barbs)
                return;
            if (barbs < Barbs)
                pendingReleaseFx = true;   // the stage went back to 0: the barbs were released
            else
            {
                pendingBarbFx = true;
                pendingStage = barbs;
            }
            Barbs = barbs;
        }

        // ---- barbs ----

        // Owner's client only (from AmazonTrick.OnHit): one more barb stage, and the barbs are released onto the enemy on the last.
        public void Barb(NPC target)
        {
            int needed = Math.Max(1, Tune.BarbHitsToRelease);
            if (Barbs + 1 >= needed)
            {
                if (Release(target))
                {
                    Barbs = 0;
                    AFx.Release(Projectile.Center);
                    YoyoShake.Hit(Projectile.Center, Tune.ReleaseShake);
                    Projectile.netUpdate = true;
                }
                return;   // no room on this enemy: the barbs wait for a hit on one with room
            }
            Barbs++;
            AFx.Barb(Projectile.Center, Barbs);
            Projectile.netUpdate = true;
        }

        // The latched vines (sections) that this player already has on one enemy, in pixels.
        float LatchedLength(int npcIndex)
        {
            int type = ModContent.ProjectileType<AmazonBarbedCoilProjectile>();
            float total = 0f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.type == type && other.owner == Projectile.owner && (int)other.ai[0] == npcIndex)
                    total += other.ai[1];
            }
            return total;
        }

        // Spawns the sections on the enemy, as many as the cap leaves room for; false when there is no room for even one.
        bool Release(NPC target)
        {
            float room = Tune.EnemyCapPx - LatchedLength(target.whoAmI);
            int count = Math.Max(1, Tune.LatchVineCount);
            int damage = Math.Max(1, (int)(Projectile.damage * Tune.LatchDamageFactor));
            int type = ModContent.ProjectileType<AmazonBarbedCoilProjectile>();
            float start = Main.rand.NextFloat(MathHelper.TwoPi);
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                float length = Math.Min(Tune.LatchSectionPx, room);
                if (length < Tune.MinEnemyRoomPx)
                    break;
                room -= length;
                float seed = start + MathHelper.TwoPi * i / count;
                int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, type, damage, 0f, Projectile.owner, target.whoAmI, length, seed);
                if (index >= 0 && index < Main.maxProjectiles)
                    Main.projectile[index].CritChance = Projectile.CritChance;
                any = true;
            }
            return any;
        }

        public override void AI()
        {
            base.AI();
            if (Main.dedServ)
                return;
            // The barb and release effects of a hit the owner decided (the other machines learn about it from the sync).
            if (pendingReleaseFx)
            {
                pendingReleaseFx = false;
                pendingBarbFx = false;
                AFx.Release(Projectile.Center);
            }
            else if (pendingBarbFx)
            {
                pendingBarbFx = false;
                AFx.Barb(Projectile.Center, pendingStage);
            }
        }

        // ---- drawing ----

        void CheckBarbArt()
        {
            if (barbArtChecked)
                return;
            barbArtChecked = true;
            ModContent.RequestIfExists<Texture2D>(Tune.Barbed1TexturePath, out barbed1);
            ModContent.RequestIfExists<Texture2D>(Tune.Barbed2TexturePath, out barbed2);
        }

        Texture2D BarbArt()
        {
            CheckBarbArt();
            Asset<Texture2D> art = Barbs == 1 ? barbed1 : Barbs == 2 ? barbed2 : null;
            return art != null && art.IsLoaded ? art.Value : null;
        }

        // The sprite of the current stage (afterimages keep the sprite they were made with).
        protected override Texture2D GetTexture()
        {
            Texture2D barbed = Barbs > 0 ? BarbArt() : null;
            return barbed ?? base.GetTexture();
        }

        // No barbed art yet: barbs drawn around the yoyo with the shared pixel line (they turn with the yoyo).
        public override void PostDraw(Color lightColor)
        {
            if (Main.dedServ || Barbs <= 0 || BarbArt() != null)
                return;
            Texture2D texture = base.GetTexture();
            float radius = texture.Width * 0.5f;
            Color color = new Color(lightColor.ToVector3() * new Vector3(0.35f, 0.65f, 0.25f));
            int count = Barbs == 1 ? 4 : 8;
            float length = Barbs == 1 ? 0.3f : 0.45f;
            for (int i = 0; i < count; i++)
            {
                float angle = SpinAngle + MathHelper.TwoPi * i / count;
                Vector2 direction = angle.ToRotationVector2();
                barbPoints[0] = Projectile.Center + direction * radius * 0.85f;
                barbPoints[1] = Projectile.Center + direction * radius * (1f + length);
                PixelLine.Draw(barbPoints, barbT, 2, 1f, t => 1f, t => color);
            }
        }
    }
}