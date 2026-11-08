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
    /// The imbue (patch 2): spawned by the owner at the moment the blade is buried in the chest (ai[0] = how many ticks it lasts). It follows
    /// the player for that long and shows the imbue to EVERY machine: the purple dust trail at the feet and the purple light. It draws
    /// the player's afterimages (patch 3) and switches on the Phase dye look (LightsBanePlayer.ImbueLook). It is VISUAL ONLY and it ends with LightsBaneImbueBuff:
    /// the speed boost lives in LightsBanePlayer on the owner's machine (vanilla syncs the player's position, so nobody else needs the speed), so a machine that
    /// misses this projectile (a late joiner) only misses some dust.
    /// </summary>
    public class LightsBaneImbueProjectile : ModProjectile
    {
        // Never drawn (PreDraw returns false); the texture only has to exist.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CrystalShard;

        bool started;
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
            Projectile.timeLeft = 600; // replaced on the first tick by ai[0]
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            // ai[0] arrives with the spawn on every machine, so the duration is read on the first tick rather than in SetDefaults.
            if (!started)
            {
                started = true;
                Projectile.timeLeft = Math.Max(2, (int)Projectile.ai[0]);
            }

            // The imbue is the buff: when it is gone (its time ran out, or the player cancelled it) so is this. The first ticks are skipped,
            // because on the other machines the buff can arrive a moment after the projectile.
            age++;
            if (age > LightsBaneTuning.ImbueBuffGraceTicks && !owner.HasBuff(ModContent.BuffType<LightsBaneImbueBuff>()))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)Math.Max(1, LightsBaneTuning.ImbueFadeTicks), 0f, 1f);
            LightsBaneFx.ImbueTrail(owner, fade);
            Lighting.AddLight(owner.Center, 0.45f * fade, 0.15f * fade, 0.9f * fade);

            // The look: the Phase dye on the player (LightsBanePlayer.ModifyDrawInfo), blinking faster and faster over the last ImbueFadeTicks.
            // Every machine runs this projectile, so everyone sees it on everyone.
            float left = Projectile.timeLeft;
            bool look = true;
            if (left < LightsBaneTuning.ImbueFadeTicks)
            {
                float t = 1f - left / Math.Max(1, LightsBaneTuning.ImbueFadeTicks);
                look = (int)(t * t * LightsBaneTuning.ImbueFlickerCycles * 2f) % 2 == 0;
            }
            owner.GetModPlayer<LightsBanePlayer>().ImbueLook = look;

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