using IsmailsWeaponReworks.Content.Buffs;
using Microsoft.Xna.Framework;
using IsmailsWeaponReworks.Common;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace IsmailsWeaponReworks.Content.Weapons.LightsBane
{
    /// <summary>
    /// The imbue's visuals, tied to LightsBaneImbueBuff and nothing else: the owner's LightsBanePlayer spawns this whenever the player has the
    /// buff (whoever gave it: the stab, a cheat, another mod) and it ends when the buff is gone. It follows the player and shows the imbue to
    /// EVERY machine: the purple dust trail at the feet, the purple light, the player's afterimages and the Phase dye look (LightsBanePlayer.ImbueLook). It is VISUAL ONLY:
    /// the speed boost lives in LightsBanePlayer on the owner's machine (vanilla syncs the player's position, so nobody else needs the speed), so a machine that
    /// misses this projectile (a late joiner) only misses some dust.
    /// </summary>
    public class LightsBaneImbueProjectile : ModProjectile
    {
        // Never drawn (PreDraw returns false); the texture only has to exist.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        int age;
        AfterimageTrail playerImages; // visual only, never synced
        Vector2 lastImagePosition;
        bool haveLastImage;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10000; // refilled every tick: it lives as long as the buff
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 10000;

            // The imbue is the buff: when it is gone (its time ran out, or the player cancelled it) so is this. The first ticks are skipped,
            // because on the other machines the buff can arrive a moment after the projectile.
            age++;
            if (age > LightsBaneTuning.ImbueBuffGraceTicks && !owner.HasBuff(ModContent.BuffType<LightsBaneImbueBuff>()))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            float fade = MathHelper.Clamp(owner.GetModPlayer<LightsBanePlayer>().ImbueTicks / (float)Math.Max(1, LightsBaneTuning.ImbueFadeTicks), 0f, 1f);
            LightsBaneFx.ImbueTrail(owner, fade);
            Lighting.AddLight(owner.Center, 0.45f * fade, 0.15f * fade, 0.9f * fade);

            owner.GetModPlayer<LightsBanePlayer>().ImbueLook = true;

            if (!Main.dedServ)
            {
                EnsurePlayerImages();
                playerImages.Tick();
                LayImages(owner);
            }
        }

        // The copies are the real player drawn at an old position (the pose of the moment they are drawn), more see-through the older they are
        // (the same trick as the Katana's dash). They go through ModifyDrawInfo too, so they wear the Phase dye while the look is on.
        void EnsurePlayerImages()
        {
            if (playerImages != null)
                return;
            playerImages = new AfterimageTrail(LightsBaneTuning.ImbueAfterimageTicks, (image, age) =>
            {
                Player player = Main.player[Projectile.owner];
                if (!player.active || player.dead)
                    return;
                float shadow = MathHelper.Lerp(MathHelper.Clamp(LightsBaneTuning.ImbueAfterimageShadow, 0f, 1f), 1f, age);
                if (shadow >= 0.99f)
                    return;
                Main.PlayerRenderer.DrawPlayer(Main.Camera, player, image.Position, player.fullRotation, player.fullRotationOrigin, shadow, 1f);
            });
        }

        // One copy every ImbueAfterimageSpacing pixels along the path the player travelled since the last tick (none while standing still).
        void LayImages(Player owner)
        {
            Vector2 current = owner.position;
            if (!haveLastImage)
            {
                haveLastImage = true;
                lastImagePosition = current;
                return;
            }
            Vector2 delta = current - lastImagePosition;
            float distance = delta.Length();
            float spacing = Math.Max(4f, LightsBaneTuning.ImbueAfterimageSpacing);
            int count = (int)(distance / spacing);
            for (int i = 1; i <= count; i++)
                playerImages.Add(lastImagePosition + delta * (i * spacing / distance), 0f, 1f, Color.White);
            if (count > 0)
                lastImagePosition += delta * (count * spacing / distance);
        }

        // The leftover copies keep fading on their own after the imbue ends.
        public override void OnKill(int timeLeft)
        {
            if (playerImages == null)
                return;
            DetachedTrails.Hand(playerImages);
            playerImages = null;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!Main.dedServ)
                playerImages?.Draw();
            return false;
        }
    }
}